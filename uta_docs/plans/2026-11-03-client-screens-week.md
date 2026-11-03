---
title: 実装計画: client/ の画面 5/6 週の行動選択・授業・支給・スキルカードの選択肢
status: draft
updated: 2026-11-03
spec: ../wiki/systems/produce.md
---

# 実装計画: client/ の画面 5/6 週の行動選択・授業・支給・スキルカードの選択肢

## 1. 目的と前提

[game-spec.md](../wiki/overview/game-spec.md) の順 7 の 5 本目。プロデュースの「週の選択」の局面（`ProducePhase.WeekSelect`）と「カードの選択肢」の局面（`CardOffer`）の画面を作る: 週の行動選択（レッスン・授業・支給・休む）、授業の選択肢、スキルカードの獲得（モーダル）。局面の遷移は [produce.md](../wiki/systems/produce.md) 10.1、RPC は [produce-server.md](../wiki/architecture/server/produce-server.md) 5〜6章、画面の叩き台は [screen-wireframes.md](../wiki/overview/screen-wireframes.md) 2・4・5。方針の正は [screen-design.md](../wiki/architecture/screen-design.md) 7章（特に 7.4: 局面が画面を決める）。

- **前提**: 1〜4 本目が done（`ProduceSession`・`ProduceActions`・`ProduceFlow`・`ProduceMenu`・`ProduceStateView`・`DisplayText`・`FakeProducePort` を使う）
- **決めないこと / 作らないもの**: レッスン・試験・結果（6 本目）、相談・おでかけ・ドリンク・育成アイテム・デッキの確認（MVP 外）、スキルカードの再抽選・除去（初期回数が 0 で MVP では使えない: [produce.md](../wiki/systems/produce.md) 3.5。状態の DTO にも回数が無い。ボタンを作らない）、特別レッスンの印（レッスンに入る前は分からない: `WeekActionDto` に無い。レッスンに入ったあとの画面（6 本目）で出す）、カードの効果の説明文（[screen-design.md](../wiki/architecture/screen-design.md) 7.3）
- **決めたこと（この計画で）**: 「獲得する」のボタンは作らず、カードをタップ → 確認ダイアログ → 獲得とする（叩き台の「選ぶ → 獲得する」と同じ結果）。授業・休むの結果は、操作の前後の状態の差を情報ダイアログで出す。支給は結果の表示を挟まず、そのままスキルカードの選択肢に進む

## 2. 触るファイル

パスは `client/uta/Assets/` からの相対。

| 操作 | パス | 中身 |
|---|---|---|
| 作成 | `Scripts/Application/Produce/ProduceChange.cs` | 操作の前後の差 |
| 作成 | `Scripts/Application/Produce/CardSummary.cs`・`CardSummaryBuilder.cs` | スキルカードの表示用の値 |
| 作成 | `Scripts/Application/Produce/WeekProgress.cs` | 次の試験までの週数 |
| 変更 | `Scripts/Presentation/Common/DisplayText.cs` | スキルカード・消費・差の文字 |
| 作成 | `Scripts/Presentation/Produce/IWeekView.cs`・`WeekPage.cs`・`WeekPresenter.cs` | 週の行動選択 |
| 作成 | `Scripts/Presentation/Produce/IClassView.cs`・`ClassPage.cs`・`ClassPresenter.cs` | 授業の選択肢 |
| 作成 | `Scripts/Presentation/Produce/ICardOfferView.cs`・`CardOfferModal.cs`・`CardOfferPresenter.cs` | スキルカードの獲得 |
| 変更 | `Scripts/Presentation/Produce/ProduceFlow.cs` | 局面 `WeekSelect`・`CardOffer` の画面を足す |
| 変更 | `Scripts/Bootstrap/UtaBootstrap.cs` | Presenter の登録 |
| 作成 | `Tests/EditMode/*.cs`（5章） | テスト |

## 3. 型とシグネチャ

