---
title: 実装計画: client/ の画面 5/6 週の行動選択・授業・支給・スキルカードの選択肢
status: ready
updated: 2026-11-07
spec: ../wiki/systems/produce.md
---

# 実装計画: client/ の画面 5/6 週の行動選択・授業・支給・スキルカードの選択肢

## この計画で決めたこと

ユーザーは、この節と次の「ユーザーの確認が要ること」だけを読めば判断できるように書く。

1. **画面は `Produce` シーンに積む**（[formation](2026-11-03-client-screens-formation.md) 決めたこと 1〜3 のとおり）。底はいつも背景のページ（`ProduceBackdropPage`）。局面の切り替えは `ProduceFlow.ShowCurrentAsync`（背景より上のページ・モーダルを全部 Pop してから、局面の画面を Push）。この計画は `ProduceFlow.PushForPhaseAsync` に `WeekSelect` → `WeekPage`、`CardOffer` → `CardOfferModal` の 2 つの `case` を足す
2. **授業・支給はページ**（[screen-wireframes.md](../wiki/overview/screen-wireframes.md)「共通の方針」の決定 2026-11-01、[screen-design.md](../wiki/architecture/screen-design.md) 7.6）。`WeekPage` の上に `ClassPage`・`SupplyPage` を積む。押された枠の番号（`SlotIndex`）は Args（`WeekSlotArgs`）で渡す（授業の枠を探し直さない）
3. **スキルカード獲得のモーダルは、背景のページの上に出る**（`ShowCurrentAsync` に合わせる）。授業・支給のページは、操作のあとの `flow.HandleStepAsync` → `ShowCurrentAsync` が `WeekPage` ごと閉じる。授業・支給の Presenter は自分では閉じない（二重に Pop しない）。カードの選択肢の Presenter も自分では閉じない（解決のあと `flow.HandleStepAsync` を呼ぶだけ。次の `ShowCurrentAsync` がモーダルを閉じる）。再開で局面が `CardOffer` でも、同じく背景の上にモーダルが出る（旧版の「再開のときだけ背景のページを積む」分岐は無い）。叩き台の決定（授業・支給の画面の上に重ねる）との差は決めたこと 15
4. **View は P3 の骨格に `IXxxView` の実装を足す**（[foundation](2026-11-03-client-screens-foundation.md) 3.3.3）。`WeekPage`（`IWeekView`）・`ClassPage`（`IClassView`）・`SupplyPage`（`ISupplyView`）・`CardOfferModal`（`ICardOfferView`）と、行の View `SkillCardRowView` に表示のメソッド `SetTexts` を足す。フィールド・子の名前は [P3](2026-11-07-client-prefabs-screens-b.md) の 3.1・3.2・4章が正で、この計画は UI を組まない（旧版の `BuildUi`・`UiFactory`・「画面のレイアウト」の節は消した）。Prefab の作り直しは要らない
5. **支給のページは「受け取る」だけ** [Uta案]: MVP の支給は育成pt もドリンクも付かず（[produce.md](../wiki/systems/produce.md) 3.3「MVP の支給」）、出す結果が無い。叩き台 5 の「結果表示 → 4」の代わりに、受け取る前のページにする。「受け取る」ボタンが確認を兼ねる（確認ダイアログを重ねない）
6. **レッスンは「レッスン」のボタン 1 つ → パラメータの選択ダイアログ**（叩き台 2 の歌唱・律動・表現の 3 ボタン、ターン数・目標の表示はしない。`WeekActions` はレッスンを 1 枠で返し、ターン数を持たないため）。選択ダイアログが確認を兼ねる
7. **獲得は「タップ → 確認ダイアログ → 獲得」**。叩き台 4 の「選ぶ → 獲得する」のボタンは作らない（選択の状態を View に持たせない）
8. **授業・休むの結果は、操作の前後の状態の差を情報ダイアログで出す**（screen-design 7.6）。ダイアログは `HandleStepAsync` の前に出すので、授業のページ・週のページの上に出る。確認文には予測の量を出さない（休むの回復量などのルールをクライアントで複製しないため。叩き台 2 の「確認ダイアログ(効果の要約)」との差）。差を出すのは `Succeeded` のときだけ（`Recovered` では `Previous` が操作の前の状態とは限らない）
9. **モーダルの背景のタップでは閉じない**: 背景は USN の `DefaultModalBackdrop`（`ModalBackdrop._closeModalWhenClicked` は既定の `false`）。Android の戻るキーは USN が扱わない（`Runtime` に処理が無い）。カードの選択肢を抜ける道は「獲得」「受け取らない」とアプリの終了だけ
10. **DI は足さない**: Presenter はどこにも登録しない（foundation 3.3.2）。使う依存は、ルート（`ProduceSession`・`ProduceActions`・`IGameMaster`・`ICardCatalog`。formation・foundation が `BootstrapEntry.RegisterUseCases` などに登録済み）と、`Produce` シーンのスコープ（`ProduceFlow`・`ProduceMenu`・`IScreenNavigator`・`PageStack`・`DialogService`。formation の `ProduceLifetimeScope`・foundation の `SceneUiInstaller`）にある
11. **例外の書き方は formation 決めたこと 12 に合わせる**: Presenter の公開 `UniTask` メソッドが `catch (Exception e)` で受け、`Debug.LogException(e)` のあと `dialogs.ShowInfoAsync("エラーが発生しました: " + e.Message)` を出す（旧版の「例外は捕まえない」を改めた）
12. **テスト用の部品は formation のものを使う**: `FakeProducePort`・`FakePlayerPort`・`TestProduce`（formation 3.7）。旧版の `ScriptedProducePort`・`ProduceStates` は作らない。`FakeProduceFlow` だけをこの計画が作る（6 本目も使う）
13. **型の名前は formation に合わせる**: 応答の写しの最上位は `ProduceProgress`（旧版の `ProduceStateView`）。それ以外（`CardSummary`・`DisplayText`・`ProduceChange`・`WeekProgress`）は旧版のまま
14. **カードの選択肢は常に未強化として出す**（`enhanced: false`）。根拠: 応答の `CardOffers` はカード ID だけの `int[]`（[produce-domain.md](../wiki/architecture/produce-domain.md) 8章）、選んだカードはサーバーが未強化でデッキに足す（`shared/Uta.Shared.Domain/Produce/ProduceEngine.cs` の `OfferPick`: `new CardInstance(..., false)`）、[produce.md](../wiki/systems/produce.md) 3.5 の候補の決め方に強化済みの提示は無い。叩き台 4 の「強化済み(+)のカードは強化版の性能を表示する」は MVP では起きない
15. **スキルカード獲得のモーダルを、授業・支給のページの上ではなく、背景のページ（週・パラメータ・体力だけの画面）の上に出してよいか**（決めたこと 3）。[screen-wireframes.md](../wiki/overview/screen-wireframes.md)「共通の方針」の決定（2026-11-01。ユーザーの判断）は「4. スキルカード獲得はモーダル（レッスン・授業・支給の画面の上に重ねる）」。formation の `ShowCurrentAsync`（局面が変わるたびに背景より上を全部閉じる）に合わせると、モーダルが出る前に授業・支給のページは閉じる。授業の上がった値のダイアログは授業のページの上に出してから移るので、結果は見える。背景のページの見出しは、授業・支給のページが閉じたときに最新の値に変わる。代わりの案は「`ProduceFlow` が局面ごとに Pop の範囲を変え、`CardOffer` のときだけ閉じない」だが、formation の 1 つの規則（背景より上を全部閉じる）が局面ごとの分岐になり、6 本目のレッスンの上に重ねる場合も分岐が増えるので採らない。タスク 5 で screen-wireframes の決定の文を「（背景のページの上に重ねる。授業・支給・レッスンのページは局面が移るときに閉じる）」に直す（ユーザーが推奨どおり了承した。2026-11-07）

## ユーザーの確認が要ること

なし（全件、2026-11-07 にユーザーが推奨どおり了承した。「この計画で決めたこと」の末尾に移した）。

## 1. 目的と前提

[game-spec.md](../wiki/overview/game-spec.md) の順 7 の C# の 5 本目。プロデュースの「週の選択」の局面（`ProducePhase.WeekSelect`）と「カードの選択肢」の局面（`CardOffer`）の画面を作る: 週の行動選択（レッスン・授業・支給・休む）のページ、授業のページ、支給のページ、スキルカードの獲得（モーダル）。局面の遷移は [produce.md](../wiki/systems/produce.md) 10.1、行動は 3.3〜3.5、RPC は [produce-server.md](../wiki/architecture/server/produce-server.md) 5〜6章、画面の叩き台は [screen-wireframes.md](../wiki/overview/screen-wireframes.md) 「共通の方針」・2・4・5。方針の正は [screen-design.md](../wiki/architecture/screen-design.md) 7章（Prefab・Addressables・TMP、7.6 プロデュースの画面の流れ）と 8章（シーン構成）。

