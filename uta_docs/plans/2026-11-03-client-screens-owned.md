---
title: 実装計画: client/ の画面 3/6 所持一覧・サポートカードの強化
status: draft
updated: 2026-11-03
spec: ../wiki/systems/idol-support.md
---

# 実装計画: client/ の画面 3/6 所持一覧・サポートカードの強化

## 1. 目的と前提

[game-spec.md](../wiki/overview/game-spec.md) の順 7 の 3 本目。所持一覧（育成アイドル・サポートカード）と、サポートカードの詳細（アビリティの表示・レベル上げ・上限解放）の画面、`ISupportService` を呼ぶクライアントの層を作る。所持一覧には、**編成（4 本目）と上限解放の素材が使う「選択モード」**を持たせる。あわせて、プランの適合の規則を Shared.Domain の `FormationValidator.IsPlanCompatible` に 1 つにまとめる（shared/ の変更）。画面の叩き台は [screen-wireframes.md](../wiki/overview/screen-wireframes.md) 10、仕様は [idol-support.md](../wiki/systems/idol-support.md)（IDL-01・SUP-01・SUP-02。2.1 編成のルール、2.2.1 上限解放の操作、2.3 アビリティ）、RPC は `shared/Uta.Shared.Contracts/Support/`・[gacha-support-server.md](../wiki/architecture/server/gacha-support-server.md)。

- 方針の正は [screen-design.md](../wiki/architecture/screen-design.md) 7章。画面の作り方（Presenter・View・Args・遷移・実行中フラグ・テストの書き方）は **[1 本目](2026-11-03-client-screens-foundation.md) 3.3 の共通ルールに従う**（この計画では繰り返さない）。基盤の `IScreenNavigator`（`external/UnityScreenNavigator`）をそのまま使う
- 関係する ADR: [0008](../wiki/decisions/0008-shared-domain-and-onion.md)（層。判定は Shared.Domain の関数を両側で使う）、[0009](../wiki/decisions/0009-client-stack.md)（uGUI + UnityScreenNavigator・VContainer・UniTask）
- 使う前の計画の型: 1 本目の `PageBase`・`UiFactory`・`UiTheme`・`DialogService`・`IGameMaster`（`GetSupportAbilities` を含む）・`RefreshPlayerUseCase`・`HomePresenter`・`CallLog`・`FakeScreenNavigator`・`FakeDialogService`。2 本目の `PortResult<T>`・`ServerReasons`・`FakeGameMaster`、`HomePresenter` に足された `IScreenNavigator`
- **前提**: 1・2 本目が done（game-spec.md の順 7 の行のとおり、3 は 2 の型を使うので 2 の後）
- **決めないこと / 作らないもの**: 育成アイドルの詳細・強化（IDL-02。MVP の画面に入れない）、アビリティの枠 2〜6（MVP は枠 1 だけ。[idol-support.md](../wiki/systems/idol-support.md) 2.3）、スキルカードサポート・イベント（MVP 外）。編成の画面での開始時の補正の表示は 4 本目が決める
- **決めたこと（この計画で）**:
  - [Uta案] **育成アイドルの一覧は `IdolId` の昇順に固定**し、並び替え・絞り込みを持たない（MVP の育成アイドルは 4 人で、並べ替える意味が小さいため）。叩き台 [screen-wireframes.md](../wiki/overview/screen-wireframes.md) 10 の「一覧の並び: レア度・Lv・入手順。絞り込み: プラン」は、MVP では**サポートカードだけ**に持たせる（叩き台との差。タスク 5 で screen-wireframes.md 10 に注記する）
  - サポートカードの「入手順」は所持ID（ULID）の昇順（ULID は発行時刻の順。`string.CompareOrdinal` で比べる。サーバーの返す並びに頼らない）
  - 上限解放の素材は、仕様（idol-support.md 2.2.1）どおり**プレイヤーが選ぶ**。選び方は所持一覧の選択モード（素材の選択）を使う
  - レベル上げ（+1／+5／最大）と上限解放は、**確認ダイアログを出してから** RPC を呼ぶ（叩き台 10「強化・上限解放…は、確認ダイアログ → サーバーで確定」）。上限解放の確認には、素材の Lv にかかわらず常に「素材は無くなります。強化に使った強化pt・ゴールドは戻りません。」を出す（仕様は「素材のレベルが 1 より大きいとき」。常に出すほうが安全なので広げる。タスク 5 で実装メモに書く）
  - サポートカードの詳細と強化（レベル上げ・上限解放）は **1 ページ**（`SupportDetailPage`）にする（screen-design.md 2・3章の「詳細 ─▶ 強化」を 1 枚にまとめる。タスク 5 で 2・3章を直す）
  - 詳細のアビリティは枠 1（`SupportAbility` の行）を、今のレベルの値で出す（`ProduceFormulas.AbilityValue`。サーバーのプロデュース開始と同じ計算）
  - 上限解放の最大回数はマスタ（`IGameMaster.GetSupportMaxLimitBreak`）から読む。サーバーは定数 `SupportRules.MaxLimitBreak = 4` を使うので、マスタの `SupportLevelCap` に各レアの `LimitBreak = 4` の行があることが前提（今のマスタはそうなっている。食い違っても、超えた分はサーバーが `SUPPORT_MAX_LIMIT_BREAK` で拒否する）

## 2. 触るファイル

パスは、`client/uta/Assets/` で始まらないものはリポジトリのルートからの相対、それ以外は `client/uta/Assets/` からの相対。新しい `.cs` の `.meta` は Unity が作ったものをコミットする。

| 操作 | パス | 中身 |
|---|---|---|
| 変更 | `shared/Uta.Shared.Domain/Produce/FormationValidator.cs` | `IsPlanCompatible` を足し、`Validate` がそれを呼ぶ（3.1） |
| 変更 | `shared/Uta.Shared.Domain.Tests/Produce/FormationValidatorTests.cs` | `IsPlanCompatible` のテストを足す（5章） |
| 作成 | `Scripts/Presentation/Common/DisplayText.cs` | レア度・プラン・タイプ・パラメータ・アビリティの表示名（3.2） |
| 作成 | `Scripts/Application/Support/ISupportPort.cs`・`SupportLevelUpResult.cs`・`SupportLimitBreakResult.cs` | 通信ポートと、ポートが返す型（3.3） |
| 作成 | `Scripts/Application/Support/SupportStatus.cs`・`SupportOutcome.cs`・`LevelUpStep.cs` | ユースケースの結果の型（3.3） |
| 作成 | `Scripts/Application/Support/SupportLevelPlanner.cs` | 目標レベルの決め方（3.3） |
| 作成 | `Scripts/Application/Support/SupportSnapshotEdits.cs` | 取り直しに失敗したときに応答でキャッシュを直す（3.3） |
| 作成 | `Scripts/Application/Support/LevelUpSupportUseCase.cs`・`LimitBreakSupportUseCase.cs` | ユースケース（3.3） |
| 作成 | `Scripts/Application/Owned/OwnedSortKey.cs`・`OwnedDisabledReason.cs`・`OwnedSelectionKind.cs`・`OwnedRowData.cs`・`OwnedSelection.cs`・`OwnedSelectionRequest.cs`・`OwnedListBuilder.cs` | 一覧の組み立て・選択モード（3.4。1 ファイル 1 型） |
| 作成 | `Scripts/Infrastructure/Api/SupportApi.cs` | `ApiServiceBase<ISupportService>`（3.5） |
| 変更 | `Scripts/Infrastructure/Api/UtaApiClient.cs` | `Support`（`SupportApi`）を足す（3.5） |
| 変更 | `Scripts/Infrastructure/Api/UtaMagicOnionClientInitializer.cs` | `typeof(ISupportService)` を足す（3.5） |
| 作成 | `Scripts/Infrastructure/Support/SupportGateway.cs` | `ISupportPort` の実装（3.5） |
| 作成 | `Scripts/Presentation/Owned/OwnedTab.cs`・`OwnedArgs.cs`・`OwnedRowViewData.cs`・`IOwnedView.cs`・`OwnedPage.cs`・`OwnedPresenter.cs` | 所持一覧（3.6） |
| 作成 | `Scripts/Presentation/Owned/SupportDetailArgs.cs`・`ISupportDetailView.cs`・`SupportDetailPage.cs`・`SupportDetailPresenter.cs` | サポートカードの詳細（3.7） |
| 変更 | `Scripts/Presentation/Home/HomePresenter.cs` | `OpenOwnedAsync` の中身を、所持一覧を積む処理に置き換える（3.8） |
| 変更 | `Scripts/Bootstrap/UtaBootstrap.cs` | 雛形の登録 2 行と DI の登録（3.8） |
| 変更 | `Tests/EditMode/<2 本目の置き場>/FakeGameMaster.cs` | アビリティを差し込む口が無ければ足す（タスク 1。置き場は 2 本目の計画のとおり） |
| 変更 | `Tests/EditMode/HomePresenterTests.cs` | 「アイドル」が準備中でなくなった分を直し、1 本足す（5章） |
| 作成 | `Tests/EditMode/*.cs`（5章） | テスト |
| 変更 | `uta_docs/wiki/architecture/screen-design.md`・`architecture/produce-domain.md`・`overview/screen-wireframes.md`・`systems/idol-support.md`・`architecture/unity7-evaluation.md` | 結果の記録（タスク 5） |

