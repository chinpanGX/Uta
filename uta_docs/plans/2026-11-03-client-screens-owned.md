---
title: 実装計画: client/ の画面 3/6 所持一覧・サポートカードの強化
status: draft
updated: 2026-11-03
spec: ../wiki/systems/idol-support.md
---

# 実装計画: client/ の画面 3/6 所持一覧・サポートカードの強化

## 1. 目的と前提

[game-spec.md](../wiki/overview/game-spec.md) の順 7 の 3 本目。所持一覧（育成アイドル・サポートカード）と、サポートカードの詳細（レベル上げ・上限解放）の画面、`ISupportService` を呼ぶクライアントの層を作る。**編成（4 本目）が使う「選択モード」**もここで作る。画面の叩き台は [screen-wireframes.md](../wiki/overview/screen-wireframes.md) 10、仕様は [idol-support.md](../wiki/systems/idol-support.md)（IDL-01・SUP-01・SUP-02）、RPC は `shared/Uta.Shared.Contracts/Support/`・[gacha-support-server.md](../wiki/architecture/server/gacha-support-server.md)。

- 方針の正は [screen-design.md](../wiki/architecture/screen-design.md) 7章。型の土台は [1 本目](2026-11-03-client-screens-foundation.md)と[2 本目](2026-11-03-client-screens-gacha.md)（`PortResult`・`ServerReasons`・`FakeGameMaster`・`FakeDialogService`・`FakeScreenNavigator` を使う）
- **前提**: 1・2 本目が done
- **決めないこと / 作らないもの**: 育成アイドルの詳細・強化（IDL-02。MVP の画面に入れない）、サポートカードのアビリティの表示（MVP は出さない。開始時の性能への反映はサーバーが行い、編成の画面にも出さない）、サポートカードのスキルカードサポート・イベント（MVP 外）
- **決めたこと（この計画で）**: 並びの「入手順」は、サポートカードは所持ID（ULID）の昇順、育成アイドルは入手日時の列が無いので `IdolId` の昇順。育成アイドルの行はタップしても何も起きない（選択モードのときだけ選べる）

## 2. 触るファイル

パスは `client/uta/Assets/` からの相対。

| 操作 | パス | 中身 |
|---|---|---|
| 作成 | `Scripts/Presentation/Common/DisplayText.cs` | レア度・プラン・タイプ・パラメータの表示名 |
| 作成 | `Scripts/Application/Support/ISupportPort.cs` | 通信ポート |
| 作成 | `Scripts/Application/Support/SupportLevelUpResult.cs`・`SupportLimitBreakResult.cs` | ポートが返す型 |
| 作成 | `Scripts/Application/Support/SupportLevelPlanner.cs` | 目標レベルの決め方 |
| 作成 | `Scripts/Application/Support/LevelUpSupportUseCase.cs`・`LimitBreakSupportUseCase.cs`・`SupportOutcome.cs` | ユースケース |
| 作成 | `Scripts/Application/Owned/OwnedListBuilder.cs`・`OwnedRowData.cs`・`OwnedSortKey.cs`・`OwnedSelectionRequest.cs`・`OwnedSelection.cs` | 一覧の組み立て・選択モード |
| 作成 | `Scripts/Infrastructure/Api/SupportApi.cs` | `ApiServiceBase<ISupportService>` |
| 変更 | `Scripts/Infrastructure/Api/UtaApiClient.cs` | `Support`（`SupportApi`）を足す |
| 変更 | `Scripts/Infrastructure/Api/UtaMagicOnionClientInitializer.cs` | `typeof(ISupportService)` を足す |
| 作成 | `Scripts/Infrastructure/Support/SupportGateway.cs` | `ISupportPort` の実装 |
| 作成 | `Scripts/Presentation/Owned/IOwnedView.cs`・`OwnedPage.cs`・`OwnedPresenter.cs` | 所持一覧 |
| 作成 | `Scripts/Presentation/Owned/ISupportDetailView.cs`・`SupportDetailPage.cs`・`SupportDetailPresenter.cs` | サポートカードの詳細 |
| 変更 | `Scripts/Presentation/Home/HomeDestinations.cs` | `OpenOwnedAsync` を実装する |
| 変更 | `Scripts/Bootstrap/UtaBootstrap.cs` | 登録 |
| 作成 | `Tests/EditMode/*.cs`（5章） | テスト |

## 3. 型とシグネチャ

