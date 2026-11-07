---
title: 実装計画: client/ の Prefab 3/3 編成・プロデュース
status: ready
updated: 2026-11-08
spec: ../wiki/architecture/screen-design.md
---

# 実装計画: client/ の Prefab 3/3 編成・プロデュース

## この計画で決めたこと

ユーザーは、この節と次の「ユーザーの確認が要ること」だけを読めば判断できるように書く。

1. **作る画面の Prefab は 10 個**（Addressables の `Screens` に登録）: `FormationPage`（`Screens/Formation/`）、`ProduceBackdropPage`・`WeekPage`・`ClassPage`・`SupplyPage`・`CardOfferModal`・`LessonPage`・`DeckListModal`・`ExamResultPage`・`ProduceResultPage`（`Screens/Produce/`）。依頼の一覧に無い **`SupplyPage`（支給）も作る**: C# の計画（[week](2026-11-03-client-screens-week.md) 3.3）に `SupplyPage` があり、Prefab が無いと Push できないため
2. **行の Prefab は 5 個**（`Assets/Prefabs/Parts/`。Addressables に入れない。画面の View が `[SerializeField]` でアセットを参照する）: `FormationSlotRow`（編成の枠）、`SkillCardRow`（スキルカード 1 枚。**手札とスキルカード獲得の選択肢で共有**: どちらも「名前・消費・説明文・タップ」で同じ形のため）、`DeckSectionHeaderRow`・`DeckCardRow`（山札などの一覧の見出しと行）、`ExamRankingRow`（試験結果の順位表）
3. **プロデュースのメニュー（[≡]）は Prefab を作らない**。[formation](2026-11-03-client-screens-formation.md) 3.4 の `ProduceMenu` は `DialogService.ChooseAsync`（3 択）で足りるので、P1 の `Dialog` を使う。[≡] のボタンだけを `WeekPage`・`LessonPage` のヘッダーの右（`Header/RightSlot`）に置く
4. **ヘッダーは P1 の `Header` を使う**。戻るの無い画面（`WeekPage`・`LessonPage`）は、ネストした `BackButton` を非アクティブにし、`TitleText` を左寄せにする。`ExamResultPage`・`ProduceResultPage`・`ProduceBackdropPage` はヘッダーを置かない（戻る先が無く、見出しは本文の中に置く）
5. **ページの中身の組み方は P2 と同じ**: `Body`（P1 の雛形。Safe Area）に縦の並び（VLG）を付け、上から `Header`（あれば）→ `Main`（`LayoutSpec.Column`。残りの高さを全部取る）。`Main` の中で余った高さは `Spacer` か縦スクロールの一覧が取る。**組み方の補助は P2 の `ScreenLayout` を使い**（`SetupBody`・`AddMain`・`AddLabel`・`AddFlexibleSpacer`・`LoadPart`）、P2 に無いプロデュースの画面だけの補助（戻るの無いヘッダー・メニューのボタン・実行時に並べる一覧の入れ物）だけを `ProduceLayout` に置く（3.3）。Editor の名前空間は P2 と同じ `Uta.Client.Editor.Prefabs.Screens`
6. **数が増減する要素はすべて実行時に C# が Instantiate する**（週の行動・授業の選択肢 = `Button`、手札・獲得の選択肢 = `SkillCardRow`、山札などの一覧 = `DeckSectionHeaderRow`・`DeckCardRow`、順位表 = `ExamRankingRow`）。**数が決まっている編成の 5 枠だけは Prefab の中に `Slot0`〜`Slot4` として置く**（`FormationPresenter.SupportSlots = 5`）
7. 文字の大きさ・色・寸法は P1 の `UiTheme` の値だけを使う（新しい色・フォントサイズを足さない）。表示の文字（ラベルの初期値）は C# の計画の文言に合わせる
8. View の骨格（`[SerializeField] private` のフィールドだけ）を、C# の計画の `IXxxView` から導いて作る（3.1）。C# の計画（formation・week・lesson）は、この計画の対応表（3.2・4章）のフィールドと、foundation 3.2 の部品の View のメソッド（`HeaderView.SetTitle`・`BackButton`、`ButtonView.Button`・`SetLabel`・`SetInteractable`、`ListRowBase.Button`・`SetInteractable`）を使う
9. **画面とシーンの関係**（formation の計画が正。Prefab はシーンに依存しない）: `FormationPage` は `Home` シーンでホームのページの上に積む。プロデュースの画面（`WeekPage` から `ProduceResultPage` まで）は `Produce` シーンに積み、**底のページはいつも `ProduceBackdropPage`**（局面の画面は背景より上を Pop してから Push する。`CardOfferModal` は背景のページの上にモーダルで出す）
10. **叩き台（[screen-wireframes.md](../wiki/overview/screen-wireframes.md)）との見た目の差**。どれも「見た目は仮」の範囲で、C# の計画の表示の形（文字の行）をそのまま Prefab にしたもの。この案で進めてよいか:（ユーザーが推奨どおり了承した。2026-11-07）
   - a. 手札は**縦の一覧**（1 枚 = 名前・消費・説明文の 3 行。4 枚を超えるとスクロール）。叩き台 3 は横に 3 枚並べるが、説明文を出すと横 1/3 の幅では読めないため
   - b. ステータスは**文字の行**（「好調 3　集中 4」）。叩き台 3 の [好調3] の札にはしない（札を並べる部品と行の C# が増えるため。MVP の後に足せる）
   - c. 週の行動は**縦に並んだボタン**（叩き台 2 は 2×2）。行動の数が週で 1〜4 と変わり、縦の並びなら数に依らず同じ形で済むため
   - d. 試験結果の順位表は 1 行 1 文字列（「2位　あなた　1,388 ◀」）で、自分の行の文字だけ `Accent` の色（C# が色を変える）
11. **記号の字形**: `≡`（メニュー）・`◀`（順位表の自分の印）・`＜` が Noto Sans JP にある前提（`≡` は JIS X 0208 にある。`◀` は JIS X 0213 の字で、Noto Sans JP にあるのは推測）。タスク 5 の目視で豆腐（□）なら、`◀` は C# の計画の文言を `"（あなた）"` などに変える案にする（その時点で確認する）（ユーザーが推奨どおり了承した。2026-11-07）

## ユーザーの確認が要ること

なし（全件、2026-11-07 にユーザーが推奨どおり了承した。「この計画で決めたこと」の末尾に移した）。

## 1. 目的と前提

[game-spec.md](../wiki/overview/game-spec.md) の順 7（client/ の画面）のうち、Prefab の 3 本目（最後）。編成とプロデュースの画面の Prefab と、Prefab のルート・行に付く View の骨格（フィールド宣言だけ）を作る。方針の正は [screen-design.md](../wiki/architecture/screen-design.md) 7章（7.1 Editor のスクリプトで生成、7.2 Addressables、7.4 部品と置き場・命名、7.6 プロデュースの画面の流れ）。画面の叩き台は [screen-wireframes.md](../wiki/overview/screen-wireframes.md) 1（編成）・2（週の行動選択）・3（レッスン・試験）・4（スキルカード獲得）・5（授業・支給）・7（試験結果）・8（最終評価）。表示する要素とその文言は、C# の計画 [formation](2026-11-03-client-screens-formation.md) 3.4・3.5、[week](2026-11-03-client-screens-week.md) 3.3・3.4・3.6、[lesson](2026-11-03-client-screens-lesson.md) 3.4・3.6 の `IXxxView` とレイアウトの節から導く。

- 関係する ADR: [0009](../wiki/decisions/0009-client-stack.md)（uGUI + UnityScreenNavigator・縦画面）、[0006](../wiki/decisions/0006-adopt-unity-7-early.md)（Unity 7）、[0001](../wiki/decisions/0001-ip-policy.md)（文言はオリジナル）
- 計画の並び（[索引](2026-11-07-client-screens-index.md)）: 計画 0 → P1（[common](2026-11-07-client-prefabs-common.md)）→ P2（[screens-a](2026-11-07-client-prefabs-screens-a.md)）→ foundation → gacha → owned → **P3（この計画）** → formation → week → lesson
- **前提 1**: P1 が done（`PrefabBuildUtil`・`IPrefabBuilder`・`RectSpec`・`LayoutSpec`・`TextSpec`・`ButtonStyle`・`PrefabBuildCommands`・`PrefabContract`／`PrefabContractTests`・部品の Prefab 10 個・`PageBase`／`ModalBase`・部品の View・`UiTheme`）。加えて P2 が done（Editor の補助 `ScreenLayout`。3.3）。P3 の View の型が使うのは P1 の型だけ、生成が使うのは P1 と P2 の `ScreenLayout` だけ（foundation・C# の計画の型には依存しない）。無ければ止まって報告する
- **前提 2**: Unity 7（`7000.0.0a12`）の Editor が、この worktree の `client/uta` を開いていること（`unity status --until-ready` が ready）。**Editor が無い環境ではこの計画のどのタスクも実行できない**。実行できないときは「確認できていない」と書いてユーザーに実行を頼む（推測で「通った」と書かない）
- **前提 3**: テストの回し方は [P1](2026-11-07-client-prefabs-common.md) の決めたこと 12（**Editor を開いたまま** `unity command --timeout 300 eval` で `TestRunnerApi` を呼ぶ。コマンドの正は [unity7-evaluation.md](../wiki/architecture/unity7-evaluation.md) 4章と CLAUDE.md「ビルド・テスト・実行」。結果は `client/uta/Temp/uta-test-results.xml`）。Editor は閉じない（`unity test` は使わない）。eval で回せないときは、Editor の Test Runner の EditMode を全部実行してもらい（手動）、確認できていないと報告する（止まらない）。この計画ではプレイヤーのビルドを確かめない（P1 の決めたこと 19）
- **この計画で決めないこと**: View のメソッド・`IXxxView` の実装・Presenter・`ProduceFlow`（C# の計画）。どのシーンにどの画面を積むか（formation の計画が正。決めたこと 9）。`HeaderView`・`ButtonView` の表示のメソッド（foundation 3.2 が足す: `HeaderView.SetTitle`、`ButtonView.SetLabel`・`SetInteractable`）