## 3. 型とシグネチャ

C# は、`shared/`・`client/` とも C# 9 の範囲（1 本目 3章の冒頭のとおり。コレクション式 `[...]` は使わず `new[] { ... }`）。名前空間はフォルダに合わせる: `Scripts/Application/Support` → `Uta.Client.Application.Support`、`Application/Owned` → `Uta.Client.Application.Owned`、`Infrastructure/Api` → `Uta.Client.Infrastructure.Api`、`Infrastructure/Support` → `Uta.Client.Infrastructure.Support`、`Presentation/Owned` → `Uta.Client.Presentation.Owned`、`Presentation/Common` → `Uta.Client.Presentation.Common`。数値の 3 桁区切りは 1 本目 3.3.6 のとおり `ToString("N0", CultureInfo.InvariantCulture)`（以下 `N0(x)` と書く）。

### 3.1 `Uta.Shared.Domain.Produce.FormationValidator`（shared/）

```csharp
public static class FormationValidator
{
    /// <summary>
    /// サポートカードのプランが育成アイドルのプランに合うか（idol-support.md 2.1 の 6）。
    /// supportPlan が Plan.Free か、idolPlan と同じなら true。それ以外（Plan.Nothing を含む）は false。
    /// クライアントの所持一覧・編成の表示と、Validate の両方がこれを使う（規則を 1 か所に置く）。
    /// </summary>
    public static bool IsPlanCompatible(Plan idolPlan, Plan supportPlan)
    {
        return supportPlan == Plan.Free || supportPlan == idolPlan;
    }

    // Validate: 2 つ目のループの条件 `plan != Plan.Free && plan != idol.Plan` を `!IsPlanCompatible(idol.Plan, plan)` に置き換える。ほかは変えない
    public static FormationCheck Validate(ProduceCatalog catalog, int idolId, IReadOnlyList<int> supportCardIds);
}
```

### 3.2 `Uta.Client.Presentation.Common.DisplayText`

```csharp
using LessonPlan = Uta.Shared.Domain.Lesson.Plan;            // メソッド名 Plan と型名 Plan がぶつかるので、本体では別名で書く
using LessonRarity = Uta.Shared.Domain.Lesson.Rarity;
using LessonParamKind = Uta.Shared.Domain.Lesson.ParamKind;
using ProduceSupportType = Uta.Shared.Domain.Produce.SupportType;
using ProduceAbilityKind = Uta.Shared.Domain.Produce.SupportAbilityKind;

public static class DisplayText
{
    public static string Rarity(LessonRarity rarity);          // N → "N"、R → "R"、SR → "SR"、SSR → "SSR"、Nothing → "-"
    public static string Plan(LessonPlan plan);                // Free → "共通"、Sense → "感性"、Logic → "理論"、Nothing → "-"
    public static string SupportType(ProduceSupportType type); // Singing → "歌唱"、Rhythm → "律動"、Expression → "表現"、Assist → "アシスト"、Nothing → "-"
    public static string Param(LessonParamKind param);         // Singing → "歌唱"、Rhythm → "律動"、Expression → "表現"、Nothing → "-"
    /// <summary>
    /// アビリティ（枠 1）の 1 行の文字。value は今のレベルの値。
    /// InitialParam → "初期" + Param(param) + " +" + value（例 "初期歌唱 +49"）。
    /// ParamBonus（千分率）→ Param(param) + "ボーナス +" + (value / 10) + "." + (value % 10) + "%"（例 85 → "歌唱ボーナス +8.5%"。float を使わない）。
    /// MaxHp → "最大体力 +" + value。Nothing → "-"。
    /// </summary>
    public static string Ability(ProduceAbilityKind kind, LessonParamKind param, int value);
}
```

- **本体では型を別名（`LessonPlan.Free` など）で書く**。クラスの中で単純名 `Plan` はメソッド `Plan` に束縛され、`Plan.Free` がコンパイルエラーになるため（引数の型の位置は型として解決されるので、シグネチャは上のままでよい）。呼び出し側は `DisplayText.Plan(x)` と書く（5・6 本目の計画もこの名前で使う）
- `Plan` の `Free` を「共通」にするのは用語集（glossary.md の「プラン」）・idol-support.md 2.1 に合わせるため
- 範囲外の値（enum に無い数値）は `"-"`

### 3.3 `Uta.Client.Application.Support`

```csharp
public sealed class SupportLevelUpResult
{
    public SupportLevelUpResult(int level, int supportPoints, int gold);
    public int Level { get; } public int SupportPoints { get; } public int Gold { get; }   // LevelUpResponse の写し（上げた後のレベル・残りの強化pt・残りのゴールド）
}
public sealed class SupportLimitBreakResult
{
    public SupportLimitBreakResult(int limitBreak, int maxLevel);
    public int LimitBreak { get; } public int MaxLevel { get; }   // LimitBreakResponse の写し
}

public interface ISupportPort
{
    UniTask<PortResult<SupportLevelUpResult>> LevelUpAsync(string supportCardUid, int targetLevel);
    UniTask<PortResult<SupportLimitBreakResult>> LimitBreakAsync(string supportCardUid, string materialUid);
}

public enum SupportStatus
{
    Succeeded,
    NotEnoughPoints,    // ServerReasons.SupportNotEnoughPoints
    NotEnoughGold,      // ServerReasons.PlayerNotEnoughGold
    InvalidTarget,      // ServerReasons.SupportInvalidTargetLevel
    MaxLimitBreak,      // ServerReasons.SupportMaxLimitBreak
    InvalidMaterial,    // ServerReasons.SupportInvalidMaterial
    NotOwned,           // ServerReasons.SupportNotOwned
    Failed              // それ以外の業務エラーと、業務エラーでない失敗（ApiCore が共通のダイアログを出し終えている）
}

public sealed class SupportOutcome
{
    public SupportStatus Status { get; }
    /// <summary>RPC は成功したが、そのあとの取り直し（RefreshPlayerUseCase）が失敗した。キャッシュは応答の値で直してある（SupportSnapshotEdits）。Succeeded 以外では false。</summary>
    public bool RefreshFailed { get; }
    public static SupportOutcome Success(bool refreshFailed);
    public static SupportOutcome Rejected(SupportStatus status);   // status が Succeeded なら ArgumentException
}

public enum LevelUpStep { Plus1, Plus5, Max }

public static class SupportLevelPlanner
{
    /// <summary>
    /// 押した時点の目標レベル。maxLevel = master.GetSupportMaxLevel(rarity, card.LimitBreak)。
    /// card.Level >= maxLevel なら card.Level（変化なし）。
    /// Plus1 は Math.Min(card.Level + 1, maxLevel)、Plus5 は Math.Min(card.Level + 5, maxLevel)（所持が足りなくても返す。足りなければサーバーが NotEnoughPoints／NotEnoughGold）。
    /// Max は t を maxLevel から card.Level + 1 まで 1 ずつ下げ、cost = master.GetSupportLevelUpCost(rarity, card.Level, t) が
    /// cost.SupportPoints <= supportPoints かつ cost.Gold <= gold になる最初の t。無ければ card.Level。
    /// </summary>
    public static int PlanTarget(OwnedSupportCard card, Rarity rarity, int supportPoints, int gold, LevelUpStep step, IGameMaster master);
}

public static class SupportSnapshotEdits
{
    /// <summary>
    /// snapshot の写しを返す。Gold = result.Gold、SupportPoints = result.SupportPoints、SupportCards のうち SupportCardUid == supportCardUid の 1 枚の Level = result.Level（並びは変えない）。
    /// ほかの値はそのまま。該当の所持が無ければ、カードの列はそのまま（Gold・SupportPoints だけ変える）。
    /// </summary>
    public static PlayerSnapshot WithLevelUp(PlayerSnapshot snapshot, string supportCardUid, SupportLevelUpResult result);
    /// <summary>
    /// snapshot の写しを返す。SupportCardUid == supportCardUid の 1 枚の LimitBreak = result.LimitBreak、SupportCardUid == materialUid の 1 枚を取り除く（ほかの並びは変えない）。ほかの値はそのまま。
    /// </summary>
    public static PlayerSnapshot WithLimitBreak(PlayerSnapshot snapshot, string supportCardUid, string materialUid, SupportLimitBreakResult result);
}

public sealed class LevelUpSupportUseCase
{
    public LevelUpSupportUseCase(ISupportPort port, RefreshPlayerUseCase refresh, PlayerCache cache);
    public UniTask<SupportOutcome> ExecuteAsync(OwnedSupportCard card, int targetLevel);
}

public sealed class LimitBreakSupportUseCase
{
    public LimitBreakSupportUseCase(ISupportPort port, RefreshPlayerUseCase refresh, PlayerCache cache);
    public UniTask<SupportOutcome> ExecuteAsync(OwnedSupportCard card, OwnedSupportCard material);
}
```

