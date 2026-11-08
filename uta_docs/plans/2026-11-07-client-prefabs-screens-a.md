---
title: 実装計画: client/ の Prefab 2/3 Title・Home・ガチャ・所持一覧
status: done
updated: 2026-11-08
spec: ../wiki/architecture/screen-design.md
---

# 実装計画: client/ の Prefab 2/3 Title・Home・ガチャ・所持一覧

## この計画で決めたこと

ユーザーは、この節と次の「ユーザーの確認が要ること」だけを読めば判断できるように書く。

1. **作る画面の Prefab は 7 個**（`Assets/Prefabs/Screens/<機能>/`。Addressables の `Screens` に登録）: `TitlePage`・`HomePage`・`GachaPage`・`GachaRatesModal`・`GachaResultModal`・`OwnedPage`・`SupportDetailPage`。**行の Prefab は 3 個**（`Assets/Prefabs/Parts/`。登録しない）: `GachaRateHeadingRow`・`GachaRateRow`・`GachaResultRow`。所持一覧の行は P1 の `ListRow`（名前・補足の 2 段）をそのまま使う
2. **View の骨格は 8 型**（フィールドだけ。メソッド・`IXxxView` の実装は C# の計画が足す）: 上の 7 画面の `XxxPage : PageBase`／`XxxModal : ModalBase` と、行の View `GachaLineRow : MonoBehaviour`（3 つの行の Prefab で共用）。フィールド名と Prefab 内の子のパスの対応は 3.3 の表が正
3. **ページの共通の形**: P1 の雛形（ルート + `Body`）の `Body` に縦のレイアウト（余白 0・間隔 0）を付け、上から `Header`（P1 の `Header`。Title・Home には無い）→（所持一覧だけ `TabBar`）→ `Main`（余白 32・間隔 24、残りの高さを全部使う）。中身は `Main` に置く（3.2）
4. **レベルのボタン（+1／+5／最大）は行の Prefab にしない**。数が 3 つで固定なので、`SupportDetailPage` に `Button` を 3 つ置き、ラベルを 32pt に下げる（「+1 → Lv 43（強化pt 5,140 / G 5,140）」が 1 行に入るように）
5. **所持一覧の並び・絞り込みのボタンは 2 段**（上段「並び」「昇順/降順」、下段「プラン」「タイプ」）。4 つを 1 段にすると 1 つの幅が約 240px で「タイプ: アシスト」が入らないため。下段を `FilterRow` にまとめ、C# は「プラン・タイプのボタン」を出す・隠すときに `FilterRow` ごと切り替える
6. **「ありません」（0 件）は一覧の外の文字 `EmptyText`**（Prefab では非表示）。C# は 0 件のとき表示する（旧 owned の「Content の中に置く」から変える。行の Instantiate と混ぜないため）
7. **生成クラスは 10 個**（`Scripts/Editor/Prefabs/Screens/<機能>/`、名前空間はどのフォルダでも `Uta.Client.Editor.Prefabs.Screens`）と、画面の生成の共通の補助 `ScreenLayout`（同じ名前空間。文字の型と、`Body`・`Main`・横並び・固定高の文字を作る関数）。P1 の `PrefabBuildCommands.BuildAll()` が `TypeCache` で拾うので、登録は要らない。**`ScreenLayout` は P3 も使う**（画面の組み方の補助はこれが正。P3 は重ねて作らず、プロデュースの画面だけに要る関数を P3 の `ProduceLayout` に足す）
8. **画面ごとのテスト**: 「View のフィールドが、表の子（のコンポーネント）を指している」ことを全フィールドについて確かめる（`TitlePrefabTests`・`HomePrefabTests`・`GachaPrefabTests`・`OwnedPrefabTests`。補助 `ScreenPrefabAssert`）。C# の計画が表のフィールドを使うときの食い違いを、ここで止める
9. **この計画は P1 の型だけに依存し、C#（foundation）の型に依存しない**。計画の順は「計画 0 → P1 → **P2（この計画）** → foundation → gacha → owned → P3 → formation → week → lesson」（2026-11-07 に決定）。foundation（C#）は、この計画が作る `TitlePage`・`HomePage` の骨格に `ITitleView`・`IHomeView` を実装する
10. 叩き台（screen-wireframes）との差: ガチャの結果は絵のグリッドではなく 1 枠 1 行の文字、ガチャ名の行と画像・「交換」ボタンは無し（gacha の C# 計画の範囲どおり）。ホームの「プロデュース中」の帯は週を出さない（foundation の C# 計画どおり）
11. **Title シーンのページは `TitlePage`**（タイトルの文字・状態の文・「スタート」のボタン）。旧 foundation の `StartupPage`（「起動中…」だけのページ）は作らない。foundation（C#）が `TitlePage` に `ITitleView`（`SetStatus`・`SetStartInteractable`・`StartClicked`。foundation 3.7）を実装する
12. **`HomePage.resumePanel` の型は `GameObject`**（foundation 3.7 の `SetResumeVisible` が `resumePanel.SetActive` を呼ぶ）
13. **タイトルの文字を「Uta」にしてよいか**（プロジェクト名。既存作品の名前ではない。ロゴの画像は作らない）（ユーザーが推奨どおり了承した。2026-11-07）
14. 見た目の値（寸法・フォントサイズ・色の割り当て）は仮。P1 の `UiTheme` の範囲で決めた（3.4）。この値で進めてよいか（ユーザーが推奨どおり了承した。2026-11-07）
15. **ホームの構成（2026-11-07 追加。ユーザーの判断。screen-design 7.8）**: 上から `ProfileText`（プレイヤーLv・ガチャ通貨）→ キャラ表示のステージ `Stage`（`RawImage`。子に枠の文字 `StageLabel`）→ 「プロデュース中」の帯 `ResumePanel`（文字だけ。ボタンは無い）→ 大きな主ボタン `PrimaryButton`（高さ 200・文字 56）→ 下部のナビ `Nav`（`NavProduceButton`・`NavGachaButton`・`NavOwnedButton`）。**旧 `Menu`・`ResumePanel/ResumeButton` は作らない**。View のフィールドの対応: `produceButton` → `navProduceButton`、`gachaButton` → `navGachaButton`、`ownedButton` → `navOwnedButton` に改名（子も `Menu/ProduceButton` → `Nav/NavProduceButton` などに改名）。`resumeButton` は削除（再開は主ボタンが「再開」の表示になり、押すと「プロデュース」と同じ選択が出る）。`stageImage`（`RawImage`）・`stageLabel`（`TMP_Text`）・`primaryButton`（`ButtonView`）を新しく足す。`profileText`・`resumePanel` は残す。ステージの `RawImage` は MVP ではテクスチャなしの色つきの枠で、将来 3D モデルを描いた `RenderTexture` を割り当てる（C# は触らない。差し替えは foundation の `ICharacterStage`）

## ユーザーの確認が要ること

なし（全件、2026-11-07 にユーザーが推奨どおり了承した。「この計画で決めたこと」の末尾に移した）。

## 1. 目的と前提

[game-spec.md](../wiki/overview/game-spec.md) の順 7（client/ の画面）のうち、Prefab の 2 本目。Title・Home・ガチャ（ページ・排出率・結果）・所持一覧・サポートカードの詳細の **Prefab と View の骨格**を、P1 の生成の仕組みで作る。方針の正は [screen-design.md](../wiki/architecture/screen-design.md) 7章（7.1 Prefab は Editor のスクリプトで生成、7.2 Addressables、7.4 部品と置き場・命名）と 8章（シーン構成）。並べるものは [screen-wireframes.md](../wiki/overview/screen-wireframes.md) の「共通の方針」「ホーム」「9. ガチャ」「10. アイドル・サポートの一覧と強化」と、C# の計画の `IXxxView`（[foundation](2026-11-03-client-screens-foundation.md) 3.7 の `IHomeView`、[gacha](2026-11-03-client-screens-gacha.md) 3.4 の `IGachaView`・`IGachaRatesView`・`IGachaResultView`、[owned](2026-11-03-client-screens-owned.md) 3.6・3.7 の `IOwnedView`・`ISupportDetailView`）から導く。

- 関係する ADR: [0009](../wiki/decisions/0009-client-stack.md)（uGUI + UnityScreenNavigator・縦画面）、[0006](../wiki/decisions/0006-adopt-unity-7-early.md)（Unity 7）、[0001](../wiki/decisions/0001-ip-policy.md)（文言・名前はオリジナル）
- **使う P1（[2026-11-07-client-prefabs-common.md](2026-11-07-client-prefabs-common.md)）の型**: `PageBase`・`ModalBase`・`UiTheme`、部品の View（`ButtonView`・`HeaderView`・`TabBarView`・`ListRowView`）、部品の Prefab（`Label`・`Button`・`Panel`・`Header`・`Tab`・`TabBar`・`ListRow`）、Editor の `PrefabBuildUtil`・`RectSpec`・`LayoutSpec`・`TextSpec`・`ButtonStyle`・`IPrefabBuilder`・`PrefabBuildCommands`、テストの `PrefabContract`。名前とシグネチャは P1 の 3.3・3.5・3.6 が正
- **前提 1**: P1 が done（部品の Prefab 10 個があり、`BuildAll()` が `built 10` を返し、EditMode のテストが通る。テストの回し方は P1 の決めたこと 12）
- **前提 2**: Unity 7（`7000.0.0a12`）の Editor が、この worktree の `client/uta` を開いていること（`unity status --until-ready` が ready）。**Editor が無い環境ではこの計画のどのタスクも実行できない**。実行できないときは「確認できていない」と書いてユーザーに実行を頼む（推測で「通った」と書かない）
- **この計画で決めないこと**: View のメソッド（`Set…`・イベント・`IXxxView` の実装）、Presenter、Args、ユースケース、シーンへの配置・DI（foundation・gacha・owned の C# の計画）。各シーンの `LifetimeScope` からのページの Push（foundation）。編成・プロデュースの画面（P3）。`SceneBuilder.cs` と既存の `TitleView`・`HomeView`（シーンの最小の uGUI。foundation が置き換える。この計画は触らない）

### 1.1 C# の計画との境界

| 項目 | この計画（P2） | C# の計画（foundation・gacha・owned） |
|---|---|---|
| View の型（`HomePage` など 7 つと `GachaLineRow`） | 作る。`[SerializeField] private` のフィールドだけ | 同じファイルに `IXxxView` の実装（`Set…`・イベント・`onClick` の購読）を足す。フィールドは足さない・名前を変えない（足りなければこの計画を直す） |
| Prefab の階層・値 | 全部（3.2） | 変えない |
| 行の Instantiate・`text` への代入 | しない（`rowPrefab` などの参照を用意するだけ） | `Object.Instantiate(rowPrefab, listContent)` してテキストを入れる |
| ボタンの `onClick` | Prefab に永続リスナーを張らない | `ButtonView` の button を購読する（P1 の方針） |
| 文言（Prefab の仮の文字） | 仮の値を入れる（3.2 の表の text） | 実行時に `Set…` で上書きする。固定の文言（見出し・ボタンの名前）は Prefab の値をそのまま使う |