C# は C# 9 の範囲。

### 3.1 `Uta.Client.Application.Produce`

```csharp
public readonly struct ProduceChange
{
    public ProduceChange(int singing, int rhythm, int expression, int hp);
    public int Singing { get; } public int Rhythm { get; } public int Expression { get; } public int Hp { get; }
    public bool IsEmpty { get; }                       // 4 つとも 0
    /// <summary>cur - prev（パラメータ 3 つと Hp）。prev が null なら全部 0。</summary>
    public static ProduceChange Create(ProduceStateView prev, ProduceStateView cur);
}

public sealed class CardSummary
{
    public CardSummary(int cardId, bool enhanced, Rarity rarity, CardType type, Plan plan, IReadOnlyList<CostDef> costs);
    public int CardId { get; } public bool Enhanced { get; } public Rarity Rarity { get; } public CardType Type { get; } public Plan Plan { get; }
    public IReadOnlyList<CostDef> Costs { get; }       // GetFace(enhanced).Costs
}
public static class CardSummaryBuilder
{
    /// <summary>catalog.Get(cardId)（CardDefinition）から作る。無いカード ID は KeyNotFoundException。costs は definition.GetFace(enhanced).Costs。</summary>
    public static CardSummary Build(ICardCatalog catalog, int cardId, bool enhanced);
}

public static class WeekProgress
{
    /// <summary>
    /// week 以降（week を含む）で最初の試験の週（WeekKind が MidExam か FinalExam）を scenario.Weeks から探し、(その週の WeekKind, その週 - week) を返す。無ければ (WeekKind.Nothing, 0)。
    /// </summary>
    public static (WeekKind kind, int weeksLeft) NextExam(ScenarioInfo scenario, int week);
}
```

（`CostDef`・`CardType`・`Rarity`・`Plan`・`ICardCatalog` は `Uta.Shared.Domain.Lesson`。`WeekKind` は `Uta.Shared.Domain.Produce`。）

### 3.2 `DisplayText` の追加（`Presentation/Common/DisplayText.cs`）

```csharp
public static string CardType(CardType type);     // Active → "アクティブ"、Mental → "メンタル"、Trouble → "不調"、Nothing → "-"
public static string CostKind(CostKind kind);     // Consume → "消費"、HpDirect → "体力"、GoodConditionTurns → "好調"、ExcellentTurns → "絶好調"、Focus → "集中"、Impression → "好印象"、Motivation → "やる気"、Nothing → "-"
public static string Card(CardSummary card);      // "カード<CardId>[+] [<Rarity>] <CardType> <Plan>"（強化済みは CardId の直後に "+"）。例 "カード7+ [SR] アクティブ 感性"
public static string Costs(CardSummary card);     // 消費の並び。"<CostKind> <値>" を "、" でつなぐ。無ければ "コストなし"。例 "消費 4、体力 2"
public static IReadOnlyList<string> Change(ProduceChange change);   // 0 でないものだけ: "歌唱 +40"、"体力 -8"（負は "-"、正は "+"）。パラメータは歌唱・律動・表現・体力の順
```

### 3.3 `Uta.Client.Presentation.Produce`