C# は C# 9 の範囲。

### 3.1 `Uta.Client.Presentation.Common.DisplayText`

```csharp
public static class DisplayText
{
    public static string Rarity(Rarity rarity);        // N / R / SR / SSR（rarity.ToString()）
    public static string Plan(Plan plan);              // Free → "フリー"、Sense → "感性"、Logic → "理論"、Nothing → "-"
    public static string SupportType(SupportType type);// Singing → "歌唱"、Rhythm → "律動"、Expression → "表現"、Assist → "アシスト"、Nothing → "-"
    public static string Param(ParamKind param);       // Singing → "歌唱"、Rhythm → "律動"、Expression → "表現"、Nothing → "-"
}
```

### 3.2 `Uta.Client.Application.Support`

```csharp
public sealed class SupportLevelUpResult { public SupportLevelUpResult(int level, int supportPoints, int gold); public int Level { get; } public int SupportPoints { get; } public int Gold { get; } }
public sealed class SupportLimitBreakResult { public SupportLimitBreakResult(int limitBreak, int maxLevel); public int LimitBreak { get; } public int MaxLevel { get; } }

public interface ISupportPort
{
    UniTask<PortResult<SupportLevelUpResult>> LevelUpAsync(string supportCardUid, int targetLevel);
    UniTask<PortResult<SupportLimitBreakResult>> LimitBreakAsync(string supportCardUid, string materialUid);
}

public enum SupportStatus
{
    Succeeded,
    NotEnoughPoints,    // SUPPORT_NOT_ENOUGH_POINTS
    NotEnoughGold,      // PLAYER_NOT_ENOUGH_GOLD
    InvalidTarget,      // SUPPORT_INVALID_TARGET_LEVEL
    MaxLimitBreak,      // SUPPORT_MAX_LIMIT_BREAK
    InvalidMaterial,    // SUPPORT_INVALID_MATERIAL
    NotOwned,           // SUPPORT_NOT_OWNED
    Failed              // それ以外（共通のダイアログが出ている）
}
public sealed class SupportOutcome
{
    public SupportStatus Status { get; }
    public static SupportOutcome Success();
    public static SupportOutcome Rejected(SupportStatus status);
}

public enum LevelUpStep { Plus1, Plus5, Max }

public static class SupportLevelPlanner
{
    /// <summary>
    /// 次に上げる目標のレベル。Plus1 は level + 1、Plus5 は level + 5。どちらも上限（master.GetSupportMaxLevel(rarity, card.LimitBreak)）で頭打ちにする。
    /// Max は、上限までのうち、所持の強化pt・ゴールドで足りる一番高いレベル（足りるものが無ければ card.Level）。
    /// card.Level が上限以上なら card.Level（変化なし）を返す。
    /// </summary>
    public static int PlanTarget(OwnedSupportCard card, Rarity rarity, int supportPoints, int gold, LevelUpStep step, IGameMaster master);
}

public sealed class LevelUpSupportUseCase
{
    public LevelUpSupportUseCase(ISupportPort port, RefreshPlayerUseCase refresh);
    /// <summary>targetLevel は card.Level より大きい値（そうでなければ ArgumentOutOfRangeException）。成功したらサインインで所持品を取り直す。</summary>
    public async UniTask<SupportOutcome> ExecuteAsync(OwnedSupportCard card, int targetLevel);
}
public sealed class LimitBreakSupportUseCase
{
    public LimitBreakSupportUseCase(ISupportPort port, RefreshPlayerUseCase refresh);
    /// <summary>material は同じ SupportCardId の別の所持（card.SupportCardUid と同じなら ArgumentException）。成功したらサインインで所持品を取り直す。</summary>
    public async UniTask<SupportOutcome> ExecuteAsync(OwnedSupportCard card, OwnedSupportCard material);
}
```

業務エラー→`SupportStatus` の対応は上のコメントのとおり（`IsBusinessError` で `Reason` を `ServerReasons` と比べる）。成功のときだけ `refresh.ExecuteAsync()` を呼ぶ。失敗で業務エラーでないものは `Failed`。

### 3.3 `Uta.Client.Application.Owned`