## 2. 触るファイル

パスは、`client/uta/Assets/` で始まらないものはリポジトリのルートからの相対、それ以外は `client/uta/Assets/` からの相対。新しい `.cs`・フォルダ・アセットには Unity が作った `.meta` をコミットする。「生成」は Editor のスクリプトが作るもの（手で書かない・直さない）。

| 操作 | パス | 中身 |
|---|---|---|
| 作成 | `Scripts/Presentation/Title/TitlePage.cs` | View の骨格（3.1） |
| 作成 | `Scripts/Presentation/Home/HomePage.cs` | View の骨格 |
| 作成 | `Scripts/Presentation/Gacha/GachaPage.cs`・`GachaRatesModal.cs`・`GachaResultModal.cs`・`GachaLineRow.cs` | View の骨格（フォルダ `Gacha/` を作る） |
| 作成 | `Scripts/Presentation/Owned/OwnedPage.cs`・`SupportDetailPage.cs` | View の骨格（フォルダ `Owned/` を作る） |
| 作成 | `Scripts/Editor/Prefabs/Screens/ScreenLayout.cs` | 画面の生成の共通の補助（3.4） |
| 作成 | `Scripts/Editor/Prefabs/Screens/Title/TitlePageBuilder.cs` | 生成クラス（3.5） |
| 作成 | `Scripts/Editor/Prefabs/Screens/Home/HomePageBuilder.cs` | 生成クラス |
| 作成 | `Scripts/Editor/Prefabs/Screens/Gacha/GachaRowBuilders.cs`（3 クラス）・`GachaPageBuilder.cs`・`GachaRatesModalBuilder.cs`・`GachaResultModalBuilder.cs` | 生成クラス |
| 作成 | `Scripts/Editor/Prefabs/Screens/Owned/OwnedPageBuilder.cs`・`SupportDetailPageBuilder.cs` | 生成クラス |
| 生成 | `Prefabs/Screens/Title/TitlePage.prefab`・`Prefabs/Screens/Home/HomePage.prefab`・`Prefabs/Screens/Gacha/GachaPage.prefab`・`GachaRatesModal.prefab`・`GachaResultModal.prefab`・`Prefabs/Screens/Owned/OwnedPage.prefab`・`SupportDetailPage.prefab` | 画面の Prefab（Addressables の `Screens` に登録） |
| 生成 | `Prefabs/Parts/GachaRateHeadingRow.prefab`・`GachaRateRow.prefab`・`GachaResultRow.prefab` | 行の Prefab |
| 生成（変更） | `AddressableAssetsData/AssetGroups/Screens.asset` | 7 個のエントリ（`AddressablesSetup.RegisterScreen` が足す） |
| 作成 | `Tests/EditMode/ScreenPrefabAssert.cs`・`TitlePrefabTests.cs`・`HomePrefabTests.cs`・`GachaPrefabTests.cs`・`OwnedPrefabTests.cs` | 画面ごとの確かめ（3.6） |
| 変更 | `uta_docs/wiki/architecture/screen-design.md` | 7.4 の表・実装メモ（タスク 6） |
| 変更 | この計画 | `status: done`（タスク 6） |

asmdef は変えない（P1 の 3.1 で `Uta.Client.Presentation` に `Unity.TextMeshPro`、`Uta.Client.Editor` に `Unity.TextMeshPro`・Addressables、テストに `Uta.Client.Presentation`・`Uta.Client.Editor`・`Unity.TextMeshPro`・`UnityEngine.UI` が足されている。`Uta.Client.Editor` は既存の `SceneBuilder` が `Uta.Client.Presentation.Title` を使っているので参照済み）。

## 3. 型とシグネチャ

C# は `client/` の `.editorconfig` の範囲（C# 9 の範囲。ファイルスコープ namespace・プライマリコンストラクタ・コレクション式 `[...]`・Index/Range・record は使わない。配列は `new[] { ... }`）。BOM 付き UTF-8、LF、Allman、`var`、private フィールドは接頭辞なしの camelCase。

### 3.1 View の骨格（`Uta.Client.Presentation.<機能>`）

全部「`[SerializeField] private` のフィールドだけ」。メソッド・プロパティ・イベントを書かない（C# の計画が足す）。`using TMPro;`・`using UnityEngine;`・`using UnityEngine.UI;`（`RawImage`。`HomePage` だけ）・`using Uta.Client.Presentation.Common;`・`using Uta.Client.Presentation.Common.Parts;` のうち使うものを書く。フィールドが使われていない警告（CS0169・CS0414・CS0649）が出ても止まらない（C# の計画が使うと消える）。

形の例（`HomePage`）:

```csharp
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Common.Parts;

namespace Uta.Client.Presentation.Home
{
    /// <summary>ホームのページ（Prefab HomePage のルート）。フィールドだけ。IHomeView の実装は foundation（C#）が足す。</summary>
    public sealed class HomePage : PageBase
    {
        [SerializeField] private TMP_Text profileText;
        [SerializeField] private RawImage stageImage;          // キャラ表示のステージ。MVP はテクスチャなし。将来 RenderTexture を割り当てる（screen-design 7.8）
        [SerializeField] private TMP_Text stageLabel;
        [SerializeField] private GameObject resumePanel;
        [SerializeField] private ButtonView primaryButton;
        [SerializeField] private ButtonView navProduceButton;
        [SerializeField] private ButtonView navGachaButton;
        [SerializeField] private ButtonView navOwnedButton;
    }
}
```

| 型（ファイル） | 基底 | フィールド（宣言の順。`[SerializeField] private`） |
|---|---|---|
| `Uta.Client.Presentation.Title.TitlePage`（`Title/TitlePage.cs`） | `PageBase` | `TMP_Text statusText`、`ButtonView startButton` |
| `Uta.Client.Presentation.Home.HomePage`（`Home/HomePage.cs`） | `PageBase` | 上の例のとおり |
| `Uta.Client.Presentation.Gacha.GachaPage`（`Gacha/GachaPage.cs`） | `PageBase` | `HeaderView header`、`TMP_Text currencyText`、`TMP_Text lineupText`、`TMP_Text exchangeText`、`ButtonView ratesButton`、`ButtonView singleButton`、`ButtonView tenButton` |
| `Uta.Client.Presentation.Gacha.GachaRatesModal`（`Gacha/GachaRatesModal.cs`） | `ModalBase` | `RectTransform listContent`、`GachaLineRow headingRowPrefab`、`GachaLineRow lineRowPrefab`、`ButtonView closeButton` |
| `Uta.Client.Presentation.Gacha.GachaResultModal`（`Gacha/GachaResultModal.cs`） | `ModalBase` | `RectTransform listContent`、`GachaLineRow rowPrefab`、`ButtonView retryButton`、`ButtonView closeButton` |
| `Uta.Client.Presentation.Gacha.GachaLineRow`（`Gacha/GachaLineRow.cs`） | `MonoBehaviour` | `TMP_Text text` |
| `Uta.Client.Presentation.Owned.OwnedPage`（`Owned/OwnedPage.cs`） | `PageBase` | `HeaderView header`、`TabBarView tabBar`、`RectTransform controls`、`RectTransform filterRow`、`ButtonView sortButton`、`ButtonView orderButton`、`ButtonView planButton`、`ButtonView typeButton`、`TMP_Text emptyText`、`RectTransform listContent`、`ListRowView rowPrefab` |
| `Uta.Client.Presentation.Owned.SupportDetailPage`（`Owned/SupportDetailPage.cs`） | `PageBase` | `HeaderView header`、`TMP_Text infoText`、`TMP_Text resourcesText`、`ButtonView plus1Button`、`ButtonView plus5Button`、`ButtonView maxButton`、`ButtonView limitBreakButton`、`TMP_Text abilitiesText` |

- `GachaLineRow` は `public sealed class`。`[DisallowMultipleComponent]` を付ける
- `headingRowPrefab`・`lineRowPrefab`・`rowPrefab` は**アセットの参照**（`Assets/Prefabs/Parts/` の行の Prefab のルートのコンポーネント）。ほかは Prefab の中の子の参照
- `OwnedPage.rowPrefab` の型は P1 の `ListRowView`（行の Prefab は `Parts/ListRow.prefab`）
- `Uta.Client.Presentation.Title` には既存の `TitleView`・`TitlePresenter`・`TitleLifetimeScope`（シーンの最小の uGUI）がある。この計画では触らない（名前は衝突しない）。`Home` の `HomeView`・`HomePresenter` も同じ

### 3.2 Prefab の階層

#### 共通の書き方

- P1 の 3.4 の書き方（「全面」「上帯 h」「中央 w×h」「レイアウト任せ」、TMP の共通の値、Image・Button の共通の値、VLG／HLG の「制御 W/H」「伸ばす W/H」、LE、「→ 部品」のネスト）に従う。部品の値（`Button` の高さ 130 など）は P1 の表のとおりで、ここでは変える値だけを書く
- 文字の型は 3.4 の `ScreenLayout` の名前（`Line`・`Block` など）で書く。高さを「LE 70」と書いたものは `minHeight = preferredHeight = 70`（作り方は 3.5 の冒頭の 3 行）
- ページのルートと `Body` は P1 の `CreatePage`、モーダルのルートと `Panel` は `CreateModal` が作る（P1 の 3.4「ページ・モーダルの雛形」。ルート: 全面・Image `Background`・View、`Body`: 全面 + `SafeAreaFitter`。モーダル: ルート全面・Image なし、`Panel`: 中央 900×0・Image `Panel`・VLG 余白 40／間隔 24・`ContentSizeFitter`）
- 以下の表で `Body` と `Panel` の行は、P1 が作ったものに**足す**値だけを書く
- 「出どころ」: WF = [screen-wireframes.md](../wiki/overview/screen-wireframes.md)、F = foundation の計画、G = gacha の計画、O = owned の計画、`[P2]` = この計画で決めた値
- 子の名前は役割の PascalCase、フィールド名は子の名前の先頭を小文字にしたもの（screen-design 7.4）。例外は 3.3 の表に書く

**ページの共通の形**（`ScreenLayout.SetupBody`・`AddMain`。Title・Home は `Header` なし）:

| 階層 | コンポーネントと値 | 出どころ |
|---|---|---|
| `Body` | VLG（`ScreenLayout.BodyLayout`）: padding 0、spacing 0、childAlignment `UpperCenter`、制御 W/H true/true、伸ばす W/H true/false | [P2] |
| ├ `Header` | → `Header`（P1。LE 150。`TitleText` の text を画面ごとに変える） | WF 9・10 の「＜ 見出し」 |
| └ `Main` | RectTransform（レイアウト任せ）。VLG（`LayoutSpec.Column`）: padding 32、spacing 24、`UpperCenter`、制御 W/H true/true、伸ばす W/H true/false。LE flexibleHeight 1 | P1 `LayoutSpec.Column` |