`LevelUpSupportUseCase.ExecuteAsync` の手順:
1. `targetLevel <= card.Level` なら `ArgumentOutOfRangeException(nameof(targetLevel))`（通信しない）
2. `var r = await port.LevelUpAsync(card.SupportCardUid, targetLevel);`
3. 失敗: `r.IsBusinessError` で `r.Reason` が `ServerReasons.SupportNotEnoughPoints` → `Rejected(NotEnoughPoints)`、`PlayerNotEnoughGold` → `NotEnoughGold`、`SupportInvalidTargetLevel` → `InvalidTarget`、`SupportNotOwned` → `NotOwned`。それ以外の reason と、業務エラーでない失敗は `Rejected(Failed)`
4. 成功: `try { await refresh.ExecuteAsync(); return SupportOutcome.Success(false); } catch (Exception) { cache.Set(SupportSnapshotEdits.WithLevelUp(cache.Current, card.SupportCardUid, r.Value)); return SupportOutcome.Success(true); }`（1 本目 3.3.5「RPC 成功後の取り直しの失敗は成功として扱う」。追加のダイアログは出さない。取り直しの通信の失敗は ApiCore が出し終えている）

`LimitBreakSupportUseCase.ExecuteAsync` の手順:
1. `material.SupportCardUid == card.SupportCardUid` なら `ArgumentException(nameof(material))`（通信しない）。**`CardId` が違うかはここで確かめない**（選択モードが同じ `CardId` だけを出す。最後はサーバーが `SUPPORT_INVALID_MATERIAL` で拒否する）
2. `var r = await port.LimitBreakAsync(card.SupportCardUid, material.SupportCardUid);`
3. 失敗: `SupportMaxLimitBreak` → `MaxLimitBreak`、`SupportInvalidMaterial` → `InvalidMaterial`、`SupportNotOwned` → `NotOwned`。それ以外は `Failed`
4. 成功: レベル上げと同じ。取り直しが失敗したら `cache.Set(SupportSnapshotEdits.WithLimitBreak(cache.Current, card.SupportCardUid, material.SupportCardUid, r.Value))` して `Success(true)`

### 3.4 `Uta.Client.Application.Owned`

```csharp
public enum OwnedSortKey { Rarity, Level, Acquired }     // サポートカードの並び。育成アイドルは使わない
public enum OwnedDisabledReason { None, AlreadyInFormation, SameCardInFormation, PlanMismatch }
public enum OwnedSelectionKind { Idol, SupportCard, LimitBreakMaterial }

public sealed class OwnedRowData   // 表示の文字列は持たない（Application は Presentation の DisplayText を参照できない。文字列は OwnedPresenter が作る）
{
    public OwnedRowData(bool isIdol, int idolId, string supportCardUid, int cardId, string name, Rarity rarity, Plan plan,
        SupportType supportType, int level, int limitBreak, int shards, bool enabled, OwnedDisabledReason disabledReason);
    public bool IsIdol { get; }
    public int IdolId { get; }                 // 育成アイドル: IdolId。サポートカード: 0
    public string SupportCardUid { get; }      // サポートカード: 所持ID。育成アイドル: null
    public int CardId { get; }                 // サポートカード: OwnedSupportCard.CardId（= SupportCardInfo.SupportCardId）。育成アイドル: 0
    public string Name { get; } public Rarity Rarity { get; } public Plan Plan { get; }
    public SupportType SupportType { get; }    // 育成アイドルは SupportType.Nothing
    public int Level { get; } public int LimitBreak { get; }   // 育成アイドルは 0
    public int Shards { get; }                 // 育成アイドルの OwnedIdol.Shards。サポートカードは 0
    public bool Enabled { get; }               // 選択モードで選べるか（閲覧モードは常に true）
    public OwnedDisabledReason DisabledReason { get; }   // Enabled なら None
}

public sealed class OwnedSelection
{
    public static OwnedSelection Cancelled { get; }                       // 1 つのインスタンスを使い回す
    public static OwnedSelection ForIdol(int idolId);
    public static OwnedSelection ForSupportCard(string supportCardUid);   // 素材の選択もこれ
    public bool IsCancelled { get; }
    public int IdolId { get; }              // ForIdol 以外は 0
    public string SupportCardUid { get; }   // ForSupportCard 以外は null
}

public sealed class OwnedSelectionRequest   // 選択モードの要求（値だけ。完了の仕組みは持たない。結果は基盤の WaitForPopAsync で返す: 3.6）
{
    /// <summary>育成アイドルを選ばせる。excludedIdolIds は選べない（AlreadyInFormation）。null は空。</summary>
    public static OwnedSelectionRequest ForIdol(IReadOnlyCollection<int> excludedIdolIds = null);
    /// <summary>
    /// 編成の枠に入れるサポートカードを選ばせる。excludedUids（ほかの枠の所持ID）、excludedSupportCardIds（ほかの枠のカード ID）、
    /// idolPlan に合わないカード（!FormationValidator.IsPlanCompatible(idolPlan, カードの Plan)）は選べない。null は空。
    /// </summary>
    public static OwnedSelectionRequest ForSupportCard(Plan idolPlan, IReadOnlyCollection<string> excludedUids, IReadOnlyCollection<int> excludedSupportCardIds);
    /// <summary>上限解放の素材を選ばせる。CardId == supportCardId で SupportCardUid != targetUid の所持だけを並べる（全部選べる）。targetUid が null・空なら ArgumentException。</summary>
    public static OwnedSelectionRequest ForLimitBreakMaterial(int supportCardId, string targetUid);

    public OwnedSelectionKind Kind { get; }
    public IReadOnlyCollection<int> ExcludedIdolIds { get; }          // Idol 以外は空
    public Plan IdolPlan { get; }                                     // SupportCard 以外は Plan.Nothing
    public IReadOnlyCollection<string> ExcludedUids { get; }          // SupportCard 以外は空
    public IReadOnlyCollection<int> ExcludedSupportCardIds { get; }   // SupportCard 以外は空
    public int MaterialCardId { get; }                                // LimitBreakMaterial 以外は 0
    public string MaterialTargetUid { get; }                          // LimitBreakMaterial 以外は null
}

public static class OwnedListBuilder
{
    /// <summary>
    /// player.Idols を IdolId の昇順に並べた行（並び替え・絞り込みは無い）。
    /// request が null なら全部 Enabled。Kind == Idol なら ExcludedIdolIds に入る行を Enabled = false・AlreadyInFormation。
    /// Kind がそれ以外なら ArgumentException。
    /// </summary>
    public static IReadOnlyList<OwnedRowData> BuildIdols(PlayerSnapshot player, IGameMaster master, OwnedSelectionRequest request);

    /// <summary>
    /// player.SupportCards の行。手順: (1) Kind == LimitBreakMaterial なら CardId == MaterialCardId かつ SupportCardUid != MaterialTargetUid の所持だけ残す。
    /// (2) planFilter が null でなければ SupportCardInfo.Plan == planFilter のものだけ（完全一致。Free を選んだら Free のカードだけ）。
    /// (3) supportTypeFilter が null でなければ SupportCardInfo.Type == supportTypeFilter のものだけ。(4) 並べる。(5) 選べるかを決める。
    /// request が null・LimitBreakMaterial なら全部 Enabled。Kind == Idol なら ArgumentException。
    /// </summary>
    public static IReadOnlyList<OwnedRowData> BuildSupportCards(PlayerSnapshot player, IGameMaster master, OwnedSortKey sort, bool descending,
        Plan? planFilter, SupportType? supportTypeFilter, OwnedSelectionRequest request);
}
```

`OwnedListBuilder` の規則:
- 名前・レア度・プラン・タイプは `master.GetIdol(idolId)`・`master.GetSupportCard(card.CardId)` から取る（無ければ `KeyNotFoundException` がそのまま出る）
- サポートカードの並べ方: 第 1 の基準で比べ、同じなら `SupportCardUid` の昇順（`string.CompareOrdinal`）で決める（所持ID は重ならないので順は 1 つに決まる）。第 1 の基準: `Rarity` は `(int)Rarity`、`Level` は `Level`、`Acquired` は `SupportCardUid`（`string.CompareOrdinal`）。`descending == true` なら第 1 の基準だけ逆にする（`Rarity` は SSR が先、`Level` は大きいほうが先、`Acquired` は新しいほうが先）。同じ値どうしの第 2 の基準（所持ID の昇順）は向きを変えない
- 選べるかの判定（`Kind == SupportCard`）は、上から順に最初に当たったものを理由にする: `ExcludedUids` に所持ID がある → `AlreadyInFormation`、`ExcludedSupportCardIds` に `CardId` がある → `SameCardInFormation`、`!FormationValidator.IsPlanCompatible(IdolPlan, info.Plan)` → `PlanMismatch`。どれにも当たらなければ `Enabled = true`・`None`
- 0 件なら空のリストを返す