```csharp
public enum OwnedSortKey { Rarity, Level, Acquired }   // Level はサポートカードだけ（育成アイドルで Level を指定したら Rarity と同じ扱い）

public sealed class OwnedRowData     // 表示の文字列は持たず値で持つ（Application は Presentation の DisplayText を参照できない）。文字列は OwnedPresenter が作る（3.5）
{
    public OwnedRowData(string key, string name, Rarity rarity, Plan plan, SupportType supportType, int level, int limitBreak, int shards, bool isIdol, bool enabled, OwnedDisabledReason disabledReason);
    public string Key { get; }            // サポートカード: SupportCardUid。育成アイドル: IdolId の 10 進文字列
    public string Name { get; } public Rarity Rarity { get; } public Plan Plan { get; }
    public SupportType SupportType { get; }   // 育成アイドルは Nothing
    public int Level { get; } public int LimitBreak { get; }   // 育成アイドルは 0
    public int Shards { get; }                // サポートカードは 0
    public bool IsIdol { get; }
    public bool Enabled { get; }          // 選択モードで選べるか（閲覧モードは常に true）
    public OwnedDisabledReason DisabledReason { get; }
}
public enum OwnedDisabledReason { None, AlreadyInFormation, PlanMismatch, SameCardInFormation }

public sealed class OwnedSelection
{
    public static OwnedSelection Cancelled { get; }
    public static OwnedSelection ForIdol(int idolId);
    public static OwnedSelection ForSupportCard(string supportCardUid);
    public bool IsCancelled { get; } public int IdolId { get; } public string SupportCardUid { get; }
}

public sealed class OwnedSelectionRequest
{
    /// <summary>育成アイドルを選ばせる。excludedIdolIds は選べない（"編成済み"）。</summary>
    public static OwnedSelectionRequest ForIdol(IReadOnlyCollection<int> excludedIdolIds = null);
    /// <summary>
    /// サポートカードを選ばせる。idolPlan のプランに合わないカード（Plan が Free でも idolPlan でもない）は選べない（"プランが合わない"）。
    /// excludedUids は選べない（"編成済み"）、excludedSupportCardIds は同じカード ID が編成済みなので選べない（"同じカードは編成できない"）。
    /// </summary>
    public static OwnedSelectionRequest ForSupportCard(Plan idolPlan, IReadOnlyCollection<string> excludedUids, IReadOnlyCollection<int> excludedSupportCardIds);
    public bool SelectsIdol { get; }
    public UniTask<OwnedSelection> Task { get; }     // 選んだら Complete される。戻る（キャンセル）は OwnedSelection.Cancelled
    public void Complete(OwnedSelection selection);  // 2 回目以降は無視する
}

public static class OwnedListBuilder
{
    /// <summary>
    /// planFilter が null でなければ、そのプランの育成アイドルだけ。sort は Rarity と Acquired（IdolId の昇順を基準）。
    /// request が null のときは全部 Enabled。request が育成アイドルの選択なら excludedIdolIds を Enabled = false（理由 "編成済み"）。
    /// </summary>
    public static IReadOnlyList<OwnedRowData> BuildIdols(PlayerSnapshot player, IGameMaster master, OwnedSortKey sort, bool descending, Plan? planFilter, OwnedSelectionRequest request);
    /// <summary>supportTypeFilter が null でなければそのタイプだけ。sort は Rarity・Level・Acquired（所持ID の昇順を基準）。planFilter / request も同様。</summary>
    public static IReadOnlyList<OwnedRowData> BuildSupportCards(PlayerSnapshot player, IGameMaster master, OwnedSortKey sort, bool descending, Plan? planFilter, SupportType? supportTypeFilter, OwnedSelectionRequest request);
}
```

`OwnedListBuilder` の規則:
- 並べ替えは安定にする。基準が同じ行は、`Acquired`（昇順）の順に残す。`descending == true` は、基準で降順（`Rarity` は SSR が先、`Level` は大きい方が先、`Acquired` は新しい方（添字の大きい方）が先）

### 3.4 `Uta.Client.Infrastructure`

```csharp
// Api/SupportApi.cs
public sealed class SupportApi : ApiServiceBase<ISupportService>
{
    public SupportApi(ApiCore core, ISupportService client) : base(core, client) { }
    public UniTask<ApiResult<LevelUpResponse>> LevelUpAsync(LevelUpRequest request, IEnumerable<ErrorCode> handledCodes = null)
        => InvokeAsync(nameof(ISupportService.LevelUpAsync), request, static (c, r) => c.LevelUpAsync(r), handledCodes);
    public UniTask<ApiResult<LimitBreakResponse>> LimitBreakAsync(LimitBreakRequest request, IEnumerable<ErrorCode> handledCodes = null)
        => InvokeAsync(nameof(ISupportService.LimitBreakAsync), request, static (c, r) => c.LimitBreakAsync(r), handledCodes);
}

// Support/SupportGateway.cs
public sealed class SupportGateway : ISupportPort { public SupportGateway(UtaApiClient api); ... }
```