```csharp
public sealed class WeekActionViewData
{
    public WeekActionViewData(int slotIndex, string label, bool enabled);
    public int SlotIndex { get; } public string Label { get; } public bool Enabled { get; }
}

public interface IWeekView
{
    void SetHeader(string weekText, string nextExamText);       // "週 3 / 12"、"中間試験まで 2 週" / "今週は試験" / ""
    void SetStatus(IReadOnlyList<string> lines);                // "歌唱 40 / 1500" "律動 …" "表現 …" "体力 18 / 30" "再挑戦 残り 3"
    void SetActions(IReadOnlyList<WeekActionViewData> actions);
    event Action<int> ActionClicked;                            // slotIndex
    event Action MenuClicked;
}
public sealed class WeekPage : PageBase, IWeekView { ... }

public sealed class WeekPresenter : Presenter<IWeekView>
{
    public WeekPresenter(ProduceSession session, IGameMaster master, ProduceActions actions, ProduceFlow flow, ProduceMenu menu, DialogService dialogs, ScreenNavigator navigator);
}

public sealed class ClassOptionViewData
{
    public ClassOptionViewData(int index, string label, bool enabled);
    public int Index { get; } public string Label { get; } public bool Enabled { get; }
}
public interface IClassView
{
    void SetHeader(string text);                                 // "授業　体力 18 / 30"
    void SetOptions(IReadOnlyList<ClassOptionViewData> options);
    event Action<int> OptionClicked;                             // ClassOption の Index
    event Action BackClicked;
}
public sealed class ClassPage : PageBase, IClassView { ... }
public sealed class ClassPresenter : Presenter<IClassView>
{
    public ClassPresenter(ProduceSession session, ProduceActions actions, ProduceFlow flow, DialogService dialogs, ScreenNavigator navigator);
}

public sealed class CardOfferItemViewData
{
    public CardOfferItemViewData(int index, string title, string costs);
    public int Index { get; } public string Title { get; } public string Costs { get; }
}
public interface ICardOfferView
{
    void SetCards(IReadOnlyList<CardOfferItemViewData> cards);
    void SetDeckCount(int count);                                // "デッキ <n> 枚"
    event Action<int> CardClicked;                               // 選択肢の添字（CardOffers の添字）
    event Action DeclineClicked;
}
public sealed class CardOfferModal : ModalBase, ICardOfferView { ... }
public sealed class CardOfferPresenter : Presenter<ICardOfferView>
{
    public CardOfferPresenter(ProduceSession session, ICardCatalog catalog, ProduceActions actions, ProduceFlow flow, DialogService dialogs, ScreenNavigator navigator);
}
```

**`WeekPresenter` の動き**
- `OnBind`/`Refresh()`: `state = session.Current`、`scenario = master.GetScenario(state.ScenarioId)`。
  - `SetHeader($"週 {state.Week} / {scenario.WeekCount}", nextExamText)`。`nextExamText`: `WeekProgress.NextExam(scenario, state.Week)` が `(Nothing, _)` なら `""`、`weeksLeft == 0` なら `"今週は試験"`、それ以外は `$"{(kind == MidExam ? "中間試験" : "最終試験")}まで {weeksLeft} 週"`
  - `SetStatus`: `$"歌唱 {state.Singing} / {scenario.ParamCap}"`、同様に律動・表現、`$"体力 {state.Hp} / {state.MaxHp}"`、`$"再挑戦 残り {state.ExamRetryLeft}"`
  - `SetActions`: `state.WeekActions` の各要素を `WeekActionViewData` にする。`label`: `Lesson` → その週の `scenario.Weeks` の `Kind` が `Finishing` なら `"仕上げレッスン"`、そうでなければ `"レッスン"`、`Class` → `"授業"`、`Supply` → `"支給"`、`Rest` → `"休む"`。`enabled = action.Selectable`
- `ActionClicked(slotIndex)`（実行中は無視する）: `action = state.WeekActions` から `SlotIndex` で探す（無ければ何もしない。`Selectable` が false でも何もしない）:
  - `Lesson`: `var i = await dialogs.ChooseAsync("どのレッスンにしますか？", ["歌唱", "律動", "表現", "やめる"]);` `3` なら何もしない。`param` = `i` の 0・1・2 を `Singing`・`Rhythm`・`Expression`。`status = await actions.ChooseWeekActionAsync(slotIndex, -1, param)` → `await flow.HandleStepAsync(status)`
  - `Class`: `await navigator.PushPageAsync<ClassPage, ClassPresenter>();`
  - `Supply`: `ConfirmAsync("支給を受け取ります。よろしいですか？")` が `Positive` なら `actions.ChooseWeekActionAsync(slotIndex, -1, ParamKind.Nothing)` → `flow.HandleStepAsync(status)`
  - `Rest`: `ConfirmAsync("休みます。体力が回復します。よろしいですか？")` が `Positive` なら `actions.ChooseWeekActionAsync(slotIndex, -1, ParamKind.Nothing)`。`Succeeded` のとき `change = ProduceChange.Create(session.Previous, session.Current)` が空でなければ `await dialogs.ShowInfoAsync(string.Join("\n", DisplayText.Change(change)))`。その後 `flow.HandleStepAsync(status)`
  - 例外: `dialogs.ShowInfoAsync("通信に失敗しました: " + e.Message)`