- **画面を作る共通ルールは [1 本目（foundation）](2026-11-03-client-screens-foundation.md) 3.3 が正**（遷移・Presenter の形・コンストラクター 2 本・`[AssetAddress]`・View の形・実行中フラグ・購読と `Dispose`・破棄の印・テストの書き方・偽の作り・DI の置き場）。この計画では繰り返さない
- **プロデュースの流れは [4 本目（formation）](2026-11-03-client-screens-formation.md) 3.4 が正**（`ProduceEntry`・`ProduceFlow.ShowCurrentAsync`・`HandleStepAsync`・`ExitToHomeAsync`・`PushForPhaseAsync`・`ProduceMenu`）
- **PR #28 の読み替え**: PR #28（未マージ。PR #28 が足す ADR）は `external/` のサブモジュールを全部消す（UnityScreenNavigator・Supplement・VolTech-MagicOnion・MasterMemoryAddons）。サーバー・shared は `packages/` の `.nupkg`、Unity は `manifest.json` の git URL で参照し、`Uta.slnx` から共通基盤が外れる。**PR #28 のあとは、共通基盤のソース（USN の `Runtime/ChinpanGX`・README・`AddressableAssetLoader`、VolTech-MagicOnion のコードなど）は `client/uta/Library/PackageCache/<パッケージ名>@<ハッシュ>/`（USN は `com.harumak.unityscreennavigator`。Unity でパッケージを解決した環境にだけある）か GitHub のリポジトリを読む。マージ前は `external/<名前>`**。この計画に書いた `external/` のパスは、そう読み替える（`external/` は直さない、の決まりは変わらない）
- **View のフィールドと Prefab の子の対応は [P3](2026-11-07-client-prefabs-screens-b.md) の 3.1・3.2・3.8〜3.11・4章が正**（`WeekPage`・`ClassPage`・`SupplyPage`・`CardOfferModal`・`SkillCardRowView`）
- 計画の並び: 計画 0 → P1 → P2 → foundation → gacha → owned → P3 → formation → **この計画** → lesson
- **前提**（実装に入る前に、次がブランチに入っていることを確かめる。無ければ止まって報告する）:
  - foundation が done: `PageBase`・`ModalBase`・`DialogService`・`PageStack`・`UiObjects`（`ClearChildren`・`AddRow`）・部品の View のメソッド（`ButtonView.Button`・`SetLabel`・`SetInteractable`、`HeaderView.BackButton`・`SetTitle`、`ListRowBase.Button`・`SetInteractable`）・`IGameMaster`（`GetScenario`・`GetCardText`）・`CallLog`・`FakeScreenNavigator`・`FakeDialogService`・`FakeSceneNavigator`
  - gacha が done: `PortResult<T>`・`ServerReasons`・`FakeGameMaster`。owned が done: `DisplayText`（`RarityName`・`ParamName`）
  - P3 が done: `Scripts/Presentation/Produce/WeekPage.cs`・`ClassPage.cs`・`SupplyPage.cs`・`CardOfferModal.cs`・`SkillCardRowView.cs`（P3 3.1 のフィールドだけの骨格）と、`Assets/Prefabs/Screens/Produce/WeekPage.prefab`・`ClassPage.prefab`・`SupplyPage.prefab`・`CardOfferModal.prefab`・`Assets/Prefabs/Parts/SkillCardRow.prefab`（Addressables の `Screens` に、アドレス `WeekPage`・`ClassPage`・`SupplyPage`・`CardOfferModal`）
  - formation が done: `ProduceProgress` と応答の写し（`ProduceWeekAction`・`ProduceClassOption`・`ProduceDeckCard`）・`IProducePort`・`ProduceSession`（`Current`・`Previous`）・`ProduceActions`・`ProduceStepStatus`・`CardOfferAction`・`ProduceFlow`（`ShowCurrentAsync`・`HandleStepAsync`・`ExitToHomeAsync` が `virtual`、`PushForPhaseAsync` が `protected virtual`）・`ProduceMenu`（`ShowAsync` が `virtual`）・`ProduceEntry`・`ProduceBackdropPage`／`ProduceBackdropPresenter`、`ProduceLifetimeScope` の登録（`ProduceFlow`・`ProduceMenu`・`ProduceEntry`）、`BootstrapEntry.RegisterUseCases` の `ProduceSession`・`ProduceActions`、テストの `FakeProducePort`・`FakePlayerPort`・`TestProduce`・`ProduceFlowTests`・`ProduceEntryTests`
  - 計画 0（[masterdata-card-text](2026-11-03-masterdata-card-text.md)）が done: `Card.Name`・`CardFace.Description`（`IGameMaster.GetCardText`）
  - Unity 7（`7000.0.0a12`）の Editor が、この worktree の `client/uta` を開いていること（`unity status --until-ready`）。**Editor を開けない環境では、コンパイル・テスト・Play のどれも確かめられない**。そのときは「確認できていない」と書いてユーザーに実行を頼む（推測で「通った」と書かない）
- **作らないもの**:
  - レッスン・試験・結果（6 本目）、相談・おでかけ・ドリンク・育成アイテム・デッキの確認（MVP 外）
  - スキルカードの再抽選・除去（初期回数が 0 で MVP では使えない: [produce.md](../wiki/systems/produce.md) 3.5。状態の DTO にも回数が無い。ボタンを作らない。P3 の Prefab にも無い）
  - 特別レッスンの印（MVP では特別レッスンを作らない: [produce.md](../wiki/systems/produce.md) 4.1）
  - カードの効果の予測値（名前・説明文は出す）
  - カードの選択肢の強化済みの表示（決めたこと 14）
  - Prefab と見た目（P3。文字の大きさ・色・固定の文言「今週の行動」「授業」「支給」「スキルカードの選択肢から 1 枚を受け取れます。」「受け取る」「スキルカードを1枚獲得」「受け取らない」は Prefab のまま。C# は変えない）

## 2. 触るファイル

パスは `client/uta/Assets/` からの相対（wiki はリポジトリのルートから）。新しい `.cs` には Unity が作った `.meta` をコミットする。

| 操作 | パス | 中身 |
|---|---|---|
| 作成 | `Scripts/Application/Produce/ProduceChange.cs` | 操作の前後の差（3.1） |
| 作成 | `Scripts/Application/Produce/CardSummary.cs`・`CardSummaryBuilder.cs` | スキルカードの表示用の値（名前・説明文を含む。3.1） |
| 作成 | `Scripts/Application/Produce/WeekProgress.cs` | 次の試験までの週数（3.1） |
| 変更 | `Scripts/Presentation/Common/DisplayText.cs` | スキルカード・コスト・差の文字（3.2） |
| 作成 | `Scripts/Presentation/Produce/WeekSlotArgs.cs` | 授業・支給のページに渡す枠の番号（3.3） |
| 作成 | `Scripts/Presentation/Produce/IWeekView.cs`・`WeekActionViewData.cs`・`WeekPresenter.cs` | 週の行動選択の IF・Presenter（3.3） |
| 作成 | `Scripts/Presentation/Produce/IClassView.cs`・`ClassOptionViewData.cs`・`ClassPresenter.cs` | 授業（3.3） |
| 作成 | `Scripts/Presentation/Produce/ISupplyView.cs`・`SupplyPresenter.cs` | 支給（3.3） |
| 作成 | `Scripts/Presentation/Produce/ICardOfferView.cs`・`CardOfferItemViewData.cs`・`CardOfferPresenter.cs` | スキルカードの獲得（3.3） |
| 変更 | `Scripts/Presentation/Produce/WeekPage.cs`・`ClassPage.cs`・`SupplyPage.cs`・`CardOfferModal.cs` | P3 の骨格に `IXxxView` の実装を足す（3.5） |
| 変更 | `Scripts/Presentation/Produce/SkillCardRowView.cs` | P3 の骨格に `SetTexts` を足す（3.5。6 本目も使う） |
| 変更 | `Scripts/Presentation/Produce/ProduceFlow.cs` | `PushForPhaseAsync` に `WeekSelect`・`CardOffer` の `case`（3.6） |
| 作成 | `Tests/EditMode/Fakes/FakeProduceFlow.cs` | テスト用の `ProduceFlow` の派生（5.1。6 本目も使う） |
| 作成 | `Tests/EditMode/ProduceChangeTests.cs`・`CardSummaryBuilderTests.cs`・`WeekProgressTests.cs`・`DisplayTextCardTests.cs`・`WeekPresenterTests.cs`・`ClassPresenterTests.cs`・`SupplyPresenterTests.cs`・`CardOfferPresenterTests.cs` | テスト（5.2） |
| 変更 | `Tests/EditMode/ProduceFlowTests.cs`・`ProduceEntryTests.cs`（formation が作ったもの） | `WeekSelect` が「準備中」でなくなるので、期待を直し、テストを足す（5.2・タスク 4b・4c） |
| 変更 | `uta_docs/wiki/architecture/screen-design.md`・`uta_docs/wiki/overview/screen-wireframes.md`・`uta_docs/wiki/architecture/unity7-evaluation.md` | 結果の記録（タスク 5） |

触らないもの: Prefab・Addressables・シーン・Editor の生成スクリプト（P3）、`ProduceLifetimeScope.cs`・`BootstrapEntry.cs`（決めたこと 10）、`ProduceEntry.cs`・`ProduceMenu.cs`（formation）。

## 3. 型とシグネチャ