- `LevelUpAsync` の `handledCodes`: `SupportErrors.InvalidTargetLevel`・`SupportErrors.NotEnoughPoints`・`PlayerErrors.NotEnoughGold`・`SupportErrors.NotOwned`。`LimitBreakAsync` の `handledCodes`: `SupportErrors.MaxLimitBreak`・`SupportErrors.InvalidMaterial`・`SupportErrors.NotOwned`
- 結果の直し方は `GachaGateway.DrawAsync` と同じ（成功 → `Success`、`Domain == "uta"` で `handledCodes` の reason → `BusinessError(Reason, ...)`、それ以外 → `Failure`）
- `UtaApiClient` に `public SupportApi Support { get; }` を足し、`Create` で `ServiceClientFactory.Create<ISupportService>(callInvoker)` を作る。`UtaMagicOnionClientInitializer` の属性に `typeof(ISupportService)` を足す（`using Uta.Shared.Contracts.Support;`）

### 3.5 `Uta.Client.Presentation.Owned`

```csharp
public enum OwnedTab { Idols, SupportCards }

public sealed class OwnedRowViewData
{
    public OwnedRowViewData(string key, string title, string detail, bool enabled);
    public string Key { get; } public string Title { get; } public string Detail { get; } public bool Enabled { get; }
}

public interface IOwnedView
{
    void SetTab(OwnedTab tab);
    void SetTabsVisible(bool visible);                 // 選択モードでは false
    void SetSortLabels(string sortKey, string order);  // "並び: レア度" "降順"/"昇順"
    void SetFilterLabels(string plan, string type, bool typeVisible);   // "プラン: 全て"、"タイプ: 全て"（育成アイドルでは typeVisible = false）
    void SetRows(IReadOnlyList<OwnedRowViewData> rows);
    void SetTitle(string title);
    event Action<OwnedTab> TabSelected;
    event Action SortKeyClicked;
    event Action SortOrderClicked;
    event Action PlanFilterClicked;
    event Action TypeFilterClicked;
    event Action<string> RowClicked;
    event Action BackClicked;
    event Action Reappeared;                           // 詳細から戻ってきた
}
public sealed class OwnedPage : PageBase, IOwnedView { ... }

public sealed class OwnedPresenter : Presenter<IOwnedView>
{
    public OwnedPresenter(PlayerCache cache, IGameMaster master, ScreenNavigator navigator);
    public OwnedSelectionRequest Selection { get; set; }   // configure で設定する。null なら閲覧モード
}

public interface ISupportDetailView
{
    void SetInfo(IReadOnlyList<string> lines);          // 名前・レア・タイプ・プラン・Lv・上限解放
    void SetResources(int supportPoints, int gold);
    void SetLevelButtons(string plus1, string plus5, string max, bool plus1Enabled, bool plus5Enabled, bool maxEnabled);   // 例 "+1 (強化pt 100 / G 200)"
    void SetLimitBreak(string label, bool enabled);     // "上限解放 1/4" / "上限解放 (最大)"
    event Action Plus1Clicked; event Action Plus5Clicked; event Action MaxClicked; event Action LimitBreakClicked; event Action BackClicked;
}
public sealed class SupportDetailPage : PageBase, ISupportDetailView { ... }

public sealed class SupportDetailPresenter : Presenter<ISupportDetailView>
{
    public SupportDetailPresenter(PlayerCache cache, IGameMaster master, LevelUpSupportUseCase levelUp, LimitBreakSupportUseCase limitBreak, DialogService dialogs, ScreenNavigator navigator);
    public string SupportCardUid { get; set; }   // configure で設定する
}
```