### 3.5 `Uta.Client.Infrastructure`

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
public sealed class SupportGateway : ISupportPort
{
    public SupportGateway(UtaApiClient api);
    public async UniTask<PortResult<SupportLevelUpResult>> LevelUpAsync(string supportCardUid, int targetLevel);
    public async UniTask<PortResult<SupportLimitBreakResult>> LimitBreakAsync(string supportCardUid, string materialUid);
}
```

- `LevelUpAsync` は `new LevelUpRequest(supportCardUid, targetLevel)` を送り、`handledCodes` に `SupportErrors.InvalidTargetLevel`・`SupportErrors.NotEnoughPoints`・`PlayerErrors.NotEnoughGold`・`SupportErrors.NotOwned` を渡す。`LimitBreakAsync` は `new LimitBreakRequest(supportCardUid, materialUid)`、`handledCodes` に `SupportErrors.MaxLimitBreak`・`SupportErrors.InvalidMaterial`・`SupportErrors.NotOwned`（どれも 1 本目で `UtaErrors` に `Stay` で登録済み。`handledCodes` に入れたものは共通ダイアログを出さずに返る）
- 結果の直し方は 2 本目の `GachaGateway.DrawAsync` と同じ: `result.IsSuccess` → `PortResult.Success(new SupportLevelUpResult(v.Level, v.SupportPoints, v.Gold))`（上限解放は `new SupportLimitBreakResult(v.LimitBreak, v.MaxLevel)`）。失敗で `result.Error.Code.Domain == "uta"` かつ `Reason` が `handledCodes` のどれかの `Reason` → `PortResult.BusinessError(Reason, result.Error.ToString())`。それ以外 → `PortResult.Failure(result.Error.ToString())`
- `UtaApiClient` に `public SupportApi Support { get; }` を足し、`Create` で `ServiceClientFactory.Create<ISupportService>(callInvoker)` から作る（`Gacha` と同じ形）。`UtaMagicOnionClientInitializer` の属性に `typeof(ISupportService)` を足す（`using Uta.Shared.Contracts.Support;`）

### 3.6 所持一覧（`Uta.Client.Presentation.Owned`）

```csharp
public enum OwnedTab { Idols, SupportCards }

public sealed class OwnedArgs   // 1 本目 3.3.4 の Args
{
    public OwnedArgs(OwnedSelectionRequest selection);   // null なら閲覧モード
    public OwnedSelectionRequest Selection { get; }
    public static OwnedArgs Browse();                    // new OwnedArgs(null)
}

public sealed class OwnedRowViewData
{
    public OwnedRowViewData(string title, string detail, bool enabled);
    public string Title { get; } public string Detail { get; } public bool Enabled { get; }
}

public interface IOwnedView
{
    void SetTitle(string title);
    void SetTabs(OwnedTab selected, bool visible);
    void SetControls(bool visible, string sortLabel, string orderLabel, string planLabel, string typeLabel, bool filtersVisible);
    void SetRows(IReadOnlyList<OwnedRowViewData> rows);
    event Action<OwnedTab> TabSelected;
    event Action SortKeyClicked;
    event Action SortOrderClicked;
    event Action PlanFilterClicked;
    event Action TypeFilterClicked;
    event Action<int> RowClicked;     // 引数は SetRows に渡した行の添字
    event Action BackClicked;
}

public sealed class OwnedPage : PageBase, IOwnedView { ... }   // レイアウトは下

[AssetAddress(nameof(OwnedPage))]
public sealed class OwnedPresenter : IScreenWithArgs<OwnedArgs>
{
    [Inject] public OwnedPresenter(OwnedPage view, OwnedArgs args, PlayerCache cache, IGameMaster master, IScreenNavigator navigator);
        // : this((IOwnedView)view, args, cache, master, navigator)
    public OwnedPresenter(IOwnedView view, OwnedArgs args, PlayerCache cache, IGameMaster master, IScreenNavigator navigator);
        // args が null なら ArgumentNullException
    public UniTask InitializeAsync();          // 7 つのイベントを購読し、Refresh()。UniTask.CompletedTask
    public void DidPopEnter();                 // Refresh()（詳細から戻ったとき）
    public void Dispose();                     // 7 つの購読を解く
    public UniTask<object> CompleteAsync();    // UniTask.FromResult<object>(result)。基盤が Pop の完了時に呼び、WaitForPopAsync の結果になる
    public UniTask SelectRowAsync(int index);  // RowClicked の受け口が Forget で呼ぶ
    public UniTask BackAsync();                // BackClicked の受け口が Forget で呼ぶ
    public void SelectTab(OwnedTab tab);       // 以下 4 つは同期（通信も遷移もない）。イベントの受け口から直接呼ぶ
    public void CycleSortKey();
    public void ToggleSortOrder();
    public void CyclePlanFilter();
    public void CycleTypeFilter();
}
```

**選択モードの呼び方**（4 本目の編成と、3.7 の上限解放の素材が使う）:

```csharp
var presenter = await navigator.PushPageAsync<OwnedPresenter, OwnedArgs>(new OwnedArgs(request), playAnimation: false);
var selection = await navigator.WaitForPopAsync<OwnedSelection>(presenter);
if (selection == null || selection.IsCancelled) { /* 何もしない */ }
```

（結果は `OwnedPresenter.CompleteAsync` が返す。画面が戻る以外の理由で閉じても、基盤が `CompleteAsync` を呼ぶので `await` は止まらない。テストの `FakeScreenNavigator` は `PopResults` が空なら `null` を返すので、`null` はキャンセルと同じに扱う。）

**`OwnedPresenter` の状態**
- `request = args.Selection`、`result = OwnedSelection.Cancelled`、`rows`（直近に組んだ `IReadOnlyList<OwnedRowData>`）、`running`（実行中フラグ。1 本目 3.3.2 の 5）、`closed`（`PopPageAsync(this)` を呼んだら true にし、戻さない）
- モードごとの初期値:

| モード（`request`） | 見出し（`SetTitle`） | `tab` | タブの表示 | 並び・絞り込みの行 | プラン・タイプのボタン | 初期の並び |
|---|---|---|---|---|---|---|
| 閲覧（null） | 所持一覧 | `Idols` | 出す | `tab == SupportCards` のときだけ出す | 出す | `Rarity`・降順 |
| `Idol` | 育成アイドルを選ぶ | `Idols` | 出さない | 出さない | — | — |
| `SupportCard` | サポートカードを選ぶ | `SupportCards` | 出さない | 出す | 出す | `Rarity`・降順 |
| `LimitBreakMaterial` | 素材にするカードを選ぶ | `SupportCards` | 出さない | 出す | 出さない | `Level`・昇順 |

- サポートカードの状態: `sortKey`・`descending`（上の表）、`planFilter`（null = 全て）、`typeFilter`（null = 全て）。タブを替えても残す（サポートに戻ったとき、ボタンのラベルに今の状態が出るので見える）

**`Refresh()`**
1. `var player = cache.Current;`
2. `tab == Idols` なら `rows = OwnedListBuilder.BuildIdols(player, master, request)`、そうでなければ `rows = OwnedListBuilder.BuildSupportCards(player, master, sortKey, descending, filtersVisible ? planFilter : null, filtersVisible ? typeFilter : null, request)`（`filtersVisible` は上の表の「プラン・タイプのボタン」）
3. 行の文字（`OwnedRowViewData`）:
   - `title` = `Name + " [" + DisplayText.Rarity(Rarity) + "]"`
   - `detail`: 育成アイドルは `DisplayText.Plan(Plan) + "　かけら " + Shards`、サポートカードは `DisplayText.SupportType(SupportType) + "　" + DisplayText.Plan(Plan) + "　Lv " + Level + "　解放 " + LimitBreak`（区切りは全角の空白 1 つ）
   - 無効の行は `detail` の末尾に `"　（編成済み）"`（`AlreadyInFormation`）／`"　（同じカードは編成できない）"`（`SameCardInFormation`）／`"　（プランが合わない）"`（`PlanMismatch`）を足す
4. `view.SetTitle(見出し)`、`view.SetTabs(tab, タブの表示)`、`view.SetControls(並び・絞り込みの行の表示, sortLabel, orderLabel, planLabel, typeLabel, filtersVisible)`、`view.SetRows(行の文字)` の順に呼ぶ
   - `sortLabel`: `Rarity` → `"並び: レア度"`、`Level` → `"並び: Lv"`、`Acquired` → `"並び: 入手順"`
   - `orderLabel`: `descending` なら `"降順"`、そうでなければ `"昇順"`
   - `planLabel`: `planFilter` が null なら `"プラン: 全て"`、そうでなければ `"プラン: " + DisplayText.Plan(planFilter.Value)`
   - `typeLabel`: `typeFilter` が null なら `"タイプ: 全て"`、そうでなければ `"タイプ: " + DisplayText.SupportType(typeFilter.Value)`
   - 行を出さないモード（`Idol`）でもラベルは上の規則で作って渡す（View が隠す）

**操作**
- `SelectTab(tab)`: `request != null` か `tab` が今と同じなら何もしない。そうでなければ `tab` を替えて `Refresh()`
- `CycleSortKey()`: `tab != SupportCards` なら何もしない。`Rarity` → `Level` → `Acquired` → `Rarity`。`Refresh()`
- `ToggleSortOrder()`: `tab != SupportCards` なら何もしない。`descending` を反転。`Refresh()`
- `CyclePlanFilter()`: `tab != SupportCards` か `filtersVisible` が false なら何もしない。null → `Free` → `Sense` → `Logic` → null。`Refresh()`
- `CycleTypeFilter()`: 同じ条件。null → `Singing` → `Rhythm` → `Expression` → `Assist` → null。`Refresh()`
- `SelectRowAsync(index)`: `if (running || closed) return; running = true; try { ... } finally { running = false; }`。中身:
  1. `index` が `rows` の範囲外なら何もしない
  2. `var row = rows[index];`
  3. 選択モード（`request != null`）: `!row.Enabled` なら何もしない。そうでなければ `result = row.IsIdol ? OwnedSelection.ForIdol(row.IdolId) : OwnedSelection.ForSupportCard(row.SupportCardUid);`、`closed = true;`、`await navigator.PopPageAsync(this, playAnimation: false);`
  4. 閲覧モードでサポートカード: `await navigator.PushPageAsync<SupportDetailPresenter, SupportDetailArgs>(new SupportDetailArgs(row.SupportCardUid), playAnimation: false);`
  5. 閲覧モードで育成アイドル: 何もしない（育成アイドルの詳細は MVP で作らない）
- `BackAsync()`: `if (running || closed) return; running = true; try { closed = true; await navigator.PopPageAsync(this, playAnimation: false); } finally { running = false; }`（`result` は `Cancelled` のまま）
- `closed` で、行のタップと「戻る」の連打・組み合わせで `PopPageAsync(this)` が 2 回呼ばれるのを防ぐ（1 本目 3.3.2 の 8。閉じた後の Presenter を渡すと基盤が `InvalidOperationException` を投げる）

**`OwnedPage` のレイアウト**（`BuildUi(body)`。見た目は仮）: `var root = UiFactory.CreateVertical(body, "Root");` の子に上から
1. "Header"（`CreateHorizontal`。`SetHeight(UiTheme.ButtonHeight)`）の子に "Back"（`CreateButton("＜")`）と "Title"（`CreateText("", UiTheme.FontTitle, TextAnchor.MiddleLeft)`）。横幅は半分ずつ（`CreateHorizontal` の既定のまま）
2. "Tabs"（`CreateHorizontal`。`SetHeight(UiTheme.ButtonHeight)`）の子に "TabIdols"「育成アイドル」と "TabSupport"「サポート」（`CreateButton`）
3. "Controls"（`CreateHorizontal`。`SetHeight(UiTheme.ButtonHeight)`）の子に "Sort"・"Order"・"Plan"・"Type"（`CreateButton("")`）
4. "List"（`CreateScrollContent`）。戻り値の Content をフィールドに持つ
- `SetTitle`: Title の `text` を替える
- `SetTabs(selected, visible)`: "Tabs" を `SetActive(visible)`。選んでいるタブのボタンを `UiFactory.SetInteractable(…, false)`（無効の色で「選んでいる」を表す）、もう一方を `true`
- `SetControls(visible, sort, order, plan, type, filtersVisible)`: "Controls" を `SetActive(visible)`。4 つのボタンのラベルを `UiFactory.SetLabel` で替え、"Plan"・"Type" を `SetActive(filtersVisible)`
- `SetRows(rows)`: `UiFactory.ClearChildren(content)` のあと、添字 `i` ごとに `var button = UiFactory.CreateButton(content, "Row" + i, row.Title + "\n" + row.Detail, 160f);`、`UiFactory.SetInteractable(button, row.Enabled)`、`var index = i; button.onClick.AddListener(() => RowClicked?.Invoke(index));`。0 件のときは `CreateText(content, "Empty", "ありません", UiTheme.FontBody, TextAnchor.MiddleCenter)` を 1 つ置く
- ボタンの `onClick`: Back → `BackClicked`、TabIdols → `TabSelected(OwnedTab.Idols)`、TabSupport → `TabSelected(OwnedTab.SupportCards)`、Sort → `SortKeyClicked`、Order → `SortOrderClicked`、Plan → `PlanFilterClicked`、Type → `TypeFilterClicked`
- 公開メソッドの先頭で `EnsureBuilt()` を呼ぶ（1 本目 3.3.3）

イベントの受け口: `TabSelected` → `SelectTab`、`SortKeyClicked` → `CycleSortKey`、`SortOrderClicked` → `ToggleSortOrder`、`PlanFilterClicked` → `CyclePlanFilter`、`TypeFilterClicked` → `CycleTypeFilter`、`RowClicked` → `SelectRowAsync(index).Forget()`、`BackClicked` → `BackAsync().Forget()`。

### 3.7 サポートカードの詳細（`Uta.Client.Presentation.Owned`）

```csharp
public sealed class SupportDetailArgs
{
    public SupportDetailArgs(string supportCardUid);
    public string SupportCardUid { get; }
}