C# は `client/` の `.editorconfig` の範囲（C# 9 の範囲。コレクション式 `[...]` は使わず `new[] { ... }`）。名前空間: `Scripts/Application/Produce` が `Uta.Client.Application.Produce`、`Scripts/Presentation/Produce` が `Uta.Client.Presentation.Produce`、テストが `Uta.Client.Infrastructure.Tests`。`using UnityScreenNavigator;` と `using Uta.Client.Application.Navigation;` の両方を書くファイルは、foundation 3.3.10 の別名を足す（この計画の Presenter は `ISceneNavigator` を使わないので、普通は要らない）。

### 3.1 `Uta.Client.Application.Produce`

```csharp
public readonly struct ProduceChange
{
    public ProduceChange(int singing, int rhythm, int expression, int hp);
    public int Singing { get; } public int Rhythm { get; } public int Expression { get; } public int Hp { get; }
    public bool IsEmpty { get; }                       // 4 つとも 0
    /// <summary>cur - prev（Singing・Rhythm・Expression・Hp）。prev が null なら全部 0。cur が null なら ArgumentNullException。</summary>
    public static ProduceChange Create(ProduceProgress prev, ProduceProgress cur);
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

（`CostDef`・`CardType`・`Rarity`・`ICardCatalog` は `Uta.Shared.Domain.Lesson`。`WeekKind` は `Uta.Shared.Domain.Produce`。`IGameMaster`・`ScenarioInfo`・`CardTextInfo` は `Uta.Client.Application.Master`。`ProduceProgress` は formation。）

### 3.2 `DisplayText` の追加（`Presentation/Common/DisplayText.cs`）

```csharp
public static string CardTypeName(CardType type);  // Active → "アクティブ"、Mental → "メンタル"、Trouble → "不調"、それ以外 → "-"
public static string CostKindName(CostKind kind);  // Consume → "消費"、HpDirect → "体力消費"、GoodConditionTurns → "好調"、ExcellentTurns → "絶好調"、Focus → "集中"、Impression → "好印象"、Motivation → "やる気"、それ以外 → "-"
/// <summary>"<Name>" + (Enhanced ? "+" : "") + " [" + RarityName(card.Rarity) + "] " + CardTypeName(card.Type)。例 "テストカード+ [SR] アクティブ"</summary>
public static string Card(CardSummary card);
/// <summary>card.Costs を順に CostKindName(kind) + " " + value にして "、" でつなぐ。0 件なら "コストなし"。例 "消費 4、体力消費 2"</summary>
public static string Costs(CardSummary card);
/// <summary>0 でないものだけを 歌唱・律動・表現・体力 の順に。書式は "<名前> +<値>"（正）／"<名前> -<絶対値>"（負）。例 "歌唱 +40"、"体力 -8"。全部 0 なら空のリスト</summary>
public static IReadOnlyList<string> Change(ProduceChange change);
```

- `DisplayText.cs` の `using` は、owned の `Uta.Shared.Domain.Lesson`（`CardType`・`CostKind`）・`Uta.Shared.Domain.Produce` に、無ければ `using System.Collections.Generic;`（`IReadOnlyList`）と `using Uta.Client.Application.Produce;`（`CardSummary`・`ProduceChange`）を足す。ほかは足さない
- メソッド名を `CardTypeName`・`CostKindName` にする（型と同じ名前のメソッドにすると、メソッドの中の `CardType.Active` がメソッドに解決されてコンパイルできないため）
- 名前の 歌唱・律動・表現 は `DisplayText.ParamName(ParamKind.Singing)` などを使う。体力は `"体力"`
- 用語は [glossary.md](../wiki/glossary.md) のコストの種類（消費 / 体力消費）に合わせる

### 3.3 `Uta.Client.Presentation.Produce` の IF・Args・Presenter

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

[AssetAddress(nameof(WeekPage))]
public sealed class WeekPresenter : IPresenter
{
    [Inject] public WeekPresenter(WeekPage view, ProduceSession session, IGameMaster master, ProduceActions actions, ProduceFlow flow, ProduceMenu menu, DialogService dialogs, IScreenNavigator navigator);   // : this((IWeekView)view, ...)
    public WeekPresenter(IWeekView view, ProduceSession session, IGameMaster master, ProduceActions actions, ProduceFlow flow, ProduceMenu menu, DialogService dialogs, IScreenNavigator navigator);
    public UniTask InitializeAsync();                  // ActionClicked・MenuClicked を購読し Refresh()。UniTask.CompletedTask
    public void DidPopEnter();                         // Refresh()（授業・支給のページから「＜」で戻ったとき）
    public void Dispose();                             // disposed = true、2 つの購読を解く
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

[AssetAddress(nameof(SupplyPage))]
public sealed class SupplyPresenter : IPresenter, IScreenWithArgs<WeekSlotArgs>
{
    [Inject] public SupplyPresenter(SupplyPage view, WeekSlotArgs args, ProduceActions actions, ProduceFlow flow, DialogService dialogs, IScreenNavigator navigator);
    public SupplyPresenter(ISupplyView view, WeekSlotArgs args, ProduceActions actions, ProduceFlow flow, DialogService dialogs, IScreenNavigator navigator);
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

[AssetAddress(nameof(CardOfferModal))]
public sealed class CardOfferPresenter : IPresenter
{
    [Inject] public CardOfferPresenter(CardOfferModal view, ProduceSession session, ICardCatalog catalog, IGameMaster master, ProduceActions actions, ProduceFlow flow, DialogService dialogs);
    public CardOfferPresenter(ICardOfferView view, ProduceSession session, ICardCatalog catalog, IGameMaster master, ProduceActions actions, ProduceFlow flow, DialogService dialogs);
    public UniTask InitializeAsync();                  // CardClicked・DeclineClicked を購読し、表示する
    public void Dispose();
    public UniTask PickAsync(int index);
    public UniTask DeclineAsync();
}
```

- `SupplyPresenter` は旧版から `DialogService` を足した（例外のダイアログのため: 決めたこと 11）。`CardOfferPresenter` は旧版から `IScreenNavigator` を外した（自分で閉じないため: 決めたこと 3）
- `[Inject]` の付いたコンストラクターは `: this((IXxxView)view, ...)` で他方に渡す（foundation 3.3.2 の 1）

**どの Presenter も foundation 3.3.2 の決まりどおり**: イベントの受け口は同期で `XxxAsync().Forget()` を呼ぶだけ。`XxxAsync` は先頭で実行中フラグ（Presenter に 1 つ。`private bool running;`）を見て、立っていれば何もしない。本体を `try`、`catch (Exception e)`、`finally` で書く:
- `try`: 3.4 の本体。**フラグは確認ダイアログの表示・RPC・`HandleStepAsync` の終わりまで立てたまま**にする（確認の表示中に別のボタンを受けない）。`await` のあと、View・`navigator`・`flow`・`actions` に触る前に `if (disposed) return;`（foundation 3.3.2 の 6）
- `catch (Exception e)`: `Debug.LogException(e); await dialogs.ShowInfoAsync("エラーが発生しました: " + e.Message);`（決めたこと 11。ポートは通信の失敗を `PortResult` で返すので、ここに来るのはプログラムの誤りだけ）
- `finally`: `running = false;`
- **`flow.HandleStepAsync`・`ShowCurrentAsync`・`ExitToHomeAsync` を呼んだ後は、View・`navigator` に触らない**（その中で自分のページ・モーダルが閉じられ、`Dispose` が呼ばれる: formation 3.4）

### 3.4 Presenter の動き

文字列の書式（数値はそのまま `ToString()`。3 桁区切りは使わない: 値が 4 桁に届かない）。`state` は `session.Current`。

**`WeekPresenter`**
- `Refresh()`（private）: `state = session.Current`。`state` が `null` なら何もしない。`scenario = master.GetScenario(state.ScenarioId)`、`thisWeek` = `scenario.Weeks` のうち `Week == state.Week` の最初のもの（無ければ `null`）。
  - `SetHeader($"週 {state.Week} / {scenario.WeekCount}", nextExamText)`。`(kind, weeksLeft) = WeekProgress.NextExam(scenario, state.Week)`。`kind == WeekKind.Nothing` なら `""`、`weeksLeft == 0` なら `"今週は試験"`（試験の週は週の始まりで試験の局面に入るので、`WeekSelect` では出ない。関数の境界として残す）、それ以外は `(kind == WeekKind.MidExam ? "中間試験" : "最終試験") + $"まで {weeksLeft} 週"`
  - `SetStatus(new[] { $"歌唱 {state.Singing} / {scenario.ParamCap}", $"律動 {state.Rhythm} / {scenario.ParamCap}", $"表現 {state.Expression} / {scenario.ParamCap}", $"体力 {state.Hp} / {state.MaxHp}", $"再挑戦 残り {state.ExamRetryLeft}" })`
  - `SetActions`: `state.WeekActions` の順に `new WeekActionViewData(a.SlotIndex, label, a.Selectable)`。`label`: `Lesson` → `thisWeek != null && thisWeek.Kind == WeekKind.Finishing` なら `"仕上げレッスン"`、そうでなければ `"レッスン"`。`Class` → `"授業"`、`Supply` → `"支給"`、`Rest` → `"休む"`、それ以外 → `"-"`