## 2. 触るファイル

パスは、`client/uta/Assets/` で始まらないものはリポジトリのルートからの相対、それ以外は `client/uta/Assets/` からの相対。新しい `.cs`・フォルダ・アセットには Unity が作った `.meta` をコミットする。「生成」は Editor のスクリプトが作るもの（手で書かない・直さない）。

| 操作 | パス | 中身 |
|---|---|---|
| 作成 | `Scripts/Presentation/Formation/FormationPage.cs`・`FormationSlotRowView.cs` | 編成の View の骨格（3.1） |
| 作成 | `Scripts/Presentation/Produce/ProduceBackdropPage.cs`・`WeekPage.cs`・`ClassPage.cs`・`SupplyPage.cs`・`CardOfferModal.cs`・`LessonPage.cs`・`DeckListModal.cs`・`ExamResultPage.cs`・`ProduceResultPage.cs` | プロデュースの View の骨格（3.1） |
| 作成 | `Scripts/Presentation/Produce/SkillCardRowView.cs`・`DeckSectionHeaderRowView.cs`・`DeckCardRowView.cs`・`ExamRankingRowView.cs` | 行の View の骨格（3.1） |
| 作成 | `Scripts/Editor/Prefabs/Screens/Produce/ProduceLayout.cs` | P3 の生成の補助（3.3） |
| 作成 | `Scripts/Editor/Prefabs/Screens/Formation/FormationSlotRowBuilder.cs`・`FormationPageBuilder.cs` | 編成の生成クラス（3.4） |
| 作成 | `Scripts/Editor/Prefabs/Screens/Produce/SkillCardRowBuilder.cs`・`DeckSectionHeaderRowBuilder.cs`・`DeckCardRowBuilder.cs`・`ExamRankingRowBuilder.cs` | 行の生成クラス（3.4） |
| 作成 | `Scripts/Editor/Prefabs/Screens/Produce/ProduceBackdropPageBuilder.cs`・`WeekPageBuilder.cs`・`ClassPageBuilder.cs`・`SupplyPageBuilder.cs`・`CardOfferModalBuilder.cs`・`LessonPageBuilder.cs`・`DeckListModalBuilder.cs`・`ExamResultPageBuilder.cs`・`ProduceResultPageBuilder.cs` | 画面の生成クラス（3.4） |
| 生成 | `Prefabs/Parts/FormationSlotRow.prefab`・`SkillCardRow.prefab`・`DeckSectionHeaderRow.prefab`・`DeckCardRow.prefab`・`ExamRankingRow.prefab` | 行（3.5） |
| 生成 | `Prefabs/Screens/Formation/FormationPage.prefab` | 編成（3.6） |
| 生成 | `Prefabs/Screens/Produce/` の 9 個（1 の表の名前 + `.prefab`） | プロデュース（3.7〜3.15） |
| 生成（変更） | `AddressableAssetsData/AssetGroups/Screens.asset` | 画面 10 個のエントリ（`RegisterScreen` が足す） |
| 作成 | `Tests/EditMode/ProducePrefabTests.cs` | 画面固有の確かめ（5章） |
| 変更 | `uta_docs/wiki/architecture/screen-design.md` | 実装メモ（タスク 6） |
| 変更 | `uta_docs/wiki/overview/screen-wireframes.md` | 2・3・7 の MVP の見た目の注記（タスク 6。決めたこと 10 は了承済み） |

## 3. 型とシグネチャ

C# は `client/` の `.editorconfig` の範囲（C# 9 の範囲。ファイルスコープ namespace・プライマリコンストラクタ・コレクション式 `[...]`・Index/Range・record は使わない。配列は `new[] { ... }`）。BOM 付き UTF-8、LF、Allman、`var`、private フィールドは接頭辞なしの camelCase。名前空間: `Scripts/Presentation/Formation` → `Uta.Client.Presentation.Formation`、`Scripts/Presentation/Produce` → `Uta.Client.Presentation.Produce`、`Scripts/Editor/Prefabs/Screens/Formation`・`Scripts/Editor/Prefabs/Screens/Produce` → どちらも `Uta.Client.Editor.Prefabs.Screens`（P2 の生成クラス・`ScreenLayout` と同じ。フォルダで名前空間を分けない）、テストは `Uta.Client.Infrastructure.Tests`。

表の書き方は P1 3.4 の「共通の書き方」（全面・上帯・中央・レイアウト任せ・TMP の共通の値・Image・Button の共通の値・VLG/HLG の「制御 W/H」「伸ばす W/H」・LE・「→ 部品」のネスト）に従う。加えて:
- 「TMP 40/`Text`/`MidlineLeft`/NoWrap/Ellipsis」は、`TextSpec(40, UiTheme.Text, TextAlignmentOptions.MidlineLeft, wrap: false, ellipsis: true)` の意味（順に fontSize・color・alignment・折り返し（`Normal`＝wrap true／`NoWrap`）・はみ出し（`Ellipsis`／`Overflow`））。「→ `Label` TMP …」は `PrefabBuildUtil.AddLabel(parent, 名前, 文字, その TextSpec)`
- 「`ScreenLayout.AddLabel` TMP …、高さ h」は `ScreenLayout.AddLabel(parent, 名前, 文字, その TextSpec, h)`（P2 3.4。`Label` をネストして LE h を付ける）
- 「LE h 52」は `SetLayout(go, minHeight: 52, preferredHeight: 52)`。「LE w 200」は `preferredWidth: 200, flexibleWidth: 0`。「LE 伸び」は `flexibleHeight: 1`（縦の並びの中）か `flexibleWidth: 1`（横の並びの中）
- 「→ `Button` Primary "選ぶ"」は `PrefabBuildUtil.AddButtonPart(parent, 名前, "選ぶ", ButtonStyle.Primary)`
- 色は `UiTheme` の名前（`Text`・`TextMuted`・`Accent`・`Panel`・`Header` など）
- 出どころの略: 「W1」= screen-wireframes.md 1 編成（同様に W2〜W8）、「FM」= formation の計画、「WK」= week の計画、「LS」= lesson の計画（節の番号を付ける）

### 3.1 View の骨格（Presentation）

全部 `[SerializeField] private` のフィールドだけ（メソッド・プロパティ・イベント・インターフェースは C# の計画が足す）。`using TMPro; using UnityEngine; using UnityEngine.UI; using Uta.Client.Presentation.Common; using Uta.Client.Presentation.Common.Parts;`（使うものだけ）。

