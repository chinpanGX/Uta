---
title: 実装計画: client/ の画面 5/6 週の行動選択・授業・支給・スキルカードの選択肢
status: draft
updated: 2026-11-03
spec: ../wiki/systems/produce.md
---

# 実装計画: client/ の画面 5/6 週の行動選択・授業・支給・スキルカードの選択肢

## 1. 目的と前提

[game-spec.md](../wiki/overview/game-spec.md) の順 7 の 5 本目。プロデュースの「週の選択」の局面（`ProducePhase.WeekSelect`）と「カードの選択肢」の局面（`CardOffer`）の画面を作る: 週の行動選択（レッスン・授業・支給・休む）のページ、授業のページ、支給のページ、スキルカードの獲得（モーダル）。局面の遷移は [produce.md](../wiki/systems/produce.md) 10.1、行動は 3.3〜3.5、RPC は [produce-server.md](../wiki/architecture/server/produce-server.md) 5〜6章、画面の叩き台は [screen-wireframes.md](../wiki/overview/screen-wireframes.md) 「共通の方針」・2・4・5。方針の正は [screen-design.md](../wiki/architecture/screen-design.md) 7章（7.3 スキルカードの表示、7.4 局面が画面を決める）。

- **書き方の正**: Presenter・ページ・Args・遷移・テストの書き方は [1 本目](2026-11-03-client-screens-foundation.md) 3.3（基盤の `IScreenNavigator`・`IPresenter`・`[AssetAddress]`・`IScreenWithArgs<TArgs>`、コンストラクター 2 本、`InitializeAsync`／`DidPopEnter`／`Dispose`、実行中フラグを `finally` で戻す、`playAnimation: false`、`CallLog` を共有する偽）に従う。この計画では繰り返さない
- **前提**: 1〜4 本目が done。使うもの: 1 本目の `PageBase`・`ModalBase`・`UiFactory`・`UiTheme`・`DialogService`・`PageStack`・`IGameMaster`（`GetScenario`・`GetCardText`）・`CallLog`・`FakeScreenNavigator`・`FakeDialogService`、2 本目の `PortResult<T>`・`FakeGameMaster`、3 本目の `DisplayText`（`Rarity`・`Param`）、4 本目の `ProduceSession`・`ProduceStateView` と応答の写し（`ProduceWeekAction`・`ProduceClassOption`・`ProduceDeckCard`）・`IProducePort`・`ProduceActions`・`ProduceStepStatus`・`CardOfferAction`・`ProduceFlow`・`ProduceMenu`・`ProduceBackdropPage`（とその Presenter）、計画 0（[masterdata-card-text](2026-11-03-masterdata-card-text.md)。`Card.Name`・`CardFace.Description`）
- **作らないもの**:
  - レッスン・試験・結果（6 本目）、相談・おでかけ・ドリンク・育成アイテム・デッキの確認（MVP 外）
  - スキルカードの再抽選・除去（初期回数が 0 で MVP では使えない: [produce.md](../wiki/systems/produce.md) 3.5。状態の DTO にも回数が無い。ボタンを作らない）
  - 特別レッスンの印（MVP では特別レッスンを作らない: [produce.md](../wiki/systems/produce.md) 4.1）
  - カードの効果の予測値（[screen-design.md](../wiki/architecture/screen-design.md) 7.3。名前・説明文は出す）
  - カードの選択肢の強化済みの表示: 選択肢は常に未強化として出す（`enhanced: false`）。根拠: 応答の `CardOffers` はカード ID だけの `int[]`（[produce-domain.md](../wiki/architecture/produce-domain.md) 8章）、選んだカードはサーバーが未強化でデッキに足す（`shared/Uta.Shared.Domain/Produce/ProduceEngine.cs` の `OfferPick`: `new CardInstance(..., false)`）、[produce.md](../wiki/systems/produce.md) 3.5 の候補の決め方に強化済みの提示は無い。叩き台 4 の「強化済み(+)のカードは強化版の性能を表示する」は MVP では起きない

### 決めたこと（この計画で。叩き台との差を含む）

1. **授業・支給はページ**（[screen-wireframes.md](../wiki/overview/screen-wireframes.md)「共通の方針」の決定 2026-11-01、[screen-design.md](../wiki/architecture/screen-design.md) 7.4）。`WeekPage` の上に `ClassPage`・`SupplyPage` を積む。押された枠の番号（`SlotIndex`）は Args（`WeekSlotArgs`）で渡す（授業の枠を探し直さない）
2. **スキルカード獲得のモーダルは、授業・支給のページの上に重ねる**（同じ決定の「レッスン・授業・支給の画面の上に重ねる」）。授業・支給のページは操作のあと自分では閉じない。カードの選択肢が終わったあとの `ProduceFlow.AdvanceAsync`（ページの局面へ移る）が、ホームより上をまとめて閉じてから次を積む（3.5）
3. **支給のページは「受け取る」だけ** [Uta案]: MVP の支給は育成pt もドリンクも付かず（[produce.md](../wiki/systems/produce.md) 3.3「MVP の支給」）、出す結果が無い。叩き台 5 の「結果表示 → 4」の代わりに、受け取る前のページにする。「受け取る」ボタンが確認を兼ねる（確認ダイアログを重ねない）
4. **レッスンは「レッスン」のボタン 1 つ → パラメータの選択ダイアログ**（叩き台 2 の歌唱・律動・表現の 3 ボタン、ターン数・目標の表示はしない。`WeekActions` はレッスンを 1 枠で返し、ターン数を持たないため）。選択ダイアログが確認を兼ねる
5. **獲得は「タップ → 確認ダイアログ → 獲得」**。叩き台 4 の「選ぶ → 獲得する」のボタンは作らない（選択の状態を View に持たせない）
6. **授業・休むの結果は、操作の前後の状態の差を情報ダイアログで出す**（screen-design 7.4）。確認文には予測の量を出さない（休むの回復量などのルールをクライアントで複製しないため。叩き台 2 の「確認ダイアログ(効果の要約)」との差）。差を出すのは `Succeeded` のときだけ（`Recovered` では `Previous` が操作の前の状態とは限らない）
7. **再開したときの局面が `CardOffer`** のときは、4 本目の `ProduceFlow.EnterAsync` が `ProduceBackdropPage`（プロデュースの背景のページ）を積んでからモーダルを出す（4 本目の決定。この計画はモーダルの側だけを作る）
8. **モーダルの背景のタップでは閉じない**: 背景は USN の `DefaultModalBackdrop`（`ModalBackdrop._closeModalWhenClicked` は既定の `false`。prefab でも設定していない）。Android の戻るキーは USN が扱わない（`Runtime` に処理が無い）。カードの選択肢を抜ける道は「獲得」「受け取らない」とアプリの終了だけ

## 2. 触るファイル

パスは `client/uta/Assets/` からの相対（wiki はリポジトリのルートから）。