- `ChooseActionAsync(slotIndex)`（実行中フラグの中）: `action` = `session.Current.WeekActions` のうち `SlotIndex == slotIndex` の最初のもの。`null` か `Selectable == false` なら何もしない。
  - `Lesson`: `var i = await dialogs.ChooseAsync("どのレッスンにしますか？", new[] { "歌唱", "律動", "表現", "やめる" });` `i` が 0〜2 でなければ（3・−1）終わる。`param` = `i` が 0 → `ParamKind.Singing`、1 → `Rhythm`、2 → `Expression`。`var status = await actions.ChooseWeekActionAsync(slotIndex, -1, param); await flow.HandleStepAsync(status);`
  - `Class`: `await navigator.PushPageAsync<ClassPresenter, WeekSlotArgs>(new WeekSlotArgs(slotIndex), playAnimation: false);`
  - `Supply`: `await navigator.PushPageAsync<SupplyPresenter, WeekSlotArgs>(new WeekSlotArgs(slotIndex), playAnimation: false);`
  - `Rest`: `await dialogs.ConfirmAsync("休みます。よろしいですか？")` が `Positive` でなければ終わる。`var status = await actions.ChooseWeekActionAsync(slotIndex, -1, ParamKind.Nothing);` `status == ProduceStepStatus.Succeeded` のとき `var change = ProduceChange.Create(session.Previous, session.Current);` が `IsEmpty` でなければ `await dialogs.ShowInfoAsync(string.Join("\n", DisplayText.Change(change)));`。そのあと（どの `status` でも）`await flow.HandleStepAsync(status);`
- `OpenMenuAsync()`（実行中フラグの中）: `await menu.ShowAsync();`

`Rejected`・`Failed`・`Recovered`・`SessionEnded` の扱いは全部 `flow.HandleStepAsync` に任せる（formation 3.4: `Succeeded`・`Recovered` は `ShowCurrentAsync`、`SessionEnded` は「プロデュースは終了しています。」のあと `ExitToHomeAsync`、`Rejected` は「その操作はできません。」、`Failed` は何もしない）。

**`ClassPresenter`**
- `InitializeAsync`: 購読のあと `state = session.Current`。`SetHeader($"授業　体力 {state.Hp} / {state.MaxHp}")`（間は全角の空白 1 つ）。`SetOptions`: `state.ClassOptions` の順に `new ClassOptionViewData(o.Index, Label(o), o.Selectable)`。`Label(o)`（private static）= `(o.HpCost == 0 ? "コストなし" : $"体力 {o.HpCost}") + ": " + DisplayText.ParamName(o.Param) + $" +{o.ParamGain}" + (o.GivesCard ? " / スキルカード 1 枚" : "")`
- `ChooseOptionAsync(index)`（実行中フラグの中）: `option` = `session.Current.ClassOptions` のうち `Index == index` の最初のもの。`null` か `Selectable == false` なら何もしない。`await dialogs.ConfirmAsync(Label(option) + " を選びます。よろしいですか？")` が `Positive` でなければ終わる。`var status = await actions.ChooseWeekActionAsync(args.SlotIndex, option.Index, ParamKind.Nothing);` `Succeeded` のとき差が空でなければ `await dialogs.ShowInfoAsync(string.Join("\n", DisplayText.Change(change)));`（`Rest` と同じ）。そのあと `await flow.HandleStepAsync(status);`。**このページは自分では閉じない**（`Succeeded`・`Recovered` では `ShowCurrentAsync` が `WeekPage` ごと閉じる。`SessionEnded` ではシーンごと破棄される。`Rejected`・`Failed` ではこのページに残る）
- `BackAsync()`（実行中フラグの中）: `await navigator.PopPageAsync(this, playAnimation: false);`（サーバーは呼ばない。局面は `WeekSelect` のまま。`WeekPresenter.DidPopEnter` が表示をやり直す）

**`SupplyPresenter`**
- `ReceiveAsync()`（実行中フラグの中）: `var status = await actions.ChooseWeekActionAsync(args.SlotIndex, -1, ParamKind.Nothing); await flow.HandleStepAsync(status);`（差は出さない。MVP の支給は値が変わらない。自分では閉じない）
- `BackAsync()`: `ClassPresenter` と同じ

**`CardOfferPresenter`**
- `InitializeAsync`: 購読のあと `state = session.Current`。`SetCards`: `state.CardOffers` の添字 `i` ごとに `var summary = CardSummaryBuilder.Build(catalog, master, state.CardOffers[i], false);`（未強化: 決めたこと 14）→ `new CardOfferItemViewData(i, DisplayText.Card(summary), DisplayText.Costs(summary), summary.Description)`。作った一覧を `items` に持つ。`SetDeckCount($"デッキ {state.Deck.Count} 枚")`
- `PickAsync(index)`（実行中フラグの中）: `index` が `items` の範囲外なら何もしない。`await dialogs.ConfirmAsync(items[index].Title + " を獲得します。よろしいですか？")` が `Positive` なら `await ResolveAsync(CardOfferAction.Pick, index)`
- `DeclineAsync()`（実行中フラグの中）: `await dialogs.ConfirmAsync("スキルカードを受け取りません。よろしいですか？")` が `Positive` なら `await ResolveAsync(CardOfferAction.Decline, 0)`
- `private async UniTask ResolveAsync(CardOfferAction kind, int index)`: `var status = await actions.ResolveCardOfferAsync(kind, index);` `if (disposed) return;` `await flow.HandleStepAsync(status);`。**モーダルは自分で閉じない**（`Succeeded`・`Recovered` では `ShowCurrentAsync` の `PopToRootAsync` がモーダルを閉じる。`Rejected`・`Failed` ではモーダルに残る。`SessionEnded` ではシーンごと破棄される）

### 3.5 View の実装（P3 の骨格に足す）

フィールドは P3 3.1 のまま（宣言を変えない）。対応は P3 3.2。形は foundation 3.3.3（ボタンの `onClick` は `Awake` で購読してイベントを起こす。解除は書かない。行は `UiObjects.ClearChildren` → `UiObjects.AddRow`）。引数が `null` の `Set…` は `ArgumentNullException`。

```csharp
// WeekPage.cs（P3 の骨格: header・menuButton・nextExamText・statusText・actionList・actionButtonPrefab）に足す
public sealed class WeekPage : PageBase, IWeekView
{
    public event Action<int> ActionClicked;
    public event Action MenuClicked;

    private void Awake()
    {
        menuButton.Button.onClick.AddListener(() => MenuClicked?.Invoke());
        // header の BackButton は Prefab で非アクティブ（P3 決めたこと 4）。購読しない
    }

    public void SetHeader(string weekText, string nextExamText);   // header.SetTitle(weekText); this.nextExamText.text = nextExamText;
    public void SetStatus(IReadOnlyList<string> lines);            // statusText.text = string.Join("\n", lines);
    /// <summary>
    /// UiObjects.ClearChildren(actionList); 各 a について:
    /// var button = UiObjects.AddRow(actionButtonPrefab, actionList, "Action" + a.SlotIndex);
    /// button.SetLabel(a.Label); button.SetInteractable(a.Enabled);
    /// var slot = a.SlotIndex; button.Button.onClick.AddListener(() => ActionClicked?.Invoke(slot));
    /// </summary>
    public void SetActions(IReadOnlyList<WeekActionViewData> actions);
}

// ClassPage.cs（P3 の骨格: header・optionList・optionButtonPrefab）に足す
public sealed class ClassPage : PageBase, IClassView
{
    public event Action<int> OptionClicked;
    public event Action BackClicked;

    private void Awake()
    {
        header.BackButton.Button.onClick.AddListener(() => BackClicked?.Invoke());
    }

    public void SetHeader(string text);   // header.SetTitle(text)
    /// <summary>
    /// UiObjects.ClearChildren(optionList); 各 o について:
    /// var button = UiObjects.AddRow(optionButtonPrefab, optionList, "Option" + o.Index);
    /// button.SetLabel(o.Label); button.SetInteractable(o.Enabled);
    /// var index = o.Index; button.Button.onClick.AddListener(() => OptionClicked?.Invoke(index));
    /// </summary>
    public void SetOptions(IReadOnlyList<ClassOptionViewData> options);
}

// SupplyPage.cs（P3 の骨格: header・receiveButton）に足す
public sealed class SupplyPage : PageBase, ISupplyView
{
    public event Action ReceiveClicked;
    public event Action BackClicked;

    private void Awake()
    {
        receiveButton.Button.onClick.AddListener(() => ReceiveClicked?.Invoke());
        header.BackButton.Button.onClick.AddListener(() => BackClicked?.Invoke());
    }
}

// CardOfferModal.cs（P3 の骨格: cardList・cardRowPrefab・deckCountText・declineButton）に足す
public sealed class CardOfferModal : ModalBase, ICardOfferView
{
    public event Action<int> CardClicked;
    public event Action DeclineClicked;

    private void Awake()
    {
        declineButton.Button.onClick.AddListener(() => DeclineClicked?.Invoke());
    }

    /// <summary>
    /// UiObjects.ClearChildren(cardList); 各 c について:
    /// var row = UiObjects.AddRow(cardRowPrefab, cardList, "Card" + c.Index);
    /// row.SetTexts(c.Title, c.Costs, c.Description); row.SetInteractable(true);
    /// var index = c.Index; row.Button.onClick.AddListener(() => CardClicked?.Invoke(index));
    /// </summary>
    public void SetCards(IReadOnlyList<CardOfferItemViewData> cards);
    public void SetDeckCount(string text);   // deckCountText.text = text
}

// SkillCardRowView.cs（P3 の骨格: titleText・costsText・descriptionText。button・background は ListRowBase）に足す
public sealed class SkillCardRowView : ListRowBase
{
    /// <summary>3 つの TMP_Text.text に入れる。null は空文字にする。6 本目（手札）も使う。</summary>
    public void SetTexts(string title, string costs, string description);
}
```