```csharp
namespace Uta.Client.Presentation.Formation
{
    public sealed class FormationPage : PageBase
    {
        [SerializeField] private HeaderView header;
        [SerializeField] private TMP_Text idolText;
        [SerializeField] private ButtonView selectIdolButton;
        [SerializeField] private FormationSlotRowView[] slots;      // 長さ 5。Slot0〜Slot4 の順
        [SerializeField] private TMP_Text scenarioText;
        [SerializeField] private ButtonView changeScenarioButton;
        [SerializeField] private ButtonView startButton;
    }

    public sealed class FormationSlotRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private ButtonView selectButton;
        [SerializeField] private ButtonView clearButton;
    }
}

namespace Uta.Client.Presentation.Produce
{
    public sealed class ProduceBackdropPage : PageBase
    {
        [SerializeField] private TMP_Text headerText;
    }

    public sealed class WeekPage : PageBase
    {
        [SerializeField] private HeaderView header;              // titleText に週の文字
        [SerializeField] private ButtonView menuButton;
        [SerializeField] private TMP_Text nextExamText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private RectTransform actionList;          // 行動のボタンを実行時に並べる親
        [SerializeField] private ButtonView actionButtonPrefab;  // Parts/Button.prefab のアセットの参照
    }

    public sealed class ClassPage : PageBase
    {
        [SerializeField] private HeaderView header;
        [SerializeField] private RectTransform optionList;
        [SerializeField] private ButtonView optionButtonPrefab;  // Parts/Button.prefab のアセットの参照
    }

    public sealed class SupplyPage : PageBase
    {
        [SerializeField] private HeaderView header;
        [SerializeField] private ButtonView receiveButton;
    }

    public sealed class CardOfferModal : ModalBase
    {
        [SerializeField] private RectTransform cardList;
        [SerializeField] private SkillCardRowView cardRowPrefab;    // Parts/SkillCardRow.prefab のアセットの参照
        [SerializeField] private TMP_Text deckCountText;
        [SerializeField] private ButtonView declineButton;
    }

    public sealed class LessonPage : PageBase
    {
        [SerializeField] private HeaderView header;              // titleText にレッスンの見出し
        [SerializeField] private TMP_Text turnText;
        [SerializeField] private ButtonView menuButton;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text lastResultText;
        [SerializeField] private ButtonView pilesButton;
        [SerializeField] private RectTransform handContent;         // 縦スクロールの Content
        [SerializeField] private SkillCardRowView handRowPrefab;    // Parts/SkillCardRow.prefab のアセットの参照
        [SerializeField] private ButtonView skipButton;
    }

    public sealed class DeckListModal : ModalBase
    {
        [SerializeField] private RectTransform listContent;         // 縦スクロールの Content
        [SerializeField] private DeckSectionHeaderRowView sectionHeaderPrefab;  // Parts/DeckSectionHeaderRow.prefab
        [SerializeField] private DeckCardRowView cardRowPrefab;                 // Parts/DeckCardRow.prefab
        [SerializeField] private ButtonView closeButton;
    }

    public sealed class ExamResultPage : PageBase
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private RectTransform rankingContent;      // 縦スクロールの Content
        [SerializeField] private ExamRankingRowView rankingRowPrefab;           // Parts/ExamRankingRow.prefab
        [SerializeField] private TMP_Text outcomeText;
        [SerializeField] private ButtonView retryButton;
        [SerializeField] private ButtonView nextButton;
    }

    public sealed class ProduceResultPage : PageBase
    {
        [SerializeField] private TMP_Text rankText;
        [SerializeField] private TMP_Text linesText;
        [SerializeField] private ButtonView homeButton;
    }

    /// <summary>スキルカード 1 枚の行（手札・獲得の選択肢）。button・background は ListRowBase。</summary>
    public sealed class SkillCardRowView : ListRowBase
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text costsText;
        [SerializeField] private TMP_Text descriptionText;
    }

    public sealed class DeckSectionHeaderRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text headerText;
    }

    public sealed class DeckCardRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
    }

    public sealed class ExamRankingRowView : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text text;
    }
}
```

- 1 ファイル 1 型（ファイル名 = 型名）。`ListRowBase` は P1 の `Uta.Client.Presentation.Common.Parts.ListRowBase`（`button`・`background`）
- 既存の `Scripts/Presentation/Produce/ProduceView.cs`・`ProducePresenter.cs`・`ProduceLifetimeScope.cs`（PR #24 の Produce シーンの最小の uGUI。foundation も残す）は触らない（名前は衝突しない。置き換えと削除は formation の計画）
- フィールドは全部 `null` 不可（`[OptionalReference]` は付けない）。`PrefabContractTests.AllPrefabs_SerializedReferences_AreAssigned` が確かめる

### 3.2 IF のメンバー → View のフィールド（C# の計画が使う）

C# の計画の `IXxxView` の各メンバーが、どのフィールドを使うか。C# の計画はこの表に合わせて実装する。部品の View のメソッド（`header.SetTitle`・`header.BackButton`、`xxxButton.Button`・`SetLabel`・`SetInteractable`、行の `Button`・`SetInteractable`）は foundation 3.2 のもの。一覧の子を消す・行を作るのは foundation 3.2 の `UiObjects.ClearChildren`・`UiObjects.AddRow`。

| IF のメンバー（計画） | 使うフィールド |
|---|---|
| `IFormationView.SetIdol`（FM 3.5） | `idolText` |
| `IFormationView.SetSlots` | `slots[i]` の `labelText`・`clearButton.SetInteractable(filled)` |
| `IFormationView.SetScenario` | `scenarioText`、`changeScenarioButton.SetInteractable(canChange)` |
| `IFormationView.SetStartEnabled` | `startButton.SetInteractable` |
| `IdolClicked`・`SlotClicked(i)`・`SlotClearClicked(i)`・`ScenarioClicked`・`StartClicked`・`BackClicked` | `selectIdolButton`、`slots[i].selectButton`、`slots[i].clearButton`、`changeScenarioButton`、`startButton`、`header.BackButton` |
| `IProduceBackdropView.SetHeader`（FM 3.4） | `headerText` |
| `IWeekView.SetHeader(weekText, nextExamText)`（WK 3.3） | `header.SetTitle(weekText)`、`nextExamText` |
| `IWeekView.SetStatus(lines)` | `statusText`（行を `"\n"` でつなぐ） |
| `IWeekView.SetActions` | `actionList` の子を消して、`actionButtonPrefab` を数だけ Instantiate（名前 `"Action" + slotIndex`） |
| `ActionClicked`・`MenuClicked` | 生成したボタン、`menuButton` |
| `IClassView.SetHeader` | `header.SetTitle` |
| `IClassView.SetOptions` | `optionList` の子を消して、`optionButtonPrefab` を数だけ Instantiate（名前 `"Option" + index`） |
| `OptionClicked`・`BackClicked` | 生成したボタン、`header.BackButton` |
| `ISupplyView.ReceiveClicked`・`BackClicked` | `receiveButton`、`header.BackButton` |
| `ICardOfferView.SetCards`（WK 3.3） | `cardList` の子を消して、`cardRowPrefab` を数だけ Instantiate（名前 `"Card" + index`）。`titleText` = `Title`、`costsText` = `Costs`、`descriptionText` = `Description` |
| `ICardOfferView.SetDeckCount` | `deckCountText` |
| `CardClicked`・`DeclineClicked` | 生成した行の `Button`、`declineButton` |
| `ILessonView.SetHeader(title, turnText)`（LS 3.4） | `header.SetTitle(title)`、`turnText` |
| `ILessonView.SetProgress`・`SetStatus`・`SetLastResult` | `progressText`・`statusText`（行を `"\n"` でつなぐ）・`lastResultText` |
| `ILessonView.SetPileCounts` | `pilesButton.SetLabel`（`"山札 {0}　捨札 {1}　除外 {2}"`） |
| `ILessonView.SetHand` | `handContent` の子を消して、`handRowPrefab` を数だけ Instantiate（名前 `"Card" + 添字`）。`Enabled == false` は行の `SetInteractable(false)` |
| `CardClicked`・`SkipClicked`・`MenuClicked`・`PilesClicked` | 生成した行の `Button`、`skipButton`、`menuButton`、`pilesButton` |
| `IDeckListView.SetSections` | `listContent` の子を消して、区分ごとに `sectionHeaderPrefab`（`headerText` = 見出し）を 1 つ、続けて行ごとに `cardRowPrefab`（`text` = 行の文字） |
| `IDeckListView.CloseClicked` | `closeButton` |
| `IExamResultView.SetTitle`・`SetOutcome` | `titleText`・`outcomeText` |
| `IExamResultView.SetRanking` | `rankingContent` の子を消して、`rankingRowPrefab` を行ごとに Instantiate。`text` = `Text`、`IsPlayer` なら `text.color = UiTheme.Accent`、そうでなければ `UiTheme.Text` |
| `IExamResultView.SetButtons(retryLabel, nextLabel)` | `retryButton`（`retryLabel` が `null` なら GameObject を非アクティブ、そうでなければ `SetLabel`）、`nextButton.SetLabel(nextLabel)` |
| `RetryClicked`・`NextClicked` | `retryButton`・`nextButton` |
| `IProduceResultView.SetRank`・`SetLines` | `rankText`・`linesText`（行を `"\n"` でつなぐ） |
| `IProduceResultView.HomeClicked` | `homeButton` |

- `WeekPage`・`LessonPage` の `header` の `backButton` は非アクティブ（3.3 の `ProduceLayout.AddHeaderWithoutBack`）。C# は購読しない
- 「子を消す」は `UiObjects.ClearChildren(actionList)` など（Prefab の中では子は 0 個）。行は `UiObjects.AddRow(rowPrefab, 親, 名前)`

### 3.3 画面を組む補助（P2 の `ScreenLayout` と、P3 の `ProduceLayout`）

**画面の組み方の補助は P2 の `ScreenLayout`（[P2](2026-11-07-client-prefabs-screens-a.md) 3.4。`Uta.Client.Editor.Prefabs.Screens`）が正**で、P3 はそれを使う（同じ役割の関数を P3 に作らない）。P3 が使うのは次の 5 つ:

| P2 の関数 | P3 での使い方 |
|---|---|
| `ScreenLayout.SetupBody(body)` | ページの `Body` に縦の並び（spacing 0・padding 0・`UpperCenter`・制御 true/true・伸ばす true/false）。子 `Header`（あれば。`Header` の LE 150）と `Main` を縦に積む |
| `ScreenLayout.AddMain(body)` | 子 `Main`（`LayoutSpec.Column`: padding 32・spacing 24、LE flexibleHeight 1）。中身はここに置く |
| `ScreenLayout.AddLabel(parent, name, text, spec, height)` | 高さの決まった文字（下の表の「`ScreenLayout.AddLabel` TMP …、高さ h」） |
| `ScreenLayout.AddFlexibleSpacer(parent, "Spacer")` | 余った高さを取る空き（下の表の `Spacer`） |
| `ScreenLayout.LoadPart<T>(partName)` | 画面の View の `rowPrefab`・`buttonPrefab` に入れる、`Assets/Prefabs/Parts/` の Prefab のアセットの参照（ネストではない） |