- `MenuClicked` → `await menu.ShowAsync();`

**`ClassPresenter` の動き**
- `OnBind`: `state = session.Current`。`SetHeader($"授業　体力 {state.Hp} / {state.MaxHp}")`。`SetOptions`: `state.ClassOptions` の各要素 → `label` = `(HpCost == 0 ? "コストなし" : $"体力 {HpCost}") + $": {DisplayText.Param(Param)} +{ParamGain}" + (GivesCard ? " / スキルカード 1 枚" : "")`、`enabled = Selectable`
- `OptionClicked(index)`（実行中は無視）: `option` を `Index` で探す（無い・`Selectable` が false は何もしない）。`ConfirmAsync($"{label} を選びます。取り消せません。よろしいですか？")` が `Positive` なら、`slot` = `state.WeekActions` のうち `Kind == WeekChoiceKind.Class` の `SlotIndex`（無ければ何もしない）として `status = await actions.ChooseWeekActionAsync(slot, option.Index, ParamKind.Nothing)`:
  - `Rejected`: `dialogs.ShowInfoAsync("その操作はできません。")`（ページは閉じない）。`Failed`: 何もしない
  - それ以外（`Succeeded`・`Recovered`・`SessionEnded`）: `await navigator.PopPageAsync();`（このページを閉じる）→ `Succeeded` で `ProduceChange.Create(session.Previous, session.Current)` が空でなければ `dialogs.ShowInfoAsync(string.Join("\n", DisplayText.Change(change)))` → `await flow.HandleStepAsync(status);`
  - 例外: `dialogs.ShowInfoAsync("通信に失敗しました: " + e.Message)`
- `BackClicked` → `navigator.PopPageAsync()`

**`CardOfferPresenter` の動き**
- `OnBind`: `SetCards`: `session.Current.CardOffers` の各 `cardId`（添字 `i`）→ `summary = CardSummaryBuilder.Build(catalog, cardId, false)` → `new CardOfferItemViewData(i, DisplayText.Card(summary), DisplayText.Costs(summary))`。`SetDeckCount(session.Current.Deck.Count)`
- `CardClicked(i)`（実行中は無視）: `ConfirmAsync($"{title} を獲得します。よろしいですか？")` が `Positive` なら `status = await actions.ResolveCardOfferAsync(CardOfferAction.Pick, i)`
- `DeclineClicked`: `ConfirmAsync("スキルカードを受け取りません。よろしいですか？")` が `Positive` なら `actions.ResolveCardOfferAsync(CardOfferAction.Decline, 0)`
- どちらも結果の扱いは共通: `Rejected` → `dialogs.ShowInfoAsync("その操作はできません。")`、`Failed` → 何もしない、それ以外 → `await navigator.PopModalAsync();` のあと `await flow.HandleStepAsync(status);`。例外は `"通信に失敗しました: " + e.Message`

**`ProduceFlow` に足す局面**（`ShowAsync(bool replace)` の `switch`。`replace` は `AdvanceAsync` が true、`EnterAsync` が false）:
- `ProducePhase.WeekSelect`: `replace ? ReplacePageAsync<WeekPage, WeekPresenter>() : PushPageAsync<WeekPage, WeekPresenter>()`
- `ProducePhase.CardOffer`: `PushModalAsync<CardOfferModal, CardOfferPresenter>()`（`replace` に関係なく、いまのページの上にモーダルを重ねる）
- そのほかの局面は今まで（6 本目が足す）