- `WeekPage.SetHeader` は引数と同じ名前のフィールド（`nextExamText`）があるので、フィールドを `this.nextExamText` と書く
- 文字の大きさ・色・固定の文言は Prefab（P3 3.8〜3.11）のまま。`ClassPage` の見出しの初期値「授業」は `SetHeader` が上書きする。`SupplyPage` の見出し「支給」は Prefab のまま

### 3.6 `ProduceFlow` に足す局面

formation 3.4 の `ProduceFlow.PushForPhaseAsync` の `switch (phase)` に、次の 2 つの `case` を足す（formation 3.4 の「5・6 本目が足す `case` の形」のとおり）。`ShowCurrentAsync`・`HandleStepAsync`・`ExitToHomeAsync`・`default` は変えない。

```csharp
case ProducePhase.WeekSelect:
    await navigator.PushPageAsync<WeekPresenter>(playAnimation: false);
    return;
case ProducePhase.CardOffer:
    await navigator.PushModalAsync<CardOfferPresenter>(playAnimation: false);
    return;
```

- `ShowCurrentAsync` が先に背景より上を全部閉じるので、`WeekSelect` の `WeekPage` も `CardOffer` のモーダルも、背景のページの真上に出る。入口が `ProduceEntry`（シーンに入ったとき・再開）でも、操作の後（`HandleStepAsync`）でも同じ
- 流れの例（授業でカードが出る週）: `[背景, 週]` →「授業」→ `[背景, 週, 授業]` → 選ぶ → 差のダイアログ → `HandleStepAsync(Succeeded)` → `ShowCurrentAsync`: `PopPage(popCount: 2)` で `[背景]`（背景の `DidPopEnter` が見出しを最新にする）→ `PushModal<CardOfferPresenter>` → 獲得 → `HandleStepAsync(Succeeded)` → `ShowCurrentAsync`: `PopModal(popCount: 1)` → `PushPage<WeekPresenter>` で `[背景, 次の週]`

## 4. タスク

各タスクの終わりに `unity recompile --format json`（`client/uta` で実行）でコンパイルエラー 0、そのタスクまでの EditMode のテストが通る状態にする。EditMode のテストは `unity test --mode EditMode`（Editor が要る。回し方は P1 タスク 5 の「テストの回し方」）。**`unity test` が動かなければ、Editor の Test Runner（EditMode）を手動で回してもらい、確認できていないと報告する。止まらない。CLAUDE.md に足すのは動いたときだけ**（P1 の全計画共通の規則）。**Editor を開けない環境では、通ったとは書かず、確かめていないと報告する**。Play のあとでコミットする前に `unity command eval 'Uta.Client.Editor.Prefabs.FontAssets.ClearDynamicData(); return "ok";'`（P1 の `FontAssets`）を実行する（foundation 3.3.7）。

### タスク 1: Application の値と文字

触る: `Application/Produce/ProduceChange.cs`・`CardSummary.cs`・`CardSummaryBuilder.cs`・`WeekProgress.cs`、`Presentation/Common/DisplayText.cs`、`ProduceChangeTests.cs`・`CardSummaryBuilderTests.cs`・`WeekProgressTests.cs`・`DisplayTextCardTests.cs`。

手順:
1. 3.1・3.2 のとおり作る・足す
2. gacha の `FakeGameMaster` は変えない（gacha タスク 1 の 2: `public Dictionary<int, CardTextInfo> CardTexts`（空で初期化）を持ち、`GetCardText(id)` は `CardTexts` に無ければ `KeyNotFoundException`）。テストではカードの文字を `master.CardTexts[1] = new CardTextInfo(1, "テストカード", "通常の説明", "強化の説明");` のように入れる。`FakeGameMaster` にこの形のフィールドが無ければ止まって報告する
3. 5.2 の 4 つのテストのファイルを作る

境界: `ProduceChange.Create(null, cur)` は全部 0。`WeekProgress.NextExam` は試験の週が無ければ `(Nothing, 0)`。`DisplayText.Costs` はコスト 0 件で `"コストなし"`。

確かめ方: 上の 4 つのテストが通る。

### タスク 2: View の実装

触る: `Presentation/Produce/IWeekView.cs`・`WeekActionViewData.cs`・`IClassView.cs`・`ClassOptionViewData.cs`・`ISupplyView.cs`・`ICardOfferView.cs`・`CardOfferItemViewData.cs`・`WeekSlotArgs.cs`、`WeekPage.cs`・`ClassPage.cs`・`SupplyPage.cs`・`CardOfferModal.cs`・`SkillCardRowView.cs`。

手順:
1. P3 の骨格（`WeekPage.cs`・`ClassPage.cs`・`SupplyPage.cs`・`CardOfferModal.cs`・`SkillCardRowView.cs`）のフィールドが P3 3.1 と同じ名前・型であることを確かめる。違えば止まって報告する
2. 3.3 の IF・ViewData・Args と、3.5 の View の実装を書く。骨格のフィールド宣言は変えず、メソッド・イベント・インターフェースだけを足す
3. `unity recompile --format json` のエラーが 0。P1 の `PrefabContractTests` と P3 の `ProducePrefabTests` が引き続き通る

確かめ方: 上の 3。View の EditMode のテストは書かない（foundation 3.3.7）。表示はタスク 5 の Play で見る。

### タスク 3: テスト用の部品

触る: `Tests/EditMode/Fakes/FakeProduceFlow.cs`。

手順:
1. formation の `FakeProducePort`・`FakePlayerPort`・`TestProduce` が `Tests/EditMode/Fakes/` にあることを確かめる（無ければ止まって報告する）
2. 5.1 のとおり `FakeProduceFlow` を作る。コンストラクターは formation の `ProduceFlow(ProduceSession, IScreenNavigator, PageStack, DialogService, ISceneNavigator)` に引数を全部 `null` で渡す（`ProduceFlow` のコンストラクターが `null` を検査して投げるなら止まって報告する）

確かめ方: コンパイルエラー 0（テストはタスク 4a〜4c で使う）。

### タスク 4 の分け方（依存の向き）

Presenter は型で次の Presenter を参照するので、**参照される側を先に作る**: `WeekPresenter` は `PushPageAsync<ClassPresenter, …>`・`PushPageAsync<SupplyPresenter, …>` を書き、`ProduceFlow` は `PushPageAsync<WeekPresenter>`・`PushModalAsync<CardOfferPresenter>` を書く。依存の向きは `ProduceFlow` → `WeekPresenter` → `ClassPresenter`・`SupplyPresenter`、`ProduceFlow` → `CardOfferPresenter`（`ClassPresenter`・`SupplyPresenter`・`CardOfferPresenter` は `ProduceFlow` を引数で受けるだけで、ほかの Presenter を参照しない）。なので 4a → 4b → 4c の順にし、**各タスクの終わりに `unity recompile --format json` のエラーが 0、`unity test --mode EditMode` で既存（formation まで）とそのタスクまでのテストが全部通る**状態にする。`ProduceFlow` の `case` は、Push する Presenter ができたタスクで足す（`WeekSelect` は 4b、`CardOffer` は 4c）。

### タスク 4a: `ClassPresenter`・`SupplyPresenter`

触る: `Presentation/Produce/ClassPresenter.cs`・`SupplyPresenter.cs`、`Tests/EditMode/ClassPresenterTests.cs`・`SupplyPresenterTests.cs`。

手順:
1. formation の `ProduceFlow.cs` を読み、次を確かめる。違えば止まって報告する: (a) `ShowCurrentAsync` が `pageStack.PopToRootAsync(navigator)` のあと `PushForPhaseAsync(session.Current.Phase)` を呼ぶ、(b) `HandleStepAsync` の `Succeeded`・`Recovered` が `ShowCurrentAsync` を呼ぶ、(c) `PushForPhaseAsync` が `switch (phase)` で、`default` が「準備中です」のあと `ExitToHomeAsync`、(d) `ShowCurrentAsync`・`HandleStepAsync`・`ExitToHomeAsync` が `virtual`（タスク 3 の `FakeProduceFlow` が上書きできる）
2. 3.3・3.4 のとおり `ClassPresenter`・`SupplyPresenter` を作る（タスク 2 の View の IF・`WeekSlotArgs`、タスク 3 の `FakeProduceFlow` を使う。`ProduceFlow` は変えない）
3. 5.2 の `ClassPresenterTests`・`SupplyPresenterTests` を作る

境界: `ClassOptions` に無い添字、`Selectable == false` の選択肢は何もしない。

確かめ方: `ClassPresenterTests`・`SupplyPresenterTests` と、formation までの EditMode のテストが全部通る（`ProduceFlow` を変えていないので、formation のテストは変わらない）。