`Main` の中の幅は、基準解像度で 1080 − 32 × 2 = 1016（Safe Area が狭い端末ではそれより狭い）。

#### TitlePage（`Prefabs/Screens/Title/TitlePage.prefab`）

| 階層 | コンポーネントと値 | 出どころ |
|---|---|---|
| `TitlePage` | 雛形のルート。View `TitlePage` | P1 |
| └ `Body` | ページの共通の形（`Header` なし） | [P2] |
| 　└ `Main` | ページの共通の形。ただし childAlignment は `MiddleCenter` | [P2] |
| 　　├ `TopSpacer` | RectTransform（レイアウト任せ）。LE flexibleHeight 1（`ScreenLayout.AddFlexibleSpacer(main, "TopSpacer")`） | [P2] |
| 　　├ `TitleText` | → `Label`。`Title`（56・`Text`・`Center`・折り返さない・省略）、text "Uta"。LE 120（`ScreenLayout.AddLabel(main, "TitleText", "Uta", ScreenLayout.Title, 120f)`） | 決めたこと 13 |
| 　　├ `MiddleSpacer` | RectTransform（レイアウト任せ）。LE flexibleHeight 1（`AddFlexibleSpacer`） | [P2] |
| 　　├ `StatusText` | → `Label`。`BlockCenter`（40・`Text`・`Center`・折り返す・省略）、text "タップしてスタート"。LE 160（3 行まで。`AddLabel(…, ScreenLayout.BlockCenter, 160f)`） | screen-design 8章（Title の状態の表示） |
| 　　├ `StartButton` | → `Button`（`Primary`）。Label "スタート" | screen-design 8章「スタートをタップ」 |
| 　　└ `BottomSpacer` | RectTransform（レイアウト任せ）。LE 120（`ScreenLayout.AddFixedSpacer(main, "BottomSpacer", 120f)`） | [P2] |

#### HomePage（`Prefabs/Screens/Home/HomePage.prefab`）

| 階層 | コンポーネントと値 | 出どころ |
|---|---|---|
| `HomePage` | 雛形のルート。View `HomePage` | P1 |
| └ `Body` | ページの共通の形（`Header` なし） | [P2] |
| 　└ `Main` | ページの共通の形 | [P2] |
| 　　├ `ProfileText` | → `Label`。`Line`（40・`Text`・`MidlineLeft`・折り返さない・省略）、text "プレイヤーLv -　ガチャ通貨 -"。LE 80（`ScreenLayout.AddLabel(main, "ProfileText", "プレイヤーLv -　ガチャ通貨 -", ScreenLayout.Line, 80f)`） | WF ホーム上部、F 3.7 の `SetProfile` |
| 　　├ `Stage` | RectTransform（レイアウト任せ。`PrefabBuildUtil.AddChild(main, "Stage")`）。`RawImage`（`stage.gameObject.AddComponent<RawImage>()`）: texture null、color `UiTheme.Panel`、raycastTarget false、uvRect (0, 0, 1, 1)（既定のまま）。`PrefabBuildUtil.MarkModified(stageImage)`。LE minHeight 600・flexibleHeight 1（`PrefabBuildUtil.SetLayout(stage.gameObject, minHeight: 600f, flexibleHeight: 1f)`。残りの高さを全部使う） | WF ホーム中央、screen-design 7.8、決めたこと 15 |
| 　　│└ `StageLabel` | → `Label`（全面のまま。`Stage` にレイアウトグループは無いので `Stage` いっぱいに広がる）。`Title`（56・`Text`・`Center`・折り返さない・省略）、text "キャラ表示"（`ScreenLayout.AddLabel(stage, "StageLabel", "キャラ表示", ScreenLayout.Title, 0f)`。LE は付けない） | screen-design 7.8（MVP の仮の枠の文字。C# の `PlaceholderCharacterStage` が育成アイドルの名前に替える） |
| 　　├ `ResumePanel` | → `Panel`（P1。VLG 余白 32・間隔 24）。LE 134（32 + 70 + 32。`PrefabBuildUtil.SetLayout(resumePanel.gameObject, minHeight: 134f, preferredHeight: 134f)`）。**アクティブのまま**（C# が `SetResumeVisible` で切り替える） | WF ホーム「プロデュース中」、F 3.7 の `SetResumeVisible` |
| 　　│└ `ResumeText` | → `Label`。`LineCenter`（40・`Text`・`Center`・折り返さない・省略）、text "プロデュース中"。LE 70（`ScreenLayout.AddLabel(resumePanel, "ResumeText", "プロデュース中", ScreenLayout.LineCenter, 70f)`。フィールドなし。固定の文字） | WF ホーム「プロデュース中」 |
| 　　├ `PrimaryButton` | → `Button`（`Primary`）。Label の文字の型を `Title`（56）に、text "プロデュース"（`ScreenLayout.SetButtonLabel(primary, "プロデュース", ScreenLayout.Title)`）。LE 200（`PrefabBuildUtil.SetLayout(primary.gameObject, minHeight: 200f, preferredHeight: 200f)`。部品の 130 を上書きする） | WF ホーム主ボタン、F 3.7 の `SetResumeVisible`（「再開」の表示）・`ProduceClicked` |
| 　　└ `Nav` | RectTransform（レイアウト任せ）。HLG（`LayoutSpec.Row`）: spacing 24、padding 0、`MiddleCenter`、制御 W/H true/true、伸ばす W/H true/true。LE 130（`ScreenLayout.AddRow(main, "Nav", 130f)`） | WF ホーム下部のナビ |
| 　　　├ `NavProduceButton` | → `Button`（`Secondary`）。Label "プロデュース" | F 3.7 の `ProduceClicked` |
| 　　　├ `NavGachaButton` | → `Button`（`Secondary`）。Label "ガチャ" | F 3.7 の `GachaClicked` |
| 　　　└ `NavOwnedButton` | → `Button`（`Secondary`）。Label "アイドル" | F 3.7 の `OwnedClicked` |

高さ（基準 1080×1920。`Body` が全面のとき）: `Main` の余白 32 + `ProfileText` 80 + 24 + `Stage` + 24 + `ResumePanel` 134 + 24 + `PrimaryButton` 200 + 24 + `Nav` 130 + 余白 32 = 704 + `Stage`。`Stage` は残りの 1920 − 704 = 1216（`ResumePanel` が非表示のときは、VLG が非アクティブの子を飛ばすので 1216 + 134 + 24 = 1374）。`Stage` の minHeight 600 を足しても 704 + 600 = 1304 で 1920 に収まる。ナビのボタン 1 つの幅は (1016 − 24 × 2) / 3 ≈ 322 で、「プロデュース」（40pt × 6 字 ≈ 240）が入る。主ボタンの「プロデュース」（56pt × 6 字 ≈ 336）は幅 1016 に入る。

#### GachaPage（`Prefabs/Screens/Gacha/GachaPage.prefab`）

| 階層 | コンポーネントと値 | 出どころ |
|---|---|---|
| `GachaPage` | 雛形のルート。View `GachaPage` | P1 |
| └ `Body` | ページの共通の形 | [P2] |
| 　├ `Header` | → `Header`。`TitleText` の text "ガチャ" | WF 9「＜ ガチャ」、G 1章（ガチャ名は固定の「ガチャ」） |
| 　└ `Main` | ページの共通の形 | [P2] |
| 　　├ `CurrencyText` | → `Label`。`LineRight`（40・`Text`・`MidlineRight`・折り返さない・省略）、text "ガチャ通貨 -"。LE 70（`ScreenLayout.AddLabel(main, "CurrencyText", "ガチャ通貨 -", ScreenLayout.LineRight, 70f)`） | WF 9 右上「ガチャ通貨 3,250」（ヘッダーの `RightSlot` は幅 200 で入らないので、ヘッダーの下の行にする [P2]） |
| 　　├ `LineupText` | → `Label`。`BlockCenter`（40・`Text`・`Center`・折り返す・省略）、text "SSR 育成アイドル: -"。LE minHeight 70・flexibleHeight 1（高さが伸びる文字: `var lineup = ScreenLayout.AddLabel(main, "LineupText", "SSR 育成アイドル: -", ScreenLayout.BlockCenter, 0f);` → `PrefabBuildUtil.SetLayout(lineup.gameObject, minHeight: 70f, flexibleHeight: 1f)`） | WF 9「SSR 育成アイドル: ○○」、G 3.4 `SetLineup` |
| 　　├ `ExchangeText` | → `Label`。`LineCenter`（40・`Text`・`Center`・折り返さない・省略）、text "交換pt - / -"。LE 70（`ScreenLayout.AddLabel(main, "ExchangeText", "交換pt - / -", ScreenLayout.LineCenter, 70f)`） | WF 9「交換pt 37 / 200」、G 3.4 `SetExchange` |
| 　　├ `RatesButton` | → `Button`（`Secondary`）。Label "排出率" | WF 9 [排出率] |
| 　　└ `DrawRow` | RectTransform（レイアウト任せ）。HLG（`LayoutSpec.Row`）。LE 130（`ScreenLayout.AddRow(main, "DrawRow", 130f)`） | WF 9 最下段 |
| 　　　├ `SingleButton` | → `Button`（`Primary`）。Label "単発" | WF 9 [単発 250] |
| 　　　└ `TenButton` | → `Button`（`Primary`）。Label "10連" | WF 9 [10連 2,500] |

#### GachaRatesModal（`Prefabs/Screens/Gacha/GachaRatesModal.prefab`）

| 階層 | コンポーネントと値 | 出どころ |
|---|---|---|
| `GachaRatesModal` | 雛形のルート。View `GachaRatesModal` | P1 |
| └ `Panel` | 雛形のまま（中央 900 幅・VLG 余白 40・間隔 24・高さは中身） | P1 |
| 　├ `TitleText` | → `Label`。`Title`、text "排出率"。LE 90（`ScreenLayout.AddLabel(panel, "TitleText", "排出率", ScreenLayout.Title, 90f)`） | G 3.4 |
| 　├ `List` | `PrefabBuildUtil.AddScrollList(panel, "List", out var scroll)`（P1。ScrollRect・`Viewport`・`Content`）→ `PrefabBuildUtil.SetLayout(scroll.gameObject, minHeight: 1200f, preferredHeight: 1200f)`（パネルは `ContentSizeFitter` なので flexible では高さが決まらない） | G 3.4（高さ 1200） |
| 　│└ `Viewport` / `Content` | P1 の `AddScrollList` のとおり。`Content` に子は置かない（C# が行を Instantiate する） | P1 |
| 　└ `CloseButton` | → `Button`（`Secondary`）。Label "閉じる" | G 3.4 |

パネルの高さ: 40 + 90 + 24 + 1200 + 24 + 130 + 40 = 1548（基準の高さ 1920 に入る）。

#### GachaResultModal（`Prefabs/Screens/Gacha/GachaResultModal.prefab`）