| 操作 | パス | 中身 |
|---|---|---|
| 作成 | `Scripts/Application/Produce/ProduceChange.cs` | 操作の前後の差 |
| 作成 | `Scripts/Application/Produce/CardSummary.cs`・`CardSummaryBuilder.cs` | スキルカードの表示用の値（名前・説明文を含む） |
| 作成 | `Scripts/Application/Produce/WeekProgress.cs` | 次の試験までの週数 |
| 変更 | `Scripts/Presentation/Common/DisplayText.cs` | スキルカード・コスト・差の文字（3.2） |
| 作成 | `Scripts/Presentation/Produce/WeekSlotArgs.cs` | 授業・支給のページに渡す枠の番号 |
| 作成 | `Scripts/Presentation/Produce/IWeekView.cs`・`WeekPage.cs`・`WeekPresenter.cs` | 週の行動選択 |
| 作成 | `Scripts/Presentation/Produce/IClassView.cs`・`ClassPage.cs`・`ClassPresenter.cs` | 授業 |
| 作成 | `Scripts/Presentation/Produce/ISupplyView.cs`・`SupplyPage.cs`・`SupplyPresenter.cs` | 支給 |
| 作成 | `Scripts/Presentation/Produce/ICardOfferView.cs`・`CardOfferModal.cs`・`CardOfferPresenter.cs` | スキルカードの獲得 |
| 変更 | `Scripts/Presentation/Produce/ProduceFlow.cs` | 局面 `WeekSelect`・`CardOffer` の画面を足す（3.5） |
| 変更 | `Scripts/Bootstrap/UtaBootstrap.cs` | `RegisterScreens` に雛形を 4 行足す |
| 作成 | `Tests/EditMode/Fakes/ProduceStates.cs` | テスト用の `ProduceStateView` の組み立て（6 本目も使う） |
| 作成 | `Tests/EditMode/Fakes/ScriptedProducePort.cs` | テスト用の `IProducePort`（`CallLog` に記録） |
| 作成 | `Tests/EditMode/Fakes/FakeProduceFlow.cs` | テスト用の `ProduceFlow` の派生（6 本目も使う） |
| 変更 | `Tests/EditMode/Fakes/FakeGameMaster.cs`（2 本目の置き場のまま） | `GetCardText` が `CardTextList` から引くようにする（2 本目で済んでいれば変えない。タスク 1） |
| 作成 | `Tests/EditMode/*.cs`（5章） | テスト |
| 変更 | `uta_docs/wiki/architecture/screen-design.md`・`uta_docs/wiki/overview/screen-wireframes.md`・`uta_docs/wiki/architecture/unity7-evaluation.md` | 結果の記録（タスク 4） |

## 3. 型とシグネチャ

C# は `client/` の `.editorconfig` の範囲（C# 9 の範囲。コレクション式 `[...]` は使わず `new[] { ... }`）。名前空間: `Scripts/Application/Produce` が `Uta.Client.Application.Produce`、`Scripts/Presentation/Produce` が `Uta.Client.Presentation.Produce`、テストが `Uta.Client.Infrastructure.Tests`。

### 3.1 `Uta.Client.Application.Produce`

```csharp
public readonly struct ProduceChange
{
    public ProduceChange(int singing, int rhythm, int expression, int hp);
    public int Singing { get; } public int Rhythm { get; } public int Expression { get; } public int Hp { get; }
    public bool IsEmpty { get; }                       // 4 つとも 0
    /// <summary>cur - prev（Singing・Rhythm・Expression・Hp）。prev が null なら全部 0。cur が null なら ArgumentNullException。</summary>
    public static ProduceChange Create(ProduceStateView prev, ProduceStateView cur);
}

public sealed class CardSummary
{
    public CardSummary(int cardId, string name, string description, bool enhanced, Rarity rarity, CardType type, IReadOnlyList<CostDef> costs);
    public int CardId { get; }
    public string Name { get; }                        // CardTextInfo.Name
    public string Description { get; }                 // enhanced なら CardTextInfo.DescriptionEnhanced、そうでなければ CardTextInfo.Description
    public bool Enhanced { get; }
    public Rarity Rarity { get; }
    public CardType Type { get; }
    public IReadOnlyList<CostDef> Costs { get; }       // CardDefinition.GetFace(enhanced).Costs
}

public static class CardSummaryBuilder
{
    /// <summary>
    /// definition = catalog.Get(cardId)（実装の CardCatalog は無いカード ID で KeyNotFoundException: shared/Uta.Shared.Domain/Lesson/CardCatalog.cs）、
    /// text = master.GetCardText(cardId)（無ければ KeyNotFoundException）から作る。
    /// Rarity・Type は definition、Costs は definition.GetFace(enhanced).Costs、Name は text.Name、Description は enhanced ? text.DescriptionEnhanced : text.Description。
    /// </summary>
    public static CardSummary Build(ICardCatalog catalog, IGameMaster master, int cardId, bool enhanced);
}

public static class WeekProgress
{
    /// <summary>
    /// scenario.Weeks（Week の昇順）のうち Week >= week で Kind が MidExam か FinalExam の最初の週を探し、(その週の Kind, その週の Week - week) を返す。無ければ (WeekKind.Nothing, 0)。
    /// </summary>
    public static (WeekKind kind, int weeksLeft) NextExam(ScenarioInfo scenario, int week);
}
```

（`CostDef`・`CardType`・`Rarity`・`ICardCatalog` は `Uta.Shared.Domain.Lesson`。`WeekKind` は `Uta.Shared.Domain.Produce`。`IGameMaster`・`ScenarioInfo`・`CardTextInfo` は `Uta.Client.Application.Master`。`ProduceStateView` は 4 本目。）

### 3.2 `DisplayText` の追加（`Presentation/Common/DisplayText.cs`）

```csharp
public static string CardTypeName(CardType type);  // Active → "アクティブ"、Mental → "メンタル"、Trouble → "不調"、それ以外 → "-"
public static string CostKindName(CostKind kind);  // Consume → "消費"、HpDirect → "体力消費"、GoodConditionTurns → "好調"、ExcellentTurns → "絶好調"、Focus → "集中"、Impression → "好印象"、Motivation → "やる気"、それ以外 → "-"
/// <summary>"<Name>" + (Enhanced ? "+" : "") + " [" + Rarity(card.Rarity) + "] " + CardTypeName(card.Type)。例 "テストカード+ [SR] アクティブ"</summary>
public static string Card(CardSummary card);
/// <summary>card.Costs を順に CostKindName(kind) + " " + value にして "、" でつなぐ。0 件なら "コストなし"。例 "消費 4、体力消費 2"</summary>
public static string Costs(CardSummary card);
/// <summary>0 でないものだけを 歌唱・律動・表現・体力 の順に。書式は "<名前> +<値>"（正）／"<名前> -<絶対値>"（負）。例 "歌唱 +40"、"体力 -8"。全部 0 なら空のリスト</summary>
public static IReadOnlyList<string> Change(ProduceChange change);
```

- メソッド名を `CardTypeName`・`CostKindName` にする（型と同じ名前のメソッドにすると、メソッドの中の `CardType.Active` がメソッドに解決されてコンパイルできないため）
- 名前の 歌唱・律動・表現 は `DisplayText.Param(ParamKind.Singing)` などを使う。体力は `"体力"`
- 用語は [glossary.md](../wiki/glossary.md) のコストの種類（消費 / 体力消費）に合わせる

### 3.3 `Uta.Client.Presentation.Produce` の型