### タスク 4b: `WeekPresenter` と `ProduceFlow` の `WeekSelect`

前提: タスク 4a が済んでいる（`WeekPresenter` が `ClassPresenter`・`SupplyPresenter` を型で参照する）。

触る: `Presentation/Produce/WeekPresenter.cs`・`ProduceFlow.cs`、`Tests/EditMode/WeekPresenterTests.cs`・`ProduceFlowTests.cs`・`ProduceEntryTests.cs`。

手順:
1. 3.3・3.4 のとおり `WeekPresenter` を作る
2. 3.6 の `case ProducePhase.WeekSelect:` だけを `ProduceFlow.PushForPhaseAsync` に足す（`CardOffer` の `case` は 4c）
3. formation の既存のテストのうち、`WeekSelect` を「画面の無い局面」として使っているものを直す（`WeekSelect` に画面ができたため）:
   - `ProduceFlowTests.ShowCurrent_UnsupportedPhase_PopsToBackdropThenShowsInfoAndGoesHome`・`ShowCurrent_ModalOpen_PopsModalFirst`: `session` の局面を `ProducePhase.Nothing` にし、期待の文字を `"Info:この局面の画面は準備中です: Nothing"` にする（`Nothing` は 6 本目のあとも画面が無いので、期待が変わらない）
   - `ProduceFlowTests.HandleStep_SucceededAndRecovered_ShowCurrent`: このテストの `session` の局面は `ProducePhase.WeekSelect` にする（formation の作りで別の局面なら `WeekSelect` に直す）。`Succeeded`・`Recovered` のどちらも期待を `"PopPage:1"`, `"PushPage:WeekPresenter"` にする（`"Info:"`・`"GoToHome"` の行が無い）
   - `ProduceEntryTests.Start_Active_PushesBackdropThenShowsCurrent`: 期待を `"PushPage:ProduceBackdropPresenter"`, `"PushPage:WeekPresenter"` にする（`PageStack(() => 1, () => 0)` なので Pop の行は無い）
   - ほかのテスト（`ProduceMenuTests`・`HomePresenterTests` など）は `PushForPhaseAsync` を通らないので変えない。直す前に `ProduceFlowTests.cs`・`ProduceEntryTests.cs` を読み、`"準備中です: WeekSelect"` を期待しているテストがほかにもあれば、同じ考え（局面を `Nothing` にする、または `WeekPresenter` の Push を期待する）で直し、報告に書く
4. 5.2 の `WeekPresenterTests` と、`ProduceFlowTests` の追記のうち `WeekSelect` の 2 本（`ShowCurrent_WeekSelect_PopsAboveBackdropThenPushesWeekPage`・`ShowCurrent_WeekSelectAfterModal_PopsModalThenPushesWeekPage`）を作る

境界: `WeekActions` に無い添字、`Selectable == false` の枠は何もしない。

確かめ方: `WeekPresenterTests`・`ProduceFlowTests`・`ProduceEntryTests`（直したもの）と、4a までのテストが全部通る。

### タスク 4c: `CardOfferPresenter` と `ProduceFlow` の `CardOffer`

前提: タスク 4b が済んでいる（`ProduceFlow.cs` に `WeekSelect` の `case` がある）。

触る: `Presentation/Produce/CardOfferPresenter.cs`・`ProduceFlow.cs`、`Tests/EditMode/CardOfferPresenterTests.cs`・`ProduceFlowTests.cs`。

手順:
1. 3.3・3.4 のとおり `CardOfferPresenter` を作る
2. 3.6 の `case ProducePhase.CardOffer:` を `ProduceFlow.PushForPhaseAsync` に足す
3. `ProduceFlowTests.cs`・`ProduceEntryTests.cs` を読み、`CardOffer` を「画面の無い局面」として期待しているテスト（`"準備中です: CardOffer"`）があれば、4b の 3 と同じ考えで直し、報告に書く（formation 5章の表には無い）
4. 5.2 の `CardOfferPresenterTests` と、`ProduceFlowTests` の追記のうち `CardOffer` の 2 本（`ShowCurrent_CardOffer_PopsPagesThenPushesModal`・`ShowCurrent_CardOfferOnBackdrop_PushesModalOnly`）を作る

境界: `CardOffers` の範囲外の添字は何もしない。

確かめ方: `CardOfferPresenterTests`・`ProduceFlowTests` と、4b までのテスト・formation の EditMode のテストが全部通る。

### タスク 5: Play の確認と結果の記録

1. **Editor の Play モード**（サーバーを起動した状態。CLAUDE.md「ローカルのサーバー」）: `unity command editor.playmode start` のあと、5.3 を手で確かめる。終わったら `unity command console get --tail 50 --level error --format json` のエラーが 0 → `unity command editor.playmode stop`。サーバーを起動できない・Editor を開けない環境では、確かめていないと報告する
2. [screen-design.md](../wiki/architecture/screen-design.md)（frontmatter の `updated` を実施日に）:
   - 7.6 の「授業・休むの結果は、操作の前後の状態の差で出す。…」の項を「授業・休むの結果（上がった値）は、操作の前後の `ProduceProgress` の差でクライアントが出し、次の局面へ移る前にダイアログで見せる（サーバーは差を返さない）。MVP の支給は値が変わらないので結果を出さない。ターン終了時の効果の 1 件ずつの表示は MVP から外す（応答に効果の記録が無い）」に直す
   - 決めたこと 15 は了承済み（2026-11-07）なので、7.6 の最初の項の「`CardOffer` → スキルカード獲得（モーダル）」を「`CardOffer` → スキルカード獲得（モーダル。背景のページの上に出す。授業・支給のページは局面が移るときに閉じる）」に直す（formation のタスク 6 が足した 7.6 の最後の項と食い違わないこと）
   - 「実装メモ」に、この計画の実装と設計のズレを書く（なければ「順 7 の 5 本目: なし」）
3. [screen-wireframes.md](../wiki/overview/screen-wireframes.md):
   - 決めたこと 15 は了承済み（2026-11-07）なので、「共通の方針」の「4. スキルカード獲得はモーダル(レッスン・授業・支給の画面の上に重ねる)」を「4. スキルカード獲得はモーダル(プロデュースの背景のページの上に重ねる。レッスン・授業・支給のページは局面が移るときに閉じる。2026-11-07 にユーザーの判断で改めた)」に直す
   - 0章の「中断したプロデュースは、ホームの「再開」から 2 または 3 に戻る。」を「中断したプロデュースは、ホームの「再開」から 2・3・4 のどれかに戻る（4 のときは、プロデュースの背景のページの上に 4 を出す）。」に直す（formation が同じ変更をしていれば何もしない）
4. [unity7-evaluation.md](../wiki/architecture/unity7-evaluation.md): モーダル（`ModalBase`・`DefaultModalBackdrop`）をページの上に出して閉じられたことを Play で確かめた場合だけ、2章の表に 1 行足す（P2 の計画・gacha・formation が足していれば何もしない）
5. `python -I .claude/skills/linting-wiki/lint_wiki.py` を回し、エラーが 0 であること
6. この計画の `status` を `done` にする

## 5. テスト

置き場: `Tests/EditMode/`（名前空間 `Uta.Client.Infrastructure.Tests`）。書き方は foundation 3.3.7（偽の View はテストのファイルの中に `private sealed class`。`CallLog` を偽の View・`FakeScreenNavigator`・`FakeDialogService`・`FakeSceneNavigator`・`FakeProducePort`・`FakeProduceFlow` で共有し、`log.Entries` の並びで確かめる。非同期は `[UnityTest]` + `UniTask.ToCoroutine`）。

### 5.1 テスト用の部品（タスク 3）

```csharp
public class FakeProduceFlow : ProduceFlow      // 6 本目も使う
{
    public FakeProduceFlow(CallLog log);         // base(null, null, null, null, null)
    public int ShowCurrentCalls { get; private set; }
    public int ExitToHomeCalls { get; private set; }
    public int HandleStepCalls { get; private set; }
    // ShowCurrentAsync: ShowCurrentCalls++、記録 "Flow.ShowCurrent"。
    // ExitToHomeAsync: ExitToHomeCalls++、記録 "Flow.ExitToHome"。
    // HandleStepAsync(status): HandleStepCalls++、記録 "Flow.HandleStep:" + status。
    // どれも UniTask.CompletedTask を返す（base を呼ばない）。PushForPhaseAsync は override しない（ShowCurrentAsync が呼ばないので届かない）
}
```