**画面のレイアウト**（`BuildUi`。見た目は仮）
- `WeekPage`: ヘッダー行（週の Text、右に「≡」ボタン）、次の試験の Text、状態の Text（`SetStatus` の行を改行でつなぐ）、`SetActions` のボタンを縦に並べる（無効は `SetInteractable(false)`）
- `ClassPage`: ヘッダー（「＜」ボタンと `SetHeader` の Text）、選択肢のボタンを縦に並べる（高さは `UiTheme.ButtonHeight`）
- `CardOfferModal`: `Panel` にタイトル「スキルカードを1枚獲得」、カードごとに縦に並んだボタン（`Title` と `Costs` の 2 行）、`SetDeckCount` の Text、「受け取らない」ボタン

## 4. タスク

各タスクの終わりに `unity recompile --format json` でコンパイルエラー 0、そのタスク分のテストが通る状態にする。Editor を開けないときは、確かめていないと報告する。

### タスク 1: Application の値と文字

触る: `Application/Produce/ProduceChange.cs`・`CardSummary.cs`・`CardSummaryBuilder.cs`・`WeekProgress.cs`、`Presentation/Common/DisplayText.cs`。
手順: 3.1・3.2 のとおり。`CardSummaryBuilder` は `ICardCatalog.Get(int)`（`shared/Uta.Shared.Domain/Lesson/CardDefinition.cs`。無いカード ID は `KeyNotFoundException`）を使う。
確かめ方: `ProduceChangeTests`・`CardSummaryBuilderTests`・`WeekProgressTests`・`DisplayTextCardTests`。

### タスク 2: 画面

触る: `Presentation/Produce/*`（2 章）、`ProduceFlow.cs`、`Bootstrap/UtaBootstrap.cs`。
手順: 3.3 のとおり。`UtaBootstrap` に `Register<WeekPresenter>(Transient)`、`Register<ClassPresenter>(Transient)`、`Register<CardOfferPresenter>(Transient)` を足す。
確かめ方: `WeekPresenterTests`・`ClassPresenterTests`・`CardOfferPresenterTests`・`ProduceFlowTests`（追記）。Play モード（下）。

### タスク 3: 結果の記録

`screen-design.md` の「実装メモ」にズレを書く（なければ「なし」）。この計画を `done` にする。

## 5. テスト