`ScreenLayout` に無い、編成・プロデュースの画面だけが使う関数は `ProduceLayout`（Editor の補助。`Scripts/Editor/Prefabs/Screens/Produce/ProduceLayout.cs`。名前空間は P2 と同じ `Uta.Client.Editor.Prefabs.Screens`）に置く。P1 の `PrefabBuildUtil` を呼ぶだけ。

```csharp
public static class ProduceLayout
{
    /// <summary>
    /// PrefabBuildUtil.AddHeader(body, title) のあと、header.transform.Find("BackButton") の GameObject を SetActive(false) にし、
    /// PrefabUtility.RecordPrefabInstancePropertyModifications(その GameObject)。
    /// header.transform.Find("TitleText") の TMP に ApplyText(new TextSpec(56, UiTheme.Text, TextAlignmentOptions.MidlineLeft, false, true))。
    /// 子が見つからなければ InvalidOperationException("Header child not found: " + 名前)。
    /// </summary>
    public static HeaderView AddHeaderWithoutBack(RectTransform body, string title);

    /// <summary>
    /// header.transform.Find("RightSlot") に AddButtonPart(slot, "MenuButton", "≡", ButtonStyle.Secondary) を置き、SetRect(Stretch())。
    /// （RightSlot はレイアウトグループを持たないので、全面で 200×130 を埋める）
    /// </summary>
    public static ButtonView AddMenuButton(HeaderView header);

    /// <summary>AddChild(parent, name) + AddVertical(new LayoutSpec(spacing, 0, 0, 0, 0, TextAnchor.UpperCenter, true, true, true, false))。子は無し（実行時に並べる）。</summary>
    public static RectTransform AddRuntimeList(RectTransform parent, string name, float spacing);
}
```

### 3.4 生成クラス（`IPrefabBuilder` の実装）

すべて `public sealed class <名前>Builder : IPrefabBuilder`（引数なしのコンストラクター）。P1 3.5 のとおり `PrefabBuildCommands.BuildAll` が `TypeCache` で集めるので、登録の表は要らない。

| クラス（`Scripts/Editor/Prefabs/Screens/` の下のフォルダ.クラス名。名前空間はどれも `Uta.Client.Editor.Prefabs.Screens`） | Order | `AssetPath` | Addressables |
|---|---|---|---|
| `Formation.FormationSlotRowBuilder` | 1000 | `PartPath("FormationSlotRow")` | 登録しない |
| `Produce.SkillCardRowBuilder` | 1010 | `PartPath("SkillCardRow")` | 登録しない |
| `Produce.DeckSectionHeaderRowBuilder` | 1020 | `PartPath("DeckSectionHeaderRow")` | 登録しない |
| `Produce.DeckCardRowBuilder` | 1030 | `PartPath("DeckCardRow")` | 登録しない |
| `Produce.ExamRankingRowBuilder` | 1040 | `PartPath("ExamRankingRow")` | 登録しない |
| `Formation.FormationPageBuilder` | 2000 | `ScreenPath("Formation", "FormationPage")` | `RegisterScreen` |
| `Produce.ProduceBackdropPageBuilder` | 2000 | `ScreenPath("Produce", "ProduceBackdropPage")` | `RegisterScreen` |
| `Produce.WeekPageBuilder` | 2000 | `ScreenPath("Produce", "WeekPage")` | `RegisterScreen` |
| `Produce.ClassPageBuilder` | 2000 | `ScreenPath("Produce", "ClassPage")` | `RegisterScreen` |
| `Produce.SupplyPageBuilder` | 2000 | `ScreenPath("Produce", "SupplyPage")` | `RegisterScreen` |
| `Produce.CardOfferModalBuilder` | 2000 | `ScreenPath("Produce", "CardOfferModal")` | `RegisterScreen` |
| `Produce.LessonPageBuilder` | 2000 | `ScreenPath("Produce", "LessonPage")` | `RegisterScreen` |
| `Produce.DeckListModalBuilder` | 2000 | `ScreenPath("Produce", "DeckListModal")` | `RegisterScreen` |
| `Produce.ExamResultPageBuilder` | 2000 | `ScreenPath("Produce", "ExamResultPage")` | `RegisterScreen` |
| `Produce.ProduceResultPageBuilder` | 2000 | `ScreenPath("Produce", "ProduceResultPage")` | `RegisterScreen` |

`Build()` の形:
- **行**: `var root = PrefabBuildUtil.CreateRoot("<名前>");` → 3.5 の表のとおりに組む → `AddComponent<…RowView>()` → `SetRef` で参照 → `PrefabBuildUtil.SavePrefab(root, AssetPath);`
- **ページ**: `var page = PrefabBuildUtil.CreatePage<XxxPage>("XxxPage", out var body);` → `ScreenLayout.SetupBody(body);` →（ヘッダーがあれば `PrefabBuildUtil.AddHeader`／`ProduceLayout.AddHeaderWithoutBack`）→ `var main = ScreenLayout.AddMain(body);` → 表のとおりに組む → `SetRef`／`SetRefs` で参照 → `PrefabBuildUtil.SavePrefab(page.gameObject, AssetPath);` → `PrefabBuildUtil.RegisterScreen(AssetPath);`
- **モーダル**: `var modal = PrefabBuildUtil.CreateModal<XxxModal>("XxxModal", out var panel);` → 表のとおりに `panel` の下へ組む（`SetupBody` は呼ばない。`Panel` は P1 の雛形のとおり VLG + ContentSizeFitter）→ `SetRef` → `SavePrefab` → `RegisterScreen`
- 子は表の上から順に作る（兄弟の順 = 表の順）。`SetRef` のフィールド名は 3.1 の名前そのまま

例（`FormationPageBuilder.Build`。ほかの画面も同じ書き方。ファイルの `using` は `System`・`TMPro`・`UnityEngine`・`Uta.Client.Presentation.Common`・`Uta.Client.Presentation.Common.Parts`・`Uta.Client.Presentation.Formation` のうち使うもの）:

```csharp
public void Build()
{
    var page = PrefabBuildUtil.CreatePage<FormationPage>("FormationPage", out var body);
    ScreenLayout.SetupBody(body);
    var header = PrefabBuildUtil.AddHeader(body, "編成");
    var main = ScreenLayout.AddMain(body);

    var idolPanel = PrefabBuildUtil.AddPanel(main, "IdolPanel");
    var idolText = ScreenLayout.AddLabel(idolPanel, "IdolText", "育成アイドル: 未選択",
        new TextSpec(UiTheme.FontBody, UiTheme.Text, TextAlignmentOptions.MidlineLeft, true, true), 110f);
    var selectIdol = PrefabBuildUtil.AddButtonPart(idolPanel, "SelectIdolButton", "育成アイドルを選ぶ");
    // …（3.6 の表のとおり）
    var slotViews = new FormationSlotRowView[FormationSlotCount];
    for (var i = 0; i < FormationSlotCount; i++)
    {
        slotViews[i] = PrefabBuildUtil.InstantiatePart<FormationSlotRowView>(slotList, "FormationSlotRow", "Slot" + i);
        // 枠ごとのラベルの初期値（ネストした FormationSlotRow の中の LabelText の TMP の text をオーバーライドする）
        var labelTransform = slotViews[i].transform.Find("LabelText");
        if (labelTransform == null)
        {
            throw new InvalidOperationException("FormationSlotRow child not found: LabelText");
        }
        var label = labelTransform.GetComponent<TextMeshProUGUI>();
        label.text = "枠" + (i + 1) + ": （空）";
        PrefabBuildUtil.MarkModified(label);
    }
    // …
    PrefabBuildUtil.SetRef(page, "header", header);
    PrefabBuildUtil.SetRef(page, "idolText", idolText);
    PrefabBuildUtil.SetRef(page, "selectIdolButton", selectIdol);
    PrefabBuildUtil.SetRefs(page, "slots", slotViews);
    // …
    PrefabBuildUtil.SavePrefab(page.gameObject, AssetPath);
    PrefabBuildUtil.RegisterScreen(AssetPath);
}
private const int FormationSlotCount = 5;   // FormationPresenter.SupportSlots と同じ（Editor のアセンブリは Presenter を参照しないので定数を写す）
```

### 3.5 行の Prefab（`Assets/Prefabs/Parts/`）

#### FormationSlotRow（出どころ: W1 のサポートカードの枠、FM 3.5 のレイアウト 3）

| 階層 | コンポーネントと値 |
|---|---|
| `FormationSlotRow` | RectTransform 全面（レイアウト任せ）。Image `Panel`、raycastTarget false。LE h 130。HLG: padding 左 24・右 24・上 10・下 10、spacing 16、`MiddleLeft`、制御 W/H true/true、伸ばす W/H false/true。`FormationSlotRowView`（labelText、selectButton、clearButton） |
| ├ `LabelText` | → `Label` TMP 32/`Text`/`MidlineLeft`/NoWrap/Ellipsis、text "枠: （空）"。LE `flexibleWidth: 1` |
| ├ `SelectButton` | → `Button` Primary "選ぶ"。LE `minHeight: 110, preferredHeight: 110, preferredWidth: 180, flexibleWidth: 0` |
| └ `ClearButton` | → `Button` Secondary "外す"。LE は `SelectButton` と同じ |