Presenter のテストの組み立て（各テストのファイルに `private static` の作り方として書く）:
- `var log = new CallLog();`、`var port = new FakeProducePort(log);`、`var session = new ProduceSession();` に最初に `session.Set(TestProduce.Progress(..., operationNumber: 1, ...))`（`TestProduce.Progress` は `scenarioId = 1`・`idolId = 1`・`examRetryLeft = 1`）
- `ProduceActions` は本物: `new ProduceActions(port, session, new RefreshPlayerUseCase(new FakePlayerPort(log), new PlayerCache()))`（このテストの応答は局面が `Finished` にならず、`SessionEnded` にもならないので、取り直しは呼ばれない）
- RPC の応答は `port.StateResults.Enqueue(PortResult<ProduceProgress>.Success(TestProduce.Progress(...)))`（gacha 3 章の `PortResult<T>` の静的メソッド）。業務エラーは `PortResult<ProduceProgress>.BusinessError(ServerReasons.ProduceInvalidArgument, "x")` など。5.2 の `BusinessError(…)` はこの略
- デッキ `n` 枚の状態は `deck: Enumerable.Range(1, n).Select(i => new ProduceDeckCard(i, 1, false)).ToList()`
- `ProduceMenu` は `WeekPresenterTests` の中の `private sealed class FakeProduceMenu : ProduceMenu`（`base(null, null, null)`。`ShowAsync` で記録 `"Menu.Show"`、`UniTask.CompletedTask`）
- マスタは gacha の `FakeGameMaster`（`ScenarioList` にシナリオ 1: `new ScenarioInfo(1, "テスト", Difficulty.Beginner, 3, 1000, 3, new[] { new ScenarioWeekInfo(1, WeekKind.Action, 25, 50), new ScenarioWeekInfo(2, WeekKind.Finishing, 75, 300), new ScenarioWeekInfo(3, WeekKind.FinalExam, 0, 0) })`）

### 5.2 テストケース

**例外のテストの書き方**（formation 5章の `FormationPresenterTests.Start_Exception_ShowsError` と同じ）: Presenter の `catch` は `Debug.LogException(e)` を呼ぶ。Unity の Test Runner は、予期しない `LogType.Exception` のログが出たテストを失敗にするので、**操作を `await` する前に `LogAssert.Expect(LogType.Exception, new Regex("FakeProducePort: no state result"));` を呼ぶ**（`using System.Text.RegularExpressions; using UnityEngine; using UnityEngine.TestTools;`。`Debug.LogException` のログの文は `"InvalidOperationException: FakeProducePort: no state result"` なので、部分一致の `Regex` で受ける）。`LogAssert.ignoreFailingMessages = true` は使わない（ほかの想定外のエラーまで隠すため）。例外は、`StateResults` を空にしておき、`ProduceActions` → `FakeProducePort` が投げる `InvalidOperationException("FakeProducePort: no state result")` で起こす（`ProduceActions` は例外を受けない: formation 決めたこと 12）。期待は「最後の記録が `"Info:エラーが発生しました: FakeProducePort: no state result"`」と「`"Flow.HandleStep:"` の行が無い」。

**記録の並びの比べ方**: 表の「並びが …」は、`InitializeAsync` の後から（操作で足された記録だけを）比べる。`InitializeAsync` のあとの `log` の件数を覚えておき、それより後の行を期待の並びと比べる（記録の全体ではない）。「記録は `InitializeAsync` の分だけ」は、その件数から増えていないこと。

`FakeProducePort` の記録は formation 3.7 の書式（`"ChooseWeekAction:" + op + "," + slot + "," + option + "," + param`、`"ResolveCardOffer:" + op + "," + kind + "," + offerIndex`、`"GetState"`）。「RPC の行が無い」は `"ChooseWeekAction:"`・`"ResolveCardOffer:"` で始まる行が無いこと。

| ファイル | テスト | 内容 |
|---|---|---|
| `ProduceChangeTests.cs` | `Create_ComputesDifferences` | prev（歌唱 100・体力 30）→ cur（歌唱 140・体力 22）で `Singing == 40`、`Hp == -8`、ほかは 0 |
| | `Create_PrevNull_AllZero` | `IsEmpty == true` |
| | `IsEmpty_AllZero_True` / `IsEmpty_OneNonZero_False` | |
| `CardSummaryBuilderTests.cs` | `Build_Enhanced_UsesEnhancedFaceAndDescription` | 下の作り方の `catalog` と `FakeGameMaster`（`master.CardTexts[1] = new CardTextInfo(1, "テストカード", "通常の説明", "強化の説明");`）。`Build(catalog, master, 1, true)` の `Costs[0].Value == 3`、`Description == "強化の説明"`、`Name == "テストカード"`、`Rarity == Rarity.SR`、`Type == CardType.Active`。`Build(..., false)` は `Costs[0].Value == 4`、`Description == "通常の説明"` |
| | `Build_UnknownCard_Throws` | `Build(catalog, master, 99, false)` が `KeyNotFoundException` |
| `WeekProgressTests.cs` | `NextExam_BeforeExam_ReturnsKindAndWeeks` | 5.1 のシナリオで `NextExam(s, 1) == (FinalExam, 2)` |
| | `NextExam_OnExamWeek_ReturnsZero` | `NextExam(s, 3) == (FinalExam, 0)` |
| | `NextExam_AfterAll_ReturnsNothing` | `NextExam(s, 4) == (Nothing, 0)` |
| `DisplayTextCardTests.cs` | `Card_Enhanced_HasPlus` | `"テストカード+ [SR] アクティブ"` |
| | `Card_NotEnhanced_NoPlus` | `"テストカード [SR] アクティブ"` |
| | `Costs_TwoCosts_JoinedInOrder` | `CostDef(Consume, 4)`・`CostDef(HpDirect, 2)` → `"消費 4、体力消費 2"` |
| | `Costs_Empty_IsNoCost` | `"コストなし"` |
| | `Change_OnlyNonZeroInOrder` | (歌唱 40, 律動 0, 表現 15, 体力 -8) → `"歌唱 +40"`, `"表現 +15"`, `"体力 -8"` |
| `WeekPresenterTests.cs` | `Initialize_ShowsHeaderStatusAndActions` | 週 1（シナリオ 1）、`WeekActions` = (0, Lesson, true)。`log` に `"SetHeader:週 1 / 3,最終試験まで 2 週"`、`"SetStatus:歌唱 100 / 1000,律動 100 / 1000,表現 100 / 1000,体力 30 / 30,再挑戦 残り 1"`、`"SetActions:0/レッスン/True"` |
| | `Initialize_FinishingWeek_LabelsFinishingLesson` | 週 2 → `"SetActions:0/仕上げレッスン/True"` |
| | `Initialize_RestDisabled_PassesSelectable` | (1, Rest, false) → `"…1/休む/False"` |
| | `ChooseAction_Lesson_ChoosesParamAndHandlesStep` | `ChooseResults` に 1、`StateResults` に成功（週 1・局面 `Lesson`）。並びが `"Choose:どのレッスンにしますか？:歌唱/律動/表現/やめる"`, `"ChooseWeekAction:1,0,-1,Rhythm"`, `"Flow.HandleStep:Succeeded"` |
| | `ChooseAction_LessonCancel_DoesNothing` | `ChooseResults` に 3 → RPC の行が無い、`"Flow.HandleStep:"` の行が無い |
| | `ChooseAction_Class_PushesClassWithSlot` | (2, Class, true) → `"PushPage:ClassPresenter"`、`navigator.Args[0]` が `WeekSlotArgs` で `SlotIndex == 2` |
| | `ChooseAction_Supply_PushesSupplyWithSlot` | (3, Supply, true) → `"PushPage:SupplyPresenter"`、`SlotIndex == 3` |
| | `ChooseAction_RestConfirmed_ShowsChangeThenHandlesStep` | 体力 10 → 応答の体力 25（最大体力 30 以下）。並びが `"Confirm:休みます。よろしいですか？"`, `"ChooseWeekAction:1,1,-1,Nothing"`, `"Info:体力 +15"`, `"Flow.HandleStep:Succeeded"` |
| | `ChooseAction_RestDeclined_DoesNothing` | `ConfirmResults` に `Negative` → RPC の行が無い |
| | `ChooseAction_NotSelectable_DoesNothing` | `Selectable == false` の枠 → 記録は `InitializeAsync` の分だけ |
| | `ChooseAction_Exception_ShowsError` | (0, Lesson, true)、`ChooseResults` に 0、`StateResults` を空にする、`LogAssert.Expect`（上の書き方）→ 最後が `"Info:エラーが発生しました: FakeProducePort: no state result"`、`"Flow.HandleStep:"` の行が無い |
| | `OpenMenu_CallsMenu` | `"Menu.Show"` |
| | `DidPopEnter_RefreshesFromLatest` | `InitializeAsync` のあと `session.Set(週 2 の Progress)` → `DidPopEnter()` → 最後の `"SetHeader:"` が `"SetHeader:週 2 / 3,最終試験まで 1 週"` |
| | `Dispose_Unsubscribes` | `Dispose()` のあと `RaiseActionClicked(0)` で `"Choose:"` が増えない |
| `ClassPresenterTests.cs` | `Initialize_BuildsOptionLabels` | `ClassOptions` = (0, 8, Singing, 40, true, true)、(1, 0, Expression, 15, false, true) → `"SetHeader:授業　体力 30 / 30"`、`"SetOptions:0/体力 8: 歌唱 +40 / スキルカード 1 枚/True,1/コストなし: 表現 +15/True"` |
| | `ChooseOption_Confirmed_ShowsChangeThenHandlesStepWithoutPop` | `WeekSlotArgs(2)`、選択肢 0、応答（歌唱 140・体力 22・局面 `CardOffer`）。並びが `"Confirm:体力 8: 歌唱 +40 / スキルカード 1 枚 を選びます。よろしいですか？"`, `"ChooseWeekAction:1,2,0,Nothing"`, `"Info:歌唱 +40\n体力 -8"`, `"Flow.HandleStep:Succeeded"`。`"PopPage:"` の行が無い |
| | `ChooseOption_Rejected_HandlesStepAndStays` | 応答が `BusinessError(ServerReasons.ProduceInvalidArgument, "x")` → `"Flow.HandleStep:Rejected"`、`"Info:"`・`"PopPage:"` の行が無い |
| | `ChooseOption_Recovered_NoChangeDialog` | `StateResults` に `BusinessError(ServerReasons.ProduceStaleOperation, "x")`、続けて成功（局面 `WeekSelect`、歌唱 140）→ 並びが `"ChooseWeekAction:1,2,0,Nothing"`, `"GetState"`, `"Flow.HandleStep:Recovered"`。`"Info:"` の行が無い |
| | `ChooseOption_Disabled_DoesNothing` | `Selectable == false` → 記録は `InitializeAsync` の分だけ |
| | `ChooseOption_Exception_ShowsError` | `WeekSlotArgs(2)`、選択肢 0、`ConfirmResults` は既定（`Positive`）、`StateResults` を空にする、`LogAssert.Expect`（上の書き方）→ 並びが `"Confirm:…"`, `"ChooseWeekAction:1,2,0,Nothing"`, `"Info:エラーが発生しました: FakeProducePort: no state result"`。`"Flow.HandleStep:"`・`"PopPage:"` の行が無い |
| | `Back_PopsSelf` | `"PopPage:ClassPresenter"` |
| `SupplyPresenterTests.cs` | `Receive_CallsActionAndHandlesStep` | `WeekSlotArgs(3)`、応答の局面 `CardOffer` → `"ChooseWeekAction:1,3,-1,Nothing"`, `"Flow.HandleStep:Succeeded"`、`"PopPage:"` の行が無い |
| | `Receive_Exception_ShowsError` | `WeekSlotArgs(3)`、`StateResults` を空にする、`LogAssert.Expect`（上の書き方）→ 並びが `"ChooseWeekAction:1,3,-1,Nothing"`, `"Info:エラーが発生しました: FakeProducePort: no state result"`。`"Flow.HandleStep:"` の行が無い |
| | `Back_PopsSelf` | `"PopPage:SupplyPresenter"` |
| `CardOfferPresenterTests.cs` | `Initialize_BuildsCardsAndDeckCount` | `catalog` は `CardSummaryBuilderTests` と同じ作り（カード 1）、`master.CardTexts[1]` も同じ値、`CardOffers` = { 1 }、デッキ 14 枚 → `"SetCards:0/テストカード [SR] アクティブ/消費 4/通常の説明"`、`"SetDeckCount:デッキ 14 枚"` |
| | `Pick_Confirmed_ResolvesThenHandlesStepWithoutPop` | 応答の局面 `WeekSelect`。並びが `"Confirm:テストカード [SR] アクティブ を獲得します。よろしいですか？"`, `"ResolveCardOffer:1,Pick,0"`, `"Flow.HandleStep:Succeeded"`。`"PopModal:"` の行が無い |
| | `Pick_OutOfRange_DoesNothing` | `PickAsync(5)` → 記録は `InitializeAsync` の分だけ |
| | `Decline_Confirmed_Declines` | `"Confirm:スキルカードを受け取りません。よろしいですか？"`, `"ResolveCardOffer:1,Decline,0"`, `"Flow.HandleStep:Succeeded"`。`"PopModal:"` の行が無い |
| | `Decline_NotConfirmed_DoesNothing` | `Negative` → RPC の行が無い |
| | `Pick_Exception_ShowsError` | `ConfirmResults` は既定（`Positive`）、`StateResults` を空にする、`LogAssert.Expect`（上の書き方）→ 並びが `"Confirm:…"`, `"ResolveCardOffer:1,Pick,0"`, `"Info:エラーが発生しました: FakeProducePort: no state result"`。`"Flow.HandleStep:"` の行が無い |
| | `Resolve_Rejected_HandlesStepAndStays` | `BusinessError(ServerReasons.ProduceInvalidArgument, "x")` → `"Flow.HandleStep:Rejected"`、`"PopModal:"` の行が無い |
| `ProduceFlowTests.cs`（追記。本物の `ProduceFlow`、formation 5章の作り） | `ShowCurrent_WeekSelect_PopsAboveBackdropThenPushesWeekPage` | 局面 `WeekSelect`、`PageStack(() => 3, () => 0)`（背景・週・授業）→ `"PopPage:2"`, `"PushPage:WeekPresenter"`。`"Info:"`・`"GoToHome"` の行が無い |
| | `ShowCurrent_WeekSelectAfterModal_PopsModalThenPushesWeekPage` | 局面 `WeekSelect`、`PageStack(() => 1, () => 1)`（背景・カードの選択肢）→ `"PopModal:1"`, `"PushPage:WeekPresenter"` |
| | `ShowCurrent_CardOffer_PopsPagesThenPushesModal` | 局面 `CardOffer`、`PageStack(() => 3, () => 0)` → `"PopPage:2"`, `"PushModal:CardOfferPresenter"` |
| | `ShowCurrent_CardOfferOnBackdrop_PushesModalOnly` | 局面 `CardOffer`、`PageStack(() => 1, () => 0)`（再開: 背景だけ）→ `"PushModal:CardOfferPresenter"` だけ（`"PopPage:"`・`"PopModal:"` の行が無い） |