| 階層 | コンポーネントと値 | 出どころ |
|---|---|---|
| `GachaResultModal` | 雛形のルート。View `GachaResultModal` | P1 |
| └ `Panel` | 雛形のまま | P1 |
| 　├ `TitleText` | → `Label`。`Title`、text "結果"。LE 90（`ScreenLayout.AddLabel(panel, "TitleText", "結果", ScreenLayout.Title, 90f)`） | G 3.4 |
| 　├ `List` | `AddScrollList(panel, "List", out var scroll)` → `SetLayout(scroll.gameObject, minHeight: 1100f, preferredHeight: 1100f)`（10 行 × 96 + 間隔 24 × 9 = 1176 に近い高さ。はみ出す分はスクロール） | G 3.4（旧 900 から [P2] で変更: 行を 2 段にしたため） |
| 　└ `ButtonRow` | RectTransform（レイアウト任せ）。HLG（`LayoutSpec.Row`）。LE 130（`ScreenLayout.AddRow(panel, "ButtonRow", 130f)`） | WF 9 結果の最下段 |
| 　　├ `RetryButton` | → `Button`（`Primary`）。Label "もう一度" | WF 9 [もう一度] |
| 　　└ `CloseButton` | → `Button`（`Secondary`）。Label "閉じる" | WF 9 [閉じる] |

#### 行の Prefab（`Prefabs/Parts/`。ガチャ）

3 つとも同じ形: ルート 1 つだけ（子なし）。RectTransform 全面（レイアウト任せ）。`TextMeshProUGUI`（P1 の TMP の共通の値 + 下の文字の型）。LE（下の高さ）。`GachaLineRow`（text = 自分の TMP）。

| Prefab | 文字の型 | 仮の text | LE（高さ） | 使う画面 | 出どころ |
|---|---|---|---|---|---|
| `GachaRateHeadingRow` | `Heading`（40・`Accent`・`MidlineLeft`・折り返さない・省略） | "通常枠" | 64 | 排出率の見出しの行（`GachaRateLine.IsHeading`） | G 3.4（見出しは FontBody・Accent） |
| `GachaRateRow` | `SmallLine`（32・`Text`・`MidlineLeft`・折り返さない・省略） | "SSR 育成アイドル 0.000%" | 52 | 排出率の行 | G 3.4（行は FontSmall） |
| `GachaResultRow` | `SmallBlock`（32・`Text`・`TopLeft`・折り返す・省略） | "1. SSR 育成アイドル 名前　NEW" | 96（2 行） | 結果の 1 枠 1 行 | G 3.4。[P2] 32pt・2 行: 「1. SSR サポートカード 名前　サポート強化pt +50」は 40pt の 1 行（幅 820）に入らないため |

#### OwnedPage（`Prefabs/Screens/Owned/OwnedPage.prefab`）

| 階層 | コンポーネントと値 | 出どころ |
|---|---|---|
| `OwnedPage` | 雛形のルート。View `OwnedPage` | P1 |
| └ `Body` | ページの共通の形（`Header` と `Main` の間に `TabBar`） | [P2] |
| 　├ `Header` | → `Header`。`TitleText` の text "所持一覧" | O 3.6 見出し |
| 　├ `TabBar` | `PrefabBuildUtil.AddTabBar(body, "TabBar", new[] { "TabIdols", "TabSupport" }, new[] { "育成アイドル", "サポート" })`（P1。LE 110。`tabs` = [`TabIdols`, `TabSupport`]） | WF 10「[育成アイドル] [サポート]」、O 3.6 |
| 　└ `Main` | ページの共通の形 | [P2] |
| 　　├ `Controls` | RectTransform（レイアウト任せ）。VLG: padding 0、spacing 16、`UpperCenter`、制御 W/H true/true、伸ばす W/H true/false | WF 10「並び: [レア▼] 絞り込み: [プラン▼]」、決めたこと 5 |
| 　　│├ `SortRow` | RectTransform（レイアウト任せ）。HLG（`LayoutSpec.Row`）。LE 130（`ScreenLayout.AddRow(controls, "SortRow", 130f)`） | [P2] |
| 　　││├ `SortButton` | → `Button`（`Secondary`）。Label "並び: レア度" | O 3.6 `sortLabel` |
| 　　││└ `OrderButton` | → `Button`（`Secondary`）。Label "降順" | O 3.6 `orderLabel` |
| 　　│└ `FilterRow` | RectTransform（レイアウト任せ）。HLG（`LayoutSpec.Row`）。LE 130（`ScreenLayout.AddRow(controls, "FilterRow", 130f)`） | [P2] |
| 　　│　├ `PlanButton` | → `Button`（`Secondary`）。Label "プラン: 全て" | O 3.6 `planLabel` |
| 　　│　└ `TypeButton` | → `Button`（`Secondary`）。Label "タイプ: 全て" | O 3.6 `typeLabel` |
| 　　├ `EmptyText` | → `Label`。`LineCenter`、text "ありません"。LE 80（`ScreenLayout.AddLabel(main, "EmptyText", "ありません", ScreenLayout.LineCenter, 80f)`）。**GameObject を非アクティブ**（`SetActive(false)`） | O 3.6（0 件）、決めたこと 6 |
| 　　└ `List` | `AddScrollList(main, "List", out _)`（P1。LE flexibleHeight 1 は `AddScrollList` が付ける）。`Content` に子は置かない（C# が `rowPrefab` を Instantiate する） | O 3.6 |

行は P1 の `ListRow`（高さ 160。`TitleText` = 名前 + レア度、`SubText` = 補足、`RightText` = 使わない（C# が空にする））。選べない行は C# が `button.interactable = false` にする（P1 の Button の ColorTint の disabled で暗くなる）。

`Controls` の高さ: 130 + 16 + 130 = 276。`EmptyText`（非表示）を除き、`List` は残り全部（基準で 1920 − 150 − 110 − 32 − 276 − 24 − 32 ≈ 1296）。

#### SupportDetailPage（`Prefabs/Screens/Owned/SupportDetailPage.prefab`）

| 階層 | コンポーネントと値 | 出どころ |
|---|---|---|
| `SupportDetailPage` | 雛形のルート。View `SupportDetailPage` | P1 |
| └ `Body` | ページの共通の形 | [P2] |
| 　├ `Header` | → `Header`。`TitleText` の text "サポートカード" | O 3.7 |
| 　└ `Main` | ページの共通の形 | [P2] |
| 　　├ `InfoText` | → `Label`。`Block`（40・`Text`・`TopLeft`・折り返す・省略しない）、text "名前\nSSR　歌唱　共通\nLv 1 / 20\n上限解放 0 / 4"（`\n` は改行の文字）。LE 240（4 行。`ScreenLayout.AddLabel(main, "InfoText", …, ScreenLayout.Block, 240f)`） | WF 10 サポート詳細の上段、O 3.7 `SetInfo`（4 行） |
| 　　├ `ResourcesText` | → `Label`。`Line`、text "強化pt -　ゴールド -"。LE 70（`AddLabel(…, ScreenLayout.Line, 70f)`） | WF 10「強化pt 5,140 / G 5,140」、O 3.7 `SetResources` |
| 　　├ `Plus1Button` | → `Button`（`Primary`）。Label の文字の型を `SmallButton`（32・`Text`・`Center`・折り返さない・省略）に、text "+1" | WF 10 [+1]、決めたこと 4 |
| 　　├ `Plus5Button` | 同上。text "+5" | WF 10 [+5] |
| 　　├ `MaxButton` | 同上。text "最大" | WF 10 [最大] |
| 　　├ `LimitBreakButton` | → `Button`（`Primary`）。Label "上限解放（素材を選ぶ）"（40 のまま） | WF 10「上限解放 [素材を選ぶ]」、O 3.7 `SetLimitBreak` |
| 　　├ `AbilityHeaderText` | → `Label`。`Heading`、text "アビリティ"。LE 70（`AddLabel(…, ScreenLayout.Heading, 70f)`。フィールドなし。固定の見出し） | WF 10「アビリティ」、O 3.7 の 6 |
| 　　└ `AbilitiesText` | → `Label`。`SmallBlock`、text "1 初期歌唱 +0"。LE minHeight 100・flexibleHeight 1（高さが伸びる文字: `var abilities = ScreenLayout.AddLabel(main, "AbilitiesText", "1 初期歌唱 +0", ScreenLayout.SmallBlock, 0f);` → `PrefabBuildUtil.SetLayout(abilities.gameObject, minHeight: 100f, flexibleHeight: 1f)`） | WF 10「1 初期歌唱 +49」、O 3.7 `SetAbilities` |

高さ: 150 + 32 + 240 + 70 + 130 × 4 + 70 + 100 + 24 × 7 + 32 = 1382（残りは `AbilitiesText` が使う）。

### 3.3 値 → 部品 → View のフィールド（C# の計画はこの表を参照する）

子のパスは View の付いたルートからの相対（`Transform.Find` に渡す形）。「アセット」は `Assets/Prefabs/Parts/` の Prefab のルートのコンポーネントへの参照。