#### SkillCardRow（出どころ: W3 の手札・W4 の選択肢、WK 3.6 の `CardOfferModal`、LS 3.6 の `SetHand`。高さ 220 は LS の値）

| 階層 | コンポーネントと値 |
|---|---|
| `SkillCardRow` | RectTransform 全面（レイアウト任せ）。Image `Panel`、raycastTarget true。Button（共通の値。targetGraphic = 自分の Image）。LE h 220。VLG: padding 左 32・右 32・上 12・下 12、spacing 4、`UpperLeft`、制御 W/H true/true、伸ばす W/H true/false。`SkillCardRowView`（button、background = 自分の Image、titleText、costsText、descriptionText） |
| ├ `TitleText` | → `Label` TMP 40/`Text`/`MidlineLeft`/NoWrap/Ellipsis、text "カード名 [R] アクティブ"。LE h 52 |
| ├ `CostsText` | → `Label` TMP 32/`Accent`/`MidlineLeft`/NoWrap/Ellipsis、text "消費 4"。LE h 44 |
| └ `DescriptionText` | → `Label` TMP 32/`TextMuted`/`TopLeft`/Normal/Ellipsis、text "説明文"。LE h 92（2 行） |

（高さ: 上下 24 + 52 + 44 + 92 + spacing 8 = 220）

#### DeckSectionHeaderRow（出どころ: LS 3.6 の `DeckListModal` の見出し）

| 階層 | コンポーネントと値 |
|---|---|
| `DeckSectionHeaderRow` | RectTransform 全面（レイアウト任せ）。TMP（`AddText`。`Label` をネストしない）40/`Accent`/`MidlineLeft`/NoWrap/Ellipsis、text "山札 0 枚"。LE h 60。`DeckSectionHeaderRowView`（headerText = 自分の TMP） |

#### DeckCardRow（出どころ: LS 3.6 の `DeckListModal` の行。行の文字は `"<名前>　<消費>\n<説明文>"`）

| 階層 | コンポーネントと値 |
|---|---|
| `DeckCardRow` | RectTransform 全面（レイアウト任せ）。TMP（`AddText`）32/`Text`/`TopLeft`/Normal/Overflow、text "カード名　消費 4\n説明文"。LE `minHeight: 50`（高さは TMP の preferredHeight。親の VLG が制御する）。`DeckCardRowView`（text = 自分の TMP） |

#### ExamRankingRow（出どころ: W7 の順位表、LS 3.6 の `SetRanking`）

| 階層 | コンポーネントと値 |
|---|---|
| `ExamRankingRow` | RectTransform 全面（レイアウト任せ）。Image `Panel`、raycastTarget false。LE h 100。`ExamRankingRowView`（background = 自分の Image、text = 子 `Text` の TMP） |
| └ `Text` | → `Label` TMP 40/`Text`/`MidlineLeft`/NoWrap/Ellipsis、text "1位　ライバル 1　1,000"。RectTransform `RectSpec.Stretch(32, 32, 0, 0)` |

### 3.6 FormationPage（出どころ: W1、FM 3.5）

| 階層 | コンポーネントと値 |
|---|---|
| `FormationPage` | P1 の雛形（Image `Background`、`FormationPage`） |
| └ `Body` | 雛形 + `SetupBody` |
| 　├ `Header` | → `Header`（`AddHeader(body, "編成")`）。BackButton は "戻る" のまま。RightSlot は空 |
| 　└ `Main` | `ScreenLayout.AddMain`（VLG Column: padding 32、spacing 24、`UpperCenter`、制御 true/true、伸ばす true/false。LE 伸び） |
| 　　├ `IdolPanel` | → `Panel`（VLG padding 32・spacing 24） |
| 　　│├ `IdolText` | → `Label` TMP 40/`Text`/`MidlineLeft`/Normal/Ellipsis、text "育成アイドル: 未選択"。LE h 110 |
| 　　│└ `SelectIdolButton` | → `Button` Primary "育成アイドルを選ぶ" |
| 　　├ `SlotsTitle` | `ScreenLayout.AddLabel` TMP 40/`Text`/`MidlineLeft`/NoWrap/Ellipsis、text "サポートカード"、高さ 60 |
| 　　├ `SlotList` | `ProduceLayout.AddRuntimeList(main, "SlotList", 16)`（名前は Runtime だが、ここでは生成時に子を置く） |
| 　　│└ `Slot0`〜`Slot4` | → `FormationSlotRow`（`InstantiatePart<FormationSlotRowView>`）。`LabelText` の text を "枠1: （空）"〜"枠5: （空）" にオーバーライド |
| 　　├ `ScenarioRow` | `AddChild` + HLG `new LayoutSpec(16, 0, 0, 0, 0, MiddleLeft, true, true, false, true)` + LE h 130 |
| 　　│├ `ScenarioText` | → `Label` TMP 40/`Text`/`MidlineLeft`/NoWrap/Ellipsis、text "シナリオ: なし"。LE `flexibleWidth: 1` |
| 　　│└ `ChangeScenarioButton` | → `Button` Secondary "変更"。LE w 200 |
| 　　├ `Spacer` | `ScreenLayout.AddFlexibleSpacer(main, "Spacer")` |
| 　　└ `StartButton` | → `Button` Primary "プロデュース開始" |

（高さの見積り: Header 150 + 上下 64 + IdolPanel 328 + 60 + SlotList 714 + 130 + 130 + spacing 144 = 1720。1920 から Safe Area で 200 まで削られても収まる）

### 3.7 ProduceBackdropPage（出どころ: FM 3.4「週・パラメータ・体力だけを出す」）

| 階層 | コンポーネントと値 |
|---|---|
| `ProduceBackdropPage` | 雛形 |
| └ `Body` | 雛形 + `SetupBody` |
| 　└ `Main` | `ScreenLayout.AddMain` |
| 　　├ `HeaderText` | `ScreenLayout.AddLabel` TMP 40/`Text`/`TopLeft`/Normal/Overflow、text "週 1"、高さ 160 |
| 　　└ `Spacer` | `ScreenLayout.AddFlexibleSpacer(main, "Spacer")` |

### 3.8 WeekPage（出どころ: W2、WK 3.6）

| 階層 | コンポーネントと値 |
|---|---|
| `WeekPage` | 雛形 |
| └ `Body` | 雛形 + `SetupBody` |
| 　├ `Header` | `ProduceLayout.AddHeaderWithoutBack(body, "週 1 / 16")`。`RightSlot/MenuButton`（`ProduceLayout.AddMenuButton`） |
| 　└ `Main` | `ScreenLayout.AddMain` |
| 　　├ `NextExamText` | `ScreenLayout.AddLabel` TMP 32/`TextMuted`/`MidlineLeft`/NoWrap/Ellipsis、text ""、高さ 50 |
| 　　├ `StatusPanel` | → `Panel` |
| 　　│└ `StatusText` | `ScreenLayout.AddLabel` TMP 40/`Text`/`TopLeft`/Normal/Overflow、text "歌唱 0 / 0"、高さ 300（5 行） |
| 　　├ `Spacer` | `ScreenLayout.AddFlexibleSpacer(main, "Spacer")` |
| 　　├ `ActionsTitle` | `ScreenLayout.AddLabel` TMP 40/`Text`/`MidlineLeft`/NoWrap/Ellipsis、text "今週の行動"、高さ 60 |
| 　　└ `ActionList` | `ProduceLayout.AddRuntimeList(main, "ActionList", 24)`。子は無し |

参照: `actionButtonPrefab` = `ScreenLayout.LoadPart<ButtonView>("Button")`。

### 3.9 ClassPage（出どころ: W5、WK 3.6）

| 階層 | コンポーネントと値 |
|---|---|
| `ClassPage` | 雛形 |
| └ `Body` | 雛形 + `SetupBody` |
| 　├ `Header` | `AddHeader(body, "授業")`（戻るあり） |
| 　└ `Main` | `ScreenLayout.AddMain` |
| 　　├ `OptionList` | `ProduceLayout.AddRuntimeList(main, "OptionList", 24)`。子は無し |
| 　　└ `Spacer` | `ScreenLayout.AddFlexibleSpacer(main, "Spacer")` |

参照: `optionButtonPrefab` = `ScreenLayout.LoadPart<ButtonView>("Button")`。

### 3.10 SupplyPage（出どころ: W5、WK 3.6・決めたこと 3）

| 階層 | コンポーネントと値 |
|---|---|
| `SupplyPage` | 雛形 |
| └ `Body` | 雛形 + `SetupBody` |
| 　├ `Header` | `AddHeader(body, "支給")`（戻るあり） |
| 　└ `Main` | `ScreenLayout.AddMain` |
| 　　├ `MessageText` | `ScreenLayout.AddLabel` TMP 40/`Text`/`Center`/Normal/Overflow、text "スキルカードの選択肢から 1 枚を受け取れます。"、高さ 200（View のフィールドにしない。文言は固定） |
| 　　├ `ReceiveButton` | → `Button` Primary "受け取る" |
| 　　└ `Spacer` | `ScreenLayout.AddFlexibleSpacer(main, "Spacer")` |

### 3.11 CardOfferModal（出どころ: W4、WK 3.6）