**`OwnedPresenter` の動き**
- 状態: `tab`（既定 `Idols`。`Selection` があるときは `Selection.SelectsIdol ? Idols : SupportCards` で固定し、`SetTabsVisible(false)`）、`sortKey`（`Rarity`）、`descending`（true）、`planFilter`（null = 全て）、`typeFilter`（null = 全て）
- `Refresh()`: `OwnedListBuilder.BuildIdols`/`BuildSupportCards`（`cache.Current` を渡す）→ `OwnedRowViewData` に変換して `SetRows`。`title` = `Name + " [" + DisplayText.Rarity + "]"`、`detail` = 育成アイドルは `DisplayText.Plan + " かけら " + Shards`、サポートカードは `DisplayText.SupportType + " " + DisplayText.Plan + " Lv " + Level + " 解放 " + LimitBreak`。無効の行は `detail` の末尾に `" （編成済み）"` / `" （プランが合わない）"` / `" （同じカードは編成できない）"` を付ける。ラベルを `SetSortLabels`・`SetFilterLabels` で更新する
- `SortKeyClicked`: 育成アイドルは `Rarity` → `Acquired` → `Rarity`、サポートカードは `Rarity` → `Level` → `Acquired` → `Rarity` と循環。`SortOrderClicked`: `descending` を反転。`PlanFilterClicked`: null → Free → Sense → Logic → null と循環。`TypeFilterClicked`（サポートカードのみ）: null → Singing → Rhythm → Expression → Assist → null。変えたら `Refresh()`
- `TabSelected(tab)`（閲覧モードのみ）: `tab` を替えて `sortKey` を `Rarity` に戻し `Refresh()`
- `RowClicked(key)`: 選択モードなら、その行が `Enabled` のときだけ `Selection.Complete(...)`（育成アイドル → `OwnedSelection.ForIdol(int.Parse(key))`、サポートカード → `ForSupportCard(key)`）のあと `navigator.PopPageAsync()`。閲覧モードでサポートカードなら `navigator.PushPageAsync<SupportDetailPage, SupportDetailPresenter>(p => p.SupportCardUid = key)`。閲覧モードの育成アイドルは何もしない
- `BackClicked`: 選択モードなら `Selection.Complete(OwnedSelection.Cancelled)` のあと `PopPageAsync()`、閲覧モードは `PopPageAsync()`
- `Dispose()`: 選択モードで `Selection` がまだ完了していなければ `Cancelled` で完了する（画面が他の理由で閉じられても `await` が止まらないように）
- ホームに戻る前に更新が見える: `SupportDetailPage` から戻ったとき `OwnedPage` は `Reappeared` で `Refresh()` する（`IOwnedView.Reappeared` を `OwnedPage` が `PageBase.Reappeared` から転送する）

**`SupportDetailPresenter` の動き**
- `OnBind`/`Refresh()`: `card = cache.Current.SupportCards` から `SupportCardUid` で探す（見つからなければ `navigator.PopPageAsync()`）。`info = master.GetSupportCard(card.SupportCardId)`。`SetInfo`: `info.Name`、`"レア <Rarity>　タイプ <SupportType>　プラン <Plan>"`、`"Lv <Level> / <maxLevel>"`（`maxLevel = master.GetSupportMaxLevel(info.Rarity, card.LimitBreak)`）。`SetResources(cache.Current.SupportPoints, cache.Current.Gold)`。3 つのレベルのボタン: `target = SupportLevelPlanner.PlanTarget(card, info.Rarity, supportPoints, gold, step, master)`、`target > card.Level` のとき有効で、ラベルは `"<+1/+5/最大> → Lv <target> (強化pt <cost.SupportPoints> / G <cost.Gold>)"`（`cost = master.GetSupportLevelUpCost(info.Rarity, card.Level, target)`）、無効のときは `"<+1/+5/最大> (上限)"`。ただし `+1`・`+5` は所持が足りなくても押せる（`target > card.Level` なら有効。足りなければサーバーが `NotEnoughPoints`/`NotEnoughGold`）。`最大` は足りる範囲なので、足りなければ `target == card.Level` で無効
- 上限解放: `maxBreak = master.GetSupportMaxLimitBreak(info.Rarity)`。`card.LimitBreak >= maxBreak` なら `SetLimitBreak("上限解放 (最大)", false)`。それ以外は `SetLimitBreak($"上限解放 {card.LimitBreak}/{maxBreak}", true)`
- レベルのボタン: `levelUp.ExecuteAsync(card, target)` → `Succeeded` で `Refresh()`。`NotEnoughPoints` は `"強化ptが足りません。"`、`NotEnoughGold` は `"ゴールドが足りません。"`、`InvalidTarget` は `"レベルを上げられません。"`、`NotOwned` は `"このカードは所持していません。"`、`Failed` は何も出さない。それぞれ `dialogs.ShowInfoAsync`。例外は `"通信に失敗しました: " + e.Message`
- 上限解放: 素材の候補 = `cache.Current.SupportCards` のうち、`SupportCardId` が同じで `SupportCardUid` が違うもの。0 件なら `dialogs.ShowInfoAsync("素材にできる同じカードがありません。")`。1 件以上なら、レベルの低い順・所持ID の昇順で先頭の 1 枚を素材として確認する: `dialogs.ConfirmAsync($"同じカード（Lv {material.Level}）を素材にして上限解放します。素材は無くなります。")`（素材の選択画面は作らない。**素材は最も Lv の低い同じカードに決める**）。`Positive` なら `limitBreak.ExecuteAsync(card, material)`。`Succeeded` で `Refresh()`、`MaxLimitBreak` は `"これ以上は解放できません。"`、`InvalidMaterial` は `"素材にできません。"`、`NotOwned` は `"このカードは所持していません。"`
- `BackClicked` → `navigator.PopPageAsync()`