| 画面 | ワイヤー／仕様の値 | Prefab の部品（子のパス） | View のフィールド（型） | 使う `IXxxView` のメンバー（C# の計画） |
|---|---|---|---|---|
| Title | 状態の文（起動中・失敗・再試行） | `Body/Main/StatusText` | `statusText`（`TMP_Text`） | `SetStatus`（foundation 3.7） |
| Title | スタート | `Body/Main/StartButton` | `startButton`（`ButtonView`） | `StartClicked`・`SetStartInteractable`（foundation 3.7） |
| Home | プレイヤーLv・ガチャ通貨 | `Body/Main/ProfileText` | `profileText`（`TMP_Text`） | `SetProfile` |
| Home | キャラ表示のステージ（枠） | `Body/Main/Stage` | `stageImage`（`RawImage`） | なし（MVP は触らない。将来の `ModelCharacterStage` が `texture` を入れる。foundation 3.7 の `HomePage.CreateStage`） |
| Home | キャラ表示のステージ（枠の文字） | `Body/Main/Stage/StageLabel` | `stageLabel`（`TMP_Text`） | `HomePage.CreateStage` が `PlaceholderCharacterStage` に渡す（foundation 3.7） |
| Home | プロデュース中の帯 | `Body/Main/ResumePanel` | `resumePanel`（`GameObject`） | `SetResumeVisible` |
| Home | 主ボタン（プロデュース／再開） | `Body/Main/PrimaryButton` | `primaryButton`（`ButtonView`） | `ProduceClicked`・`SetResumeVisible`（ラベル） |
| Home | ナビ（プロデュース・ガチャ・アイドル） | `Body/Main/Nav/NavProduceButton`・`NavGachaButton`・`NavOwnedButton` | `navProduceButton`・`navGachaButton`・`navOwnedButton`（`ButtonView`） | `ProduceClicked`・`GachaClicked`・`OwnedClicked` |
| Gacha | ＜（戻る）・見出し | `Body/Header` | `header`（`HeaderView`。戻るは `header` の `backButton`） | `BackClicked` |
| Gacha | ガチャ通貨 | `Body/Main/CurrencyText` | `currencyText`（`TMP_Text`） | `SetCurrency` |
| Gacha | SSR の顔ぶれ | `Body/Main/LineupText` | `lineupText`（`TMP_Text`） | `SetLineup` |
| Gacha | 交換pt | `Body/Main/ExchangeText` | `exchangeText`（`TMP_Text`） | `SetExchange` |
| Gacha | 排出率 | `Body/Main/RatesButton` | `ratesButton`（`ButtonView`） | `RatesClicked` |
| Gacha | 単発・10連 | `Body/Main/DrawRow/SingleButton`・`TenButton` | `singleButton`・`tenButton`（`ButtonView`） | `SingleClicked`・`TenClicked`・`SetCosts` |
| GachaRates | 排出率の一覧 | `Panel/List/Viewport/Content` | `listContent`（`RectTransform`） | `SetLines` |
| GachaRates | 見出しの行・行 | アセット `GachaRateHeadingRow`・`GachaRateRow` | `headingRowPrefab`・`lineRowPrefab`（`GachaLineRow`） | `SetLines`（`IsHeading` で選ぶ） |
| GachaRates | 閉じる | `Panel/CloseButton` | `closeButton`（`ButtonView`） | `CloseClicked` |
| GachaResult | 結果の一覧 | `Panel/List/Viewport/Content` | `listContent`（`RectTransform`） | `SetLines` |
| GachaResult | 1 枠の行 | アセット `GachaResultRow` | `rowPrefab`（`GachaLineRow`） | `SetLines` |
| GachaResult | もう一度・閉じる | `Panel/ButtonRow/RetryButton`・`CloseButton` | `retryButton`・`closeButton`（`ButtonView`） | `RetryClicked`・`CloseClicked` |
| 行（ガチャ） | 1 行の文字 | ルート自身 | `GachaLineRow.text`（`TMP_Text`） | — |
| Owned | ＜・見出し（モードで変わる） | `Body/Header` | `header`（`HeaderView`） | `BackClicked`・`SetTitle` |
| Owned | タブ | `Body/TabBar`（`tabs[0]` = `TabIdols`、`tabs[1]` = `TabSupport`） | `tabBar`（`TabBarView`） | `SetTabs`・`TabSelected` |
| Owned | 並び・絞り込みの行 | `Body/Main/Controls` | `controls`（`RectTransform`） | `SetControls(visible, …)` |
| Owned | プラン・タイプの段 | `Body/Main/Controls/FilterRow` | `filterRow`（`RectTransform`） | `SetControls(…, filtersVisible)` |
| Owned | 並び・昇順/降順 | `Body/Main/Controls/SortRow/SortButton`・`OrderButton` | `sortButton`・`orderButton`（`ButtonView`） | `SortKeyClicked`・`SortOrderClicked`・`SetControls` のラベル |
| Owned | プラン・タイプ | `Body/Main/Controls/FilterRow/PlanButton`・`TypeButton` | `planButton`・`typeButton`（`ButtonView`） | `PlanFilterClicked`・`TypeFilterClicked`・`SetControls` のラベル |
| Owned | 0 件 | `Body/Main/EmptyText` | `emptyText`（`TMP_Text`） | `SetRows`（0 件で表示） |
| Owned | 一覧 | `Body/Main/List/Viewport/Content` | `listContent`（`RectTransform`） | `SetRows`・`RowClicked` |
| Owned | 行 | アセット `ListRow`（P1） | `rowPrefab`（`ListRowView`） | `SetRows` |
| SupportDetail | ＜・見出し | `Body/Header` | `header`（`HeaderView`） | `BackClicked` |
| SupportDetail | 名前・レア度・Lv・上限解放 | `Body/Main/InfoText` | `infoText`（`TMP_Text`） | `SetInfo`（行を `"\n"` でつなぐ） |
| SupportDetail | 強化pt・ゴールド | `Body/Main/ResourcesText` | `resourcesText`（`TMP_Text`） | `SetResources` |
| SupportDetail | +1・+5・最大 | `Body/Main/Plus1Button`・`Plus5Button`・`MaxButton` | `plus1Button`・`plus5Button`・`maxButton`（`ButtonView`） | `SetLevelButtons`・`Plus1Clicked`・`Plus5Clicked`・`MaxClicked` |
| SupportDetail | 上限解放 | `Body/Main/LimitBreakButton` | `limitBreakButton`（`ButtonView`） | `SetLimitBreak`・`LimitBreakClicked` |
| SupportDetail | アビリティ | `Body/Main/AbilitiesText` | `abilitiesText`（`TMP_Text`） | `SetAbilities`（行を `"\n"` でつなぐ） |

フィールド名の例外（子の名前の先頭を小文字にした形でないもの）: `header`（子 `Header` はそのまま）、`tabBar`（`TabBar`）、`listContent`（子 `Content`。一覧の入れ物と分かる名前にする）、`resumePanel`・`controls`・`filterRow`（そのまま）、`stageImage`（子 `Stage` の `RawImage`。型が分かる名前にする）。行の Prefab の参照は `rowPrefab`／`headingRowPrefab`／`lineRowPrefab`。

### 3.4 `ScreenLayout`（`Uta.Client.Editor.Prefabs.Screens`。`Scripts/Editor/Prefabs/Screens/ScreenLayout.cs`）

```csharp
using TMPro;
using UnityEngine;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Common.Parts;

namespace Uta.Client.Editor.Prefabs.Screens
{
    /// <summary>画面の生成クラスが共通で使う形と文字の型。P3 も使う（P3 は同じ役割の関数を作らない）。</summary>
    public static class ScreenLayout
    {
        /// <summary>Body の VLG: spacing 0、padding 0、UpperCenter、制御 true/true、伸ばす true/false。</summary>
        public static LayoutSpec BodyLayout => new LayoutSpec(0f, 0, 0, 0, 0, TextAnchor.UpperCenter, true, true, true, false);

        // 文字の型（TextSpec(fontSize, color, alignment, wrap, ellipsis)）
        public static TextSpec Title => TextSpec.Title;                                                                    // 56, Text, Center, false, true
        public static TextSpec Line => TextSpec.SingleLine;                                                                // 40, Text, MidlineLeft, false, true
        public static TextSpec LineRight => new TextSpec(UiTheme.FontBody, UiTheme.Text, TextAlignmentOptions.MidlineRight, false, true);
        public static TextSpec LineCenter => new TextSpec(UiTheme.FontBody, UiTheme.Text, TextAlignmentOptions.Center, false, true);
        public static TextSpec Block => new TextSpec(UiTheme.FontBody, UiTheme.Text, TextAlignmentOptions.TopLeft, true, false);
        public static TextSpec BlockCenter => new TextSpec(UiTheme.FontBody, UiTheme.Text, TextAlignmentOptions.Center, true, true);
        public static TextSpec Heading => new TextSpec(UiTheme.FontBody, UiTheme.Accent, TextAlignmentOptions.MidlineLeft, false, true);
        public static TextSpec SmallLine => new TextSpec(UiTheme.FontSmall, UiTheme.Text, TextAlignmentOptions.MidlineLeft, false, true);
        public static TextSpec SmallBlock => new TextSpec(UiTheme.FontSmall, UiTheme.Text, TextAlignmentOptions.TopLeft, true, true);
        public static TextSpec SmallButton => new TextSpec(UiTheme.FontSmall, UiTheme.Text, TextAlignmentOptions.Center, false, true);

        /// <summary>PrefabBuildUtil.AddVertical(body.gameObject, BodyLayout)。</summary>
        public static void SetupBody(RectTransform body);

        /// <summary>AddChild(parent, "Main") → AddVertical(LayoutSpec.Column) → SetLayout(flexibleHeight: 1)。戻り値は Main。</summary>
        public static RectTransform AddMain(Transform parent);

        /// <summary>AddChild(parent, name) → AddHorizontal(LayoutSpec.Row) → SetLayout(minHeight: height, preferredHeight: height, flexibleHeight: 0)。flexibleHeight 0 は、伸ばす H が true の HLG が 1 を返して親の VLG の余りを吸うのを防ぐ。</summary>
        public static RectTransform AddRow(Transform parent, string name, float height);

        /// <summary>AddChild(parent, name) → SetLayout(flexibleHeight: 1)。</summary>
        public static RectTransform AddFlexibleSpacer(Transform parent, string name);

        /// <summary>AddChild(parent, name) → SetLayout(minHeight: height, preferredHeight: height)。</summary>
        public static RectTransform AddFixedSpacer(Transform parent, string name, float height);

        /// <summary>PrefabBuildUtil.AddLabel(parent, name, text, spec) → height > 0 なら SetLayout(minHeight: height, preferredHeight: height)。戻り値は TMP。</summary>
        public static TextMeshProUGUI AddLabel(Transform parent, string name, string text, TextSpec spec, float height);

        /// <summary>button の子 "Label" の TMP（無ければ InvalidOperationException("Label not found: " + button.name)）に ApplyText(spec) し、text を入れる。MarkModified。</summary>
        public static void SetButtonLabel(ButtonView button, string text, TextSpec spec);

        /// <summary>AssetDatabase.LoadAssetAtPath&lt;GameObject&gt;(PrefabBuildUtil.PartPath(partName)) のルートの GetComponent&lt;T&gt;()。Prefab か T が無ければ InvalidOperationException("part not found: " + partName)。</summary>
        public static T LoadPart<T>(string partName) where T : Component;
    }
}
```

- ヘッダーの見出しは P1 の `AddHeader(parent, title)` が入れるので、`ScreenLayout` に見出しを変える関数は作らない
- `TextSpec` は P1 で `sealed class`（プロパティは get だけ）なので、毎回 `new` してよい
- `LayoutSpec` の引数の順は P1 の 3.5（`spacing, left, right, top, bottom, alignment, controlWidth, controlHeight, expandWidth, expandHeight`）

### 3.5 生成クラス（`Uta.Client.Editor.Prefabs.Screens`）

3.2 の表の読み方（生成クラスはこのとおりに書く）:
- **表の階層の順 = `AddChild`（と `AddLabel`・`AddRow`・`AddButtonPart` などの子を作る呼び出し）を呼ぶ順**。兄弟の順は表の上から下
- **表の「LE n」は `ScreenLayout.AddLabel`／`ScreenLayout.AddRow` の `height` 引数**（`minHeight = preferredHeight = n` になる）。高さが伸びる文字（「LE minHeight m・flexibleHeight 1」。`LineupText`・`AbilitiesText`）は `ScreenLayout.AddLabel(…, 0f)` で作り、そのあと `PrefabBuildUtil.SetLayout(label.gameObject, minHeight: m, flexibleHeight: 1f)`。文字でもボタンの行でもないもの（`ResumePanel`・`List`・`Stage`）と、部品の高さを上書きするボタン（`PrimaryButton`）は `PrefabBuildUtil.SetLayout` を直接呼ぶ（表のセルに書いた呼び出し）
- **`Style` の指定が無いボタンは `Primary`**（`AddButtonPart` の既定値）