```csharp
public sealed class WeekSlotArgs
{
    public WeekSlotArgs(int slotIndex) { SlotIndex = slotIndex; }
    public int SlotIndex { get; }                      // ProduceWeekAction.SlotIndex
}

public sealed class WeekActionViewData
{
    public WeekActionViewData(int slotIndex, string label, bool enabled);
    public int SlotIndex { get; } public string Label { get; } public bool Enabled { get; }
}
public interface IWeekView
{
    void SetHeader(string weekText, string nextExamText);
    void SetStatus(IReadOnlyList<string> lines);
    void SetActions(IReadOnlyList<WeekActionViewData> actions);
    event Action<int> ActionClicked;                   // slotIndex
    event Action MenuClicked;
}
public sealed class WeekPage : PageBase, IWeekView { /* 3.6 */ }

[AssetAddress(nameof(WeekPage))]
public sealed class WeekPresenter : IPresenter
{
    [Inject] public WeekPresenter(WeekPage view, ProduceSession session, IGameMaster master, ProduceActions actions, ProduceFlow flow, ProduceMenu menu, DialogService dialogs, IScreenNavigator navigator);
    public WeekPresenter(IWeekView view, ProduceSession session, IGameMaster master, ProduceActions actions, ProduceFlow flow, ProduceMenu menu, DialogService dialogs, IScreenNavigator navigator);
    public UniTask InitializeAsync();                  // ActionClicked・MenuClicked を購読し Refresh()
    public void DidPopEnter();                         // Refresh()（授業・支給のページから「＜」で戻ったとき）
    public void Dispose();                             // 2 つの購読を解く
    public UniTask ChooseActionAsync(int slotIndex);   // ActionClicked の本体
    public UniTask OpenMenuAsync();                    // MenuClicked の本体
}

public sealed class ClassOptionViewData
{
    public ClassOptionViewData(int index, string label, bool enabled);
    public int Index { get; } public string Label { get; } public bool Enabled { get; }
}
public interface IClassView
{
    void SetHeader(string text);
    void SetOptions(IReadOnlyList<ClassOptionViewData> options);
    event Action<int> OptionClicked;                   // ProduceClassOption.Index
    event Action BackClicked;
}
public sealed class ClassPage : PageBase, IClassView { /* 3.6 */ }

[AssetAddress(nameof(ClassPage))]
public sealed class ClassPresenter : IPresenter, IScreenWithArgs<WeekSlotArgs>
{
    [Inject] public ClassPresenter(ClassPage view, WeekSlotArgs args, ProduceSession session, ProduceActions actions, ProduceFlow flow, DialogService dialogs, IScreenNavigator navigator);
    public ClassPresenter(IClassView view, WeekSlotArgs args, ProduceSession session, ProduceActions actions, ProduceFlow flow, DialogService dialogs, IScreenNavigator navigator);
    public UniTask InitializeAsync();                  // OptionClicked・BackClicked を購読し、表示する
    public void Dispose();
    public UniTask ChooseOptionAsync(int index);
    public UniTask BackAsync();
}

public interface ISupplyView
{
    event Action ReceiveClicked;
    event Action BackClicked;
}
public sealed class SupplyPage : PageBase, ISupplyView { /* 3.6 */ }

[AssetAddress(nameof(SupplyPage))]
public sealed class SupplyPresenter : IPresenter, IScreenWithArgs<WeekSlotArgs>
{
    [Inject] public SupplyPresenter(SupplyPage view, WeekSlotArgs args, ProduceActions actions, ProduceFlow flow, IScreenNavigator navigator);
    public SupplyPresenter(ISupplyView view, WeekSlotArgs args, ProduceActions actions, ProduceFlow flow, IScreenNavigator navigator);
    public UniTask InitializeAsync();                  // 2 つを購読
    public void Dispose();
    public UniTask ReceiveAsync();
    public UniTask BackAsync();
}

public sealed class CardOfferItemViewData
{
    public CardOfferItemViewData(int index, string title, string costs, string description);
    public int Index { get; } public string Title { get; } public string Costs { get; } public string Description { get; }
}
public interface ICardOfferView
{
    void SetCards(IReadOnlyList<CardOfferItemViewData> cards);
    void SetDeckCount(string text);                    // "デッキ <n> 枚"
    event Action<int> CardClicked;                     // CardOffers の添字
    event Action DeclineClicked;
}
public sealed class CardOfferModal : ModalBase, ICardOfferView { /* 3.6 */ }

[AssetAddress(nameof(CardOfferModal))]
public sealed class CardOfferPresenter : IPresenter
{
    [Inject] public CardOfferPresenter(CardOfferModal view, ProduceSession session, ICardCatalog catalog, IGameMaster master, ProduceActions actions, ProduceFlow flow, DialogService dialogs, IScreenNavigator navigator);
    public CardOfferPresenter(ICardOfferView view, ProduceSession session, ICardCatalog catalog, IGameMaster master, ProduceActions actions, ProduceFlow flow, DialogService dialogs, IScreenNavigator navigator);
    public UniTask InitializeAsync();                  // CardClicked・DeclineClicked を購読し、表示する
    public void Dispose();
    public UniTask PickAsync(int index);
    public UniTask DeclineAsync();
}
```

どの Presenter も 1 本目 3.3.2 の決まりどおり: イベントの受け口は同期で `XxxAsync().Forget()` を呼ぶだけ。`XxxAsync` は先頭で実行中フラグ（Presenter に 1 つ。`private bool running;`）を見て、立っていれば何もしない。本体を `try`、`finally` で戻す。**フラグは確認ダイアログの表示・RPC・`HandleStepAsync` の終わりまで立てたまま**にする（確認の表示中に別のボタンを受けない）。例外は捕まえない（ポートは失敗を `PortResult` で返し、共通のダイアログは `ApiCore` が出す。例外はプログラムの誤りで、`Forget` が Unity のログに出す）。

### 3.4 Presenter の動き

文字列の書式（数値はそのまま `ToString()`。3 桁区切りは使わない: 値が 4 桁に届かない）。`state` は `session.Current`。

**`WeekPresenter`**
- `Refresh()`: `state = session.Current`、`scenario = master.GetScenario(state.ScenarioId)`、`thisWeek` = `scenario.Weeks` のうち `Week == state.Week` の最初のもの（無ければ `null`）。
  - `SetHeader($"週 {state.Week} / {scenario.WeekCount}", nextExamText)`。`(kind, weeksLeft) = WeekProgress.NextExam(scenario, state.Week)`。`kind == WeekKind.Nothing` なら `""`、`weeksLeft == 0` なら `"今週は試験"`（試験の週は週の始まりで試験の局面に入るので、`WeekSelect` では出ない。関数の境界として残す）、それ以外は `(kind == WeekKind.MidExam ? "中間試験" : "最終試験") + $"まで {weeksLeft} 週"`
  - `SetStatus(new[] { $"歌唱 {state.Singing} / {scenario.ParamCap}", $"律動 {state.Rhythm} / {scenario.ParamCap}", $"表現 {state.Expression} / {scenario.ParamCap}", $"体力 {state.Hp} / {state.MaxHp}", $"再挑戦 残り {state.ExamRetryLeft}" })`
  - `SetActions`: `state.WeekActions` の順に `new WeekActionViewData(a.SlotIndex, label, a.Selectable)`。`label`: `Lesson` → `thisWeek != null && thisWeek.Kind == WeekKind.Finishing` なら `"仕上げレッスン"`、そうでなければ `"レッスン"`。`Class` → `"授業"`、`Supply` → `"支給"`、`Rest` → `"休む"`、それ以外 → `"-"`