public interface ISupportDetailView
{
    void SetInfo(IReadOnlyList<string> lines);
    void SetAbilities(IReadOnlyList<string> lines);
    void SetResources(string text);
    void SetLevelButtons(string plus1, string plus5, string max, bool plus1Enabled, bool plus5Enabled, bool maxEnabled);
    void SetLimitBreak(string label, bool enabled);
    event Action Plus1Clicked;
    event Action Plus5Clicked;
    event Action MaxClicked;
    event Action LimitBreakClicked;
    event Action BackClicked;
}

public sealed class SupportDetailPage : PageBase, ISupportDetailView { ... }   // レイアウトは下

[AssetAddress(nameof(SupportDetailPage))]
public sealed class SupportDetailPresenter : IScreenWithArgs<SupportDetailArgs>
{
    [Inject] public SupportDetailPresenter(SupportDetailPage view, SupportDetailArgs args, PlayerCache cache, IGameMaster master,
        LevelUpSupportUseCase levelUp, LimitBreakSupportUseCase limitBreak, DialogService dialogs, IScreenNavigator navigator);
        // : this((ISupportDetailView)view, ...)
    public SupportDetailPresenter(ISupportDetailView view, SupportDetailArgs args, PlayerCache cache, IGameMaster master,
        LevelUpSupportUseCase levelUp, LimitBreakSupportUseCase limitBreak, DialogService dialogs, IScreenNavigator navigator);
    public UniTask InitializeAsync();             // 5 つのイベントを購読し、Refresh()
    public void DidPopEnter();                    // Refresh()（素材の選択から戻ったとき）
    public void Dispose();                        // 5 つの購読を解く
    public UniTask LevelUpAsync(LevelUpStep step);
    public UniTask LimitBreakAsync();
    public UniTask BackAsync();
}
```

`FindCard()` = `cache.Current.SupportCards` のうち `SupportCardUid == args.SupportCardUid` の 1 枚（無ければ null）。以下 `info = master.GetSupportCard(card.CardId)`、`maxLevel = master.GetSupportMaxLevel(info.Rarity, card.LimitBreak)`、`maxBreak = master.GetSupportMaxLimitBreak(info.Rarity)`、`fullMaxLevel = master.GetSupportMaxLevel(info.Rarity, maxBreak)`、`sp = cache.Current.SupportPoints`、`gold = cache.Current.Gold`。

**`Refresh()`**
- `card` が null（素材にした・キャッシュから消えた）: `SetInfo(new[] { "このカードは所持していません。" })`、`SetAbilities(new string[0])`、`SetResources("")`、`SetLevelButtons("+1", "+5", "最大", false, false, false)`、`SetLimitBreak("上限解放", false)` で終わる
- `SetInfo` の 4 行: `info.Name`、`DisplayText.Rarity(info.Rarity) + "　" + DisplayText.SupportType(info.Type) + "　" + DisplayText.Plan(info.Plan)`、`"Lv " + card.Level + " / " + maxLevel`、`"上限解放 " + card.LimitBreak + " / " + maxBreak`
- `SetAbilities`: `master.GetSupportAbilities(info.SupportCardId)` の各行 `a`（`Order` の昇順）を `a.Order + " " + DisplayText.Ability(a.Kind, a.Param, ProduceFormulas.AbilityValue(a.ValueAtLv1, a.ValueAtMax, card.Level, fullMaxLevel))`（例 `"1 初期歌唱 +49"`）。0 件なら `new[] { "なし" }`
- `SetResources("強化pt " + N0(sp) + "　ゴールド " + N0(gold))`
- `SetLevelButtons`: 3 つの `step`（名前は `+1`・`+5`・`最大`）ごとに `target = SupportLevelPlanner.PlanTarget(card, info.Rarity, sp, gold, step, master)`
  - `target > card.Level`: 有効。ラベル `名前 + " → Lv " + target + "（強化pt " + N0(cost.SupportPoints) + " / G " + N0(cost.Gold) + "）"`（`cost = master.GetSupportLevelUpCost(info.Rarity, card.Level, target)`）
  - `card.Level >= maxLevel`: 無効。ラベル `名前 + "（上限）"`
  - それ以外（`最大` で、1 レベルも払えない）: 無効。ラベル `"最大（足りません）"`
- `SetLimitBreak`: `card.LimitBreak >= maxBreak` なら `("上限解放（最大）", false)`、それ以外は `("上限解放（素材を選ぶ）", true)`

**`LevelUpAsync(step)`**（`running`・`closed` は 3.6 と同じ。`if (running || closed) return; running = true; try { ... } finally { running = false; }`）
1. `card = FindCard()`。null なら終わる
2. **押した時点の値で**計算し直す: `target = SupportLevelPlanner.PlanTarget(card, info.Rarity, sp, gold, step, master)`。`target <= card.Level` なら何もしない（無効のボタンのイベントが来ても安全）
3. `cost = master.GetSupportLevelUpCost(info.Rarity, card.Level, target)`。`await dialogs.ConfirmAsync("Lv " + card.Level + " → Lv " + target + " に上げます。\n強化pt " + N0(cost.SupportPoints) + "・ゴールド " + N0(cost.Gold) + " を使います。")`。`Negative` なら終わる
4. `var outcome = await levelUp.ExecuteAsync(card, target);`
5. `Succeeded` → `Refresh()`（`RefreshFailed` でもキャッシュは直してあるので同じ。追加のダイアログは出さない）。`NotEnoughPoints` → `dialogs.ShowInfoAsync("強化ptが足りません。")`、`NotEnoughGold` → `"ゴールドが足りません。"`、`InvalidTarget` → `"レベルを上げられません。"`、`NotOwned` → `"このカードは所持していません。"`（どれも `await` し、そのあと `Refresh()`）。`Failed` → 何も出さない（ApiCore が出し終えている）

**`LimitBreakAsync()`**（実行中フラグは同じ）
1. `card = FindCard()`。null なら終わる。`card.LimitBreak >= maxBreak` なら何もしない
2. 素材の候補 = `cache.Current.SupportCards` のうち `CardId == card.CardId` かつ `SupportCardUid != card.SupportCardUid`。0 件なら `await dialogs.ShowInfoAsync("素材にできる同じカードがありません。")` で終わる
3. 素材を選ばせる: `var presenter = await navigator.PushPageAsync<OwnedPresenter, OwnedArgs>(new OwnedArgs(OwnedSelectionRequest.ForLimitBreakMaterial(card.CardId, card.SupportCardUid)), playAnimation: false); var selection = await navigator.WaitForPopAsync<OwnedSelection>(presenter);` `selection == null || selection.IsCancelled` なら終わる
4. `material = cache.Current.SupportCards` から `selection.SupportCardUid` で探す。null なら終わる
5. `await dialogs.ConfirmAsync("Lv " + material.Level + " の「" + info.Name + "」を素材にして上限解放します（" + card.LimitBreak + " → " + (card.LimitBreak + 1) + "）。\n素材は無くなります。強化に使った強化pt・ゴールドは戻りません。")`。`Negative` なら終わる
6. `var outcome = await limitBreak.ExecuteAsync(card, material);`
7. `Succeeded` → `Refresh()`。`MaxLimitBreak` → `"これ以上は解放できません。"`、`InvalidMaterial` → `"素材にできません。"`、`NotOwned` → `"このカードは所持していません。"`（`ShowInfoAsync` を `await` し、そのあと `Refresh()`）。`Failed` → 何も出さない

**`BackAsync()`**: 3.6 の `BackAsync` と同じ（`closed = true` のあと `PopPageAsync(this, playAnimation: false)`）。

**`SupportDetailPage` のレイアウト**（`BuildUi(body)`。見た目は仮）: `var root = UiFactory.CreateVertical(body, "Root");` の子に上から
1. "Header"（`CreateHorizontal`。`SetHeight(UiTheme.ButtonHeight)`）の子に "Back"（`CreateButton("＜")`）と "Title"（`CreateText("サポートカード", UiTheme.FontTitle, TextAnchor.MiddleLeft)`）
2. "Info"（`CreateText("", UiTheme.FontBody, TextAnchor.UpperLeft)`）。`SetInfo` は行を `"\n"` でつないで入れる
3. "Resources"（`CreateText("", UiTheme.FontBody, TextAnchor.MiddleLeft)`）
4. "Plus1"・"Plus5"・"Max"（`CreateButton("")`。縦に 3 つ）。`SetLevelButtons` で `SetLabel` と `SetInteractable`
5. "LimitBreak"（`CreateButton("")`）。`SetLimitBreak` で `SetLabel` と `SetInteractable`
6. "AbilityHeader"（`CreateText("アビリティ", UiTheme.FontBody, TextAnchor.MiddleLeft)`）
7. "Abilities"（`CreateText("", UiTheme.FontSmall, TextAnchor.UpperLeft)`）。`SetAbilities` は行を `"\n"` でつないで入れる
8. "Spacer"（`CreateRect`。`SetFlexibleHeight(1)`）
- `onClick`: Back → `BackClicked`、Plus1 → `Plus1Clicked`、Plus5 → `Plus5Clicked`、Max → `MaxClicked`、LimitBreak → `LimitBreakClicked`
- イベントの受け口: `Plus1Clicked` → `LevelUpAsync(LevelUpStep.Plus1).Forget()`、`Plus5Clicked` → `Plus5`、`MaxClicked` → `Max`、`LimitBreakClicked` → `LimitBreakAsync().Forget()`、`BackClicked` → `BackAsync().Forget()`

### 3.8 ホームと Bootstrap

- `HomePresenter.OpenOwnedAsync()`（1 本目 3.7。2 本目で `IScreenNavigator navigator` がコンストラクターに足されている）: 実行中フラグ（1 本目 3.3.2 の 5）の中身を `await navigator.PushPageAsync<OwnedPresenter, OwnedArgs>(OwnedArgs.Browse(), playAnimation: false);` に置き換える（「準備中です。」を出さなくなる）。コンストラクターの依存は足さない。`using Uta.Client.Presentation.Owned;` を足す
- `UtaBootstrap.RegisterScreens` に `uiRoot.RegisterPage<OwnedPage>();`・`uiRoot.RegisterPage<SupportDetailPage>();` の 2 行を足す
- DI（通常起動・診断モードで共通の登録の後ろ）: `builder.Register<SupportGateway>(Lifetime.Singleton).As<ISupportPort>();`、`builder.Register<LevelUpSupportUseCase>(Lifetime.Singleton);`、`builder.Register<LimitBreakSupportUseCase>(Lifetime.Singleton);`。**Presenter は登録しない**（1 本目 3.3.2。基盤が Push のたびに子スコープで作る）

## 4. タスク

各タスクの終わりの確かめ方は 1 本目 4章の冒頭のとおり（`client/uta` で `unity recompile --format json` のコンパイルエラー 0、EditMode のテストは Editor が開いている環境で `unity test --mode EditMode`。1 本目のタスク 1 で使えなかったときは Editor の Test Runner の EditMode）。**Editor を開けない環境では、通ったとは書かず、確かめていないと報告する**。shared/ のテストは `dotnet test --project shared/Uta.Shared.Domain.Tests`。

### タスク 1: Shared.Domain のプランの適合と、表示名・一覧の組み立て

触る: `shared/Uta.Shared.Domain/Produce/FormationValidator.cs`、`shared/Uta.Shared.Domain.Tests/Produce/FormationValidatorTests.cs`、`Presentation/Common/DisplayText.cs`、`Application/Owned/*`、`FakeGameMaster.cs`（必要なとき）。
手順:
1. 3.1 のとおり `IsPlanCompatible` を足し、`Validate` の条件を置き換える。5章の `FormationValidatorTests` を足す。`dotnet build Uta.slnx`（警告 0・エラー 0）と `dotnet test --project shared/Uta.Shared.Domain.Tests` を通す（既存の `Validate_Cases_ReturnsExpected` が変わらず通ること）。サーバーも `FormationValidator.Validate` を使うので `dotnet test --project server/Uta.Server.Tests` も回す（Docker が無ければ結合テストは確かめていないと報告する）
2. 3.2・3.4 のとおり作る
3. 2 本目の `FakeGameMaster` に、`GetSupportAbilities` が返す行を差し込む口（public フィールド）が無ければ、`public Dictionary<int, List<SupportAbilityInfo>> AbilityList = new Dictionary<int, List<SupportAbilityInfo>>();` を足し、`GetSupportAbilities(id)` は `SupportCardList` に `id` が無ければ `KeyNotFoundException`、あれば `AbilityList` の値（無いキーは空のリスト）を返すようにする。口が既にあれば、それを使い、このファイルは触らない
確かめ方: `DisplayTextTests`・`OwnedListBuilderTests`・`OwnedSelectionRequestTests` が通る。

### タスク 2: ユースケース・API・ゲートウェイ

触る: `Application/Support/*`、`Infrastructure/Api/SupportApi.cs`・`UtaApiClient.cs`・`UtaMagicOnionClientInitializer.cs`、`Infrastructure/Support/SupportGateway.cs`。
手順: 3.3・3.5 のとおり作る。
確かめ方: `SupportLevelPlannerTests`・`SupportSnapshotEditsTests`・`LevelUpSupportUseCaseTests`・`LimitBreakSupportUseCaseTests` が通る。`unity recompile` のあと、`ISupportService` のクライアントの生成コード（`UtaMagicOnionClientInitializer` の `partial`）でエラーが出ていないこと（コンソールの読み出しは [unity7-evaluation.md](../wiki/architecture/unity7-evaluation.md) 4章のコマンド）。

### タスク 3: 所持一覧の画面

触る: `Presentation/Owned/OwnedTab.cs`・`OwnedArgs.cs`・`OwnedRowViewData.cs`・`IOwnedView.cs`・`OwnedPage.cs`・`OwnedPresenter.cs`、`Presentation/Home/HomePresenter.cs`、`Bootstrap/UtaBootstrap.cs`（`OwnedPage` の雛形の登録と DI の 3 行）、`Tests/EditMode/HomePresenterTests.cs`。
手順: 3.6・3.8 のとおり作る。`OwnedPresenter` の閲覧モードの行のタップは `SupportDetailPresenter` を使うので、このタスクでは `SupportDetailArgs`・`SupportDetailPresenter` の型が無いとコンパイルが通らない。**`SupportDetailArgs.cs` と、3.7 のシグネチャだけを持つ `SupportDetailPresenter`（`InitializeAsync` は `UniTask.CompletedTask`、ほかは空。`[AssetAddress(nameof(SupportDetailPage))]` と `SupportDetailPage`（`BuildUi` が空））をこのタスクで作り**、タスク 4 で中身を書く。
確かめ方: `OwnedPresenterTests`・`HomePresenterTests` が通る。

### タスク 4: サポートカードの詳細の画面

触る: `Presentation/Owned/ISupportDetailView.cs`・`SupportDetailPage.cs`・`SupportDetailPresenter.cs`、`Bootstrap/UtaBootstrap.cs`（`SupportDetailPage` の雛形の登録）。
手順: 3.7 のとおり作る。
確かめ方: `SupportDetailPresenterTests` が通る。Play モード（5章）。

### タスク 5: 結果の記録

1. [screen-design.md](../wiki/architecture/screen-design.md): 2章の表の「所持一覧・詳細」と「サポートカードの強化」を、「詳細と強化（レベル上げ・上限解放）は 1 ページ（`SupportDetailPage`）」に直す。3章の図の「所持一覧 ─▶ 詳細 ─▶ 強化」を「所持一覧 ─▶ サポートカードの詳細（強化を含む）」に直し、「上限解放の素材は所持一覧の選択モードで選ぶ」を足す。7.2 の部品の表に、所持一覧の選択モード（`OwnedArgs`・`OwnedSelectionRequest`・`WaitForPopAsync<OwnedSelection>`）を 1 行足す。実装メモに、計画とのズレ（なければ「なし」）を書く
2. [produce-domain.md](../wiki/architecture/produce-domain.md) の Shared.Domain の型の一覧（`FormationValidator(編成の検証 5・6)` の行）に `IsPlanCompatible`（プランの適合。クライアントの表示も使う）を足す
3. [idol-support.md](../wiki/systems/idol-support.md): 2.1 の「検証の置き場」の「同じ関数」に `FormationValidator.IsPlanCompatible`（6）と書き添える。「実装メモ」に「上限解放の確認ダイアログは、素材の Lv にかかわらず常に『強化に使った強化pt・ゴールドは戻らない』を出す（2.2.1 の『素材のレベルが 1 より大きいとき』より広い）」と書く
4. [screen-wireframes.md](../wiki/overview/screen-wireframes.md) 10 の「一覧の並び」の行の後ろに「[Uta案] MVP では育成アイドルは 4 人なので、`IdolId` の昇順に固定し、並び・絞り込みはサポートカードだけに出す（[計画](../../plans/2026-11-03-client-screens-owned.md)）」を足す
5. [unity7-evaluation.md](../wiki/architecture/unity7-evaluation.md): 2章の表・3.3 に、所持一覧（スクロール・動的に作る行のボタン）と詳細の画面で確かめたこと・確かめていないことを足す
6. この計画の frontmatter を `status: done`、`updated` をその日にする

## 5. テスト

置き場・名前空間・書き方は 1 本目 5章・3.3.7（client/ の EditMode。偽の View は各テストのファイルの中の `private sealed class`。`FakeScreenNavigator`・`FakeDialogService`・偽の View は同じ `CallLog` を使う）。shared/ は xUnit（既存の `FormationValidatorTests` に足す）。

偽の View の記録の書式（3.3.7 の `"<メソッド名>:<引数を , でつないだもの>"` に従い、リストは `|` でつなぐ）:
- `FakeOwnedView`: `"SetTitle:" + title`、`"SetTabs:" + selected + "," + visible`、`"SetControls:" + visible + "," + sort + "," + order + "," + plan + "," + type + "," + filtersVisible`、`"SetRows:" + string.Join("|", rows の各 r の r.Title + "/" + r.Detail + "/" + r.Enabled)`。`RaiseRowClicked(int)` などでイベントを起こす
- `FakeSupportDetailView`: `"SetInfo:" + string.Join("|", lines)`、`"SetAbilities:" + string.Join("|", lines)`、`"SetResources:" + text`、`"SetLevelButtons:" + plus1 + "," + plus5 + "," + max + "," + 3 つの bool`、`"SetLimitBreak:" + label + "," + enabled`
- 偽のポート（`FakeSupportPort : ISupportPort`。返す `PortResult` を public フィールドで差し込み、呼ばれた引数を `List<string>` に記録）と偽の `IPlayerPort`（`SignInAsync` の結果を public フィールドで差し込む。`Failure` を返すと `RefreshPlayerUseCase` が例外を投げる）は、使うテストのファイルの中に書く

| ファイル | テスト | 内容 |
|---|---|---|
| `shared/.../FormationValidatorTests.cs` | `IsPlanCompatible_Cases_ReturnsExpected`（`[Theory]`） | `(Sense, Free)` true、`(Sense, Sense)` true、`(Sense, Logic)` false、`(Logic, Sense)` false、`(Logic, Free)` true、`(Sense, Nothing)` false |
| `DisplayTextTests.cs` | `Rarity_AllValues` / `Plan_AllValues` / `SupportType_AllValues` / `Param_AllValues` | 各 enum の全部の値（`Nothing` を含む）と、範囲外の値（`(Plan)99` など）が `"-"` |
| | `Ability_InitialParam` / `Ability_ParamBonus_PerThousand` / `Ability_MaxHp` | `"初期歌唱 +49"`、85 → `"歌唱ボーナス +8.5%"`・100 → `"歌唱ボーナス +10.0%"`、`"最大体力 +9"` |
| `SupportLevelPlannerTests.cs` | `Plus1_BelowMax_ReturnsLevelPlusOne` / `Plus5_NearMax_ClampsToMax` / `AtMax_ReturnsCurrentLevel` | `FakeGameMaster.MaxLevel = (_, lb) => 20 + lb * 5` |
| | `Plus5_NotAffordable_StillReturnsTarget` | 強化pt 0 でも Lv 3 → 8 |
| | `Max_AffordableLimited_ReturnsHighestAffordable` | `LevelUpCost = (_, from, to) => new SupportLevelUpCost((to - from) * 100, (to - from) * 50)`、強化pt 250・G 1000 なら Lv + 2 |
| | `Max_CannotAffordAny_ReturnsCurrentLevel` | |
| `SupportSnapshotEditsTests.cs` | `WithLevelUp_ReplacesLevelAndResources` | 該当の 1 枚の Level と Gold・SupportPoints だけ変わり、カードの並び・ほかの値は同じ |
| | `WithLimitBreak_RaisesLimitBreakAndRemovesMaterial` | 該当の LimitBreak が応答の値、素材が消え、ほかの並びは同じ |
| `OwnedSelectionRequestTests.cs` | `ForIdol_NullExcluded_IsEmpty` / `ForSupportCard_NullCollections_AreEmpty` | |
| | `ForLimitBreakMaterial_SetsFields` / `ForLimitBreakMaterial_EmptyTarget_Throws` | |
| `OwnedListBuilderTests.cs` | `BuildIdols_OrderedByIdolId` | `player.Idols` が 3, 1, 2 の順でも 1, 2, 3 |
| | `BuildIdols_Selection_DisablesExcluded` / `BuildIdols_SupportRequest_Throws` | |
| | `BuildSupportCards_SortByLevelDescending_TieByUidAscending` | Lv が同じ 2 枚は所持ID の昇順 |
| | `BuildSupportCards_SortByAcquiredDescending` | 所持ID の降順 |
| | `BuildSupportCards_PlanFilter_ExactMatch` / `BuildSupportCards_TypeFilter` | `Free` で絞ると `Free` のカードだけ |
| | `BuildSupportCards_Selection_DisablesByPriority` | `idolPlan = Sense`。`excludedUids` の 1 枚（プランも合わない）→ `AlreadyInFormation`、`excludedSupportCardIds` の CardId の別の所持（プランも合わない）→ `SameCardInFormation`、`Logic` → `PlanMismatch`、`Free` → 有効 |
| | `BuildSupportCards_Material_ListsSameCardExceptTarget` | 同じ CardId の 3 枚（target を含む）と別の CardId 1 枚 → target 以外の 2 枚だけ、全部有効 |
| | `BuildSupportCards_IdolRequest_Throws` / `BuildSupportCards_Empty_ReturnsEmpty` | |
| `LevelUpSupportUseCaseTests.cs` | `Execute_Success_Refreshes` | `RefreshFailed == false`、キャッシュはサインインの値 |
| | `Execute_SuccessButRefreshFails_PatchesCache` | `IPlayerPort.SignInAsync` が `Failure` → `Succeeded`・`RefreshFailed == true`、キャッシュの該当カードの Level と Gold・SupportPoints が応答の値 |
| | `Execute_NotEnoughPoints_ReturnsStatus` / `Execute_NotEnoughGold_ReturnsStatus` / `Execute_InvalidTarget_ReturnsStatus` / `Execute_NotOwned_ReturnsStatus` | |
| | `Execute_OtherFailure_ReturnsFailed` / `Execute_TargetNotAboveLevel_Throws` | 後者はポートが呼ばれない |
| `LimitBreakSupportUseCaseTests.cs` | `Execute_Success_Refreshes` / `Execute_SuccessButRefreshFails_PatchesCache` | 後者は素材がキャッシュから消え、該当の LimitBreak が応答の値 |
| | `Execute_SameUid_Throws` / `Execute_InvalidMaterial_ReturnsStatus` / `Execute_MaxLimitBreak_ReturnsStatus` / `Execute_NotOwned_ReturnsStatus` | |
| `OwnedPresenterTests.cs` | `InitializeAsync_Browse_ShowsIdolsWithoutControls` | `"SetTitle:所持一覧"`, `"SetTabs:Idols,True"`, `"SetControls:False,…"`, `"SetRows:…"`（IdolId の昇順） |
| | `SelectTab_Support_ShowsControlsAndSortedRows` | `"SetControls:True,並び: レア度,降順,プラン: 全て,タイプ: 全て,True"`、行はレア度の降順 |
| | `CycleSortKey_CyclesThreeKeys` / `CyclePlanFilter_Cycles` / `CycleTypeFilter_Cycles` | ラベルが `並び: Lv` → `並び: 入手順` → `並び: レア度`、`プラン: 共通` → `感性` → `理論` → `全て` など |
| | `SelectTab_KeepsSupportFilters` | サポートで `typeFilter = 歌唱` → 育成アイドル → サポートに戻ると `タイプ: 歌唱` のまま |
| | `SelectTab_InSelectionMode_Ignored` | |
| | `SelectRowAsync_BrowseSupport_PushesDetail` | `"PushPage:SupportDetailPresenter"`、`Args[0]` が `SupportDetailArgs`（所持ID が行のもの） |
| | `SelectRowAsync_BrowseIdol_DoesNothing` | `log` に `"PushPage:"`・`"PopPage:"` が無い |
| | `SelectRowAsync_Selection_SetsResultAndPops` | `"PopPage:OwnedPresenter"`、`await CompleteAsync()` が `ForIdol(行の IdolId)` |
| | `SelectRowAsync_Twice_PopsOnce` | 2 回呼ぶ（2 回目は 1 回目の後）→ `"PopPage:"` が 1 回 |
| | `SelectRowThenBack_PopsOnce` | `SelectRowAsync` の後の `BackAsync` で `"PopPage:"` が増えない。結果は選んだもの |
| | `SelectRowAsync_DisabledRow_DoesNothing` / `SelectRowAsync_OutOfRange_DoesNothing` | |
| | `BackAsync_Selection_CompletesCancelled` | `CompleteAsync()` の結果の `IsCancelled == true` |
| | `InitializeAsync_IdolSelection_HidesTabsAndControls` / `InitializeAsync_Material_HidesFiltersAndSortsByLevelAscending` | `"SetTitle:育成アイドルを選ぶ"`・`"SetTabs:Idols,False"`・`"SetControls:False,…"`／`"SetTitle:素材にするカードを選ぶ"`・`"SetControls:True,並び: Lv,昇順,…,False"` |
| | `Dispose_UnsubscribesEvents` | `Dispose()` のあと `RaiseBackClicked()` しても `"PopPage:"` が無い |
| `SupportDetailPresenterTests.cs` | `InitializeAsync_ShowsInfoAbilitiesAndButtons` | `"SetInfo:名前|SSR　歌唱　共通|Lv 3 / 40|上限解放 0 / 4"`、`"SetAbilities:1 初期歌唱 +…"`、ボタンのラベルと有効/無効 |
| | `InitializeAsync_AtMaxLevel_ButtonsShowCap` / `InitializeAsync_MaxNotAffordable_ShowsShortage` | `"+1（上限）"` 等／`"最大（足りません）"`・無効 |
| | `LevelUpAsync_Confirmed_ExecutesAndRefreshes` | `"Confirm:Lv 3 → Lv 4 に上げます。\n強化pt …"` の後、ポートに `(uid, 4)`、View の再表示 |
| | `LevelUpAsync_Declined_DoesNotExecute` | `ConfirmResults` に `Negative` → ポートが呼ばれない |
| | `LevelUpAsync_UsesCacheAtClickTime` | 表示の後にキャッシュの Level を 5 に差し替えて `Plus1` → ポートに `(uid, 6)` |
| | `LevelUpAsync_NotEnoughPoints_ShowsInfo` | `"Info:強化ptが足りません。"` |
| | `LimitBreakAsync_NoMaterial_ShowsInfo` | `"Info:素材にできる同じカードがありません。"`、`"PushPage:"` が無い |
| | `LimitBreakAsync_SelectsMaterialThenConfirms` | `PopResults` に `OwnedSelection.ForSupportCard(素材の uid)`。`log` が `"PushPage:OwnedPresenter"`, `"WaitForPop"`, `"Confirm:Lv … を素材にして上限解放します（0 → 1）。\n素材は無くなります。強化に使った強化pt・ゴールドは戻りません。"` の順、ポートに `(uid, 素材の uid)`、`Args[0]` の `Selection.Kind == LimitBreakMaterial` |
| | `LimitBreakAsync_SelectionCancelled_DoesNotExecute` | `PopResults` が空（null）→ `"Confirm:"` が無く、ポートが呼ばれない |
| | `LimitBreakAsync_AtMax_DoesNothing` | `LimitBreak == 4` → `"PushPage:"` も `"Info:"` も無い |
| | `Refresh_CardGone_DisablesAll` | キャッシュに所持が無い → `"SetInfo:このカードは所持していません。"`、ボタンが全部無効 |
| `HomePresenterTests.cs`（変更） | `Clicks_ShowNotReady` | 「アイドル」（`OwnedClicked`）は `"Info:準備中です。"` を出さなくなるので、期待する回数から 1 つ減らす（2 本目の変更の後の回数から） |
| | `OpenOwnedAsync_PushesOwnedBrowse`（追加） | `"PushPage:OwnedPresenter"`、`Args[0]` が `OwnedArgs` で `Selection == null` |

Play モード・手動（Editor、サーバー起動済み。初期付与済みのプレイヤーに、ガチャで同じサポートカードを 2 枚以上引かせておく）:
- ホーム →「アイドル」で所持一覧。育成アイドルのタブは IdolId の順で、並び・絞り込みの行が出ない。サポートのタブで並び・プラン・タイプが効く
- サポートカードをタップ → 詳細。アビリティが「1 初期歌唱 +N」の形で出る。「+1」→ 確認 → Lv が上がり、強化pt・ゴールドが減る。「最大」で足りる範囲の最大まで上がる
- 強化ptが足りないときの「+5」→ 確認 →「強化ptが足りません。」
- 同じカードを 2 枚持った状態で「上限解放（素材を選ぶ）」→ 素材の一覧（同じカードだけ）→ 選ぶ → 確認 → 解放数が増え、所持一覧に戻ると素材が消えている
- 素材の一覧で「＜」→ 詳細に戻り、何も起きない
- 選択モード（育成アイドル・サポートカード）は 4 本目の編成で確かめる

## 6. 未決事項

なし