| 階層 | コンポーネントと値 |
|---|---|
| `CardOfferModal` | P1 のモーダルの雛形（`CardOfferModal`、panel = 子 `Panel`） |
| └ `Panel` | 雛形（中央 900×0、Image `Panel`、VLG padding 40・spacing 24、ContentSizeFitter） |
| 　├ `TitleText` | `ScreenLayout.AddLabel` TMP 56/`Text`/`Center`/NoWrap/Ellipsis、text "スキルカードを1枚獲得"、高さ 80（固定の文言。フィールドにしない） |
| 　├ `CardList` | `ProduceLayout.AddRuntimeList(panel, "CardList", 24)`。子は無し |
| 　├ `DeckCountText` | `ScreenLayout.AddLabel` TMP 32/`TextMuted`/`MidlineLeft`/NoWrap/Ellipsis、text "デッキ 0 枚"、高さ 50 |
| 　└ `DeclineButton` | → `Button` Secondary "受け取らない" |

参照: `cardRowPrefab` = `ScreenLayout.LoadPart<SkillCardRowView>("SkillCardRow")`。再抽選・除去のボタンは作らない（WK 1 章: MVP で回数 0）。高さは 3 枚で約 1120（ContentSizeFitter が決める）。

### 3.12 LessonPage（出どころ: W3、LS 3.6）

| 階層 | コンポーネントと値 |
|---|---|
| `LessonPage` | 雛形 |
| └ `Body` | 雛形 + `SetupBody` |
| 　├ `Header` | `ProduceLayout.AddHeaderWithoutBack(body, "レッスン")`。`RightSlot/MenuButton`（`ProduceLayout.AddMenuButton`）。ヘッダーの子は足さない（`Header` のまま `BackButton`・`TitleText`・`RightSlot`） |
| 　└ `Main` | `ScreenLayout.AddMain` |
| 　　├ `TurnText` | **`Main` の先頭の子**（ヘッダーの外）。`ScreenLayout.AddLabel` TMP 40/`Text`/`MidlineRight`/NoWrap/Ellipsis、text "残り 0 / 0"、高さ 60 |
| 　　├ `ProgressText` | `ScreenLayout.AddLabel` TMP 40/`Text`/`TopLeft`/Normal/Overflow、text ""、高さ 170（3 行） |
| 　　├ `StatusText` | `ScreenLayout.AddLabel` TMP 32/`Text`/`TopLeft`/Normal/Overflow、text ""、高さ 220（5 行） |
| 　　├ `LastResultText` | `ScreenLayout.AddLabel` TMP 32/`Accent`/`MidlineLeft`/NoWrap/Ellipsis、text ""、高さ 50 |
| 　　├ `PilesButton` | → `Button` Secondary "山札 0　捨札 0　除外 0" |
| 　　├ `HandScroll` | `PrefabBuildUtil.AddScrollList(main, "HandScroll", out _)`（LE 伸び。戻り値の `Content` を `handContent` に） |
| 　　└ `SkipButton` | → `Button` Primary "スキップ" |

参照: `handRowPrefab` = `ScreenLayout.LoadPart<SkillCardRowView>("SkillCardRow")`、`turnText` = `Main/TurnText` の TMP（`ScreenLayout.AddLabel` の戻り値）、`menuButton` = `Header/RightSlot/MenuButton`。

（高さの見積り: Main の内側 1920 − 150 − 64 = 1706。固定 60 + 170 + 220 + 50 + 130 + 130 = 760、spacing 24 × 6 = 144 → 手札は 802（`SkillCardRow` 220 が 3 枚と間隔 2 つで 708 なので収まる。4 枚目からスクロール））

### 3.13 DeckListModal（出どころ: W3 の「山札などはタップで中身を表示」、LS 1.2・3.6。PRD-08）

| 階層 | コンポーネントと値 |
|---|---|
| `DeckListModal` | モーダルの雛形 |
| └ `Panel` | 雛形 |
| 　├ `TitleText` | `ScreenLayout.AddLabel` TMP 56/`Text`/`Center`/NoWrap/Ellipsis、text "山札・捨札・除外"、高さ 80（固定。フィールドにしない） |
| 　├ `ListScroll` | `var content = PrefabBuildUtil.AddScrollList(panel, "ListScroll", out var scroll);` のあと、`PrefabBuildUtil.SetLayout(scroll.gameObject, minHeight: 1200f, preferredHeight: 1200f)`、`PrefabBuildUtil.AddVertical(content.gameObject, new LayoutSpec(8f, 0, 0, 0, 0, TextAnchor.UpperCenter, true, true, true, false))`（`Content` に付いている VLG をそのまま使って spacing を 24 から 8 に変える。`AddVertical` は既にあれば使う） |
| 　└ `CloseButton` | → `Button` Secondary "閉じる" |

参照: `listContent` = `ListScroll/Viewport/Content`（上の `content`）、`sectionHeaderPrefab` = `ScreenLayout.LoadPart<DeckSectionHeaderRowView>("DeckSectionHeaderRow")`、`cardRowPrefab` = `ScreenLayout.LoadPart<DeckCardRowView>("DeckCardRow")`。

### 3.14 ExamResultPage（出どころ: W7、LS 3.6）

| 階層 | コンポーネントと値 |
|---|---|
| `ExamResultPage` | 雛形 |
| └ `Body` | 雛形 + `SetupBody`（ヘッダーなし） |
| 　└ `Main` | `ScreenLayout.AddMain` |
| 　　├ `TitleText` | `ScreenLayout.AddLabel` TMP 56/`Text`/`Center`/NoWrap/Ellipsis、text "試験結果"、高さ 100 |
| 　　├ `RankingScroll` | `AddScrollList(main, "RankingScroll", out _)`（LE 伸び） |
| 　　├ `OutcomeText` | `ScreenLayout.AddLabel` TMP 56/`Accent`/`Center`/NoWrap/Ellipsis、text ""、高さ 100 |
| 　　├ `RetryButton` | → `Button` Secondary "再挑戦" |
| 　　└ `NextButton` | → `Button` Primary "次へ" |

参照: `rankingContent` = `RankingScroll/Viewport/Content`、`rankingRowPrefab` = `ScreenLayout.LoadPart<ExamRankingRowView>("ExamRankingRow")`。

### 3.15 ProduceResultPage（出どころ: W8、LS 3.6。戻るボタンは作らない）

| 階層 | コンポーネントと値 |
|---|---|
| `ProduceResultPage` | 雛形 |
| └ `Body` | 雛形 + `SetupBody`（ヘッダーなし） |
| 　└ `Main` | `ScreenLayout.AddMain` |
| 　　├ `RankText` | `ScreenLayout.AddLabel` TMP 56/`Accent`/`Center`/NoWrap/Ellipsis、text "ランク -"、高さ 120 |
| 　　├ `LinesText` | → `Label` TMP 40/`Text`/`TopLeft`/Normal/Overflow、text ""。LE `flexibleHeight: 1`（10 行で約 520） |
| 　　└ `HomeButton` | → `Button` Primary "ホームへ" |

## 4. 値 → Prefab の部品 → View のフィールド（子の名前の対応表）

C# の計画はこの表が正。子のパスは Prefab のルートからの相対（`Transform.Find` の書き方）。