- `ChooseActionAsync(slotIndex)`（実行中フラグの中）: `action` = `session.Current.WeekActions` のうち `SlotIndex == slotIndex` の最初のもの。`null` か `Selectable == false` なら何もしない。
  - `Lesson`: `var i = await dialogs.ChooseAsync("どのレッスンにしますか？", new[] { "歌唱", "律動", "表現", "やめる" });` `i == 3` なら終わる。`param` = `i` が 0 → `ParamKind.Singing`、1 → `Rhythm`、2 → `Expression`。`var status = await actions.ChooseWeekActionAsync(slotIndex, -1, param); await flow.HandleStepAsync(status);`
  - `Class`: `await navigator.PushPageAsync<ClassPresenter, WeekSlotArgs>(new WeekSlotArgs(slotIndex), playAnimation: false);`
  - `Supply`: `await navigator.PushPageAsync<SupplyPresenter, WeekSlotArgs>(new WeekSlotArgs(slotIndex), playAnimation: false);`
  - `Rest`: `await dialogs.ConfirmAsync("休みます。よろしいですか？")` が `Positive` でなければ終わる。`var status = await actions.ChooseWeekActionAsync(slotIndex, -1, ParamKind.Nothing);` `status == ProduceStepStatus.Succeeded` のとき `var change = ProduceChange.Create(session.Previous, session.Current);` が `IsEmpty` でなければ `await dialogs.ShowInfoAsync(string.Join("\n", DisplayText.Change(change)));`。そのあと（どの `status` でも）`await flow.HandleStepAsync(status);`
- `OpenMenuAsync()`（実行中フラグの中）: `await menu.ShowAsync();`

`Rejected`・`Failed`・`Recovered`・`SessionEnded` の扱いは全部 `flow.HandleStepAsync` に任せる（4 本目: `Rejected` は「その操作はできません。」、`Failed` は何もしない、`SessionEnded` はホームへ、`Succeeded`・`Recovered` は次の局面へ）。

**`ClassPresenter`**
- `InitializeAsync`: 購読のあと `state = session.Current`。`SetHeader($"授業　体力 {state.Hp} / {state.MaxHp}")`（間は全角の空白 1 つ）。`SetOptions`: `state.ClassOptions` の順に `new ClassOptionViewData(o.Index, Label(o), o.Selectable)`。`Label(o)` = `(o.HpCost == 0 ? "コストなし" : $"体力 {o.HpCost}") + ": " + DisplayText.Param(o.Param) + $" +{o.ParamGain}" + (o.GivesCard ? " / スキルカード 1 枚" : "")`
- `ChooseOptionAsync(index)`（実行中フラグの中）: `option` = `session.Current.ClassOptions` のうち `Index == index` の最初のもの。`null` か `Selectable == false` なら何もしない。`await dialogs.ConfirmAsync(Label(option) + " を選びます。よろしいですか？")` が `Positive` でなければ終わる。`var status = await actions.ChooseWeekActionAsync(args.SlotIndex, option.Index, ParamKind.Nothing);` `Succeeded` のとき差が空でなければ `await dialogs.ShowInfoAsync(string.Join("\n", DisplayText.Change(change)));`（`Rest` と同じ）。そのあと `await flow.HandleStepAsync(status);`。**このページは自分では閉じない**（次の局面が `CardOffer` ならモーダルがこのページの上に出る。ページの局面へ移るときは `ProduceFlow` が閉じる。`Rejected`・`Failed` ではこのページに残る）
- `BackAsync()`（実行中フラグの中）: `await navigator.PopPageAsync(this, playAnimation: false);`（サーバーは呼ばない。局面は `WeekSelect` のまま）

**`SupplyPresenter`**
- `ReceiveAsync()`（実行中フラグの中）: `var status = await actions.ChooseWeekActionAsync(args.SlotIndex, -1, ParamKind.Nothing); await flow.HandleStepAsync(status);`（差は出さない。MVP の支給は値が変わらない。自分では閉じない）
- `BackAsync()`: `ClassPresenter` と同じ

**`CardOfferPresenter`**
- `InitializeAsync`: 購読のあと `state = session.Current`。`SetCards`: `state.CardOffers` の添字 `i` ごとに `var summary = CardSummaryBuilder.Build(catalog, master, state.CardOffers[i], false);`（未強化: 1 章）→ `new CardOfferItemViewData(i, DisplayText.Card(summary), DisplayText.Costs(summary), summary.Description)`。作った一覧を `items` に持つ。`SetDeckCount($"デッキ {state.Deck.Count} 枚")`
- `PickAsync(index)`（実行中フラグの中）: `index` が `items` の範囲外なら何もしない。`await dialogs.ConfirmAsync(items[index].Title + " を獲得します。よろしいですか？")` が `Positive` なら `await ResolveAsync(CardOfferAction.Pick, index)`
- `DeclineAsync()`（実行中フラグの中）: `await dialogs.ConfirmAsync("スキルカードを受け取りません。よろしいですか？")` が `Positive` なら `await ResolveAsync(CardOfferAction.Decline, 0)`
- `private async UniTask ResolveAsync(CardOfferAction kind, int index)`: `var status = await actions.ResolveCardOfferAsync(kind, index);` `status` が `Rejected` か `Failed` なら `await flow.HandleStepAsync(status);` だけ（モーダルに残る）。それ以外は `await navigator.PopModalAsync(this, playAnimation: false);` のあと `await flow.HandleStepAsync(status);`

### 3.5 `ProduceFlow` に足す局面

4 本目の `ProduceFlow` の、局面から画面を出す `switch`（4 本目の計画で「5・6 本目が `case` を足す」とした場所）に足す。遷移は `playAnimation: false`。

| 局面 | `EnterAsync`（ホームの上で始める） | `AdvanceAsync`（操作のあと） |
|---|---|---|
| `WeekSelect` | `await navigator.PushPageAsync<WeekPresenter>(playAnimation: false);` | 4 本目の「ページの局面」の共通の処理（ホームより上のモーダルとページを全部閉じる）のあと、`Enter` と同じ Push |
| `CardOffer` | 4 本目のとおり `ProduceBackdropPage` を積んでから `await navigator.PushModalAsync<CardOfferPresenter>(playAnimation: false);` | **何も閉じずに** `await navigator.PushModalAsync<CardOfferPresenter>(playAnimation: false);`（いまの一番上のページ（`ClassPage`・`SupplyPage`・6 本目の `LessonPage`）の上に重ねる） |