すべて `public sealed class <名前>Builder : IPrefabBuilder`（引数なしのコンストラクター）。`Build()` は「`CreatePage`／`CreateModal`（行は `CreateRoot`）→ 3.2 の表の上から順に組む → `SetRef` で 3.1 のフィールドを入れる → `SavePrefab` → 画面だけ `RegisterScreen(同じパス)`」。`using` は `UnityEngine`・`UnityEngine.UI`（`RawImage`。`HomePageBuilder` だけ）・`TMPro`・`Uta.Client.Presentation.Common`（`UiTheme`）・`Uta.Client.Presentation.Common.Parts` と各画面の名前空間のうち使うもの（`Uta.Client.Editor.Prefabs` は親の名前空間なので書かなくてよい）。

| クラス（ファイル） | Order | `AssetPath` | 登録 |
|---|---|---|---|
| `GachaRateHeadingRowBuilder`（`Gacha/GachaRowBuilders.cs`） | 1000 | `PrefabBuildUtil.PartPath("GachaRateHeadingRow")` | しない |
| `GachaRateRowBuilder`（同） | 1000 | `PartPath("GachaRateRow")` | しない |
| `GachaResultRowBuilder`（同） | 1000 | `PartPath("GachaResultRow")` | しない |
| `TitlePageBuilder`（`Title/TitlePageBuilder.cs`） | 2000 | `PrefabBuildUtil.ScreenPath("Title", "TitlePage")` | する |
| `HomePageBuilder`（`Home/HomePageBuilder.cs`） | 2000 | `ScreenPath("Home", "HomePage")` | する |
| `GachaPageBuilder`（`Gacha/GachaPageBuilder.cs`） | 2000 | `ScreenPath("Gacha", "GachaPage")` | する |
| `GachaRatesModalBuilder`（`Gacha/GachaRatesModalBuilder.cs`） | 2000 | `ScreenPath("Gacha", "GachaRatesModal")` | する |
| `GachaResultModalBuilder`（`Gacha/GachaResultModalBuilder.cs`） | 2000 | `ScreenPath("Gacha", "GachaResultModal")` | する |
| `OwnedPageBuilder`（`Owned/OwnedPageBuilder.cs`） | 2000 | `ScreenPath("Owned", "OwnedPage")` | する |
| `SupportDetailPageBuilder`（`Owned/SupportDetailPageBuilder.cs`） | 2000 | `ScreenPath("Owned", "SupportDetailPage")` | する |

`GachaRowBuilders.cs` は 3 クラスと、共通の `internal static class GachaRowPrefab { public static void Build(string name, string sampleText, TextSpec spec, float height); }` を持つ。`Build` の中身:

```csharp
var root = PrefabBuildUtil.CreateRoot(name);
var text = PrefabBuildUtil.AddText(root, sampleText, spec);
PrefabBuildUtil.SetLayout(root, minHeight: height, preferredHeight: height);
var row = root.AddComponent<GachaLineRow>();
PrefabBuildUtil.SetRef(row, "text", text);
PrefabBuildUtil.SavePrefab(root, PrefabBuildUtil.PartPath(name));
```

各クラスの `Build()` は `GachaRowPrefab.Build("GachaRateHeadingRow", "通常枠", ScreenLayout.Heading, 64f)`、`("GachaRateRow", "SSR 育成アイドル 0.000%", ScreenLayout.SmallLine, 52f)`、`("GachaResultRow", "1. SSR 育成アイドル 名前　NEW", ScreenLayout.SmallBlock, 96f)`。

1 クラスの全文（`GachaRateHeadingRowBuilder`。残り 2 クラスは名前・`AssetPath`・`Build` の引数だけが違う）:

```csharp
// ファイルの先頭: using UnityEngine; using Uta.Client.Presentation.Gacha;（GachaRowPrefab が使う）
// PrefabBuildUtil・IPrefabBuilder は親の名前空間 Uta.Client.Editor.Prefabs にあるので using は要らない
namespace Uta.Client.Editor.Prefabs.Screens
{
    /// <summary>排出率の見出しの行（Parts/GachaRateHeadingRow.prefab）を作る。Addressables に登録しない。</summary>
    public sealed class GachaRateHeadingRowBuilder : IPrefabBuilder
    {
        private const string Name = "GachaRateHeadingRow";

        public int Order => 1000;

        public string AssetPath => PrefabBuildUtil.PartPath(Name);

        public void Build()
        {
            GachaRowPrefab.Build(Name, "通常枠", ScreenLayout.Heading, 64f);
        }
    }
}
```

`GachaRowPrefab` は同じファイル・同じ名前空間の `internal static class`（中身は上のコード）。

画面の `Build()` の例（`HomePageBuilder`。ほかも同じ書き方で 3.2 の表をなぞる）:

```csharp
public void Build()
{
    var page = PrefabBuildUtil.CreatePage<HomePage>("HomePage", out var body);
    ScreenLayout.SetupBody(body);
    var main = ScreenLayout.AddMain(body);

    var profile = ScreenLayout.AddLabel(main, "ProfileText", "プレイヤーLv -　ガチャ通貨 -", ScreenLayout.Line, 80f);

    var stage = PrefabBuildUtil.AddChild(main, "Stage");
    var stageImage = stage.gameObject.AddComponent<RawImage>();   // using UnityEngine.UI;
    stageImage.texture = null;
    stageImage.color = UiTheme.Panel;
    stageImage.raycastTarget = false;
    PrefabBuildUtil.MarkModified(stageImage);
    PrefabBuildUtil.SetLayout(stage.gameObject, minHeight: 600f, flexibleHeight: 1f);
    var stageLabel = ScreenLayout.AddLabel(stage, "StageLabel", "キャラ表示", ScreenLayout.Title, 0f);

    var resumePanel = PrefabBuildUtil.AddPanel(main, "ResumePanel");
    PrefabBuildUtil.SetLayout(resumePanel.gameObject, minHeight: 134f, preferredHeight: 134f);
    ScreenLayout.AddLabel(resumePanel, "ResumeText", "プロデュース中", ScreenLayout.LineCenter, 70f);

    var primary = PrefabBuildUtil.AddButtonPart(main, "PrimaryButton", "プロデュース");
    ScreenLayout.SetButtonLabel(primary, "プロデュース", ScreenLayout.Title);
    PrefabBuildUtil.SetLayout(primary.gameObject, minHeight: 200f, preferredHeight: 200f);

    var nav = ScreenLayout.AddRow(main, "Nav", UiTheme.ButtonHeight);
    var navProduce = PrefabBuildUtil.AddButtonPart(nav, "NavProduceButton", "プロデュース", ButtonStyle.Secondary);
    var navGacha = PrefabBuildUtil.AddButtonPart(nav, "NavGachaButton", "ガチャ", ButtonStyle.Secondary);
    var navOwned = PrefabBuildUtil.AddButtonPart(nav, "NavOwnedButton", "アイドル", ButtonStyle.Secondary);

    PrefabBuildUtil.SetRef(page, "profileText", profile);
    PrefabBuildUtil.SetRef(page, "stageImage", stageImage);
    PrefabBuildUtil.SetRef(page, "stageLabel", stageLabel);
    PrefabBuildUtil.SetRef(page, "resumePanel", resumePanel.gameObject);   // フィールドは GameObject
    PrefabBuildUtil.SetRef(page, "primaryButton", primary);
    PrefabBuildUtil.SetRef(page, "navProduceButton", navProduce);
    PrefabBuildUtil.SetRef(page, "navGachaButton", navGacha);
    PrefabBuildUtil.SetRef(page, "navOwnedButton", navOwned);

    PrefabBuildUtil.SavePrefab(page.gameObject, AssetPath);
    PrefabBuildUtil.RegisterScreen(AssetPath);
}
```

モーダルの `Build()` の骨格（`CreateModal` のあと `Main` を作らず、`panel` の直下に表の順で置く）:

```csharp
// GachaRatesModalBuilder: TitleText → List → CloseButton
public void Build()
{
    var modal = PrefabBuildUtil.CreateModal<GachaRatesModal>("GachaRatesModal", out var panel);

    ScreenLayout.AddLabel(panel, "TitleText", "排出率", ScreenLayout.Title, 90f);
    var content = PrefabBuildUtil.AddScrollList(panel, "List", out var scroll);
    PrefabBuildUtil.SetLayout(scroll.gameObject, minHeight: 1200f, preferredHeight: 1200f);
    var close = PrefabBuildUtil.AddButtonPart(panel, "CloseButton", "閉じる", ButtonStyle.Secondary);

    PrefabBuildUtil.SetRef(modal, "listContent", content);
    PrefabBuildUtil.SetRef(modal, "headingRowPrefab", ScreenLayout.LoadPart<GachaLineRow>("GachaRateHeadingRow"));
    PrefabBuildUtil.SetRef(modal, "lineRowPrefab", ScreenLayout.LoadPart<GachaLineRow>("GachaRateRow"));
    PrefabBuildUtil.SetRef(modal, "closeButton", close);

    PrefabBuildUtil.SavePrefab(modal.gameObject, AssetPath);
    PrefabBuildUtil.RegisterScreen(AssetPath);
}

// GachaResultModalBuilder: TitleText → List → ButtonRow（RetryButton が先、CloseButton が後）
public void Build()
{
    var modal = PrefabBuildUtil.CreateModal<GachaResultModal>("GachaResultModal", out var panel);

    ScreenLayout.AddLabel(panel, "TitleText", "結果", ScreenLayout.Title, 90f);
    var content = PrefabBuildUtil.AddScrollList(panel, "List", out var scroll);
    PrefabBuildUtil.SetLayout(scroll.gameObject, minHeight: 1100f, preferredHeight: 1100f);
    var buttonRow = ScreenLayout.AddRow(panel, "ButtonRow", UiTheme.ButtonHeight);
    var retry = PrefabBuildUtil.AddButtonPart(buttonRow, "RetryButton", "もう一度");
    var close = PrefabBuildUtil.AddButtonPart(buttonRow, "CloseButton", "閉じる", ButtonStyle.Secondary);

    PrefabBuildUtil.SetRef(modal, "listContent", content);
    PrefabBuildUtil.SetRef(modal, "rowPrefab", ScreenLayout.LoadPart<GachaLineRow>("GachaResultRow"));
    PrefabBuildUtil.SetRef(modal, "retryButton", retry);
    PrefabBuildUtil.SetRef(modal, "closeButton", close);

    PrefabBuildUtil.SavePrefab(modal.gameObject, AssetPath);
    PrefabBuildUtil.RegisterScreen(AssetPath);
}
```

`OwnedPageBuilder` の `Build()` の骨格（`SortRow`・`FilterRow` は `Controls` の子。4 つのボタンはどれも `Secondary`）:

```csharp
public void Build()
{
    var page = PrefabBuildUtil.CreatePage<OwnedPage>("OwnedPage", out var body);
    ScreenLayout.SetupBody(body);
    var header = PrefabBuildUtil.AddHeader(body, "所持一覧");
    var tabBar = PrefabBuildUtil.AddTabBar(body, "TabBar", new[] { "TabIdols", "TabSupport" }, new[] { "育成アイドル", "サポート" });
    var main = ScreenLayout.AddMain(body);

    var controls = PrefabBuildUtil.AddChild(main, "Controls");
    PrefabBuildUtil.AddVertical(controls.gameObject, new LayoutSpec(16f, 0, 0, 0, 0, TextAnchor.UpperCenter, true, true, true, false));
    var sortRow = ScreenLayout.AddRow(controls, "SortRow", 130f);
    var sort = PrefabBuildUtil.AddButtonPart(sortRow, "SortButton", "並び: レア度", ButtonStyle.Secondary);
    var order = PrefabBuildUtil.AddButtonPart(sortRow, "OrderButton", "降順", ButtonStyle.Secondary);
    var filterRow = ScreenLayout.AddRow(controls, "FilterRow", 130f);
    var plan = PrefabBuildUtil.AddButtonPart(filterRow, "PlanButton", "プラン: 全て", ButtonStyle.Secondary);
    var type = PrefabBuildUtil.AddButtonPart(filterRow, "TypeButton", "タイプ: 全て", ButtonStyle.Secondary);

    var empty = ScreenLayout.AddLabel(main, "EmptyText", "ありません", ScreenLayout.LineCenter, 80f);
    empty.gameObject.SetActive(false);
    var content = PrefabBuildUtil.AddScrollList(main, "List", out _);   // LE flexibleHeight 1 は AddScrollList が付ける

    PrefabBuildUtil.SetRef(page, "header", header);
    PrefabBuildUtil.SetRef(page, "tabBar", tabBar);
    PrefabBuildUtil.SetRef(page, "controls", controls);
    PrefabBuildUtil.SetRef(page, "filterRow", filterRow);
    PrefabBuildUtil.SetRef(page, "sortButton", sort);
    PrefabBuildUtil.SetRef(page, "orderButton", order);
    PrefabBuildUtil.SetRef(page, "planButton", plan);
    PrefabBuildUtil.SetRef(page, "typeButton", type);
    PrefabBuildUtil.SetRef(page, "emptyText", empty);
    PrefabBuildUtil.SetRef(page, "listContent", content);
    PrefabBuildUtil.SetRef(page, "rowPrefab", ScreenLayout.LoadPart<ListRowView>("ListRow"));

    PrefabBuildUtil.SavePrefab(page.gameObject, AssetPath);
    PrefabBuildUtil.RegisterScreen(AssetPath);
}
```

画面ごとの注意（3.2 の表に無い手順）:
- `TitlePageBuilder`: `Main` の VLG の `childAlignment` を `AddMain` のあとで `TextAnchor.MiddleCenter` にする（`main.GetComponent<VerticalLayoutGroup>().childAlignment = …` → `PrefabBuildUtil.MarkModified` は不要（ネストした部品ではない））
- ヘッダーのある画面: `var header = PrefabBuildUtil.AddHeader(body, "<見出し>");`（P1。名前は "Header"）を `AddMain` の**前**に呼ぶ（子の順番 = 表の順番）。`OwnedPageBuilder` は `AddHeader` → `AddTabBar` → `AddMain` の順
- `GachaRatesModalBuilder`・`GachaResultModalBuilder`・`OwnedPageBuilder`: 上の骨格のとおり（行の参照は Order 1000 で先に作られている）
- `SupportDetailPageBuilder`: レベルの 3 つは `AddButtonPart(main, "Plus1Button", "+1")` のあと `ScreenLayout.SetButtonLabel(button, "+1", ScreenLayout.SmallButton)`（`+5`・`最大` も同じ）
- `GachaPageBuilder`: `RatesButton` は `ButtonStyle.Secondary`
- 仮の text の中の「　」は全角の空白（U+3000）

### 3.6 画面ごとのテスト（EditMode。`Tests/EditMode/`。名前空間 `Uta.Client.Infrastructure.Tests`）

```csharp
/// <summary>画面の Prefab の確かめの補助（P3 も使う）。</summary>
public static class ScreenPrefabAssert
{
    /// <summary>AssetDatabase.LoadAssetAtPath&lt;GameObject&gt;(assetPath)。null なら Assert.Fail("prefab not found: " + assetPath)。</summary>
    public static GameObject Load(string assetPath);

    /// <summary>prefab のルートの GetComponent&lt;T&gt;()。null なら Assert.Fail(assetPath + ": root has no " + typeof(T).Name)。</summary>
    public static T RootView<T>(GameObject prefab) where T : Component;

    /// <summary>
    /// new SerializedObject(view).FindProperty(fieldName)（null なら Fail）の objectReferenceValue が、
    /// view.transform.Find(childPath)（null なら Fail(childPath + " not found")）の GameObject 上のコンポーネント（GameObject そのものでもよい）であること。
    /// 比べ方: objectReferenceValue が Component なら ((Component)value).gameObject == child.gameObject、
    /// GameObject なら（HomePage.resumePanel）(GameObject)value == child.gameObject、どちらでもない・null なら Fail。
    /// 失敗の文に view の型名・fieldName・childPath を入れる。
    /// </summary>
    public static void FieldIsChild(Component view, string fieldName, string childPath);

    /// <summary>同じく、objectReferenceValue の AssetDatabase.GetAssetPath が assetPath であること（Prefab のアセットの参照）。</summary>
    public static void FieldIsAsset(Component view, string fieldName, string assetPath);
}
```

| ファイル | テスト | 確かめること |
|---|---|---|
| `TitlePrefabTests.cs` | `TitlePage_FieldsPointToChildren` | `TitlePage` の 2 フィールドが 3.3 の表の子を指す |
| `HomePrefabTests.cs` | `HomePage_FieldsPointToChildren` | `HomePage` の 8 フィールドが 3.3 の表の子を指す（`profileText` → `Body/Main/ProfileText`、`stageImage` → `Body/Main/Stage`、`stageLabel` → `Body/Main/Stage/StageLabel`、`resumePanel` → `Body/Main/ResumePanel`、`primaryButton` → `Body/Main/PrimaryButton`、`navProduceButton`・`navGachaButton`・`navOwnedButton` → `Body/Main/Nav/NavProduceButton`・`NavGachaButton`・`NavOwnedButton`） |
| | `HomePage_StageIsPlaceholder` | `Body/Main/Stage` の `RawImage` の `texture` が null、`raycastTarget` が false。`Body/Main/Stage/StageLabel` の `TMP_Text.text` が "キャラ表示"。`Body/Main/PrimaryButton` の `LayoutElement.preferredHeight` が 200 |
| `GachaPrefabTests.cs` | `GachaPage_FieldsPointToChildren` | `GachaPage` の 7 フィールド（`header` → `Body/Header`） |
| | `GachaRatesModal_FieldsPointToChildrenAndRows` | `listContent` → `Panel/List/Viewport/Content`、`closeButton` → `Panel/CloseButton`、`headingRowPrefab` → `Assets/Prefabs/Parts/GachaRateHeadingRow.prefab`、`lineRowPrefab` → `…/GachaRateRow.prefab` |
| | `GachaResultModal_FieldsPointToChildrenAndRow` | `listContent`・`retryButton`・`closeButton`、`rowPrefab` → `…/GachaResultRow.prefab` |
| | `GachaRows_TextIsOwnTmp` | 3 つの行の Prefab のルートに `GachaLineRow` があり、`text` がルート自身の `TMP_Text`（`FieldIsChild(row, "text", "")` ではなく、`objectReferenceValue == row.GetComponent<TMP_Text>()` で見る） |
| `OwnedPrefabTests.cs` | `OwnedPage_FieldsPointToChildren` | `OwnedPage` の 10 フィールド（`rowPrefab` 以外）と、`rowPrefab` → `Assets/Prefabs/Parts/ListRow.prefab` |
| | `OwnedPage_TabsAreIdolsThenSupport` | `tabBar` の `tabs` の要素数が 2、`[0]` の GameObject 名 `TabIdols`、`[1]` が `TabSupport`（`SerializedObject` の `tabs.GetArrayElementAtIndex(i)`） |
| | `OwnedPage_EmptyTextIsInactive` | `Body/Main/EmptyText` の `activeSelf` が false |
| | `SupportDetailPage_FieldsPointToChildren` | `SupportDetailPage` の 8 フィールド |

- どのテストも `[Test]`（同期）。Prefab のパスは `"Assets/Prefabs/Screens/<機能>/<名前>.prefab"` の文字列で書く。テストのアセンブリは `Uta.Client.Editor` を参照している（P1 の 3.1）が、`PrefabBuildUtil.ScreenPath` などは使わない（期待値をテストの側で固定し、生成クラスのパスの書き間違いを見つけるため）
- 共通の確かめ（参照が null でない・ルートの View の名前・`Screens` グループに登録・アドレス = ファイル名）は P1 の `PrefabContractTests` が全 Prefab に行う。ここでは繰り返さない

## 4. タスク

各タスクの終わりに、Editor のコンパイルエラーが 0（`unity recompile --format json`）で、EditMode のテストが全部通る状態にする。Editor が開けないときは、どのタスクも「確認できていない」と書いて止まり、ユーザーに実行を頼む。

**テストの回し方**（以下「テストを回す」）: [P1](2026-11-07-client-prefabs-common.md) の決めたこと 12 のとおり、**Editor を開いたまま** `unity command --timeout 300 eval` で `TestRunnerApi` を呼ぶ（コマンドの正は [unity7-evaluation.md](../wiki/architecture/unity7-evaluation.md) 4章と CLAUDE.md「ビルド・テスト・実行」。結果は `client/uta/Temp/uta-test-results.xml` の `<test-run>` の `failed` が 0 であることを見る）。Editor は閉じない（`unity test` は Editor が開いていると使えないので使わない）。1 つのクラスに絞るときは、同じコマンドの `Filter` に `groupNames = new[] { "<テストのクラス名>" }` を足す。eval で回せないときは、Editor の Test Runner で EditMode を全部実行してもらい（手動）、確認できていないと報告する（止まらない）。**この計画ではプレイヤーのビルド・`BuildAllBatch` のバッチ起動を確かめない**（P1 の決めたこと 19）。

`BuildAll()` の呼び方（P1 の 3.5）: `unity command eval 'return Uta.Client.Editor.Prefabs.PrefabBuildCommands.BuildAll();'`。以下の件数は P1 の部品 10 個を含む（P3 が先に入っていれば、その分を足した数になる）。

### タスク 1: View の骨格

触る: `Scripts/Presentation/Title/TitlePage.cs`・`Home/HomePage.cs`・`Gacha/` の 4 ファイル・`Owned/` の 2 ファイル。