| 画面 | ワイヤー／仕様の値（出どころ） | 子のパス | View のフィールド（型） |
|---|---|---|---|
| FormationPage | 戻る（W1「＜」） | `Body/Header`（`BackButton`） | `header`（`HeaderView`） |
| | 育成アイドルの名前・レア度・プラン（FM `SetIdol`） | `Body/Main/IdolPanel/IdolText` | `idolText`（`TMP_Text`） |
| | 「変更」（W1） | `Body/Main/IdolPanel/SelectIdolButton` | `selectIdolButton`（`ButtonView`） |
| | サポートカード 5 枠（W1・FM `SetSlots`） | `Body/Main/SlotList/Slot0`〜`Slot4` | `slots`（`FormationSlotRowView[5]`） |
| | 枠の文字・選ぶ・外す | `SlotN/LabelText`・`SelectButton`・`ClearButton` | `FormationSlotRowView.labelText`・`selectButton`・`clearButton` |
| | シナリオ（FM `SetScenario`） | `Body/Main/ScenarioRow/ScenarioText`・`ChangeScenarioButton` | `scenarioText`、`changeScenarioButton` |
| | プロデュース開始（W1） | `Body/Main/StartButton` | `startButton` |
| ProduceBackdropPage | 週・パラメータ・体力（FM 3.4） | `Body/Main/HeaderText` | `headerText` |
| WeekPage | 週 9 / 16（W2） | `Body/Header`（`TitleText`） | `header` |
| | [≡]（W2） | `Body/Header/RightSlot/MenuButton` | `menuButton` |
| | 次: 中間試験まで（W2） | `Body/Main/NextExamText` | `nextExamText` |
| | 歌唱・律動・表現・体力（W2） | `Body/Main/StatusPanel/StatusText` | `statusText` |
| | 今週の行動のボタン（W2） | `Body/Main/ActionList`（子は実行時） | `actionList`、`actionButtonPrefab`（`Parts/Button`） |
| ClassPage | 戻る・見出し「授業　体力」（W5） | `Body/Header` | `header` |
| | 選択肢（W5） | `Body/Main/OptionList`（子は実行時） | `optionList`、`optionButtonPrefab`（`Parts/Button`） |
| SupplyPage | 戻る（WK） | `Body/Header` | `header` |
| | 受け取る（WK 決めたこと 3） | `Body/Main/ReceiveButton` | `receiveButton` |
| CardOfferModal | 選択肢 3 枚（W4） | `Panel/CardList`（子は実行時） | `cardList`、`cardRowPrefab`（`Parts/SkillCardRow`） |
| | デッキ 14枚（W4） | `Panel/DeckCountText` | `deckCountText` |
| | 受け取らない（W4） | `Panel/DeclineButton` | `declineButton` |
| LessonPage | 律動レッスン（特別）（W3） | `Body/Header`（`TitleText`） | `header` |
| | 残り 4/6（W3） | `Body/Main/TurnText` | `turnText` |
| | [≡]（W3） | `Body/Header/RightSlot/MenuButton` | `menuButton` |
| | 目標・獲得／スコア・順位・審査（W3） | `Body/Main/ProgressText` | `progressText` |
| | 体力・元気・ステータス（W3） | `Body/Main/StatusText` | `statusText` |
| | 操作の前後の差（LS 1.2） | `Body/Main/LastResultText` | `lastResultText` |
| | 山札 8 捨札 5 除外 2（W3） | `Body/Main/PilesButton` | `pilesButton` |
| | 手札（W3） | `Body/Main/HandScroll/Viewport/Content`（子は実行時） | `handContent`、`handRowPrefab`（`Parts/SkillCardRow`） |
| | スキップ（W3） | `Body/Main/SkipButton` | `skipButton` |
| DeckListModal | 区分の見出しとカード（LS 3.4.2） | `Panel/ListScroll/Viewport/Content`（子は実行時） | `listContent`、`sectionHeaderPrefab`、`cardRowPrefab` |
| | 閉じる | `Panel/CloseButton` | `closeButton` |
| ExamResultPage | 中間試験 結果（W7） | `Body/Main/TitleText` | `titleText` |
| | 順位表（W7） | `Body/Main/RankingScroll/Viewport/Content`（子は実行時） | `rankingContent`、`rankingRowPrefab`（`Parts/ExamRankingRow`） |
| | 合格！（W7） | `Body/Main/OutcomeText` | `outcomeText` |
| | 再挑戦・次へ（W7） | `Body/Main/RetryButton`・`NextButton` | `retryButton`・`nextButton` |
| ProduceResultPage | ランク A（W8） | `Body/Main/RankText` | `rankText` |
| | 評価値・パラメータ・報酬（W8） | `Body/Main/LinesText` | `linesText` |
| | ホームへ（W8） | `Body/Main/HomeButton` | `homeButton` |
| 行 | スキルカードの名前・消費・説明文（W3・W4） | `SkillCardRow/TitleText`・`CostsText`・`DescriptionText` | `SkillCardRowView.titleText`・`costsText`・`descriptionText`（`button`・`background` は `ListRowBase`） |
| | 一覧の見出し | `DeckSectionHeaderRow`（ルート） | `DeckSectionHeaderRowView.headerText` |
| | 一覧の行 | `DeckCardRow`（ルート） | `DeckCardRowView.text` |
| | 順位表の行 | `ExamRankingRow/Text` | `ExamRankingRowView.text`・`background` |

（プロデュースのメニュー（W2・W3 の [≡] の中身）は Prefab を作らない: P1 の `Dialog`（`DialogService.ChooseAsync`））

## 5. タスク

各タスクの終わりに、Editor のコンパイルエラーが 0（`client/uta` で `unity recompile --format json`）で、既存の EditMode のテストが通る状態にする（前提 3）。**Editor が開けないときは、どのタスクも「確認できていない」と書いて止まり、ユーザーに実行を頼む**。Prefab の生成は P1 の入口（`unity command eval 'return Uta.Client.Editor.Prefabs.PrefabBuildCommands.BuildAll();'`）だけで行う（ほかの方法で `.prefab` を作らない・手で直さない）。

### タスク 1: View の骨格

触る: `Scripts/Presentation/Formation/` の 2 ファイル、`Scripts/Presentation/Produce/` の 13 ファイル（3.1）。

手順:
1. 3.1 のとおりに作る（フィールドだけ）
2. `unity recompile --format json` でエラー 0。警告 CS0169／CS0649（使われないフィールド）が出ても直さない（C# の計画が使う）

終わったことの確かめ方: コンパイルエラー 0。既存のテストが通る。

### タスク 2: 補助と行の生成

触る: `Scripts/Editor/Prefabs/Screens/Produce/ProduceLayout.cs`、行の生成クラス 5 つ（3.4）、生成物 `Prefabs/Parts/` の 5 個。

手順:
1. 基準値を取る（ファイルを足す前）: `BuildAll()` を実行し、戻り値の件数 `built N` の N を記録する（P1・P2 が done なら 20 のはず。違っても止まらず、記録した N を基準にする）。続けて `unity command eval 'return UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings.FindGroup("Screens").entries.Count;'` の値 E を記録する（P2 が done なら 7）。`git status` で差分が無いこと（あれば、何が変わったかを報告に書く）
2. 3.3・3.4・3.5 のとおりに作る
3. `BuildAll()` を実行し、戻り値が `built (N + 5)`。`unity command console get --tail 50 --level error --format json` にエラーが無い
4. `git status` で、`Prefabs/Parts/` に増えたのが 5 個（と `.meta`）だけで、`AddressableAssetsData/` に差分が無い（行は登録しない）。`Screens` のエントリ数が E のまま

境界: 行の生成クラスは画面の生成クラスより先（Order 1000 台）。部品の Prefab（P1）が無ければ `InstantiatePart` が例外を投げる（止まって報告する）。

終わったことの確かめ方: 件数が 5 増えた（N + 5）、エラー無し。N・E と N + 5 を報告に書く（タスク 3・4 の基準になる）。

### タスク 3: 編成・背景・週・授業・支給・獲得の画面

触る: `FormationPageBuilder`・`ProduceBackdropPageBuilder`・`WeekPageBuilder`・`ClassPageBuilder`・`SupplyPageBuilder`・`CardOfferModalBuilder`、生成物 6 個、`Screens.asset`。

手順:
1. 基準値: タスク 2 の終わりの件数（N + 5）と `Screens` のエントリ数 E を使う。別のセッションで続けるなどで分からなければ、ファイルを足す前に `BuildAll()` とエントリ数の eval（タスク 2 の手順 1 と同じ）を実行して取り直す
2. 3.6〜3.11 のとおりに作る
3. `BuildAll()` の件数が基準より 6 多い（N + 11）。エラーが無い
4. `unity command eval 'return UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings.FindGroup("Screens").entries.Count;'` が基準より 6 多い（E + 6）

終わったことの確かめ方: 件数、エラー無し、`Screens` のエントリが 6 増えた。

### タスク 4: レッスン・一覧・試験結果・プロデュース結果の画面

触る: `LessonPageBuilder`・`DeckListModalBuilder`・`ExamResultPageBuilder`・`ProduceResultPageBuilder`、生成物 4 個、`Screens.asset`。

手順: タスク 3 と同じ（3.12〜3.15）。基準はタスク 3 の終わりの値（N + 11・E + 6。分からなければ足す前に取り直す）で、件数とエントリがそれぞれ 4 増える（N + 15・E + 10）。

### タスク 5: テストと見た目の確認

触る: `Tests/EditMode/ProducePrefabTests.cs`（6章）。

手順:
1. 6章のテストを作る
2. テストを回す（前提 3。Editor を開いたまま）。P1 の `PrefabContractTests`（全部の Prefab の参照・画面の名前と登録）と `ProducePrefabTests` が全部通る
3. 冪等の確かめ: もう一度 `BuildAll()` を実行し、`git diff --stat` で `Prefabs/Parts/`・`Prefabs/Screens/`・`AddressableAssetsData/` に差分が出るかを見る。**出ても止まらない**（P1 タスク 4 と同じ扱い）。差分の有無と、出たならどの値かを報告に書く。`Fonts/NotoSansJP-Regular SDF.asset` に差分が無いこと
4. ユーザーに、Editor で 10 個の画面と 5 個の行の Prefab を Prefab モードで開き、表の配置になっていること、日本語と `≡`（`MenuButton`）が豆腐でないことを見てもらう。`◀`・`＜` は仮の文字に無いので、`ExamRankingRow` を Prefab モードで開き、`Text` の TMP の text 欄に `◀＜` を入れて見るだけにし、保存せずに閉じる（手動。決めたこと 11）

終わったことの確かめ方: テストが通る。手動の確認が済んだ（豆腐があれば決めたこと 11 のとおりユーザーに聞く）。

### タスク 6: 結果の記録

触る: `uta_docs/wiki/architecture/screen-design.md`、`uta_docs/wiki/overview/screen-wireframes.md`、この計画。