この計画が前提にする 4 本目の約束（**タスク 3 の初めに 4 本目の `ProduceFlow.cs` を読んで確かめ、違えば止まって報告する**）:
1. `AdvanceAsync` でページの局面へ移るときは、ホームより上のモーダルとページを**全部**閉じる（`pageStack.PopToRootAsync(navigator)` 相当）。`WeekPage` の上に `ClassPage`／`SupplyPage` が残っていても、まとめて閉じるため（1 章の決めたこと 2）
2. `AdvanceAsync` で `CardOffer` へ移るときは、ページを閉じずにモーダルだけを積める
3. `EnterAsync` で `CardOffer` のときは、`ProduceBackdropPage` を積んでからモーダルを積む

### 3.6 画面のレイアウト（`BuildUi`。見た目は仮）

共通: フォントサイズは `UiTheme.FontTitle`・`FontBody`・`FontSmall` だけ。「ヘッダー」は `var header = UiFactory.CreateHorizontal(root, "Header"); UiFactory.SetHeight(header, UiTheme.ButtonHeight);`。横並びの子の幅の比は、子の `LayoutElement`（無ければ `AddComponent<LayoutElement>()`。`CreateButton` は `SetHeight` で付いている）の `flexibleWidth` で決める。ボタンの `onClick` はイベントを発火するだけ。一覧のボタンは `Set…` のたびに `UiFactory.ClearChildren(コンテナ)` してから作り直し、`onClick` のラムダには添字をローカル変数に写してから渡す。

- **`WeekPage`**: `var root = UiFactory.CreateVertical(body, "Root");` の子に上から
  1. "Header": 子 "Week"（`CreateText("", UiTheme.FontTitle, TextAnchor.MiddleLeft)`、`flexibleWidth = 4`）、"Menu"（`CreateButton("≡")`、`flexibleWidth = 1`。`MenuClicked`）
  2. "NextExam"（`CreateText("", UiTheme.FontSmall, TextAnchor.MiddleLeft, UiTheme.TextMuted)`、`SetHeight(60)`）
  3. "Status"（`CreatePanel`）の子 "StatusText"（`CreateText("", UiTheme.FontBody, TextAnchor.UpperLeft)`、`SetHeight(300)`）。`SetStatus` は行を `"\n"` でつなぐ
  4. "Spacer"（`CreateRect`、`SetFlexibleHeight(1)`）
  5. "ActionsTitle"（`CreateText("今週の行動", UiTheme.FontBody, TextAnchor.MiddleLeft)`、`SetHeight(70)`）
  6. "Actions"（`CreateVertical(root, "Actions", UiTheme.Spacing, 0f)`）。`SetActions` は `WeekActions` の順に `CreateButton(actions, "Action" + slotIndex, label)`、`SetInteractable(button, enabled)`、`onClick` → `ActionClicked?.Invoke(slotIndex)`
  - `SetHeader` は "Week" と "NextExam" の `text` を変える
- **`ClassPage`**: root（同上）の子に上から
  1. "Header": 子 "Back"（`CreateButton("＜")`、`flexibleWidth = 1`。`BackClicked`）、"Title"（`CreateText("", UiTheme.FontBody, TextAnchor.MiddleLeft)`、`flexibleWidth = 4`。`SetHeader` の文字）
  2. "Options"（`CreateVertical(root, "Options", UiTheme.Spacing, 0f)`）。`SetOptions` は順に `CreateButton(options, "Option" + index, label)`、`SetInteractable`、`onClick` → `OptionClicked?.Invoke(index)`
  3. "Spacer"（`CreateRect`、`SetFlexibleHeight(1)`）
- **`SupplyPage`**: root の子に上から
  1. "Header": "Back"（`flexibleWidth = 1`）、"Title"（`CreateText("支給", UiTheme.FontBody, TextAnchor.MiddleLeft)`、`flexibleWidth = 4`）
  2. "Message"（`CreateText("スキルカードの選択肢から 1 枚を受け取れます。", UiTheme.FontBody, TextAnchor.MiddleCenter)`、`SetHeight(200)`）
  3. "Receive"（`CreateButton("受け取る")`。`ReceiveClicked`）
  4. "Spacer"（`CreateRect`、`SetFlexibleHeight(1)`）
- **`CardOfferModal`**（`BuildUi(panel)`）: `panel` の子に上から
  1. "Title"（`CreateText("スキルカードを1枚獲得", UiTheme.FontTitle, TextAnchor.MiddleCenter)`、`SetHeight(90)`）
  2. "Cards"（`CreateVertical(panel, "Cards", UiTheme.Spacing, 0f)`）。`SetCards` はカードごとに `CreateButton(cards, "Card" + index, Title + "\n" + Costs + "\n" + Description, 300f)` を作り、子 "Label" の `Text` を `fontSize = UiTheme.FontSmall`、`alignment = TextAnchor.MiddleLeft` にし、その `RectTransform` を `offsetMin = new Vector2(24f, 0f)`、`offsetMax = new Vector2(-24f, 0f)` にする。`onClick` → `CardClicked?.Invoke(index)`
  3. "Deck"（`CreateText("", UiTheme.FontSmall, TextAnchor.MiddleLeft, UiTheme.TextMuted)`、`SetHeight(60)`。`SetDeckCount` の文字）
  4. "Decline"（`CreateButton("受け取らない")`。`DeclineClicked`）

## 4. タスク

各タスクの終わりに `unity recompile --format json`（`client/uta` で実行）でコンパイルエラー 0、そのタスクまでの EditMode のテストが通る状態にする。テストは Editor が開いているときに `unity test --mode EditMode`（1 本目のタスク 1 で使えると確かめた場合。使えなければ Editor の Test Runner の EditMode）で回す。**Editor を開けない環境では、通ったとは書かず、確かめていないと報告する**。

### タスク 1: Application の値と文字

触る: `Application/Produce/ProduceChange.cs`・`CardSummary.cs`・`CardSummaryBuilder.cs`・`WeekProgress.cs`、`Presentation/Common/DisplayText.cs`、`Tests/EditMode/Fakes/FakeGameMaster.cs`（必要なときだけ）、テスト 4 ファイル。

手順:
1. 3.1・3.2 のとおり作る・足す
2. 2 本目の `FakeGameMaster` の `GetCardText` を読む。`public List<CardTextInfo> CardTextList` フィールドから `CardId` で探し、無ければ `KeyNotFoundException` を投げる形になっていなければ、その形に直す（フィールドを足し、`GetCardText` の中身を置き換える。ほかのメンバーは変えない）
3. 5章の `ProduceChangeTests`・`CardSummaryBuilderTests`・`WeekProgressTests`・`DisplayTextCardTests` を作る

境界: `ProduceChange.Create(null, cur)` は全部 0。`WeekProgress.NextExam` は試験の週が無ければ `(Nothing, 0)`。`DisplayText.Costs` はコスト 0 件で `"コストなし"`。

確かめ方: 上の 4 つのテストが通る。

### タスク 2: テスト用の部品

触る: `Tests/EditMode/Fakes/ProduceStates.cs`・`ScriptedProducePort.cs`・`FakeProduceFlow.cs`。

手順: 5.1 のとおり作る。`FakeProduceFlow` のコンストラクターは、4 本目の `ProduceFlow` のコンストラクターに引数を全部 `null` で渡す（`ProduceFlow` のコンストラクターが `null` を検査して投げるなら止まって報告する）。

確かめ方: コンパイルエラー 0（テストはタスク 3 で使う）。

### タスク 3: 画面と `ProduceFlow`