| ファイル | テスト | 内容 |
|---|---|---|
| `ProduceChangeTests.cs` | `Create_ComputesDifferences` / `Create_PrevNull_AllZero` / `IsEmpty` | |
| `CardSummaryBuilderTests.cs` | `Build_UsesFaceOfEnhanced` | 強化の有無でコストが変わるカード定義（テストで `CardCatalog.Create` に行を渡して作る。`MasterCardCatalogTests` と同じ作り）で、`Costs` が `GetFace(enhanced).Costs` |
| | `Build_Unknown_Throws` | `KeyNotFoundException` |
| `WeekProgressTests.cs` | `NextExam_BeforeMid_ReturnsMidAndWeeks` / `NextExam_OnExamWeek_ReturnsZero` / `NextExam_AfterAll_ReturnsNothing` | |
| `DisplayTextCardTests.cs` | `Card_Enhanced_HasPlus` / `Costs_Empty_IsNoCost` / `Change_OnlyNonZeroInOrder` / `Change_Negative_HasMinus` | |
| `WeekPresenterTests.cs` | `Bind_ShowsHeaderStatusAndActions` | `FakeProducePort` ではなく、`ProduceSession` に `ProduceStateView` を `Set` して偽の `IWeekView` を渡す。ラベル（仕上げの週は "仕上げレッスン"）・`Selectable` の反映 |
| | `Bind_ExamWeekNext_ShowsText` | |
| | `LessonClicked_PicksParam_CallsAction` | `FakeDialogService.ChooseResult = 1` → `ParamKind.Rhythm`、`FakeProducePort` の `ChooseWeekActionAsync` の引数が `(operationNumber, slot, -1, Rhythm)` |
| | `LessonClicked_Cancel_DoesNothing` | `ChooseResult = 3` |
| | `ClassClicked_PushesClassPage` | `FakeScreenNavigator.Calls` に `PushPage:ClassPage` |
| | `SupplyClicked_Declined_DoesNothing` / `SupplyClicked_Confirmed_CallsAction` | |
| | `RestClicked_Succeeded_ShowsChange` | 前後の状態の体力の差が情報ダイアログに出る |
| | `ActionClicked_NotSelectable_DoesNothing` | |
| | `MenuClicked_ShowsMenu` | |
| `ClassPresenterTests.cs` | `Bind_BuildsOptionLabels` | "体力 8: 歌唱 +40 / スキルカード 1 枚"、"コストなし: 表現 +15" |
| | `OptionClicked_Confirmed_PopsThenShowsChangeThenHandlesStep` | 呼び出しの順（`Calls` と `Messages`）: `PopPage` → 情報ダイアログ → `HandleStepAsync` が呼ばれる（`ProduceFlow` の派生で記録） |
| | `OptionClicked_Rejected_ShowsInfoAndStays` | `PopPage` が呼ばれない |
| | `OptionClicked_Disabled_DoesNothing` | |
| `CardOfferPresenterTests.cs` | `Bind_BuildsCardsAndDeckCount` | |
| | `CardClicked_Confirmed_PicksIndex` | `ResolveCardOfferAsync(Pick, i)` |
| | `DeclineClicked_Confirmed_Declines` | `(Decline, 0)` |
| | `Resolve_Succeeded_PopsModalThenHandlesStep` | |
| | `Resolve_Rejected_ShowsInfoAndStays` | |
| `ProduceFlowTests.cs`（追記） | `Enter_WeekSelect_PushesWeekPage` / `Advance_WeekSelect_ReplacesWeekPage` / `Enter_CardOffer_PushesModalOnly` | `FakeScreenNavigator.Calls` |

（`ProduceFlow` の派生・`ProduceActions` の偽: `ProduceActions` は `ProduceActions(IProducePort, ProduceSession, RefreshPlayerUseCase)` なので、テストは `FakeProducePort` と偽の `IPlayerPort` で本物の `ProduceActions` を作る。`ProduceFlow` は 4 本目で公開メソッドが `virtual` なので、`Tests/EditMode/Support/FakeProduceFlow.cs`（`ProduceFlow` の派生。`base(session, null, null, null)`。`EnterAsync`・`AdvanceAsync`・`ExitToHomeAsync`・`HandleStepAsync` を記録して完了済みの `UniTask` を返す）を作る。）

Play モード・手動（Editor、サーバー起動済み。`masterdata` の最短シナリオを編成で選ぶ）:
- 編成 → 開始 → 週の行動選択（週 1/3、レッスンのボタン）。ステータスの表示が編成した育成アイドルの初期値
- 初級のシナリオ: 授業 → 選択肢 3 つ → 選ぶと確認 → 上がった値のダイアログ → スキルカードの獲得モーダル → カードを選ぶ → 週が進む。支給 → 確認 → スキルカードの獲得モーダル。休む（体力が減っているとき）→ 回復量のダイアログ。体力が最大のとき「休む」が無効
- 「≡」→ ホームへ戻る → ホームの「再開」で同じ週の画面に戻る
- 「≡」→ 放棄 → 確認 → ホーム（「再開」が消える）
- レッスンを選んだあとの画面は 6 本目（それまでは「この局面の画面は準備中です: Lesson」）

## 6. 未決事項

なし