**画面のレイアウト**（`BuildUi`。見た目は仮）
- `OwnedPage`: ヘッダー行（「＜」ボタン、タイトル）、タブ行（「育成アイドル」「サポート」。`SetTabsVisible(false)` で非表示）、並び・絞り込みの行（`CreateHorizontal` にボタン 4 つ: 並びの種類・昇降・プラン・タイプ）、`CreateScrollContent` に行（`CreateButton` 相当の横長の `Button`。2 行の Text。無効の行は `UiFactory.SetInteractable(false)`）
- `SupportDetailPage`: ヘッダー（「＜」）、`SetInfo` の行を並べた Text、所持の強化pt・ゴールド、レベルのボタン 3 つ、上限解放のボタン

`HomeDestinations.OpenOwnedAsync` は `await navigator.PushPageAsync<OwnedPage, OwnedPresenter>();`（`Selection` は設定しない）。

## 4. タスク

各タスクの終わりに `unity recompile --format json` でコンパイルエラー 0、そのタスク分のテストが通る状態にする。Editor を開けないときは、確かめていないと報告する。

### タスク 1: 表示名・目標レベル・一覧の組み立て

触る: `Presentation/Common/DisplayText.cs`、`Application/Support/SupportLevelPlanner.cs`、`Application/Owned/*`。
手順: 3.1・3.2（`SupportLevelPlanner`）・3.3 のとおり作る。`SupportLevelPlanner.PlanTarget` の `Max`: `maxLevel` から `card.Level + 1` まで下がりながら、`master.GetSupportLevelUpCost(rarity, card.Level, t)` が `supportPoints`・`gold` の両方以下になる最初の `t` を返す（無ければ `card.Level`）。`OwnedListBuilder` の `Acquired` の基準: サポートカードは `player.SupportCards` の並び（サーバーが所持ID 昇順で返す）、育成アイドルは `IdolId` の昇順に並べ直してから基準にする。
確かめ方: `SupportLevelPlannerTests`・`OwnedListBuilderTests`・`OwnedSelectionRequestTests`・`DisplayTextTests`。

### タスク 2: ユースケース・API・ゲートウェイ

触る: `Application/Support/ISupportPort.cs` ほか、`Infrastructure/Api/SupportApi.cs`・`UtaApiClient.cs`・`UtaMagicOnionClientInitializer.cs`、`Infrastructure/Support/SupportGateway.cs`。
手順: 3.2・3.4 のとおり。
確かめ方: `LevelUpSupportUseCaseTests`・`LimitBreakSupportUseCaseTests`。`unity recompile` のあとコンソールのエラーが 0。

### タスク 3: 画面

触る: `Presentation/Owned/*`、`Presentation/Home/HomeDestinations.cs`、`Bootstrap/UtaBootstrap.cs`。
手順: 3.5 のとおり。`UtaBootstrap` に `Register<SupportGateway>(Singleton).As<ISupportPort>()`、`Register<LevelUpSupportUseCase>(Singleton)`、`Register<LimitBreakSupportUseCase>(Singleton)`、`Register<OwnedPresenter>(Transient)`、`Register<SupportDetailPresenter>(Transient)` を足す。
確かめ方: `OwnedPresenterTests`・`SupportDetailPresenterTests`。Play モード（下）。

### タスク 4: 結果の記録