触る: `Presentation/Produce/*`（2章）、`ProduceFlow.cs`、`Bootstrap/UtaBootstrap.cs`、テスト 5 ファイル。

手順:
1. 4 本目の `ProduceFlow.cs` を読み、3.5 の 3 つの約束を確かめる。違えば止まって報告する
2. 3.3・3.4・3.6 のとおり作る。3.5 の `case` を `ProduceFlow` に足す
3. `UtaBootstrap.RegisterScreens` に `uiRoot.RegisterPage<WeekPage>(); uiRoot.RegisterPage<ClassPage>(); uiRoot.RegisterPage<SupplyPage>(); uiRoot.RegisterModal<CardOfferModal>();` を足す（Presenter は登録しない: 1 本目 3.3.2）
4. 5章の `WeekPresenterTests`・`ClassPresenterTests`・`SupplyPresenterTests`・`CardOfferPresenterTests`・`ProduceFlowTests`（追記）を作る
5. Play モードで手動の確認（5.3）

境界: `WeekActions`・`ClassOptions` に無い添字、`Selectable == false` の枠・選択肢は何もしない。`CardOffers` の範囲外の添字は何もしない。

確かめ方: 上のテストが通る。5.3 の手動の確認（サーバーを起動できない・Editor を開けない環境では、確かめていないと報告する）。

### タスク 4: 結果の記録

1. [screen-design.md](../wiki/architecture/screen-design.md):
   - 7.4 の最後の項「授業・支給・休むの結果（上がった値）は…」を「授業・休むの結果（上がった値）は、操作の前後の `ProduceStateView` の差でクライアントが出す（サーバーは差を返さない）。MVP の支給は値が変わらないので結果を出さない。スキルカード獲得のモーダルは授業・支給のページの上に重ね、カードの選択肢が終わったあと、ホームより上をまとめて閉じて次の局面のページを積む」に直す
   - 「実装メモ」に、この計画の実装と設計のズレを書く（なければ「順 7 の 5 本目: なし」）
2. [screen-wireframes.md](../wiki/overview/screen-wireframes.md) 0章の「中断したプロデュースは、ホームの「再開」から 2 または 3 に戻る。」を「中断したプロデュースは、ホームの「再開」から 2・3・4 のどれかに戻る（4 のときは、プロデュースの背景のページの上に 4 を出す）。」に直す（4 本目が同じ変更をしていれば何もしない）
3. [unity7-evaluation.md](../wiki/architecture/unity7-evaluation.md) 3.3 に 1 本目が「この計画では出していない。2 本目で確かめる」と書いたモーダル（`ModalBase`・`DefaultModalBackdrop`）について、ページの上にモーダルを重ねて閉じられたことを、Play モードで確かめた場合だけ 2章の表に 1 行足す（2 本目が足していれば何もしない）
4. `python -I .claude/skills/linting-wiki/lint_wiki.py` を回し、エラーが 0 であること
5. この計画の `status` を `done` にする

## 5. テスト

置き場: `Tests/EditMode/`（名前空間 `Uta.Client.Infrastructure.Tests`）。書き方は 1 本目 3.3.7（偽の View はテストのファイルの中に `private sealed class`。`CallLog` を偽の View・`FakeScreenNavigator`・`FakeDialogService`・`ScriptedProducePort`・`FakeProduceFlow` で共有し、`log.Entries` の並びで確かめる）。

### 5.1 テスト用の部品（タスク 2）

```csharp
public static class ProduceStates
{
    /// <summary>
    /// 4 本目の ProduceStateView のコンストラクターに渡す。deck は deckCount 枚の new ProduceDeckCard(instanceId: 1 から順, cardId: 1, enhanced: false)。
    /// weekActions・classOptions・cardOffers は null なら空の配列。idolId は 1。lesson・examResult は null（4 本目のコンストラクターにほかの引数があれば、その既定は null・0・空の配列）。
    /// </summary>
    public static ProduceStateView Create(ProducePhase phase = ProducePhase.WeekSelect, int operationNumber = 1, int scenarioId = 1, int week = 1,
        int singing = 100, int rhythm = 100, int expression = 100, int hp = 30, int maxHp = 30, int examRetryLeft = 3, int deckCount = 0,
        ProduceWeekAction[] weekActions = null, ProduceClassOption[] classOptions = null, int[] cardOffers = null);
}

public sealed class ScriptedProducePort : IProducePort
{
    public ScriptedProducePort(CallLog log);
    /// <summary>ChooseWeekActionAsync・ResolveCardOfferAsync が順に返す値。空なら InvalidOperationException("no scripted result")。</summary>
    public Queue<PortResult<ProduceStateView>> Results { get; }
    // ChooseWeekActionAsync: 記録 "Port.ChooseWeekAction:" + operationNumber + "," + slotIndex + "," + optionIndex + "," + param（enum は名前。例 "Port.ChooseWeekAction:1,0,-1,Rhythm"）
    // ResolveCardOfferAsync: 記録 "Port.ResolveCardOffer:" + operationNumber + "," + kind + "," + offerIndex（例 "Port.ResolveCardOffer:1,Pick,2"）
    // ほかの 5 メソッド: NotImplementedException
}

public class FakeProduceFlow : ProduceFlow      // 6 本目も使う
{
    public FakeProduceFlow(CallLog log);         // base(4 本目のコンストラクターの引数を全部 null)
    public int EnterCalls { get; private set; }
    public int AdvanceCalls { get; private set; }
    public int HandleStepCalls { get; private set; }
    // EnterAsync: EnterCalls++、記録 "Flow.Enter"。AdvanceAsync: AdvanceCalls++、記録 "Flow.Advance"。
    // ExitToHomeAsync: 記録 "Flow.ExitToHome"。HandleStepAsync(status): HandleStepCalls++、記録 "Flow.HandleStep:" + status。
    // どれも UniTask.CompletedTask を返す（base を呼ばない）
}
```

`ProduceActions` はテストで本物を使う: `new ProduceActions(port, session, new RefreshPlayerUseCase(null, new PlayerCache()))`（`port` は `ScriptedProducePort`。このテストの応答は局面が `Finished` にならず業務エラーも返さないので、`RefreshPlayerUseCase` は呼ばれない）。`ProduceSession` は本物で、最初に `session.Set(ProduceStates.Create(...))` する。`ProduceMenu` は `WeekPresenterTests` の中の `private sealed class FakeProduceMenu : ProduceMenu`（4 本目のコンストラクターに引数を全部 `null`。`ShowAsync` で記録 `"Menu.Show"`）。マスタは 2 本目の `FakeGameMaster`（`ScenarioList` にシナリオ 1: `new ScenarioInfo(1, "テスト", Difficulty.Beginner, 3, 1000, 3, new[] { new ScenarioWeekInfo(1, WeekKind.Action, 25, 50), new ScenarioWeekInfo(2, WeekKind.Finishing, 75, 300), new ScenarioWeekInfo(3, WeekKind.FinalExam, 0, 0) })`）。

### 5.2 テストケース