手順:
1. 3.1 の表のとおりに 8 型を作る（フィールドだけ）
2. `unity recompile --format json` でエラー 0（CS0169・CS0414・CS0649 の警告は残ってよい）
3. `BuildAll()` が `built 10`（まだ生成クラスが無いので P1 の分だけ）

終わったことの確かめ方: コンパイルエラー 0。既存のテストが通る。

### タスク 2: 共通の補助と Title・Home

触る: `Scripts/Editor/Prefabs/Screens/ScreenLayout.cs`・`Title/TitlePageBuilder.cs`・`Home/HomePageBuilder.cs`、生成物 `Prefabs/Screens/Title/TitlePage.prefab`・`Prefabs/Screens/Home/HomePage.prefab`、`AddressableAssetsData/`、`Tests/EditMode/ScreenPrefabAssert.cs`・`TitlePrefabTests.cs`・`HomePrefabTests.cs`。

手順:
1. 3.4・3.5 のとおりに作る
2. `BuildAll()` が `built 12`。`unity command console get --tail 50 --level error --format json` にエラーが無い
3. 3.6 の `ScreenPrefabAssert` と Title・Home のテストを作り、テストを回して全部通る（P1 の `PrefabContractTests` の `ScreenPrefabs_RootViewIsNamedAfterPrefab`・`ScreenPrefabs_AreRegisteredInScreensGroup` が、この 2 個を対象に通ることを含む）
4. ユーザーに、Editor で `TitlePage.prefab`・`HomePage.prefab` を Prefab モードで開き、3.2 の表の並びになっていること・日本語の仮の文字が出ていることを見てもらう（手動）
5. コミットの前に `unity command eval 'Uta.Client.Editor.Prefabs.FontAssets.ClearDynamicData(); return "ok";'`（`BuildAll()` は最後に同じ処理を呼ぶが、手順 4 の表示でフォントのアトラスに字が足されるため。それでも `Assets/Fonts/NotoSansJP/` の `.asset` に差分が残ったら、コミットせず `git restore` で戻す。P1 の決めたこと 4）

境界: `Prefabs/Screens/` が無い状態からでもフォルダごとできる（`SavePrefab` の `EnsureFolder`）。2 回目の `BuildAll()` で Prefab の GUID と Addressables のエントリが重複しない（`git diff` で `Screens.asset` のエントリが 2 個のまま）。

終わったことの確かめ方: `built 12`、テストが通る、手動の見た目の確認が済んだ。

### タスク 3: ガチャ

触る: `Scripts/Editor/Prefabs/Screens/Gacha/` の 4 ファイル、生成物 `Prefabs/Parts/GachaRate*Row.prefab`・`GachaResultRow.prefab`・`Prefabs/Screens/Gacha/*.prefab`、`AddressableAssetsData/`、`Tests/EditMode/GachaPrefabTests.cs`。

手順:
1. 3.5 のとおりに 6 クラスを作る
2. `BuildAll()` が `built 18`。コンソールにエラーが無い
3. `GachaPrefabTests` を作り、テストを回して全部通る（`ScreensGroup_ContainsOnlyScreenPrefabs` が通る = 行の Prefab が `Screens` に入っていない）
4. ユーザーに、`GachaPage`・`GachaRatesModal`・`GachaResultModal` を Prefab モードで開いて見てもらう（手動。モーダルの `Panel` の高さが 排出率 1548・結果 1448 前後になっていること）
5. `ClearDynamicData`（タスク 2 の 5 と同じ）

終わったことの確かめ方: `built 18`、テストが通る、手動の確認が済んだ。

### タスク 4: 所持一覧・サポートカードの詳細

触る: `Scripts/Editor/Prefabs/Screens/Owned/` の 2 ファイル、生成物 `Prefabs/Screens/Owned/*.prefab`、`AddressableAssetsData/`、`Tests/EditMode/OwnedPrefabTests.cs`。

手順:
1. 3.5 のとおりに 2 クラスを作る
2. `BuildAll()` が `built 20`。コンソールにエラーが無い
3. `OwnedPrefabTests` を作り、テストを回して全部通る
4. ユーザーに、`OwnedPage`・`SupportDetailPage` を Prefab モードで開いて見てもらう（手動。`EmptyText` が非表示、タブが 2 つ並ぶ、レベルのボタンの文字が 32pt）
5. `ClearDynamicData`

終わったことの確かめ方: `built 20`、テストが通る、手動の確認が済んだ。

### タスク 5: 再生成と壊したときの確かめ

触る: なし（確かめだけ）。

手順（すべて Editor を開いたまま行う）:
1. `BuildAll()` をもう一度実行し、`git status`・`git diff --stat` で `Prefabs/Screens/`・`Prefabs/Parts/Gacha*`・`AddressableAssetsData/` に差分が出るかを見る。**出ても止まらない**（P1 のタスク 4 と同じ扱い）。差分の有無と、出たならどの値か（`m_` のどの値か・fileID か）を報告に書く
2. わざと壊す（Prefab の子を改名して保存する）。次の 1 式を実行し、`renamed` が返る（`unity command eval` は `return` が要る。見つからなければ `not found` が返るので、止まって報告する）:
   ```
   unity command eval 'var path = "Assets/Prefabs/Screens/Home/HomePage.prefab"; var root = UnityEditor.PrefabUtility.LoadPrefabContents(path); try { var child = root.transform.Find("Body/Main/Nav/NavGachaButton"); if (child == null) { return "not found"; } child.name = "NavGachaButton2"; UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, path); return "renamed"; } finally { UnityEditor.PrefabUtility.UnloadPrefabContents(root); }'
   ```
   - `NavGachaButton` は `Button` の Prefab をネストしたインスタンスなので、名前の変更は `HomePage.prefab` の中のオーバーライド（`m_Name` の property modification）として保存される。`Button.prefab` そのものは変わらない（`git status` で `Parts/Button.prefab` に差分が無いことを見る）
   - 続けて `unity command eval 'UnityEditor.AssetDatabase.SaveAssets(); return "ok";'`
3. テストを回す（`Filter` に `groupNames = new[] { "HomePrefabTests" }`。絞り込みは P1 のタスク 5 で効いた）。`uta-test-results.xml` で `HomePage_FieldsPointToChildren` が失敗し、失敗の文に `navGachaButton` と `Body/Main/Nav/NavGachaButton` が入る。絞り込みが効かない（0 件しか走らない・全件が走る）ときは、絞らずに全件を回し、失敗したテストの名前が `HomePage_FieldsPointToChildren` だけであること（ほかに失敗が無いこと）を見る。どちらで確かめたかを報告に書く
4. `BuildAll()`（eval）で元に戻す。`built 20`
5. テストを回す（全件）。全部通る
6. `git status` で `Prefabs/Screens/Home/HomePage.prefab` に手順 2 の改名が残っていないこと。`Fonts/NotoSansJP/` の差分は `git restore` で戻す

終わったことの確かめ方: 手順 3 で壊すと失敗し、手順 5 で戻すと通る。

**この計画では Play モードとプレイヤーのビルドで画面を出さない**（Presenter と Push は C# の計画が作る。画面が Addressables から読めることの確かめは、foundation・gacha・owned の C# の計画のタスクで行う。P1 のスパイクで `AddressableAssetLoader` の Push は確かめ済み）。

### タスク 6: 結果の記録

触る: `uta_docs/wiki/architecture/screen-design.md`、この計画。

手順:
1. screen-design.md（frontmatter の `updated` を実施日に）:
   - 7.4 の表の「Prefab」の行に「画面の Prefab: `Title/TitlePage`・`Home/HomePage`・`Gacha/GachaPage`・`GachaRatesModal`・`GachaResultModal`・`Owned/OwnedPage`・`SupportDetailPage`（P2 の計画 3.2。フィールドと子の対応は同 3.3）」を足す。「P2 の計画」は、この計画（`uta_docs/plans/2026-11-07-client-prefabs-screens-a.md`）への相対リンクにする。screen-design.md は `uta_docs/wiki/architecture/` にあるので、リンク先は `../../plans/2026-11-07-client-prefabs-screens-a.md`
   - 実装メモに、叩き台との差（決めたこと 10）と、タスク 5 の再生成の差分の結果、実装と計画のズレ（あれば）を書く
2. `python -I .claude/skills/linting-wiki/lint_wiki.py` がエラー 0
3. この計画の frontmatter を `status: done`、`updated` を実施日にする

終わったことの確かめ方: Lint がエラー 0。

## 5. テスト

| ファイル | テスト | 確かめること |
|---|---|---|
| `TitlePrefabTests.cs` | `TitlePage_FieldsPointToChildren` | 3.6 |
| `HomePrefabTests.cs` | `HomePage_FieldsPointToChildren`・`HomePage_StageIsPlaceholder` | 3.6 |
| `GachaPrefabTests.cs` | `GachaPage_FieldsPointToChildren`・`GachaRatesModal_FieldsPointToChildrenAndRows`・`GachaResultModal_FieldsPointToChildrenAndRow`・`GachaRows_TextIsOwnTmp` | 3.6 |
| `OwnedPrefabTests.cs` | `OwnedPage_FieldsPointToChildren`・`OwnedPage_TabsAreIdolsThenSupport`・`OwnedPage_EmptyTextIsInactive`・`SupportDetailPage_FieldsPointToChildren` | 3.6 |
| P1 の `PrefabContractTests.cs`（変えない） | `AllPrefabs_SerializedReferences_AreAssigned`・`ScreenPrefabs_RootViewIsNamedAfterPrefab`・`ScreenPrefabs_AreRegisteredInScreensGroup`・`ScreensGroup_ContainsOnlyScreenPrefabs` | この計画の 10 個の Prefab が自動で対象になる |

実行: 4章の冒頭の「テストの回し方」（Editor を開いたまま `TestRunnerApi` を eval で回す。P1 の決めたこと 12）。

Editor でしか確かめられないもの（自動のテストにしない）:
- 見た目（並び・文字が日本語で出る・はみ出し） → タスク 2〜4 の手動の確認
- Safe Area での縮み（ノッチのある端末）・Windows の 9:16 → C# の計画で画面を出せるようになってから（Android・iOS の実機は順 7 の範囲外）

## 6. 未決事項

いまは ready。ユーザーの確認は全件了承済み（2026-11-07）。

- 他の計画への影響は反映済み（2026-11-07）: foundation（C#）は `TitlePage`・`HomePage` の骨格に `ITitleView`・`IHomeView` を実装し、部品の View の表示のメソッド（`ButtonView.SetLabel`・`SetInteractable`、`HeaderView.SetTitle` など。foundation 3.2）を足す。gacha・owned（C#）は 3.3 の表のフィールドを使う。P3 は `ScreenLayout` を使う
- ホームの構成の変更（決めたこと 15。2026-11-07）は、foundation の 3.7・3.9・5章（`IHomeView` のイベントが 3 つ、`ICharacterStage`・`PlaceholderCharacterStage`、`HomePage.CreateStage`）と、gacha・owned・formation の `HomePresenter` の節に反映済み