手順:
1. screen-design.md（frontmatter の `updated` を実施日に）の「実装メモ」に、P3 で作った Prefab（画面 10・行 5）、プロデュースのメニューは `Dialog` で Prefab を作らないこと、手札と獲得の選択肢が `SkillCardRow` を共有すること、実装と計画のズレ（あれば）、タスク 5 の再生成の差分の結果を書く
2. 決めたこと 10 は了承済みなので、screen-wireframes.md（`updated` を実施日に）の 2・3・7 の節の末尾に「MVP の実装は … [決定。<日付>。ユーザーの判断]」を 1 行ずつ足す（2: 行動は縦のボタン、3: 手札は縦の一覧・ステータスは文字の行、7: 順位表は 1 行 1 文字列で自分の行を色で示す）
3. `python -I .claude/skills/linting-wiki/lint_wiki.py` がエラー 0
4. この計画の frontmatter を `status: done`、`updated` を実施日にする

## 6. テスト

`Tests/EditMode/ProducePrefabTests.cs`（`public sealed class ProducePrefabTests`。NUnit。`AssetDatabase.LoadAssetAtPath<GameObject>` で読む。パスは文字列の定数で持つ: テストのアセンブリは `Uta.Client.Editor` を参照している（P1 の 3.1）が、`PrefabBuildUtil.ScreenPath`・`PartPath` は使わず、期待値をテストの側で固定して生成クラスのパスの書き間違いを見つけるため）。P1 の `PrefabContractTests` は書き換えない。

| テスト | 確かめること |
|---|---|
| `ProduceScreens_ExistWithRootView` | 10 個の画面のパス（2章）が読めて、ルートに同名の型（`FormationPage` など）がある |
| `ProduceRows_ExistWithRowView` | 5 個の行のパスが読めて、ルートに行の View（`FormationSlotRowView`・`SkillCardRowView`・`DeckSectionHeaderRowView`・`DeckCardRowView`・`ExamRankingRowView`）がある |
| `ProduceScreens_HaveNamedChildren` | 下の「`Find` するパスの一覧」の各パスが、その Prefab のルートの `transform.Find` で見つかる（1 本のテストで全部回し、見つからないものを `"<Prefab のパス>: <子のパス>"` の一覧にして `Assert.That(missing, Is.Empty)`） |
| `HeaderBackButton_ActiveOnlyWhereBackExists` | `Body/Header/BackButton` の `activeSelf` が、`WeekPage`・`LessonPage` で false、`FormationPage`・`ClassPage`・`SupplyPage` で true |
| `LessonPage_TurnTextIsFirstInMain` | `LessonPage` の `Body/Header` の子の名前が順に `BackButton`・`TitleText`・`RightSlot`（ヘッダーに子を足していない）。`Body/Main` の子の名前が順に `TurnText`・`ProgressText`・`StatusText`・`LastResultText`・`PilesButton`・`HandScroll`・`SkipButton` |
| `FormationPage_HasFiveSlotsInOrder` | `FormationPage` の `SerializedObject` の `slots` の `arraySize` が 5、要素 `i` の GameObject の名前が `"Slot" + i` |
| `RowPrefabFields_ReferencePartAssets` | 次のフィールドの `objectReferenceValue` がアセット（`AssetDatabase.GetAssetPath` が次のパス）: `WeekPage.actionButtonPrefab`・`ClassPage.optionButtonPrefab` → `Assets/Prefabs/Parts/Button.prefab`、`CardOfferModal.cardRowPrefab`・`LessonPage.handRowPrefab` → `SkillCardRow.prefab`、`DeckListModal.sectionHeaderPrefab` → `DeckSectionHeaderRow.prefab`、`DeckListModal.cardRowPrefab` → `DeckCardRow.prefab`、`ExamResultPage.rankingRowPrefab` → `ExamRankingRow.prefab`（ネストしたインスタンスを指していないこと） |
| `RuntimeLists_AreEmpty` | `WeekPage` の `ActionList`、`ClassPage` の `OptionList`、`CardOfferModal` の `CardList`、`LessonPage`・`DeckListModal`・`ExamResultPage` のスクロールの `Content` の子が 0 個 |

**`Find` するパスの一覧**（`ProduceScreens_HaveNamedChildren` にこのまま書く。4章の表から取った。範囲は展開し、1 セルに 2 本あるものは分けた。行の Prefab の子も同じテストで確かめる）:

```csharp
private static readonly (string Prefab, string[] Children)[] NamedChildren =
{
    ("Assets/Prefabs/Screens/Formation/FormationPage.prefab", new[]
    {
        "Body/Header",
        "Body/Header/BackButton",
        "Body/Main/IdolPanel/IdolText",
        "Body/Main/IdolPanel/SelectIdolButton",
        "Body/Main/SlotList/Slot0",
        "Body/Main/SlotList/Slot0/LabelText",
        "Body/Main/SlotList/Slot0/SelectButton",
        "Body/Main/SlotList/Slot0/ClearButton",
        "Body/Main/SlotList/Slot1",
        "Body/Main/SlotList/Slot1/LabelText",
        "Body/Main/SlotList/Slot1/SelectButton",
        "Body/Main/SlotList/Slot1/ClearButton",
        "Body/Main/SlotList/Slot2",
        "Body/Main/SlotList/Slot2/LabelText",
        "Body/Main/SlotList/Slot2/SelectButton",
        "Body/Main/SlotList/Slot2/ClearButton",
        "Body/Main/SlotList/Slot3",
        "Body/Main/SlotList/Slot3/LabelText",
        "Body/Main/SlotList/Slot3/SelectButton",
        "Body/Main/SlotList/Slot3/ClearButton",
        "Body/Main/SlotList/Slot4",
        "Body/Main/SlotList/Slot4/LabelText",
        "Body/Main/SlotList/Slot4/SelectButton",
        "Body/Main/SlotList/Slot4/ClearButton",
        "Body/Main/ScenarioRow/ScenarioText",
        "Body/Main/ScenarioRow/ChangeScenarioButton",
        "Body/Main/StartButton",
    }),
    ("Assets/Prefabs/Screens/Produce/ProduceBackdropPage.prefab", new[]
    {
        "Body/Main/HeaderText",
    }),
    ("Assets/Prefabs/Screens/Produce/WeekPage.prefab", new[]
    {
        "Body/Header",
        "Body/Header/TitleText",
        "Body/Header/RightSlot/MenuButton",
        "Body/Main/NextExamText",
        "Body/Main/StatusPanel/StatusText",
        "Body/Main/ActionList",
    }),
    ("Assets/Prefabs/Screens/Produce/ClassPage.prefab", new[]
    {
        "Body/Header",
        "Body/Main/OptionList",
    }),
    ("Assets/Prefabs/Screens/Produce/SupplyPage.prefab", new[]
    {
        "Body/Header",
        "Body/Main/ReceiveButton",
    }),
    ("Assets/Prefabs/Screens/Produce/CardOfferModal.prefab", new[]
    {
        "Panel/CardList",
        "Panel/DeckCountText",
        "Panel/DeclineButton",
    }),
    ("Assets/Prefabs/Screens/Produce/LessonPage.prefab", new[]
    {
        "Body/Header",
        "Body/Header/TitleText",
        "Body/Header/RightSlot/MenuButton",
        "Body/Main/TurnText",
        "Body/Main/ProgressText",
        "Body/Main/StatusText",
        "Body/Main/LastResultText",
        "Body/Main/PilesButton",
        "Body/Main/HandScroll/Viewport/Content",
        "Body/Main/SkipButton",
    }),
    ("Assets/Prefabs/Screens/Produce/DeckListModal.prefab", new[]
    {
        "Panel/ListScroll/Viewport/Content",
        "Panel/CloseButton",
    }),
    ("Assets/Prefabs/Screens/Produce/ExamResultPage.prefab", new[]
    {
        "Body/Main/TitleText",
        "Body/Main/RankingScroll/Viewport/Content",
        "Body/Main/OutcomeText",
        "Body/Main/RetryButton",
        "Body/Main/NextButton",
    }),
    ("Assets/Prefabs/Screens/Produce/ProduceResultPage.prefab", new[]
    {
        "Body/Main/RankText",
        "Body/Main/LinesText",
        "Body/Main/HomeButton",
    }),
    ("Assets/Prefabs/Parts/SkillCardRow.prefab", new[]
    {
        "TitleText",
        "CostsText",
        "DescriptionText",
    }),
    ("Assets/Prefabs/Parts/ExamRankingRow.prefab", new[]
    {
        "Text",
    }),
};
```

- `DeckSectionHeaderRow`・`DeckCardRow` は子が無い（ルート自身が TMP）ので一覧に入れない。ルートの View は `ProduceRows_ExistWithRowView` が確かめる
- 4章の表を変えたときは、この一覧も同じ変更で直す

共通の確かめ（P1 の `PrefabContractTests` が自動で対象にする）: 全フィールドの参照が `null` でない、画面のルートの型名 = ファイル名、`Screens` グループにアドレス = ファイル名で登録・ラベル無し、`Screens` に画面以外が無い。

実行: Editor を開いたまま `TestRunnerApi` を eval で回す（前提 3）。

Editor でしか確かめられないもの（自動のテストにしない）: 見た目と日本語・記号の字形（タスク 5 の手動）。Addressables からの読み込みと画面の動きは、Presenter ができる C# の計画（formation・week・lesson）の Play モードと Windows の IL2CPP で確かめる。

## 7. 未決事項

なし。ユーザーの確認は全件了承済み（2026-11-07）。formation・week・lesson（C#）の計画は、この計画の 3.2・4章のフィールドと行の View を使う形に直してある。