| ファイル | テスト | 内容 |
|---|---|---|
| `ProduceChangeTests.cs` | `Create_ComputesDifferences` | prev（歌唱 100・体力 30）→ cur（歌唱 140・体力 22）で `Singing == 40`、`Hp == -8`、ほかは 0 |
| | `Create_PrevNull_AllZero` | `IsEmpty == true` |
| | `IsEmpty_AllZero_True` / `IsEmpty_OneNonZero_False` | |
| `CardSummaryBuilderTests.cs` | `Build_Enhanced_UsesEnhancedFaceAndDescription` | 下の作り方の `catalog` と `FakeGameMaster`（`CardTextList` に `new CardTextInfo(1, "テストカード", "通常の説明", "強化の説明")`）。`Build(catalog, master, 1, true)` の `Costs[0].Value == 3`、`Description == "強化の説明"`、`Name == "テストカード"`、`Rarity == Rarity.SR`、`Type == CardType.Active`。`Build(..., false)` は `Costs[0].Value == 4`、`Description == "通常の説明"` |
| | `Build_UnknownCard_Throws` | `Build(catalog, master, 99, false)` が `KeyNotFoundException` |
| `WeekProgressTests.cs` | `NextExam_BeforeExam_ReturnsKindAndWeeks` | 5.1 のシナリオで `NextExam(s, 1) == (FinalExam, 2)` |
| | `NextExam_OnExamWeek_ReturnsZero` | `NextExam(s, 3) == (FinalExam, 0)` |
| | `NextExam_AfterAll_ReturnsNothing` | `NextExam(s, 4) == (Nothing, 0)` |
| `DisplayTextCardTests.cs` | `Card_Enhanced_HasPlus` | `"テストカード+ [SR] アクティブ"` |
| | `Card_NotEnhanced_NoPlus` | `"テストカード [SR] アクティブ"` |
| | `Costs_TwoCosts_JoinedInOrder` | `CostDef(Consume, 4)`・`CostDef(HpDirect, 2)` → `"消費 4、体力消費 2"` |
| | `Costs_Empty_IsNoCost` | `"コストなし"` |
| | `Change_OnlyNonZeroInOrder` | (歌唱 40, 律動 0, 表現 15, 体力 -8) → `"歌唱 +40"`, `"表現 +15"`, `"体力 -8"` |
| `WeekPresenterTests.cs` | `Initialize_ShowsHeaderStatusAndActions` | 週 1（シナリオ 1）、`WeekActions` = (0, Lesson, true)。`log` に `"SetHeader:週 1 / 3,最終試験まで 2 週"`、`"SetStatus:歌唱 100 / 1000,律動 100 / 1000,表現 100 / 1000,体力 30 / 30,再挑戦 残り 3"`、`"SetActions:0/レッスン/True"`（偽の View は `SetActions` を `"SetActions:" + 要素ごとの "SlotIndex/Label/Enabled" を , でつないだもの` で記録） |
| | `Initialize_FinishingWeek_LabelsFinishingLesson` | 週 2 → `"SetActions:0/仕上げレッスン/True"` |
| | `Initialize_RestDisabled_PassesSelectable` | (1, Rest, false) → `"…1/休む/False"` |
| | `ChooseAction_Lesson_ChoosesParamAndHandlesStep` | `ChooseResults` に 1、`Results` に成功（週 1・局面 `Lesson`）。`log` の並びが `"Choose:どのレッスンにしますか？:歌唱/律動/表現/やめる"`, `"Port.ChooseWeekAction:1,0,-1,Rhythm"`, `"Flow.HandleStep:Succeeded"` |
| | `ChooseAction_LessonCancel_DoesNothing` | `ChooseResults` に 3 → `"Port."` で始まる行が無い |
| | `ChooseAction_Class_PushesClassWithSlot` | (2, Class, true) → `"PushPage:ClassPresenter"`、`navigator.Args[0]` が `WeekSlotArgs` で `SlotIndex == 2` |
| | `ChooseAction_Supply_PushesSupplyWithSlot` | (3, Supply, true) → `"PushPage:SupplyPresenter"`、`SlotIndex == 3` |
| | `ChooseAction_RestConfirmed_ShowsChangeThenHandlesStep` | 体力 10 → 応答の体力 31。並びが `"Confirm:休みます。よろしいですか？"`, `"Port.ChooseWeekAction:1,1,-1,Nothing"`, `"Info:体力 +21"`, `"Flow.HandleStep:Succeeded"` |
| | `ChooseAction_RestDeclined_DoesNothing` | `ConfirmResults` に `Negative` → `"Port."` の行が無い |
| | `ChooseAction_NotSelectable_DoesNothing` | `Selectable == false` の枠 → 記録は `Initialize` の分だけ |
| | `ChooseAction_WhileRunning_Ignored` | `FakeDialogService.InfoGate`… ではなく、`ConfirmAsync` を止められないので次で確かめる: Rest の確認で `ConfirmResults` に `Negative` を入れ、`var t1 = ChooseActionAsync(1)` と `var t2 = ChooseActionAsync(1)` を await せず続けて呼ぶ → 偽が同期で終わるので `t1` が終わってから `t2` が走り、`"Confirm:"` は 2 回（同期で終わる偽ではフラグの効果は見えない）。**このテストは作らない**（実行中フラグの確認は 1 本目の `HomePresenterTests.OpenGachaAsync_WhileRunning_Ignored` で済んでいる） |
| | `OpenMenu_CallsMenu` | `"Menu.Show"` |
| | `Dispose_Unsubscribes` | `Dispose()` のあと `RaiseActionClicked(0)` で `"Choose:"` が増えない |
| `ClassPresenterTests.cs` | `Initialize_BuildsOptionLabels` | `ClassOptions` = (0, 8, Singing, 40, true, true)、(1, 0, Expression, 15, false, true) → `"SetHeader:授業　体力 30 / 30"`、`"SetOptions:0/体力 8: 歌唱 +40 / スキルカード 1 枚/True,1/コストなし: 表現 +15/True"` |
| | `ChooseOption_Confirmed_ShowsChangeThenHandlesStepWithoutPop` | `WeekSlotArgs(2)`、選択肢 0、応答（歌唱 140・体力 22・局面 `CardOffer`）。並びが `"Confirm:体力 8: 歌唱 +40 / スキルカード 1 枚 を選びます。よろしいですか？"`, `"Port.ChooseWeekAction:1,2,0,Nothing"`, `"Info:歌唱 +40\n体力 -8"`, `"Flow.HandleStep:Succeeded"`。`"PopPage:"` の行が無い |
| | `ChooseOption_Rejected_HandlesStepAndStays` | 応答が `PortResult.BusinessError("PRODUCE_INVALID_ARGUMENT", "x")` → `"Flow.HandleStep:Rejected"`、`"Info:"`・`"PopPage:"` の行が無い |
| | `ChooseOption_Recovered_NoChangeDialog` | 応答が `BusinessError("PRODUCE_STALE_OPERATION", "x")`、続けて `GetStateAsync` の応答が要るので、このテストは `ScriptedProducePort` の `GetStateAsync` を使わずに済むよう **作らない**（`Recovered` の差を出さない分岐は `Succeeded` 以外を全部同じに扱うコードなので、`Rejected` のテストで足りる） |
| | `ChooseOption_Disabled_DoesNothing` | `Selectable == false` → 記録は `Initialize` の分だけ |
| | `Back_PopsSelf` | `"PopPage:ClassPresenter"` |
| `SupplyPresenterTests.cs` | `Receive_CallsActionAndHandlesStep` | `WeekSlotArgs(3)`、応答の局面 `CardOffer` → `"Port.ChooseWeekAction:1,3,-1,Nothing"`, `"Flow.HandleStep:Succeeded"`、`"PopPage:"` の行が無い |
| | `Back_PopsSelf` | `"PopPage:SupplyPresenter"` |
| `CardOfferPresenterTests.cs` | `Initialize_BuildsCardsAndDeckCount` | `catalog` は `CardSummaryBuilderTests` と同じ作り（カード 1）、`CardOffers` = { 1 }、`deckCount` = 14 → `"SetCards:0/テストカード [SR] アクティブ/消費 4/通常の説明"`、`"SetDeckCount:デッキ 14 枚"` |
| | `Pick_Confirmed_PopsModalThenHandlesStep` | 応答の局面 `WeekSelect`。並びが `"Confirm:テストカード [SR] アクティブ を獲得します。よろしいですか？"`, `"Port.ResolveCardOffer:1,Pick,0"`, `"PopModal:CardOfferPresenter"`, `"Flow.HandleStep:Succeeded"` |
| | `Pick_OutOfRange_DoesNothing` | `PickAsync(5)` → 記録は `Initialize` の分だけ |
| | `Decline_Confirmed_Declines` | `"Confirm:スキルカードを受け取りません。よろしいですか？"`, `"Port.ResolveCardOffer:1,Decline,0"`, `"PopModal:CardOfferPresenter"`, `"Flow.HandleStep:Succeeded"` |
| | `Decline_NotConfirmed_DoesNothing` | `Negative` → `"Port."` の行が無い |
| | `Resolve_Rejected_HandlesStepAndStays` | `BusinessError("PRODUCE_INVALID_ARGUMENT", "x")` → `"Flow.HandleStep:Rejected"`、`"PopModal:"` の行が無い |
| `ProduceFlowTests.cs`（追記） | `Enter_WeekSelect_PushesWeekPage` | 4 本目の `ProduceFlowTests` と同じ作り（`FakeScreenNavigator`・`PageStack(() => 1, () => 0)`）。`session` の局面 `WeekSelect` → `log` の最後が `"PushPage:WeekPresenter"` |
| | `Advance_WeekSelect_ClosesAllAboveHomeThenPushes` | `PageStack(() => 3, () => 0)`（ホーム・週・授業）→ `"PopPage:2"`, `"PushPage:WeekPresenter"` |
| | `Advance_CardOffer_PushesModalOnly` | `PageStack(() => 3, () => 0)` → `"PushModal:CardOfferPresenter"` だけ（`"PopPage:"` の行が無い） |
| | `Enter_CardOffer_PushesBackdropThenModal` | `PageStack(() => 1, () => 0)` → 4 本目の背景のページの Push の行、`"PushModal:CardOfferPresenter"` の順 |