`screen-design.md` の「実装メモ」にズレを書く（なければ「なし」）。`unity7-evaluation.md` に確かめた項目・未確認の項目を足す。この計画を `done` にする。

## 5. テスト

| ファイル | テスト | 内容 |
|---|---|---|
| `DisplayTextTests.cs` | `Plan_AllValues` ほか | 各 enum の表示名 |
| `SupportLevelPlannerTests.cs` | `Plus1_BelowMax_ReturnsLevelPlusOne` / `Plus5_NearMax_ClampsToMax` / `AtMax_ReturnsCurrentLevel` | `FakeGameMaster` の `MaxLevel = (_, lb) => 20 + lb * 5` |
| | `Max_AffordableLimited_ReturnsHighestAffordable` | 1 レベルあたり強化pt 100・G 50 のとき、強化pt 250・G 1000 なら +2 |
| | `Max_CannotAffordAny_ReturnsCurrentLevel` | |
| `OwnedListBuilderTests.cs` | `BuildSupportCards_SortByLevelDescending` | 安定（Lv が同じなら `Acquired` の昇順） |
| | `BuildSupportCards_PlanFilter` / `TypeFilter` | |
| | `BuildIdols_Acquired_OrderedByIdolId` | |
| | `BuildSupportCards_Selection_DisablesPlanMismatchAndExcluded` | `idolPlan = Sense` で `Logic` のカードは `PlanMismatch`、`Free` は有効、`excludedUids` は `AlreadyInFormation`、`excludedSupportCardIds` は `SameCardInFormation` |
| | `BuildIdols_Selection_DisablesExcluded` | |
| `OwnedSelectionRequestTests.cs` | `Complete_Twice_KeepsFirst` | 2 回目は無視 |
| | `Task_CompletesWithSelection` | |
| `LevelUpSupportUseCaseTests.cs` | `Execute_Success_Refreshes` / `Execute_NotEnoughPoints_ReturnsStatus` / `Execute_NotEnoughGold_ReturnsStatus` / `Execute_TargetNotAboveLevel_Throws` / `Execute_OtherFailure_ReturnsFailed` | 偽の `ISupportPort`・`IPlayerPort` |
| `LimitBreakSupportUseCaseTests.cs` | `Execute_Success_Refreshes` / `Execute_SameUid_Throws` / `Execute_InvalidMaterial_ReturnsStatus` / `Execute_MaxLimitBreak_ReturnsStatus` | |
| `OwnedPresenterTests.cs` | `Bind_Browse_ShowsIdolsSortedByRarityDescending` | 偽の `IOwnedView` |
| | `SortKeyClicked_CyclesKeysPerTab` | 育成アイドルは Rarity↔Acquired、サポートは 3 つ |
| | `RowClicked_BrowseSupport_PushesDetail` | `FakeScreenNavigator.Calls` に `PushPage:SupportDetailPage` |
| | `RowClicked_Selection_CompletesAndPops` | `Selection.Task` が完了し `PopPage` |
| | `RowClicked_SelectionDisabledRow_DoesNothing` | |
| | `BackClicked_Selection_CompletesCancelled` | |
| | `Dispose_SelectionPending_CompletesCancelled` | |
| `SupportDetailPresenterTests.cs` | `Bind_ShowsInfoAndButtons` | ボタンのラベルと有効/無効 |
| | `Plus1Clicked_Success_RefreshesView` | |
| | `LimitBreakClicked_NoMaterial_ShowsInfo` | |
| | `LimitBreakClicked_Confirmed_PicksLowestLevelMaterial` | `LimitBreakAsync` に渡る素材が最も Lv の低い同じカード |
| | `LimitBreakClicked_AtMax_ButtonDisabled` | |

Play モード・手動（Editor、サーバー起動済み・初期付与済みのプレイヤーにガチャで SR 以上を引かせておく）:
- ホーム →「アイドル・サポート」で一覧。タブ切り替え、並び・絞り込みが効く
- サポートカードをタップ → 詳細。「+1」で Lv が上がり、強化pt・ゴールドが減る。「最大」で足りる範囲の最大まで上がる
- 強化ptが足りないときの「+5」で「強化ptが足りません。」
- 同じカードを 2 枚持った状態で「上限解放」→ 確認 → 解放数が増え、素材が一覧から消える
- 選択モードは 4 本目の編成で確かめる

## 6. 未決事項

なし