偽の View の記録の書式（foundation 3.3.7）: `SetHeader`（Week）は `"SetHeader:" + weekText + "," + nextExamText`、`SetStatus` は `"SetStatus:" + 行を , で`、`SetActions` は `"SetActions:" + 要素ごとの "SlotIndex/Label/Enabled" を , で`、`SetHeader`（Class）は `"SetHeader:" + text`、`SetOptions` は `"SetOptions:" + "Index/Label/Enabled" を , で`、`SetCards` は `"SetCards:" + 要素ごとの "Index/Title/Costs/Description" を , で`、`SetDeckCount` は `"SetDeckCount:" + text`。偽の View は `RaiseXxx()` でイベントを起こせるようにする。

作らないテスト: 実行中フラグの確認（偽が同期で終わるので効果が見えない。foundation の `HomePresenterTests` と formation の `Start_WhileRunning_Ignored` で済んでいる）。

`CardSummaryBuilderTests` の `catalog` の作り方（`shared/Uta.Shared.Domain.Tests/Lesson/CardCatalogTests.cs` の `Create_OneCardWithBothFaces_BuildsDefinition` と同じ行。クライアントのテストからは xUnit のヘルパーを使えないので、テストのファイルに `private static ICardCatalog CreateCatalog()` として書く）:

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

`CardOfferPresenterTests` も同じ関数を自分のファイルに private で書く（2 か所の重複は受け入れる）。

### 5.3 Play モード・手動（Editor、サーバー起動済み。CLAUDE.md「ローカルのサーバー」）

- ホーム →「プロデュース」→ 編成で「最短シナリオ（仮）」を選んで開始 → `Produce` シーンの週の行動選択（見出し「週 1 / 3」、「最終試験まで 2 週」、「レッスン」のボタン 1 つ）。ステータスが編成した育成アイドルの初期値
- 「初級（仮）」で開始: 週 1 のレッスンは 6 本目の画面（それまでは formation の「この局面の画面は準備中です: Lesson」→ ホーム）。週 2 で「授業」→ 授業のページ（選択肢 3 つ、体力が足りない選択肢は無効）→ 選ぶ → 確認 → 上がった値のダイアログ（授業のページの上）→ 授業のページが閉じ、背景のページ（見出しの値が授業のあとの値）の上にスキルカードの獲得のモーダル（名前・レア度・種類・コスト・説明文）→ カードを選ぶ → 確認 → 次の週の行動選択（授業のページ・モーダルが残っていない）
- 「支給」→ 支給のページ → 「受け取る」→ 背景のページの上にモーダル → 「受け取らない」→ 確認 → 次の週
- 授業・支給のページの「＜」で週の行動選択に戻る（週は進まない）
- 体力が最大のとき「休む」が無効。体力が減っているとき「休む」→ 確認 → 回復量のダイアログ → 次の週
- カードの選択肢でアプリを終了 → 起動 → ホームの「再開」→ `Produce` シーンの背景のページの上にモーダル → 獲得 → 次の週の行動選択 → 「≡」→「ホームへ戻る」で `Home` シーン（「再開」が出ている）
- 「≡」→ 放棄 → 確認 → `Home` シーン（「再開」が消える）
- モーダルの背景をタップしても閉じない
- 文字が豆腐（□）になっていない（`≡`・`＜` を含む）

## 6. 未決事項

いまは ready。ユーザーの確認は全件了承済み（2026-11-07）。

- （2026-11-07 に照合済み）formation の名前（`ProduceProgress`・`ProduceSession.Previous`・`ProduceFlow`／`ProduceMenu` のコンストラクター・`TestProduce.Progress`・`FakeProducePort.StateResults`）、P3 の `SkillCardRowView`、foundation の `ListRowBase`・`UiObjects.AddRow` は一致。lesson はこの版の `FakeProduceFlow`・`SkillCardRowView.SetTexts` を使う形に直した。`DisplayText` の owned の分は `RarityName`・`PlanName`・`SupportTypeName`・`ParamName` にそろえた