`CardSummaryBuilderTests` の `catalog` の作り方（`shared/Uta.Shared.Domain.Tests/Lesson/CardCatalogTests.cs` の `Create_OneCardWithBothFaces_BuildsDefinition` と同じ行。クライアントのテストからは xUnit のヘルパーを使えないので、テストのファイルに `private static ICardCatalog CreateCatalog()` として書く。`CardCatalogTestRows` のような共有はしない）:

```csharp
var cards = new[] { new CardRow(1, Plan.Free, CardType.Active, Rarity.SR, 1, false, false, false) };
var faces = new[] { new CardFaceRow(1, CardFaceKind.Normal, 10, 0), new CardFaceRow(1, CardFaceKind.Enhanced, 11, 0) };
var costs = new[] { new CardCostRow(1, CardFaceKind.Normal, 1, CostKind.Consume, 4), new CardCostRow(1, CardFaceKind.Enhanced, 1, CostKind.Consume, 3) };
var effects = new[]
{
    new EffectRow(10, 1, EffectOp.GainParam, StatusKind.Nothing, PersistentTrigger.Nothing, 9, 0, 0, 0, 0, 0, 0),
    new EffectRow(11, 1, EffectOp.GainParam, StatusKind.Nothing, PersistentTrigger.Nothing, 14, 0, 0, 0, 0, 0, 0),
};
return CardCatalog.Create(cards, faces, costs, effects, new ConditionRow[0]);
```

`CardOfferPresenterTests` も同じ関数を自分のファイルに private で書く（2 か所の重複は受け入れる）。偽の View の記録の書式: `SetCards` は `"SetCards:" + 要素ごとの "Index/Title/Costs/Description" を , でつないだもの`、`SetOptions` は `"SetOptions:" + "Index/Label/Enabled" を , で`、`SetHeader`（Week）は `"SetHeader:" + weekText + "," + nextExamText`、`SetStatus` は `"SetStatus:" + 行を , で`。

### 5.3 Play モード・手動（Editor、サーバー起動済み。CLAUDE.md「ローカルのサーバー」）

- 編成で「最短シナリオ（仮）」を選んで開始 → 週の行動選択（「週 1 / 3」「最終試験まで 2 週」、「レッスン」のボタン 1 つ）。ステータスが編成した育成アイドルの初期値
- 「初級（仮）」で開始: 週 1 のレッスンは 6 本目の画面（それまでは 4 本目の「この局面の画面は準備中です: Lesson」）。週 2 で「授業」→ 授業のページ（選択肢 3 つ、体力が足りない選択肢は無効）→ 選ぶ → 確認 → 上がった値のダイアログ → 授業のページの上にスキルカードの獲得のモーダル（名前・レア度・種類・コスト・説明文）→ カードを選ぶ → 確認 → 次の週の行動選択（授業のページが残っていない）
- 「支給」→ 支給のページ → 「受け取る」→ 支給のページの上にモーダル → 「受け取らない」→ 確認 → 次の週
- 授業・支給のページの「＜」で週の行動選択に戻る（週は進まない）
- 体力が最大のとき「休む」が無効。体力が減っているとき「休む」→ 確認 → 回復量のダイアログ → 次の週
- カードの選択肢でアプリを終了 → 起動 → ホームの「再開」→ 背景のページの上にモーダル → 獲得 → 次の週の行動選択 → 「≡」→「ホームへ戻る」でホーム（ホームがスタックの底に残っている）
- 「≡」→ 放棄 → 確認 → ホーム（「再開」が消える）
- モーダルの背景をタップしても閉じない

## 6. 未決事項

- （4 本目の最終形を見て確かめる）3.5 の 3 つの約束（`AdvanceAsync` がページの局面でホームより上を全部閉じる、`CardOffer` はモーダルだけを積む、`EnterAsync` の `CardOffer` は背景のページを積む）と、4 本目の型の名前（`ProduceWeekAction`・`ProduceClassOption`・`ProduceDeckCard`・`ProduceBackdropPage` の Presenter の名前・`ProduceFlow`／`ProduceMenu` のコンストラクター）。この計画は 4 本目の改訂前の版と方針メモから書いた。4 本目が決まったら、3.5・5.1・5.2 の `ProduceFlowTests` の期待をそれに合わせる（進行役）
