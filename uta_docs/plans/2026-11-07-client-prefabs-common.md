---
title: 実装計画: client/ の Prefab 1/3 共通部品・Addressables・TextMeshPro
status: done
updated: 2026-11-08
spec: ../wiki/architecture/screen-design.md
---

# 実装計画: client/ の Prefab 1/3 共通部品・Addressables・TextMeshPro

## この計画で決めたこと

ユーザーは、この節と次の「ユーザーの確認が要ること」だけを読めば判断できるように書く。

1. **最初のタスクはスパイク（止まる条件つき）**。Addressables 4.1.2 と TextMeshPro（日本語の動的 SDF フォント）が Unity 7（`7000.0.0a12`）の Editor の Play と Windows の IL2CPP のプレイヤーで動くかを、**使い捨ての専用シーン `Assets/Spike/Spike.unity`** で確かめる（Editor の Play もこのシーンから始める。サーバー・`Bootstrap` は要らず、シーンの切り替えでプローブが消えない）。動かなければ止まって報告する（Resources・legacy `Text` などへ勝手に変えない）。確かめ終えたら使い捨てのファイルは消す
2. **Unity 7 の TMP は Essential Resources の取り込みが無くなった**（uGUI 7.0.0 の CHANGELOG: 既定のフォント・シェーダー・TMP Settings がパッケージに同梱され、取り込みの窓とメニューは削除）。そのため screen-design 7.3 の「Essential Resources を取り込む」はやらず、代わりに**プロジェクトの TMP Settings（`Assets/TextMesh Pro/Resources/TMP Settings.asset`）を Editor のスクリプトで作り、既定のフォントを Noto Sans JP の動的 SDF にする**（決めたこと 16）
3. **フォントは作成済みのものを使う（この計画では作らない）**。別の作業（PR #29。ブランチ `claude/noto-sans-jp-font`、コミット `a86f436`。この計画の前に main へ入っている前提: 前提 2・決めたこと 18）が `Assets/Fonts/NotoSansJP/` に Noto Sans JP の静的な ttf（Regular・Bold）・`OFL.txt`・TMP のフォントアセット 2 つを置いた: `NotoSansJP-Regular SDF.asset`・`NotoSansJP-Bold SDF.asset`（Atlas Population Mode = Dynamic、Multi Atlas 有効、サンプリング 48、パディング 5、アトラス 1024、Clear Dynamic Data On Build 有効。シェーダーは `TextMeshPro/Mobile/Distance Field`）。この計画はアセットを作り直さず、設定も変えない。文字は Regular、**太字は Bold の SDF アセットを font に入れる**（`FontStyles.Bold` の疑似太字は使わない。`fontStyle` は常に `Normal`）
4. **フォントの読み込みは Prefab からのシリアライズ参照**。`TMP_Text.font` は生成スクリプトが Prefab に入れる（Regular か Bold のアセットへの参照）。TMP Settings の既定フォントも同じ Regular のアセット。**フォントは Addressables に入れない**（Addressables に入れるのは画面の Prefab だけ。フォントは画面の Prefab・シーンの `UiRoot` から参照され、ビルドに一緒に入る）。動的アセットは Editor で文字を描くとアトラスに字が足され `.asset` が変わるので、`BuildAll()` の最後に 2 つのアセットの動的データを空に戻す（`FontAssets.ClearDynamicData()`）。それでも `Fonts/NotoSansJP/*.asset` に差分が残ったら、コミットせず `git restore` で戻す（コミット済みのアセットはアトラスが空）
5. **共通部品の Prefab は 10 個**（`Assets/Prefabs/Parts/`）: `Label`・`Button`・`Panel`・`ListRow`・`Header`・`Tab`・`TabBar`・`Dialog`（確認・お知らせ・選択を 1 つで。ボタンは実行時に `Button` から並べる）・`BusyBlocker`・`UiRoot`。値（色・寸法・フォントサイズ）は旧 foundation の `UiTheme` を流用し、足りない色（ヘッダー・二次ボタン）を足した（3.2）
6. **Addressables**: 設定は `Assets/AddressableAssetsData/`（Editor のスクリプトが作る。コミットする）。グループ `Screens`（スキーマは Default Local Group の写し）、アドレス = Prefab のファイル名、ラベル無し。**登録するのは画面の Prefab だけ**（`Assets/Prefabs/Screens/` の下。P2・P3 が足す）。部品の Prefab は画面の Prefab にネストし、`UiRoot` はシーンに置くので、Addressables に入れない。`BuildAddressablesWithPlayerBuild` は `AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer` にし、`BuildCommands` が明示的に `BuildPlayerContent` を呼ぶ
7. **USN のコンテナーの AssetLoader**: `PageContainer`・`ModalContainer` の `AssetLoader` は実行時だけのプロパティ（シリアライズされない。未設定なら USN の設定の既定＝Resources）。`UiRoot`（MonoBehaviour）が `Awake` と各プロパティの初回の取得で、1 つの `AddressableAssetLoader` を 3 つのコンテナー（Pages・Modals・Overlays の `ModalContainer`）に入れる（3.3）。USN の設定アセット（`UnityScreenNavigatorSettings`。internal）は作らない
8. **View の骨格**: View・部品の View は「`[SerializeField] private` のフィールドだけ」を持つ（メソッドは C# の計画が足す）。例外として、実行時の振る舞いが Prefab の成立に要る小さな部品（`UiRoot` の AssetLoader の設定、`SafeAreaFitter`、`WindowAspectKeeper`）はこの計画で中身まで作る。`UiTheme`（定数）もこの計画で作る。旧 foundation の `UiFactory`・`UiFonts`・`EnsureBuilt` は作らない
9. **Prefab の生成**: `Scripts/Editor/Prefabs/` の Editor スクリプト。共通の補助 `PrefabBuildUtil`、部品ごとの生成クラス（`IPrefabBuilder` を実装）、入口 `PrefabBuildCommands`（メニュー `Uta/Prefabs/Build All`、`unity command eval` から呼ぶ `BuildAll()`、バッチモード用の `BuildAllBatch()`）。生成クラスは `TypeCache` で集めて `Order` の順に作るので、**P2・P3 は生成クラスを 1 つ足すだけで Build All に入る**（登録の表は持たない）。作業は一時的なプレビュー用のシーンで行い、開いているシーンを汚さない
10. **UiRoot は各シーン（Bootstrap・Title・Home・Produce）に 1 つ置く**（screen-design 7.4「シーンの `UiRoot`」。`DontDestroyOnLoad` にしない）。シーンへの配置と DI への登録は C# の計画: `Bootstrap`・`Title`・`Home` は foundation（決めたこと 3）、`Produce` は formation（決めたこと 14）
11. **PrefabContractTests**（EditMode）は Prefab の一覧を持たず、`Assets/Prefabs/` の下を全部読む。P2・P3 は Prefab を足すだけで自動で確かめの対象になる（3.6）
12. **EditMode のテストは、Editor を開いたまま `unity command --timeout 300 eval` で `TestRunnerApi` を呼んで回す**（**テストの回し方**。ユーザーの判断。2026-11-08）。コマンドの正は [unity7-evaluation.md](../wiki/architecture/unity7-evaluation.md) 4章（結果は `Temp/uta-test-results.xml`、絞り込みは `Filter.groupNames`）で、CLAUDE.md「ビルド・テスト・実行」から引く。Editor は閉じない（`unity test` は同じプロジェクトを開いた Editor があると終了コード 6 で使えない。閉じて開き直す手間は無駄なため）。P2・P3 と C# の計画もこの回し方を使う。eval で回せないときは、Editor の Test Runner（EditMode）を手動で回してもらい、確認できていないと報告する（止まらない）。当初の案（`unity close .` → `unity test` → `unity open .`）はやめた
13. **テストのアセンブリは `Uta.Client.Editor` を参照する**（`TmpSettings_DefaultFont_IsNotoSansJpDynamic` が `FontAssets.RegularPath` を使うため。3.1）。テストの asmdef は Editor 専用なので、Editor のアセンブリを参照してよい
14. **foundation（C#）との境界は foundation の計画で決まった**（2026-11-07）: 旧版の `UiFactory`・`UiFonts`・`EnsureBuilt`・`UiFactoryTests` などを消し、`UiTheme`・`SafeAreaFitter`・`WindowAspectKeeper`・`PageBase`・`ModalBase`・`UiRoot` は P1 が作る（foundation の決めたこと 2）。`DialogService` の公開 API（`DialogView` を使う形）は foundation 3.2 が正。各シーンの `UiRoot` の `BusyIndicator`・`DialogService` は、ルートの `ActiveSceneUi` に各シーンのスコープが入れて、`ApiActivityBridge`・`ErrorDialogView` がそこから取る（foundation の決めたこと 4）。この計画はそれに合わせてある（1.1 の表）
15. **Noto Sans JP（SIL Open Font License 1.1）のライセンス表示**。ライセンス文 `Assets/Fonts/NotoSansJP/OFL.txt` はフォントと一緒にリポジトリに入っている。アプリ内のライセンス表示の画面は MVP では作らず、ストアに出すときに作る。この案でよいか（ユーザーが推奨どおり了承した。2026-11-07）
16. **screen-design 7.3 の「TMP の Essential Resources を取り込む」を「プロジェクトの TMP Settings を作り、既定のフォントを設定する」に直したこと**（このページの 7.3 は [Uta案] として直してある。印は実装時に外す）。Unity 7 の uGUI 7.0.0 で取り込み自体が無くなったため。ユーザーの判断（2026-11-07）の中身を変えるものなので確認する（ユーザーが推奨どおり了承した。2026-11-07）
17. 見た目の値（色・寸法）は仮。旧 foundation の値に、ヘッダーの色 `(0.14, 0.15, 0.20)` と二次ボタンの色 `(0.30, 0.33, 0.42)` を足した。角丸・アイコンの画像は使わない（単色の四角）。この案で進めてよいか（ユーザーが推奨どおり了承した。2026-11-07）
18. **PR #28（`external/` のサブモジュールを消す）と PR #29（Noto Sans JP のフォント）を、この計画の実装の前に両方 main へマージしてよいか**（[索引](2026-11-07-client-screens-index.md)の確認 1）。この計画の実装ブランチは main から切る前提で書いた（前提 2）。片方だけでは前提がそろわない（1 つの PR の base は 1 つしか選べない）（ユーザーが推奨どおり了承した。2026-11-07）
19. **Prefab を足すだけの作業では、プレイヤーのビルドを確かめない**（ユーザーの判断。2026-11-08）。Windows の IL2CPP のビルド、`Uta.exe -uta-quit-after-check`、`BuildAllBatch` のバッチ起動の確認、`Build/` の大きさの測定は、この計画（タスク 4・6）から外した。ビルドが要るときは、Editor を開いている worktree とは別のワークスペースで行う（unity7-evaluation 4章）

## ユーザーの確認が要ること

なし（全件、2026-11-07 にユーザーが推奨どおり了承した。「この計画で決めたこと」の末尾に移した）。

## 1. 目的と前提

[game-spec.md](../wiki/overview/game-spec.md) の順 7（client/ の画面）のうち、Prefab の 1 本目。画面を Prefab で作るための土台（Addressables・TextMeshPro・日本語のフォント・共通部品の Prefab・`UiRoot` の Prefab・生成の補助・Prefab の契約のテスト・プレイヤーのビルドへの `BuildPlayerContent`）を作る。方針の正は [screen-design.md](../wiki/architecture/screen-design.md) 7章（7.1 Prefab は Editor のスクリプトで生成、7.2 Addressables、7.3 TextMeshPro、7.4 部品と置き場）と 8章（シーン構成）。見た目の方針は [screen-wireframes.md](../wiki/overview/screen-wireframes.md)「共通の方針」（基準解像度 1080×1920、`CanvasScaler` Match = 0、ページのルートを Safe Area に合わせ背景だけ全面）。

- 関係する ADR: [0009](../wiki/decisions/0009-client-stack.md)（uGUI + UnityScreenNavigator・VContainer・UniTask・縦画面）、[0006](../wiki/decisions/0006-adopt-unity-7-early.md)（Unity 7）、[0005](../wiki/decisions/0005-foundation-submodule.md)（共通基盤。`external/` は直さない）、[0001](../wiki/decisions/0001-ip-policy.md)（文言・名前はオリジナル）
- 計画の並び（[索引](2026-11-07-client-screens-index.md)）: 計画 0（[masterdata-card-text](2026-11-03-masterdata-card-text.md)）→ **P1（この計画）** → P2（[screens-a](2026-11-07-client-prefabs-screens-a.md)）→ foundation（[C#](2026-11-03-client-screens-foundation.md)。シーン・`UiRoot` の配置・Bootstrap の接続、`BusyIndicator`・`DialogService` のロジック）→ gacha → owned（C#）→ P3（[screens-b](2026-11-07-client-prefabs-screens-b.md)）→ formation → week → lesson（C#）。この計画は計画 0 の型には依存しない
- **前提 1**: Unity 7（`7000.0.0a12`）の Editor が、この worktree の `client/uta` を開いていること（`unity status --until-ready` が ready）。**Editor が無い環境ではこの計画のどのタスクも実行できない**。実行できないときは「確認できていない」と書いてユーザーに実行を頼む（推測で「通った」と書かない）
- **前提 2**: PR #28・PR #29 が main に入っていること（決めたこと 18。[索引](2026-11-07-client-screens-index.md)の「前提となる未マージの PR」。2026-11-07 時点でどちらも未マージ）。この計画の実装のブランチは `main` から切る（`tools/worktree-pool.sh start <番号> <新ブランチ>`。base は既定の `origin/main`）。PR #29 のフォント（`client/uta/Assets/Fonts/NotoSansJP/` に `NotoSansJP-Regular.ttf`・`NotoSansJP-Bold.ttf`・`NotoSansJP-Regular SDF.asset`・`NotoSansJP-Bold SDF.asset`・`OFL.txt`）が main にある。タスク 1 の前に 5 つのファイルがあることを確かめ、無ければ止まって報告する（フォントのブランチから切って進めない）
  - 補足（使わない予定の代わりの手順）: ユーザーが #29 だけを先にしない・遅らせると決めたときだけ、ブランチを `claude/noto-sans-jp-font` から切り、PR の base もそのブランチにする積み PR にできる。この場合 PR #28 の変更（`manifest.json`・`external/` の削除）は入らないので、下の「PR #28 の読み替え」は使わず `external/` のソースを読む
- **前提 3**: Editor は閉じない（決めたこと 12・19）。テストは Editor を開いたまま回し、Windows の IL2CPP のビルドはこの計画では行わない（タスク 1 のスパイクのビルドだけは行った）。ビルドが要るときは別のワークスペースで行う（unity7-evaluation 4章）
- **PR #28 の読み替え**: PR #28（前提 2 で main に入っている。PR #28 が足す ADR）は `external/` のサブモジュールを全部消す（UnityScreenNavigator・Supplement・VolTech-MagicOnion・MasterMemoryAddons）。サーバー・shared は `packages/` の `.nupkg`、Unity は `manifest.json` の git URL で参照し、`Uta.slnx` から共通基盤が外れる。**PR #28 のあとは、共通基盤のソース（USN の `Runtime/ChinpanGX`・README・`AddressableAssetLoader`、VolTech-MagicOnion のコードなど）は `client/uta/Library/PackageCache/<パッケージ名>@<ハッシュ>/`（USN は `com.harumak.unityscreennavigator`。Unity でパッケージを解決した環境にだけある）か GitHub のリポジトリを読む。マージ前は `external/<名前>`**。この計画に書いた `external/` のパスは、そう読み替える（`external/` は直さない、の決まりは変わらない）
- **この計画で決めないこと**: 画面ごとの Prefab と View（P2・P3）。`BusyIndicator` の `Begin`／`End`、`DialogService`、`PageStack`、Presenter、シーンへの `UiRoot` の配置と DI の登録、`ProjectSettings` の Windows のウィンドウ設定（foundation）。アプリ内のライセンス表示の画面（MVP 外。確認 1）。フォントのアセットの作成と設定（作成済み。前提 2）

### 1.1 C# の計画（foundation）との境界

| 項目 | この計画（P1） | foundation（C#） |
|---|---|---|
| `UiTheme` | 定数を全部作る（3.2） | 使う |
| `PageBase`／`ModalBase` | `[SerializeField]` のフィールドと、それを返す `protected` プロパティだけ | 何も足さない（派生の View が使う） |
| 部品の View（`ButtonView` など） | フィールドだけ | 表示のメソッド（ラベルを変える・選択の見た目など）を足す |
| `BusyIndicator` | フィールド `blocker` だけ（Prefab の `BusyBlocker` のルートに付く） | `Begin()`・`End()`・`IsBusy` を足す（3.5） |
| `DialogService` | 作らない。`Dialog` の Prefab と `DialogView` のフィールド、`UiRoot.DialogLayer`・`UiRoot.DialogPrefab` を用意する | `DialogService` を作る（3.5 の公開 API） |
| `UiRoot` | Prefab と MonoBehaviour（AssetLoader の設定とプロパティ）を全部作る | 各シーンに置き、`LifetimeScope` に登録する |
| `SafeAreaFitter`・`WindowAspectKeeper` | 中身まで作る | 作らない（foundation の 2 章の表から消す） |
| asmdef の参照（Presentation・Editor・テスト） | 足す（3.1） | 足りないものだけ足す |
| EditMode のテストの回し方の記録 | 4章と CLAUDE.md に足す（タスク 5・7） | 足さない |

## 2. 触るファイル

パスは、`client/uta/Assets/` で始まらないものはリポジトリのルートからの相対、それ以外は `client/uta/Assets/` からの相対。新しい `.cs`・フォルダ・アセットには Unity が作った `.meta` をコミットする。「生成」は Editor のスクリプトが作るもの（手で書かない・直さない）。

| 操作 | パス | 中身 |
|---|---|---|
| 使う（作成済み。触らない） | `Fonts/NotoSansJP/NotoSansJP-Regular SDF.asset`・`NotoSansJP-Bold SDF.asset`（と ttf 2 本・`OFL.txt`） | 動的 SDF のフォントアセット（前提 2）。動的データが足されたら `BuildAll()` が空に戻す（決めたこと 4） |
| 生成 | `TextMesh Pro/Resources/TMP Settings.asset` | プロジェクトの TMP Settings（`TmpSettingsBuilder`） |
| 生成 | `AddressableAssetsData/`（設定・グループ `Default Local Group`・`Screens`・テンプレート） | Addressables の設定（`AddressablesSetup`） |
| 生成 | `Prefabs/Parts/Label.prefab`・`Button.prefab`・`Panel.prefab`・`ListRow.prefab`・`Header.prefab`・`Tab.prefab`・`TabBar.prefab`・`Dialog.prefab`・`BusyBlocker.prefab`・`UiRoot.prefab` | 共通部品（3.4） |
| 変更 | `Scripts/Presentation/Uta.Client.Presentation.asmdef` | 参照を足す（3.1） |
| 作成 | `Scripts/Presentation/Common/UiTheme.cs` | 色・寸法の定数（3.2） |
| 作成 | `Scripts/Presentation/Common/OptionalReferenceAttribute.cs` | `null` を許す参照の印（3.6） |
| 作成 | `Scripts/Presentation/Common/SafeAreaFitter.cs`・`WindowAspectKeeper.cs` | Safe Area・Windows の 9:16（3.3） |
| 作成 | `Scripts/Presentation/Common/PageBase.cs`・`ModalBase.cs` | ページ・モーダルの基底の骨格（3.3） |
| 作成 | `Scripts/Presentation/Common/UiRoot.cs` | `UiRoot` の MonoBehaviour（3.3） |
| 作成 | `Scripts/Presentation/Common/BusyIndicator.cs` | ブロッカーの骨格（3.3） |
| 作成 | `Scripts/Presentation/Common/Parts/ButtonView.cs`・`HeaderView.cs`・`TabView.cs`・`TabBarView.cs`・`DialogView.cs`・`ListRowBase.cs`・`ListRowView.cs` | 部品の View の骨格（3.3） |
| 変更 | `Scripts/Editor/Uta.Client.Editor.asmdef` | 参照を足す（3.1） |
| 作成 | `Scripts/Editor/Prefabs/PrefabBuildUtil.cs` | 生成の共通の補助（3.5） |
| 作成 | `Scripts/Editor/Prefabs/RectSpec.cs`・`LayoutSpec.cs`・`TextSpec.cs`・`ButtonStyle.cs`・`IPrefabBuilder.cs` | 補助の値の型（3.5） |
| 作成 | `Scripts/Editor/Prefabs/FontAssets.cs`・`TmpSettingsBuilder.cs`・`AddressablesSetup.cs` | フォントのアセットの読み込み・TMP Settings・Addressables（3.5） |
| 作成 | `Scripts/Editor/Prefabs/PrefabBuildCommands.cs` | メニューとバッチの入口（3.5） |
| 作成 | `Scripts/Editor/Prefabs/Parts/LabelBuilder.cs`・`ButtonBuilder.cs`・`PanelBuilder.cs`・`TabBuilder.cs`・`TabBarBuilder.cs`・`ListRowBuilder.cs`・`HeaderBuilder.cs`・`DialogBuilder.cs`・`BusyBlockerBuilder.cs`・`UiRootBuilder.cs` | 部品ごとの生成クラス（3.4） |
| 変更 | `Scripts/Editor/BuildCommands.cs` | `BuildPlayerContent` を足す（3.7） |
| 変更 | `Tests/EditMode/Uta.Client.Infrastructure.Tests.EditMode.asmdef` | 参照を足す（3.1） |
| 作成 | `Tests/EditMode/PrefabContract.cs`・`PrefabContractTests.cs` | Prefab の契約のテスト（3.6） |
| 作成→削除 | `Spike/`（`Uta.Spike.asmdef`・`AddressablesSpikeProbe.cs`・`SpikePage.prefab`・`Spike.unity`・`Editor/Uta.Spike.Editor.asmdef`・`Editor/SpikeCommands.cs`） | タスク 1 のスパイク。タスク 1 の終わりに消す |
| 変えない | `.gitignore` | テストの結果は `Temp/uta-test-results.xml`（`Temp/` は除外済み）に出すので、足さない（当初は `client/*/test-results.xml` を足す案。タスク 5） |
| 変更 | `CLAUDE.md` | 「ビルド・テスト・実行」に EditMode のテストと Prefab の生成のコマンドを足す（タスク 7） |
| 変更 | `uta_docs/wiki/architecture/unity7-evaluation.md` | 2章の表・3.3・4章（タスク 1・5・6・7） |
| 変更 | `uta_docs/wiki/architecture/screen-design.md` | 7.2・7.3・7.4・実装メモ（タスク 7） |
| 変更 | `uta_docs/wiki/architecture/repository-layout.md` | 3章の `Uta.Client.Presentation` の参照（タスク 7） |

## 3. 型とシグネチャ

C# は `client/` の `.editorconfig` の範囲（C# 9 の範囲。ファイルスコープ namespace・プライマリコンストラクタ・コレクション式 `[...]`・Index/Range・record は使わない。配列は `new[] { ... }`）。BOM 付き UTF-8、LF、Allman、`var`、private フィールドは接頭辞なしの camelCase。名前空間: `Scripts/Presentation/Common` → `Uta.Client.Presentation.Common`、`Common/Parts` → `Uta.Client.Presentation.Common.Parts`、`Scripts/Editor/Prefabs` → `Uta.Client.Editor.Prefabs`、`Editor/Prefabs/Parts` → `Uta.Client.Editor.Prefabs.Parts`、テストは `Uta.Client.Infrastructure.Tests`（既存のテストと同じ）、スパイクは `Uta.Spike`・`Uta.Spike.Editor`。

### 3.1 asmdef の参照

- `Uta.Client.Presentation.asmdef` の `references` を次にする（`overrideReferences: true` のまま）:
  ```
  "Uta.Client.Application", "Uta.Shared.Domain", "UniTask", "VContainer",
  "UnityEngine.UI", "UnityScreenNavigator", "Unity.TextMeshPro"
  ```
  `unity recompile` が `AddressableAssetLoader` の利用について CS0012（参照していないアセンブリの型）を出したときだけ、`"Unity.Addressables"`, `"Unity.ResourceManager"` を足す
- **一般則（どの asmdef でも）**: どの asmdef でも CS0012 が出たら、エラーが名指しする型を含むアセンブリのうち、`Unity.ResourceManager`・`Unity.Addressables`・`Unity.TextMeshPro`・`UnityEngine.UI`・`UnityScreenNavigator` に限って、その asmdef の references に足す。それ以外の名前が出たら止まって報告する
- `Uta.Client.Editor.asmdef` の `references` の最後に `"UnityScreenNavigator"`, `"Unity.TextMeshPro"`, `"Unity.Addressables"`, `"Unity.Addressables.Editor"`, `"Unity.ResourceManager"` を足す（タスク 1）
- `Uta.Client.Infrastructure.Tests.EditMode.asmdef` の `references` の最後に `"Uta.Client.Presentation"`, `"Uta.Client.Editor"`, `"UnityScreenNavigator"`, `"UnityEngine.UI"`, `"Unity.TextMeshPro"`, `"Unity.Addressables"`, `"Unity.Addressables.Editor"` を足す（タスク 5）。`Uta.Client.Editor` は `PrefabContractTests` の `TmpSettings_DefaultFont_IsNotoSansJpDynamic` が `FontAssets.RegularPath`・`BoldPath` を使うため（決めたこと 13。テストの asmdef も Editor 専用なので参照できる）。既に参照に入っていれば足さない
- asmdef の名前は実在を確かめてある: `Unity.TextMeshPro`・`UnityEngine.UI`（`com.unity.ugui` 7.0.0）、`Unity.Addressables`・`Unity.Addressables.Editor`・`Unity.ResourceManager`（`com.unity.addressables` 4.1.2）。`UnityScreenNavigator` の asmdef は `com.unity.addressables` があると `USN_USE_ADDRESSABLES` を定義し、`AddressableAssetLoader` が使える

### 3.2 `UiTheme`（`Uta.Client.Presentation.Common`）

値の出どころ: 旧 foundation 3.2 の `UiTheme`（そのまま流用）、`[P1]` はこの計画で足した値（決めたこと 17）。

```csharp
public static class UiTheme
{
    public static readonly Vector2 ReferenceResolution = new Vector2(1080f, 1920f);   // screen-wireframes 共通の方針

    public static readonly Color Background = new Color(0.10f, 0.11f, 0.15f, 1f);
    public static readonly Color Header = new Color(0.14f, 0.15f, 0.20f, 1f);          // [P1] ヘッダー・タブの帯
    public static readonly Color Panel = new Color(0.18f, 0.20f, 0.27f, 1f);
    public static readonly Color ButtonPrimary = new Color(0.25f, 0.45f, 0.85f, 1f);   // 旧 Button
    public static readonly Color ButtonSecondary = new Color(0.30f, 0.33f, 0.42f, 1f); // [P1] 戻る・いいえ
    public static readonly Color ButtonDanger = new Color(0.80f, 0.30f, 0.30f, 1f);    // 旧 Danger
    public static readonly Color Text = new Color(1f, 1f, 1f, 1f);
    public static readonly Color TextMuted = new Color(0.70f, 0.72f, 0.78f, 1f);
    public static readonly Color Accent = new Color(1f, 0.78f, 0.25f, 1f);
    public static readonly Color Overlay = new Color(0f, 0f, 0f, 0.6f);
    public static readonly Color Transparent = new Color(0f, 0f, 0f, 0f);

    // Button の ColorTint（[P1]。押した・使えないの見た目を、C# の処理なしで出す）
    public static readonly Color TintNormal = new Color(1f, 1f, 1f, 1f);
    public static readonly Color TintHighlighted = new Color(0.92f, 0.92f, 0.92f, 1f);
    public static readonly Color TintPressed = new Color(0.75f, 0.75f, 0.75f, 1f);
    public static readonly Color TintDisabled = new Color(0.45f, 0.45f, 0.45f, 1f);

    public const int FontTitle = 56;
    public const int FontBody = 40;
    public const int FontSmall = 32;

    public const float ButtonHeight = 130f;
    public const float HeaderHeight = 150f;   // [P1] 上下の余白 10 + ボタン 130
    public const float TabHeight = 110f;      // [P1]
    public const float RowHeight = 160f;      // [P1]
    public const float Spacing = 24f;
    public const int Padding = 32;            // 旧 float。RectOffset に入れるので int にする
    public const float DialogWidth = 880f;
    public const float ModalWidth = 900f;
}
```

### 3.3 実行時の型（`Uta.Client.Presentation.Common`）

```csharp
/// <summary>この印の付いた [SerializeField] の参照は null でよい（PrefabContractTests が見逃す）。</summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class OptionalReferenceAttribute : Attribute { }

[RequireComponent(typeof(RectTransform))]
public sealed class SafeAreaFitter : MonoBehaviour
{
    // OnEnable で 1 回合わせ、LateUpdate で Screen.safeArea・Screen.width・Screen.height を前回の値と比べ、変わったときだけ合わせる。
    // 合わせ方: anchorMin = safeArea.position / (Screen.width, Screen.height)、
    //           anchorMax = (safeArea.position + safeArea.size) / (Screen.width, Screen.height)、offsetMin = offsetMax = Vector2.zero。
    // Screen.width か Screen.height が 0 のときは何もしない。
}

public sealed class WindowAspectKeeper : MonoBehaviour
{
    // #if UNITY_STANDALONE_WIN && !UNITY_EDITOR のときだけ中身を持つ（ほかでは空のクラス）。
    // Update で Screen.fullScreen が false のとき、Screen.height と Mathf.RoundToInt(Screen.width * 16f / 9f) の差の絶対値が 1 より大きければ
    // Screen.SetResolution(Screen.width, Mathf.RoundToInt(Screen.width * 16f / 9f), FullScreenMode.Windowed) を呼び、
    // 呼んだ次の 1 フレームは判定を休む（反映待ちで連続して呼ばない）。
}

/// <summary>ページの基底。Prefab のルートに付く。子 "Body"（SafeAreaFitter 付き）を持つ。</summary>
public abstract class PageBase : Page   // UnityScreenNavigator.Runtime.Core.Page.Page
{
    [SerializeField] private RectTransform body;
    protected RectTransform Body => body;
}

/// <summary>モーダルの基底。Prefab のルートに付く。中央の子 "Panel" を持つ。暗幕は USN の DefaultModalBackdrop。</summary>
public abstract class ModalBase : Modal   // UnityScreenNavigator.Runtime.Core.Modal.Modal
{
    [SerializeField] private RectTransform panel;
    protected RectTransform Panel => panel;
}

/// <summary>通信中のブロッカー（BusyBlocker のルート）。この計画ではフィールドだけ。Begin/End/IsBusy は foundation が足す。</summary>
public sealed class BusyIndicator : MonoBehaviour
{
    [SerializeField] private Image blocker;
}

[DisallowMultipleComponent]
public sealed class UiRoot : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private PageContainer pageContainer;
    [SerializeField] private ModalContainer modalContainer;
    [SerializeField] private OverlayContainer overlayContainer;
    [SerializeField] private BusyIndicator busyIndicator;
    [SerializeField] private RectTransform dialogLayer;
    [SerializeField] private DialogView dialogPrefab;   // Parts/Dialog.prefab のアセット（ネストではなく参照）

    private AddressableAssetLoader assetLoader;           // UnityScreenNavigator.Runtime.Foundation.AssetLoader

    public Canvas Canvas => canvas;
    public PageContainer Pages { get { EnsureConfigured(); return pageContainer; } }
    public ModalContainer Modals { get { EnsureConfigured(); return modalContainer; } }
    public OverlayContainer Overlays { get { EnsureConfigured(); return overlayContainer; } }
    public BusyIndicator Busy => busyIndicator;
    public RectTransform DialogLayer => dialogLayer;
    public DialogView DialogPrefab => dialogPrefab;

    private void Awake() { EnsureConfigured(); }

    /// <summary>
    /// 冪等。assetLoader が null のときだけ、new AddressableAssetLoader() を作り、
    /// pageContainer.AssetLoader・modalContainer.AssetLoader・overlayContainer.Container.AssetLoader に同じインスタンスを入れる。
    /// （AssetLoader は USN のコンテナーの実行時だけのプロパティ。Awake の順番に頼らないよう、プロパティの取得でも呼ぶ。
    ///   foundation の LifetimeScope は Pages/Modals/Overlays のプロパティ経由でコンテナーを登録するので、ScreenNavigator が使う前に必ず設定される）
    /// </summary>
    private void EnsureConfigured();
}
```

部品の View（`Uta.Client.Presentation.Common.Parts`。フィールドだけ。全部 `public sealed class … : MonoBehaviour`、`ListRowBase` だけ `public abstract`）:

| 型 | フィールド（`[SerializeField] private`） | 付く Prefab |
|---|---|---|
| `ButtonView` | `Button button`、`Image background`、`TMP_Text label` | `Button` のルート |
| `HeaderView` | `ButtonView backButton`、`TMP_Text titleText`、`RectTransform rightSlot` | `Header` のルート |
| `TabView` | `Button button`、`TMP_Text label`、`Image underline` | `Tab` のルート |
| `TabBarView` | `TabView[] tabs`（部品の Prefab では空の配列。画面の生成で入れる） | `TabBar` のルート |
| `DialogView` | `TMP_Text messageText`、`RectTransform buttonList`、`ButtonView buttonPrefab`（`Parts/Button.prefab` のアセットの参照） | `Dialog` のルート |
| `ListRowBase`（abstract） | `Button button`、`Image background` | 行の Prefab のルート（派生を付ける） |
| `ListRowView : ListRowBase` | `TMP_Text titleText`、`TMP_Text subText`、`TMP_Text rightText` | `ListRow` のルート |

- `Label`・`Panel` には View を付けない（画面の View は `TMP_Text`・`RectTransform` を直接持つ）
- 画面の View が部品を持つときは、部品の View の型（`ButtonView`・`HeaderView`・`TabBarView`）でフィールドを宣言する。ボタンの `onClick` は C# が購読する（Prefab に永続リスナーを張らない）

### 3.4 共通部品の Prefab（`Assets/Prefabs/Parts/`）

共通の書き方:
- 「全面」= anchorMin (0,0)、anchorMax (1,1)、pivot (0.5,0.5)、offsetMin = offsetMax = (0,0)。「上帯 h」= anchorMin (0,1)、anchorMax (1,1)、pivot (0.5,1)、offsetMin (0,−h)、offsetMax (0,0)。「中央 w×h」= anchorMin = anchorMax = pivot = (0.5,0.5)、offsetMin (−w/2,−h/2)、offsetMax (w/2,h/2)。「レイアウト任せ」= 親のレイアウトグループが位置と大きさを決める（RectTransform の値は全面にしておく）
- TMP の文字（以下「TMP」）は `TextMeshProUGUI`。共通: font = `NotoSansJP-Regular SDF`（太字にするものだけ `NotoSansJP-Bold SDF`。この計画の部品には無い）、`richText = false`（マスタやサーバーの文字に `<` が入っても崩さない）、`raycastTarget = false`、`enableAutoSizing = false`、`fontStyle = Normal`。表の「折り返し」は `textWrappingMode`（`Normal`／`NoWrap`）、「はみ出し」は `overflowMode`（`Overflow`／`Ellipsis`）
- Image は sprite なし（単色の四角）、`type = Simple`
- Button は `transition = ColorTint`、`targetGraphic` = 同じ GameObject の Image、colors = normal `TintNormal`・highlighted `TintHighlighted`・pressed `TintPressed`・selected `TintNormal`・disabled `TintDisabled`、`colorMultiplier = 1`、`fadeDuration = 0.1`、`navigation.mode = None`
- VLG = `VerticalLayoutGroup`、HLG = `HorizontalLayoutGroup`。表の「制御 W/H」は `childControlWidth`／`childControlHeight`、「伸ばす W/H」は `childForceExpandWidth`／`childForceExpandHeight`。`childScaleWidth/Height` はどちらも false
- LE = `LayoutElement`。書いていない値は既定（−1）
- 部品の中で別の部品を使うときは、部品の Prefab のインスタンスをネストする（「→ `Label`」のように書く）。ネストした部品の値の変更は Prefab のオーバーライドになる

#### Label（出どころ: 旧 foundation `CreateText`、screen-design 7.3）

| 階層 | コンポーネントと値 |
|---|---|
| `Label` | RectTransform 全面。TMP: text "ラベル"、fontSize 40（`FontBody`）、color `Text`、alignment `MidlineLeft`、折り返し `Normal`、はみ出し `Overflow` |

#### Button（出どころ: 旧 foundation `CreateButton`。高さ 130）

| 階層 | コンポーネントと値 |
|---|---|
| `Button` | RectTransform 中央 600×130。Image color `ButtonPrimary`、raycastTarget true。Button（共通の値）。LE minHeight 130・preferredHeight 130。`ButtonView`（button = 自分の Button、background = 自分の Image、label = 子 `Label` の TMP） |
| └ `Label` | RectTransform 全面で offsetMin (16,0)・offsetMax (−16,0)。TMP: text "ボタン"、fontSize 40、color `Text`、alignment `Center`、折り返し `NoWrap`、はみ出し `Ellipsis` |

ボタンの種類（`ButtonStyle`。画面の生成で Image の色をオーバーライドする）: `Primary` = `ButtonPrimary`、`Secondary` = `ButtonSecondary`、`Danger` = `ButtonDanger`。

#### Panel（出どころ: 旧 foundation `CreatePanel`）

| 階層 | コンポーネントと値 |
|---|---|
| `Panel` | RectTransform 全面（レイアウト任せ）。Image color `Panel`、raycastTarget true。VLG: padding 32（上下左右）、spacing 24、childAlignment `UpperCenter`、制御 W/H true/true、伸ばす W/H true/false |

#### Tab（[P1]。所持一覧のタブ（screen-wireframes 10）など）

| 階層 | コンポーネントと値 |
|---|---|
| `Tab` | RectTransform 全面（レイアウト任せ）。Image color `Header`、raycastTarget true。Button（共通の値）。LE minHeight 110・preferredHeight 110・flexibleWidth 1。`TabView`（button、label = 子 `Label`、underline = 子 `Underline` の Image） |
| ├ `Label` | RectTransform 全面で offsetMin (8,8)・offsetMax (−8,0)。TMP: text "タブ"、fontSize 40、color `Text`、alignment `Center`、折り返し `NoWrap`、はみ出し `Ellipsis` |
| └ `Underline` | RectTransform: anchorMin (0,0)、anchorMax (1,0)、pivot (0.5,0)、offsetMin (0,0)、offsetMax (0,8)。Image color `Accent`、raycastTarget false、**`enabled = false`**（選ばれたタブだけ C# が有効にする） |

#### TabBar（[P1]）

| 階層 | コンポーネントと値 |
|---|---|
| `TabBar` | RectTransform 上帯 110。Image color `Header`、raycastTarget true。HLG: padding 0、spacing 0、childAlignment `MiddleCenter`、制御 W/H true/true、伸ばす W/H true/true。LE minHeight 110・preferredHeight 110・flexibleHeight 0（P2 で追加）。`TabBarView`（tabs = 空の配列） |

タブは画面の生成が `PrefabBuildUtil.AddTabBar` で `Tab` のインスタンスを子に並べ、`tabs` に入れる（3.5）。

#### ListRow（[P1]。一覧の行の基底。所持一覧・ガチャの一覧など）

| 階層 | コンポーネントと値 |
|---|---|
| `ListRow` | RectTransform 全面（レイアウト任せ）。Image color `Panel`、raycastTarget true。Button（共通の値）。LE minHeight 160・preferredHeight 160。HLG: padding 左 32・右 32・上 16・下 16、spacing 24、childAlignment `MiddleLeft`、制御 W/H true/true、伸ばす W/H false/true。`ListRowView`（button、background、titleText、subText、rightText） |
| ├ `TextColumn` | RectTransform（レイアウト任せ）。VLG: padding 0、spacing 4、childAlignment `MiddleLeft`、制御 W/H true/true、伸ばす W/H true/false。LE flexibleWidth 1 |
| │├ `TitleText` | → `Label`。fontSize 40、color `Text`、alignment `MidlineLeft`、折り返し `NoWrap`、はみ出し `Ellipsis`、text "名前" |
| │└ `SubText` | → `Label`。fontSize 32、color `TextMuted`、alignment `MidlineLeft`、折り返し `NoWrap`、はみ出し `Ellipsis`、text "補足" |
| └ `RightText` | → `Label`。fontSize 32、color `Text`、alignment `MidlineRight`、折り返し `NoWrap`、はみ出し `Ellipsis`、text "" 。LE preferredWidth 240・flexibleWidth 0 |

行が別の形（ボタンを持つ・2 段より多い）の画面は、P2・P3 が `ListRowBase` を継承した行の View と行の Prefab（`Parts/<名前>Row.prefab`）を自分で作る。

#### Header（[P1]。戻るボタン＋タイトル。右上の [≡] は画面が `RightSlot` に置く（screen-wireframes 共通の方針））

| 階層 | コンポーネントと値 |
|---|---|
| `Header` | RectTransform 上帯 150。Image color `Header`、raycastTarget true。HLG: padding 左 24・右 24・上 10・下 10、spacing 16、childAlignment `MiddleCenter`、制御 W/H true/true、伸ばす W/H false/true。LE minHeight 150・preferredHeight 150・flexibleHeight 0（P2 で追加）。`HeaderView`（backButton、titleText、rightSlot） |
| ├ `BackButton` | → `Button`。Image color `ButtonSecondary`、Label の text "戻る"。LE preferredWidth 200・flexibleWidth 0 |
| ├ `TitleText` | → `Label`。fontSize 56（`FontTitle`）、alignment `Center`、折り返し `NoWrap`、はみ出し `Ellipsis`、text "タイトル"。LE flexibleWidth 1 を足す |
| └ `RightSlot` | RectTransform（レイアウト任せ）。LE preferredWidth 200・flexibleWidth 0（タイトルを中央に保つ。画面がここにボタンを全面で置く） |

#### Dialog（出どころ: 旧 foundation `DialogService` の "Dialog"／"Panel"）

| 階層 | コンポーネントと値 |
|---|---|
| `Dialog` | RectTransform 全面。Image color `Overlay`、raycastTarget true（下の入力を止める）。`DialogView`（messageText、buttonList、buttonPrefab = `Parts/Button.prefab` の `ButtonView`） |
| └ `Panel` | RectTransform 中央 880×0（高さは ContentSizeFitter が決める）。Image color `Panel`、raycastTarget true。VLG: padding 40、spacing 32、childAlignment `UpperCenter`、制御 W/H true/true、伸ばす W/H true/false。ContentSizeFitter: horizontalFit `Unconstrained`、verticalFit `PreferredSize` |
| 　├ `MessageText` | → `Label`。fontSize 40、alignment `Center`、折り返し `Normal`、はみ出し `Overflow`、text "メッセージ" |
| 　└ `ButtonList` | RectTransform（レイアウト任せ）。VLG: padding 0、spacing 24、childAlignment `UpperCenter`、制御 W/H true/true、伸ばす W/H true/false。子は無し（`DialogService` が `buttonPrefab` から実行時に並べる） |

#### BusyBlocker（出どころ: screen-design 7.5「全面の透明な Image で入力を止める」）

| 階層 | コンポーネントと値 |
|---|---|
| `BusyBlocker` | RectTransform 全面。Image color `Transparent`、raycastTarget true、**`enabled = false`**。`BusyIndicator`（blocker = 自分の Image） |

#### UiRoot（出どころ: screen-wireframes 共通の方針、screen-design 7.2・7.5、旧 foundation `UiRoot.Create`）

| 階層（上から順＝後ろが手前） | コンポーネントと値 |
|---|---|
| `UiRoot` | RectTransform（Canvas が決める）。Canvas: renderMode `ScreenSpaceOverlay`、sortingOrder 0、pixelPerfect false。CanvasScaler: uiScaleMode `ScaleWithScreenSize`、referenceResolution (1080,1920)、screenMatchMode `MatchWidthOrHeight`、matchWidthOrHeight 0、referencePixelsPerUnit 100。GraphicRaycaster（既定）。`WindowAspectKeeper`。`UiRoot`（下の参照） |
| ├ `Background` | RectTransform 全面。Image color `Background`、raycastTarget false（Safe Area の外も塗る） |
| ├ `Pages` | RectTransform 全面。`PageContainer`（`_name` は空のまま） |
| ├ `Modals` | RectTransform 全面。`ModalContainer`（既定の値） |
| ├ `Overlays` | RectTransform 全面。`ModalContainer` + `OverlayContainer`（同じ GameObject。`ScreenNavigator` のコンストラクターが要る。Uta は Overlay に画面を出さない） |
| ├ `Busy` | → `BusyBlocker`（全面） |
| ├ `Dialogs` | RectTransform 全面。ほかのコンポーネントなし（ダイアログの層。Busy より手前なので、通信中でもダイアログは押せる） |
| └ `EventSystem` | Transform（RectTransform にしない）。`EventSystem` + `InputSystemUIInputModule`（生成で `AssignDefaultActions()` を呼ぶ。`SceneBuilder` と同じ） |

`UiRoot` の参照: canvas = 自分の Canvas、pageContainer = `Pages`、modalContainer = `Modals`、overlayContainer = `Overlays` の `OverlayContainer`、busyIndicator = `Busy` の `BusyIndicator`、dialogLayer = `Dialogs`、dialogPrefab = `Parts/Dialog.prefab` の `DialogView`。

`PageContainer` の子はすべて `Page` でなければならない（USN が子の `Page` の描画順を見る）ので、`Pages`・`Modals`・`Overlays` の下には何も置かない。

#### ページ・モーダルの雛形（P2・P3 が `PrefabBuildUtil.CreatePage`／`CreateModal` で作る形）

| 階層 | コンポーネントと値 |
|---|---|
| `<名前>Page`（ルート） | RectTransform 全面（USN が Push のときに親いっぱいに広げる）。Image color `Background`、raycastTarget true（下のページを隠し、入力を止める）。`<名前>Page : PageBase`（body = 子 `Body`） |
| └ `Body` | RectTransform 全面 + `SafeAreaFitter`。画面の中身はここに置く |
| `<名前>Modal`（ルート） | RectTransform 全面。Image なし（暗幕と入力の遮断は USN の `DefaultModalBackdrop`）。`<名前>Modal : ModalBase`（panel = 子 `Panel`） |
| └ `Panel` | RectTransform 中央 900×0。Image color `Panel`、raycastTarget true。VLG: padding 40、spacing 24、childAlignment `UpperCenter`、制御 W/H true/true、伸ばす W/H true/false。ContentSizeFitter: verticalFit `PreferredSize` |

#### 値 → 部品 → View のフィールド（この計画の分）

| ワイヤー／仕様の値 | Prefab の部品（子の名前） | View のフィールド |
|---|---|---|
| 戻る（各画面の左上） | `Header/BackButton` | `HeaderView.backButton` |
| 画面の見出し | `Header/TitleText` | `HeaderView.titleText` |
| 右上の [≡]（プロデュース） | `Header/RightSlot`（P3 がボタンを置く） | `HeaderView.rightSlot` |
| タブ | `TabBar/<タブ名>` → `Tab/Label`・`Underline` | `TabBarView.tabs[i]`、`TabView.label`・`underline` |
| 一覧の行（名前・補足・右の値） | `ListRow/TextColumn/TitleText`・`SubText`、`RightText` | `ListRowView.titleText`・`subText`・`rightText` |
| 確認・お知らせ・選択のダイアログの文 | `Dialog/Panel/MessageText` | `DialogView.messageText` |
| ダイアログのボタン（1〜4 個） | `Dialog/Panel/ButtonList` の子（実行時） | `DialogView.buttonList`・`buttonPrefab` |
| 通信中のブロッカー | `UiRoot/Busy`（`BusyBlocker`） | `BusyIndicator.blocker`、`UiRoot.busyIndicator` |
| ページ・モーダル・オーバーレイの層 | `UiRoot/Pages`・`Modals`・`Overlays` | `UiRoot.pageContainer`・`modalContainer`・`overlayContainer` |
| ダイアログの層 | `UiRoot/Dialogs` | `UiRoot.dialogLayer` |

### 3.5 Editor スクリプト（`Uta.Client.Editor.Prefabs`）

#### 値の型

```csharp
/// <summary>RectTransform の置き方。offsetMin/offsetMax で持つ（pos/size と同じ意味）。</summary>
public readonly struct RectSpec
{
    public RectSpec(Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 offsetMin, Vector2 offsetMax);
    public Vector2 AnchorMin { get; } public Vector2 AnchorMax { get; } public Vector2 Pivot { get; }
    public Vector2 OffsetMin { get; } public Vector2 OffsetMax { get; }

    public static RectSpec Stretch();                                              // 全面
    public static RectSpec Stretch(float left, float right, float top, float bottom); // 全面から内側へ（offsetMin (left,bottom)、offsetMax (−right,−top)）
    public static RectSpec Top(float height);                                      // 上帯
    public static RectSpec Bottom(float height);                                   // anchorMin (0,0)、anchorMax (1,0)、pivot (0.5,0)、offsetMin (0,0)、offsetMax (0,height)
    public static RectSpec Center(float width, float height);                      // 中央
}

/// <summary>レイアウトグループの設定。</summary>
public readonly struct LayoutSpec
{
    public LayoutSpec(float spacing, int left, int right, int top, int bottom, TextAnchor alignment,
        bool controlWidth, bool controlHeight, bool expandWidth, bool expandHeight);
    // プロパティは引数と同じ名前の PascalCase
    public static LayoutSpec Column { get; }   // spacing 24、padding 32、UpperCenter、制御 true/true、伸ばす true/false（旧 CreateVertical）
    public static LayoutSpec Row { get; }      // spacing 24、padding 0、MiddleCenter、制御 true/true、伸ばす true/true（旧 CreateHorizontal）
}

/// <summary>TMP の文字の設定。</summary>
public sealed class TextSpec
{
    public TextSpec(int fontSize, Color color, TextAlignmentOptions alignment, bool wrap, bool ellipsis);
    public int FontSize { get; } public Color Color { get; } public TextAlignmentOptions Alignment { get; }
    public bool Wrap { get; }       // true → TextWrappingModes.Normal、false → NoWrap
    public bool Ellipsis { get; }   // true → TextOverflowModes.Ellipsis、false → Overflow
    public static TextSpec Title { get; }       // 56、Text、Center、wrap false、ellipsis true
    public static TextSpec Body { get; }        // 40、Text、MidlineLeft、wrap true、ellipsis false
    public static TextSpec BodyCenter { get; }  // 40、Text、Center、wrap true、ellipsis false
    public static TextSpec Small { get; }       // 32、TextMuted、MidlineLeft、wrap true、ellipsis false
    public static TextSpec SingleLine { get; }  // 40、Text、MidlineLeft、wrap false、ellipsis true
}

public enum ButtonStyle { Primary, Secondary, Danger }

/// <summary>Prefab を 1 つ作る生成クラス。引数なしのコンストラクターを持つ。PrefabBuildCommands.BuildAll が TypeCache で集める。</summary>
public interface IPrefabBuilder
{
    /// <summary>小さい順に作る。部品 100〜199、画面の行の部品 1000〜1999、画面 2000〜。同じ値なら AssetPath の序数順。</summary>
    int Order { get; }
    /// <summary>作る Prefab のパス（"Assets/Prefabs/..." で始まり ".prefab" で終わる）。</summary>
    string AssetPath { get; }
    /// <summary>Prefab を作って AssetPath に保存する（既にあれば上書き。GUID は保たれる）。</summary>
    void Build();
}
```

#### `PrefabBuildUtil`（共通の補助）

```csharp
public static class PrefabBuildUtil
{
    public const string PartsFolder = "Assets/Prefabs/Parts";
    public const string ScreensFolder = "Assets/Prefabs/Screens";

    /// <summary>PartsFolder + "/" + partName + ".prefab"。</summary>
    public static string PartPath(string partName);
    /// <summary>ScreensFolder + "/" + feature + "/" + name + ".prefab"（feature はフォルダ名。例 "Home"）。</summary>
    public static string ScreenPath(string feature, string name);

    // --- 作業用のシーン ---
    /// <summary>EditorSceneManager.NewPreviewScene() を開いて保持する。既に開いていれば何もしない。BuildAll が最初に呼ぶ。</summary>
    public static void BeginSession();
    /// <summary>保持しているプレビューのシーンを EditorSceneManager.ClosePreviewScene で閉じる。BuildAll が finally で呼ぶ。</summary>
    public static void EndSession();

    // --- 作成・保存 ---
    /// <summary>new GameObject(name, typeof(RectTransform)) を作り（セッションのシーンへ SceneManager.MoveGameObjectToScene。セッションが無ければ InvalidOperationException）、RectSpec.Stretch() を当て、localScale 1 にして返す。</summary>
    public static GameObject CreateRoot(string name);
    /// <summary>folderPath（"Assets/..."）が無ければ、親から順に AssetDatabase.CreateFolder で作る。</summary>
    public static void EnsureFolder(string folderPath);
    /// <summary>
    /// EnsureFolder(親フォルダ) → PrefabUtility.SaveAsPrefabAsset(root, assetPath, out var success) → Object.DestroyImmediate(root)。
    /// success が false なら InvalidOperationException("prefab save failed: " + assetPath)。既存のアセットは上書き（.meta と GUID は保たれる）。
    /// 戻り値は保存したアセット（AssetDatabase.LoadAssetAtPath&lt;GameObject&gt;）。
    /// </summary>
    public static GameObject SavePrefab(GameObject root, string assetPath);

    // --- 子 ---
    /// <summary>new GameObject(name, typeof(RectTransform)) を parent の子にし（SetParent(parent, false)）、spec を当てる。spec 省略時は Stretch。</summary>
    public static RectTransform AddChild(Transform parent, string name);
    public static RectTransform AddChild(Transform parent, string name, RectSpec spec);
    /// <summary>
    /// PartPath(partName) の Prefab を PrefabUtility.InstantiatePrefab で parent の下に置き（無ければ InvalidOperationException("part not found: " + partName)）、
    /// GameObject.name = name にする。&lt;T&gt; 版はルートの GetComponent&lt;T&gt;()（無ければ InvalidOperationException）。
    /// </summary>
    public static GameObject InstantiatePart(Transform parent, string partName, string name);
    public static T InstantiatePart<T>(Transform parent, string partName, string name) where T : Component;

    // --- RectTransform ---
    public static void SetRect(RectTransform rect, RectSpec spec);   // anchorMin/anchorMax/pivot/offsetMin/offsetMax の順に代入。MarkModified(rect)

    // --- 見た目 ---
    /// <summary>Image を付け（あれば使う）、color・raycastTarget を入れる。sprite = null、type = Simple。MarkModified。</summary>
    public static Image AddImage(GameObject target, Color color, bool raycastTarget);
    /// <summary>Button を付け（あれば使う）、3.4 の共通の値と targetGraphic を入れる。MarkModified。</summary>
    public static Button AddButton(GameObject target, Graphic targetGraphic);
    /// <summary>TextMeshProUGUI を付け（あれば使う）、text と ApplyText(spec) を当てる。</summary>
    public static TextMeshProUGUI AddText(GameObject target, string text, TextSpec spec);
    /// <summary>font = DefaultFont、fontSize・color・alignment・textWrappingMode・overflowMode を spec から、richText = false、raycastTarget = false、enableAutoSizing = false。MarkModified。</summary>
    public static void ApplyText(TMP_Text text, TextSpec spec);
    /// <summary>FontAssets.LoadRegular() を返す（Regular の SDF。TMP Settings の既定フォントと同じアセット）。</summary>
    public static TMP_FontAsset DefaultFont { get; }
    /// <summary>FontAssets.LoadBold() を返す。太字の文字は ApplyText のあとに font = BoldFont を入れて MarkModified する（fontStyle は Normal のまま）。</summary>
    public static TMP_FontAsset BoldFont { get; }

    // --- レイアウト ---
    public static VerticalLayoutGroup AddVertical(GameObject target, LayoutSpec spec);     // 付けて（あれば使う）値を入れる。childScale は false。MarkModified
    public static HorizontalLayoutGroup AddHorizontal(GameObject target, LayoutSpec spec);
    /// <summary>LayoutElement を付け（あれば使う）、引数が −1 でないものだけ入れる。MarkModified。</summary>
    public static LayoutElement SetLayout(GameObject target, float minHeight = -1f, float preferredHeight = -1f,
        float preferredWidth = -1f, float flexibleWidth = -1f, float flexibleHeight = -1f);
    /// <summary>ContentSizeFitter（horizontalFit = Unconstrained、verticalFit = PreferredSize）。</summary>
    public static ContentSizeFitter AddVerticalFitter(GameObject target);

    // --- 部品を置く（部品の Prefab をネストして値をオーバーライドする） ---
    /// <summary>InstantiatePart(parent, "Label", name) → ApplyText(spec) → text。</summary>
    public static TextMeshProUGUI AddLabel(Transform parent, string name, string text, TextSpec spec);
    /// <summary>InstantiatePart&lt;ButtonView&gt;(parent, "Button", name)。Label の text = label、Image の color を style の色に。</summary>
    public static ButtonView AddButtonPart(Transform parent, string name, string label, ButtonStyle style = ButtonStyle.Primary);
    /// <summary>InstantiatePart(parent, "Panel", name) の RectTransform。</summary>
    public static RectTransform AddPanel(Transform parent, string name);
    /// <summary>InstantiatePart&lt;HeaderView&gt;(parent, "Header", "Header")。TitleText の text = title。</summary>
    public static HeaderView AddHeader(Transform parent, string title);
    /// <summary>
    /// InstantiatePart&lt;TabBarView&gt;(parent, "TabBar", name)。tabNames と labels の長さが違う・0 なら ArgumentException。
    /// 各 i について InstantiatePart&lt;TabView&gt;(bar, "Tab", tabNames[i]) を置き、Label の text = labels[i]。
    /// 最後に SetRefs(bar, "tabs", 置いた TabView の配列)。
    /// </summary>
    public static TabBarView AddTabBar(Transform parent, string name, string[] tabNames, string[] labels);
    /// <summary>
    /// 縦スクロール（旧 foundation CreateScrollContent と同じ形）: AddChild(parent, name) + ScrollRect（horizontal false、vertical true、movementType Clamped、scrollSensitivity 30）+ SetLayout(flexibleHeight: 1)、
    /// 子 "Viewport"（全面、Image Transparent raycastTarget true、RectMask2D）、その子 "Content"（anchorMin (0,1)、anchorMax (1,1)、pivot (0.5,1)、offset 0、
    /// VLG spacing 24・padding 0・UpperCenter・制御 true/true・伸ばす true/false、AddVerticalFitter）。ScrollRect.viewport = Viewport、content = Content。
    /// scrollRect に ScrollRect を返し、戻り値は Content。
    /// </summary>
    public static RectTransform AddScrollList(Transform parent, string name, out ScrollRect scrollRect);

    // --- 画面の雛形（3.4 の表のとおり） ---
    /// <summary>CreateRoot(name) + AddImage(Background, true) + AddComponent&lt;TPage&gt;() + 子 "Body"（Stretch + SafeAreaFitter）+ SetRef(page, "body", Body)。</summary>
    public static TPage CreatePage<TPage>(string name, out RectTransform body) where TPage : PageBase;
    /// <summary>CreateRoot(name) + AddComponent&lt;TModal&gt;() + 子 "Panel"（Center(ModalWidth, 0)、Image Panel、AddVertical(40/24/UpperCenter/true,true,true,false)、AddVerticalFitter）+ SetRef(modal, "panel", Panel)。</summary>
    public static TModal CreateModal<TModal>(string name, out RectTransform panel) where TModal : ModalBase;

    // --- 参照の代入 ---
    /// <summary>
    /// new SerializedObject(target).FindProperty(fieldName)。null なら InvalidOperationException(target の型名 + "." + fieldName + " not found")。
    /// objectReferenceValue = value → ApplyModifiedPropertiesWithoutUndo()。value が null でも代入する（テストが見つける）。
    /// </summary>
    public static void SetRef(Object target, string fieldName, Object value);
    /// <summary>配列のフィールド: arraySize = values.Length、各要素の objectReferenceValue を入れて Apply。</summary>
    public static void SetRefs(Object target, string fieldName, Object[] values);

    // --- Addressables ---
    /// <summary>AddressablesSetup.RegisterScreen(prefabPath) を呼ぶ（画面の生成クラスが SavePrefab のあとに呼ぶ）。</summary>
    public static void RegisterScreen(string prefabPath);

    /// <summary>component がプレハブのインスタンスの一部なら PrefabUtility.RecordPrefabInstancePropertyModifications(component)。上のメソッドが値を変えたあとに呼ぶ。</summary>
    public static void MarkModified(Component component);
}
```

#### `FontAssets`・`TmpSettingsBuilder`・`AddressablesSetup`

フォントのアセットは作成済み（前提 2）。`FontAssets` は読み込みと確かめだけを行い、アセットを作らない・設定を変えない。

```csharp
public static class FontAssets
{
    public const string RegularPath = "Assets/Fonts/NotoSansJP/NotoSansJP-Regular SDF.asset";
    public const string BoldPath = "Assets/Fonts/NotoSansJP/NotoSansJP-Bold SDF.asset";

    /// <summary>Load(RegularPath)。</summary>
    public static TMP_FontAsset LoadRegular();
    /// <summary>Load(BoldPath)。</summary>
    public static TMP_FontAsset LoadBold();

    /// <summary>
    /// AssetDatabase.LoadAssetAtPath&lt;TMP_FontAsset&gt;(path)。null なら InvalidOperationException("font asset not found: " + path + "（PR #29 のフォントが main に入っているか確かめる）")。
    /// atlasPopulationMode が Dynamic でなければ InvalidOperationException("font asset is not dynamic: " + path)。
    /// </summary>
    private static TMP_FontAsset Load(string path);

    /// <summary>
    /// RegularPath・BoldPath のそれぞれについて、アセットがあれば ClearFontAssetData(setAtlasSizeToZero: true) → EditorUtility.SetDirty。最後に SaveAssets。無ければ何もしない。
    /// Editor で文字を描いて足された動的データを空に戻す（決めたこと 4。コミット済みのアセットはアトラスが空）。
    /// </summary>
    public static void ClearDynamicData();
}

public static class TmpSettingsBuilder
{
    public const string SettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";   // uGUI 7.0.0 の Project Settings > TextMesh Pro が作る場所と同じ
    private const string BuiltInSettingsGuid = "0104df30113748678d4d2f9079d0d107";           // パッケージ同梱の TMP Settings（TMP_Settings.k_BuiltInSettingsGUID）

    /// <summary>
    /// SettingsPath に TMP_Settings が無ければ: パッケージ同梱の設定（BuiltInSettingsGuid）を Object.Instantiate で複製し、name = "TMP Settings"、
    /// SerializedObject で "m_IsBuiltInSettings" = false にして、フォルダを作って AssetDatabase.CreateAsset(SettingsPath)。
    /// あってもなくても SerializedObject で "m_defaultFontAsset" = font を入れ、SetDirty → SaveAssets → AssetDatabase.ImportAsset(SettingsPath)
    /// （TMP の TMP_SettingsPostProcessor が静的なキャッシュを捨てる）。
    /// 最後に TMP_Settings.defaultFontAsset が font でなければ InvalidOperationException（止まる条件）。
    /// プロジェクトに TMP_Settings（同梱以外）が SettingsPath 以外にもあれば InvalidOperationException（TMP は 1 つしか使わない）。
    /// </summary>
    public static void EnsureProjectSettings(TMP_FontAsset font);
}

public static class AddressablesSetup
{
    public const string ScreensGroupName = "Screens";

    /// <summary>
    /// AddressableAssetSettingsDefaultObject.GetSettings(true)（無ければ Assets/AddressableAssetsData/ に作る）。
    /// settings.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer（PlayerBuildOption は AddressableAssetSettings の入れ子の enum）。
    /// FindGroup(ScreensGroupName) が null なら CreateGroup(ScreensGroupName, false, false, true, settings.DefaultGroup.Schemas)（Default Local Group のスキーマの写し）。
    /// SetDirty → SaveAssets。戻り値は Screens グループ。
    /// </summary>
    public static AddressableAssetGroup EnsureSettingsAndGroup();

    /// <summary>
    /// prefabPath の GUID（AssetDatabase.AssetPathToGUID。空なら InvalidOperationException）を
    /// settings.CreateOrMoveEntry(guid, Screens グループ, false, true) で入れ、address = Path.GetFileNameWithoutExtension(prefabPath)、
    /// 付いているラベルは全部外す（entry.labels を写してから SetLabel(label, false)）。SetDirty → SaveAssets。
    /// </summary>
    public static void RegisterScreen(string prefabPath);
}
```

#### `PrefabBuildCommands`（入口）

```csharp
public static class PrefabBuildCommands
{
    /// <summary>
    /// 1. FontAssets.LoadRegular()・FontAssets.LoadBold()（無ければ例外） 2. TmpSettingsBuilder.EnsureProjectSettings(Regular) 3. AddressablesSetup.EnsureSettingsAndGroup()
    /// 4. PrefabBuildUtil.BeginSession() 5. TypeCache.GetTypesDerivedFrom&lt;IPrefabBuilder&gt;() のうち抽象でないクラスを Activator.CreateInstance し、
    ///    Order → AssetPath（StringComparer.Ordinal）の順に Build()。AssetPath が重複していれば、作り始める前に InvalidOperationException
    /// 6. finally で PrefabBuildUtil.EndSession() 7. FontAssets.ClearDynamicData() 8. AssetDatabase.SaveAssets()
    /// 9. Debug.Log("[Uta] prefabs: built " + 件数) し、"built " + 件数 を返す。
    /// 例外はそのまま投げる（eval・バッチがエラーとして受け取る）。
    /// </summary>
    public static string BuildAll();

    /// <summary>-batchmode -executeMethod 用。BuildAll() を呼び、成功で EditorApplication.Exit(0)、例外は Debug.LogException して Exit(1)。</summary>
    public static void BuildAllBatch();

    [MenuItem("Uta/Prefabs/Build All")]
    private static void BuildAllMenu();            // BuildAll() を呼ぶだけ
}
```

呼び方（Editor が開いているとき。unity7-evaluation 4章の `unity command eval`）: `unity command eval 'return Uta.Client.Editor.Prefabs.PrefabBuildCommands.BuildAll();'`。Editor が閉じているとき: `Unity.exe -batchmode -nographics -projectPath client/uta -executeMethod Uta.Client.Editor.Prefabs.PrefabBuildCommands.BuildAllBatch -logFile <ログ>`（タスク 4 で動くことを確かめ、4章に足す）。

#### 部品ごとの生成クラス（`Uta.Client.Editor.Prefabs.Parts`）

すべて `public sealed class <名前>Builder : IPrefabBuilder`。`AssetPath` は `PrefabBuildUtil.PartPath("<部品名>")`。`Build()` は `CreateRoot` → 3.4 の表のとおりに組む → 参照を `SetRef` で入れる → `SavePrefab`。ネストする部品は先に作られている必要があるので Order を次にする:

| クラス | Order | 作るもの | 使う部品 |
|---|---|---|---|
| `LabelBuilder` | 100 | `Label` | — |
| `ButtonBuilder` | 110 | `Button` | — |
| `PanelBuilder` | 120 | `Panel` | — |
| `TabBuilder` | 130 | `Tab` | — |
| `TabBarBuilder` | 140 | `TabBar` | — |
| `ListRowBuilder` | 150 | `ListRow` | `Label` |
| `HeaderBuilder` | 160 | `Header` | `Button`、`Label` |
| `DialogBuilder` | 170 | `Dialog` | `Label`、`Button`（アセットの参照） |
| `BusyBlockerBuilder` | 180 | `BusyBlocker` | — |
| `UiRootBuilder` | 190 | `UiRoot` | `BusyBlocker`、`Dialog`（アセットの参照） |

- `DialogBuilder` の `buttonPrefab` と `UiRootBuilder` の `dialogPrefab` は、`AssetDatabase.LoadAssetAtPath<GameObject>(PartPath(...)).GetComponent<…View>()` を `SetRef` で入れる（ネストではない）
- `UiRootBuilder` の `EventSystem` は `new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule))` を `SetParent(root, false)` し、`GetComponent<InputSystemUIInputModule>().AssignDefaultActions()`
- P2・P3 の書き方: 画面ごとに `public sealed class <名前>PageBuilder : IPrefabBuilder`（Order 2000、行の部品は 1000）を `Scripts/Editor/Prefabs/Screens/<機能>/` に作り、`CreatePage<THomePage>("HomePage", out var body)` → 中身 → `SavePrefab(root, ScreenPath("Home", "HomePage"))` → `RegisterScreen(同じパス)`。Build All への登録は要らない

### 3.6 PrefabContractTests（EditMode。`Tests/EditMode/`）

```csharp
/// <summary>P2・P3 のテストも使う補助。</summary>
public static class PrefabContract
{
    /// <summary>"Assets/Prefabs" の下の全 Prefab のパス（AssetDatabase.FindAssets("t:Prefab", new[] { folder }) を GUIDToAssetPath して序数順に並べる）。</summary>
    public static string[] FindPrefabPaths(string folder);

    /// <summary>
    /// prefab の子孫（非アクティブも含む）の MonoBehaviour のうち、型の Assembly の名前が "Uta.Client.Presentation" のものについて、
    /// SerializedObject の全プロパティを NextVisible(true) で回り、propertyType == ObjectReference かつ objectReferenceValue == null のものを集める。
    /// "m_Script" は除く。フィールドは、プロパティのパスの最初の "." の前の名前で、型とその基底から GetField(NonPublic | Public | Instance | DeclaredOnly) で探す。
    /// 見つかったフィールドの DeclaringType の Assembly が "Uta.Client.Presentation" でないもの（USN の Page・Modal のフィールド）と、
    /// [OptionalReference] の付いたものは除く。戻り値は "<GameObject の階層のパス>.<型名>.<プロパティのパス>" の一覧。
    /// </summary>
    public static List<string> CollectMissingReferences(GameObject prefab);
}
```

| テスト | 確かめること |
|---|---|
| `AllPrefabs_SerializedReferences_AreAssigned` | `FindPrefabPaths("Assets/Prefabs")` の全部について `CollectMissingReferences` が空（失敗の文に Prefab のパスと一覧を入れる。1 本のテストで全部回し、`Assert.That(errors, Is.Empty)`） |
| `Parts_AllExist` | 3.4 の 10 個の `PartPath` が読める |
| `UiRoot_HasLayersInOrder` | `UiRoot.prefab` の子の名前が順に `Background`・`Pages`・`Modals`・`Overlays`・`Busy`・`Dialogs`・`EventSystem`。`Overlays` に `ModalContainer` と `OverlayContainer` の両方がある。`CanvasScaler` が `ScaleWithScreenSize`・(1080,1920)・Match 0 |
| `ScreenPrefabs_RootViewIsNamedAfterPrefab` | `Assets/Prefabs/Screens` の下の各 Prefab のルートに `PageBase` か `ModalBase` の派生があり、その型名がファイル名と同じ（0 個なら何もせず通る） |
| `ScreenPrefabs_AreRegisteredInScreensGroup` | 同じ各 Prefab が、`AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(guid)` で見つかり、グループ名 `Screens`、`address` がファイル名、`labels` が空（0 個なら通る） |
| `ScreensGroup_ContainsOnlyScreenPrefabs` | `Screens` グループの全エントリのパスが `Assets/Prefabs/Screens/` で始まる（スパイクの残りや部品の登録の誤りを見つける） |
| `TmpSettings_DefaultFont_IsNotoSansJpDynamic` | `TMP_Settings.defaultFontAsset` が `FontAssets.RegularPath` のアセットで、`atlasPopulationMode == Dynamic`。`FontAssets.BoldPath` のアセットも読めて Dynamic |

- 画面の計画（P2・P3）は、Prefab を足すだけで上の共通の確かめの対象になる。画面固有の確かめ（子の名前など）が要るときは、自分のテストファイル（`<機能>PrefabTests.cs`）に書き、`PrefabContract` を使う。`PrefabContractTests` は書き換えない
- 文字の描画で動的フォントにグリフが足されると `.asset` が汚れるので、EditMode のテストでは `HasCharacters(…, tryAddCharacter: true)` を呼ばない（日本語が出ることはスパイクと Play で確かめる）

### 3.7 `BuildCommands` の変更

`BuildWindowsIl2Cpp()` の先頭（`SetScriptingBackend` の前）に次を足す（`using UnityEditor.AddressableAssets.Settings;`）:

1. `EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64` なら `Debug.LogError("[Uta] build: active target is " + …)` → `EditorApplication.Exit(1)` → return（Addressables の内容は今のビルドターゲット向けに作られるため）
2. `AddressableAssetSettings.BuildPlayerContent(out var result);` `!string.IsNullOrEmpty(result.Error)` なら `Debug.LogError("[Uta] addressables: " + result.Error)` → `EditorApplication.Exit(1)` → return
3. そのあとは今のまま（`BuildPipeline.BuildPlayer`）

## 4. タスク

各タスクの終わりに、Editor のコンパイルエラーが 0（`unity recompile --format json`）で、既存の EditMode のテストが通る状態にする。Editor が開けないときは、どのタスクも「確認できていない」と書いて止まり、ユーザーに実行を頼む。

### タスク 1: スパイク（Addressables・TMP が Unity 7 で動くか。止まる条件あり）

触る: `Scripts/Editor/Uta.Client.Editor.asmdef`（3.1）、`Scripts/Editor/Prefabs/FontAssets.cs`・`TmpSettingsBuilder.cs`・`AddressablesSetup.cs`（3.5。本番のもの）、`Spike/` 一式（使い捨て）、`uta_docs/wiki/architecture/unity7-evaluation.md`。

実施済み（2026-11-08、コミット `b998380`）。手順 6・8 の Editor を閉じる・開く（`unity close .`・`unity open .`）とプレイヤーのビルドは、このスパイクでだけ行った。以後の Prefab の作業では Editor を閉じず、ビルドもしない（決めたこと 12・19）。

フォントのアセット（前提 2）で確かめてあるのは、`com.unity.ugui` 7.0.0 に TMPro の型があること、`Assets/` に `TextMesh Pro` フォルダが無くても `Shader.Find("TextMeshPro/Distance Field")` が見つかること、アセットのシェーダーが `TextMeshPro/Mobile/Distance Field` であること、Editor で日本語 15 文字がアトラスに載ること。**このタスクで確かめるのは、実際の `TMP_Text` の表示（Editor の Play・プレイヤー）、TMP Settings の既定フォントの設定状況、プレイヤービルド**。

手順:
1. 前提 2 の 5 つのファイルがあることを確かめる。無ければ止まって報告する
2. Editor の asmdef に参照を足し、`FontAssets`・`TmpSettingsBuilder`・`AddressablesSetup` を 3.5 のとおり作る。`unity recompile --format json` でエラー 0。続けて、手を入れる前の TMP Settings の状況を記録する: `unity command eval 'return (UnityEditor.AssetDatabase.LoadAssetAtPath<TMPro.TMP_Settings>("Assets/TextMesh Pro/Resources/TMP Settings.asset") != null) + " default=" + (TMPro.TMP_Settings.defaultFontAsset != null ? TMPro.TMP_Settings.defaultFontAsset.name : "null");'` の結果を報告に書く（どの値でも止まらない）
3. 使い捨てのファイルを作る:
   - `Spike/Uta.Spike.asmdef`（name `Uta.Spike`、references `UnityScreenNavigator`・`Unity.TextMeshPro`・`UnityEngine.UI`、autoReferenced true）と `Spike/AddressablesSpikeProbe.cs`（`Uta.Spike.AddressablesSpikeProbe : MonoBehaviour`）。`Start` のコルーチンで: Canvas（ScreenSpaceOverlay、sortingOrder 1000）+ CanvasScaler（3.4 の UiRoot と同じ値）+ GraphicRaycaster の GameObject を作り、その子 `Pages`（全面）に `PageContainer` を付け、`AssetLoader = new AddressableAssetLoader()`。`var handle = container.Push("SpikePage", false);` を `yield return`。`handle.Exception` が null で `container.Pages.Count == 1` なら `Debug.Log("[Uta] spike addressables: ok")`、そうでなければ `Debug.LogError("[Uta] spike addressables: FAILED " + handle.Exception)`。続けて読み込んだページの `GetComponentsInChildren<TMP_Text>()`（`Label` と `BoldLabel` の 2 つ）のそれぞれについて `font.HasCharacters(text.text, out uint[] missing, false, true)` が true なら `Debug.Log("[Uta] spike tmp: ok font=" + font.name)`、false なら `Debug.LogError("[Uta] spike tmp: FAILED font=" + font.name + " missing=" + missing.Length)`（2 つとも ok で tmp を ok とする）。さらに `TMP_Settings.defaultFontAsset` が null でなく名前が `"NotoSansJP-Regular SDF"` なら `Debug.Log("[Uta] spike tmp-settings: ok")`、そうでなければ `Debug.LogError("[Uta] spike tmp-settings: FAILED default=" + 名前か "null")`。`Application.isBatchMode` なら、3 つ（addressables・tmp・tmp-settings）とも ok で `Application.Quit(0)`、それ以外 `Application.Quit(1)`
   - `Spike/Editor/Uta.Spike.Editor.asmdef`（name `Uta.Spike.Editor`、includePlatforms `Editor`、references `Uta.Spike`・`Uta.Client.Editor`・`UnityScreenNavigator`・`Unity.TextMeshPro`・`UnityEngine.UI`・`Unity.Addressables`・`Unity.Addressables.Editor`）と `Spike/Editor/SpikeCommands.cs`:
     - `public static string CreateSpikeAssets()`: `var font = FontAssets.LoadRegular()`・`var bold = FontAssets.LoadBold()` → `TmpSettingsBuilder.EnsureProjectSettings(font)` → `AddressablesSetup.EnsureSettingsAndGroup()` → `Spike/SpikePage.prefab`（ルート `SpikePage`: RectTransform・Image `(0.10,0.11,0.15,1)`・`Page`、子 `Label`: 上半分（anchorMin (0,0.5)、anchorMax (1,1)、offset 0）・`TextMeshProUGUI` text "日本語の表示を確かめる。" fontSize 56 font = `font`・Center、子 `BoldLabel`: 下半分（anchorMin (0,0)、anchorMax (1,0.5)、offset 0）・`TextMeshProUGUI` text "太字の表示を確かめる。" fontSize 56 font = `bold`・fontStyle Normal・Center）を `PrefabUtility.SaveAsPrefabAsset` で保存 → `AddressablesSetup.RegisterScreen("Assets/Spike/SpikePage.prefab")` → 専用のシーン `Assets/Spike/Spike.unity` を作る: `var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);`、`new GameObject("Main Camera", typeof(Camera))` と `new GameObject("SpikeProbe", typeof(AddressablesSpikeProbe))` を作って `SceneManager.MoveGameObjectToScene(…, scene)`、`EditorSceneManager.SaveScene(scene, "Assets/Spike/Spike.unity")`（false なら例外）→ `EditorSceneManager.CloseScene(scene, true)`（今開いているシーンには触らない）→ `FontAssets.ClearDynamicData()` → `"ok"` を返す
     - `public static string PreparePlay()`: 開いているシーンに未保存の変更（`SceneManager.GetSceneAt(i).isDirty`）が 1 つでもあれば `"dirty: " + そのシーンのパス` を返して何もしない。無ければ、いまのアクティブなシーンのパスを `EditorPrefs.SetString("Uta.Spike.PreviousScene", パス)` に残し、`EditorSceneManager.OpenScene("Assets/Spike/Spike.unity", OpenSceneMode.Single)` → `EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Spike/Spike.unity")`（既存の `BootstrapPlayMode` が Play の開始を `Bootstrap` に固定しているので、スパイクの間だけ差し替える）→ `"ok"`
     - `public static string RestorePlay()`: `EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Bootstrap.unity")`（`BootstrapPlayMode` と同じ値に戻す）→ `EditorPrefs` に残したパスが空でなければ `OpenScene(そのパス, OpenSceneMode.Single)`、空なら `Assets/Scenes/Bootstrap.unity` を開く → `EditorPrefs.DeleteKey("Uta.Spike.PreviousScene")` → `"ok"`
     - `public static void BuildWindowsIl2Cpp()`: `BuildCommands.BuildWindowsIl2Cpp` と同じ設定で、3.7 の 1・2 を行ってから、scenes = `{ "Assets/Spike/Spike.unity" }`、locationPathName = `"Build/Spike/UtaSpike.exe"` でビルドし、成否で `EditorApplication.Exit(0/1)`
4. Editor で: `unity command eval 'return Uta.Spike.Editor.SpikeCommands.CreateSpikeAssets();'` が `ok`
5. Editor の Play（Addressables の Play Mode Script は既定の「Use Asset Database」のまま）。**Play は常に専用シーン `Assets/Spike/Spike.unity` から始める**。`Bootstrap`・`Title` のシーンもサーバーも使わない（`Bootstrap` はサーバーからマスタを読んでから `Title` へ進むので、そちらを通す手順は取らない）:
   1. `unity command eval 'return Uta.Spike.Editor.SpikeCommands.PreparePlay();'` が `ok`（`EditorSceneManager.OpenScene` で Spike を開き、`BootstrapPlayMode` が差し替えた `playModeStartScene` をスパイクの間だけ Spike に固定する。`dirty: …` なら、ユーザーにそのシーンを保存するか変更を捨てるかを決めてもらってからやり直す。勝手に保存・破棄しない）
   2. `unity command editor.playmode start` → 数秒待つ → `unity command eval 'return UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;'` が `Assets/Spike/Spike.unity` であること。**別のシーン（`Assets/Scenes/Bootstrap.unity` など）だったら**（Play に入るときのドメインの再読み込みで `BootstrapPlayMode` が開始シーンを戻した可能性。推測）、その結果を報告に書き、差し替えを Play の開始の直前にもう一度行う（止まらない）: `unity command editor.playmode stop` → `unity command eval 'UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>("Assets/Spike/Spike.unity"); UnityEditor.EditorApplication.EnterPlaymode(); return "ok";'` → 数秒待つ → 上の eval でアクティブなシーンが `Assets/Spike/Spike.unity` であること。これでも別のシーンなら止まる（S6）
   3. `unity command console get --tail 50 --format json` で `[Uta] spike addressables: ok`・`[Uta] spike tmp: ok`（2 行。Regular と Bold）・`[Uta] spike tmp-settings: ok` があること
   4. ユーザーに Game ビューで日本語が出ていること（豆腐（□）でないこと）と、下の行が太字（Bold のアセット）で出ていることを見てもらう（手動）
   5. `unity command editor.playmode stop` → `unity command eval 'return Uta.Spike.Editor.SpikeCommands.RestorePlay();'` が `ok`
6. Editor を閉じ（`unity command eval 'UnityEditor.AssetDatabase.SaveAssets(); return "ok";'` のあと、`client/uta` で `unity close .`。`unity close` は保存せずに閉じる。閉じられなければユーザーに Editor を閉じてもらう）、バッチモードで `Unity.exe -batchmode -nographics -projectPath client/uta -buildTarget Win64 -executeMethod Uta.Spike.Editor.SpikeCommands.BuildWindowsIl2Cpp -logFile <ログ>` が終了コード 0。`client/uta/Build/Spike/UtaSpike.exe -batchmode -nographics -logFile <ログ>` が終了コード 0 で、ログに手順 5 と同じ `ok` の行（addressables・tmp 2 行・tmp-settings）がある。**`-nographics` で tmp だけ失敗したときは**、`-nographics` を外して（`-batchmode` は残す）もう一度実行し、両方の結果を記録する（外して通れば止まらない）。ユーザーに `UtaSpike.exe` を引数なしで起動してもらい、日本語が出ることを見てもらう（手動）
7. 結果を unity7-evaluation.md 2章の表に 1 行ずつ足す（Addressables 4.1.2: グループの作成・登録・Editor の Play・`BuildPlayerContent`・Windows の IL2CPP、TMP: プロジェクトの TMP Settings・動的 SDF の日本語・Editor の Play・IL2CPP。方法と実施日）。Essential Resources の取り込みが uGUI 7.0.0 で無くなったことも 4章に書く
8. 使い捨てを消す: Editor を開き（`client/uta` で `unity open .` → `unity status --until-ready` が ready。開けなければユーザーに開いてもらう）、`unity command eval 'UnityEditor.AssetDatabase.DeleteAsset("Assets/Spike"); return "ok";'`。`Screens` グループのエントリが 0 になったことを `unity command eval 'return UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings.FindGroup("Screens").entries.Count;'` が `0` で確かめる（0 でなければ、残ったエントリを `RemoveAssetEntry` で消す）
9. `unity command eval 'UnityEditor.AssetDatabase.SaveAssets(); return "ok";'` のあと、`git status` で残るのが `TextMesh Pro/`・`AddressableAssetsData/`・`Scripts/Editor/`（と `.meta`）・`ProjectSettings/` の変更だけであること（`Spike` が残っていないこと）。`Fonts/NotoSansJP/` に差分が出ていたら、差分の中身（どのフィールドか）を報告に書き、`git restore client/uta/Assets/Fonts/NotoSansJP` で戻す（決めたこと 4。止まらない）

**止まる条件**（当たったら、その時点のログを添えて止まって報告する。別の読み込み方法・legacy `Text` に変えない）:
- S1: Addressables の設定・グループの作成・登録で例外が出る、またはコンパイルエラーが出る
- S2: 手順 5 で `[Uta] spike addressables: FAILED`（Editor の Play で `AddressableAssetLoader` 経由の `Push` が失敗）
- S3: `BuildPlayerContent` の `result.Error` が空でない
- S4: 手順 6 で `UtaSpike.exe` が `-nographics` あり・なしの両方で終了コード 0 にならない、またはビルドが失敗する
- S5: `FontAssets.LoadRegular()`／`LoadBold()` が例外（アセットが無い・Dynamic でない）、`TMP_Settings.defaultFontAsset` が設定できない（`EnsureProjectSettings` の例外）、Editor の Play かプレイヤーで `spike tmp: FAILED` か `spike tmp-settings: FAILED`、目で見て日本語が豆腐になる
- S6: 手順 5 の 2 で、`EnterPlaymode()` の直前に差し替えても Play が `Assets/Spike/Spike.unity` から始まらない（`Bootstrap`・`Title` を通す手順に変えない）

終わったことの確かめ方: 手順 4〜6 がすべて通った。止まる条件に当たらない。unity7-evaluation.md に結果がある。`Spike/` が無い。既存の EditMode のテストが通る（テストの回し方はタスク 5 で決めるので、ここでは Editor の Test Runner の EditMode を全部実行してもらう。手動）。

### タスク 2: 実行時の型（UiTheme・基底・部品の View・UiRoot）

触る: `Scripts/Presentation/Uta.Client.Presentation.asmdef`（3.1）、`Scripts/Presentation/Common/` の 3.3 の全ファイルと `UiTheme.cs`（3.2）、`Common/Parts/` の 7 ファイル。

手順:
1. Presentation の asmdef の参照を 3.1 のとおりにする
2. 3.2・3.3 のとおりに作る（View・部品の View はフィールドだけ。`UiRoot`・`SafeAreaFitter`・`WindowAspectKeeper` は中身まで）
3. `unity recompile --format json` でエラー 0（CS0012 が出たら 3.1 の条件と一般則どおりに参照を足す）

境界: `UiRoot.EnsureConfigured` は 2 回目以降何もしない。`SafeAreaFitter` は画面の大きさが 0 のとき何もしない。

終わったことの確かめ方: コンパイルエラー 0。既存のテストが通る（手動の Test Runner。タスク 5 からは決めたこと 12 の回し方）。

### タスク 3: 生成の補助と入口

触る: `Scripts/Editor/Prefabs/PrefabBuildUtil.cs`・`RectSpec.cs`・`LayoutSpec.cs`・`TextSpec.cs`・`ButtonStyle.cs`・`IPrefabBuilder.cs`・`PrefabBuildCommands.cs`。

手順:
1. 3.5 のとおりに作る
2. `unity command eval 'return Uta.Client.Editor.Prefabs.PrefabBuildCommands.BuildAll();'` が `built 0`（生成クラスがまだ無い）。フォント・TMP Settings・Addressables の設定がそのまま（`git status` で `Fonts/NotoSansJP/`・`TextMesh Pro/`・`AddressableAssetsData/` に差分が出ない。出たら差分の中身を報告に書く。フォントの差分は `git restore` で戻す）

境界: `IPrefabBuilder` の `AssetPath` の重複で例外。セッションが無いのに `CreateRoot` を呼ぶと例外。`SetRef` のフィールド名の誤りで例外。

終わったことの確かめ方: コンパイルエラー 0。`built 0`。

### タスク 4: 共通部品の生成

触る: `Scripts/Editor/Prefabs/Parts/` の 10 クラス、生成物 `Prefabs/Parts/*.prefab`。

手順:
1. 3.4 の表と 3.5 の Order のとおりに 10 クラスを作る
2. `unity command eval 'return Uta.Client.Editor.Prefabs.PrefabBuildCommands.BuildAll();'` が `built 10`。`unity command console get --tail 50 --level error --format json` にエラーが無い
3. もう一度 `BuildAll()` を実行し、`git status`・`git diff --stat` で `Prefabs/Parts/` に差分が出るかを見る。**出ても止まらない**（fileID が作り直されている可能性。推測）。差分の有無と、出たならどの行か（`m_` のどの値か・fileID か）を報告に書く。`Fonts/NotoSansJP/` の 2 つの `.asset` に差分が出ていないこと（出たら `FontAssets.ClearDynamicData()` で戻りきっていない。差分の中身を報告に書き、`git restore` で戻す。止まらない）
4. ユーザーに、Editor で `UiRoot.prefab`・`Dialog.prefab`・`Header.prefab`・`ListRow.prefab` を Prefab モードで開いて、日本語の仮の文字が出ていること、3.4 の表の配置になっていることを見てもらう（手動）

バッチの入口（`BuildAllBatch`。Editor が閉じているとき用）は、実施時（2026-11-08）に 1 回だけ終了コード 0 を確かめた。決めたこと 19 により、以後の Prefab の作業では確かめない。

境界: 部品の Prefab が無い状態から（`Prefabs/Parts/` を消してから）でも、Order の順で全部できる。

終わったことの確かめ方: `built 10`、エラー無し、手動の見た目の確認が済んだ。

### タスク 5: PrefabContractTests とテストのコマンド

触る: `Tests/EditMode/Uta.Client.Infrastructure.Tests.EditMode.asmdef`（3.1）、`Tests/EditMode/PrefabContract.cs`・`PrefabContractTests.cs`、`CLAUDE.md`、`uta_docs/wiki/architecture/unity7-evaluation.md`。

実施済み（2026-11-08、コミット `4ca8753`）。手順 2〜4 は、実際に行った形（決めたこと 12。Editor を閉じない）で書いてある。

手順:
1. テストの asmdef の参照を足し、3.6 のとおりに作る
2. テストを回す（**テストの回し方**。P2・P3 と C# の計画もこれを使う）: Editor を開いたまま、[unity7-evaluation.md](../wiki/architecture/unity7-evaluation.md) 4章のコマンド（`unity command --timeout 300 eval` で `TestRunnerApi` を `runSynchronously = true` で実行し、`Temp/uta-test-results.xml` に書く）を実行し、XML の `<test-run>` の `failed` が 0 であることを見る。絞り込むときは同じコマンドの `Filter` に `groupNames = new[] { "<テストのクラス名>" }` を足す。結果: 全件 43/43 が通った
   - `unity test --mode EditMode` は 1 回だけ試した（開いた Editor があると終了コード 6。閉じれば動く）。以後は使わない（4章に記録）
3. わざと壊して効くことを確かめる: `unity command eval` で `Header.prefab` の `HeaderView.titleText` を `PrefabBuildUtil.SetRef(…, "titleText", null)` で外して保存 → 手順 2 で回すと、失敗したのは `AllPrefabs_SerializedReferences_AreAssigned` だけ → `BuildAll()`（eval）で戻し、手順 2 で全部通った
4. 回し方を unity7-evaluation.md 4章に書いた。CLAUDE.md「ビルド・テスト・実行」の行はタスク 7 で足す
5. `.gitignore` は変えない（結果は `Temp/` の下に出るため）

境界: `PrefabContract.FindPrefabPaths` は、フォルダ（`Assets/Prefabs/Screens` など）が無いと空を返す（画面の Prefab が 0 個で通るため）。

終わったことの確かめ方: 手順 2 で既存のテストと `PrefabContractTests` が全部通る。手順 3 で壊すと失敗する。

### タスク 6: プレイヤーのビルドに BuildPlayerContent を足す

触る: `Scripts/Editor/BuildCommands.cs`（3.7）、`uta_docs/wiki/architecture/unity7-evaluation.md`。

実施済み（2026-11-08、コミット `fa27662`）。決めたこと 19 により、プレイヤーのビルド・大きさの測定・`-uta-quit-after-check` はこのタスクから外した。

手順:
1. 3.7 のとおりに変える（ビルドターゲットの確かめと `BuildPlayerContent`）
2. `BuildPlayerContent` を Editor の `unity command eval` で単体で実行し、`result.Error` が空であること（`Screens` が空なので、できるのは `catalog.bin` などだけでバンドルは無い）
3. unity7-evaluation.md 2章の「シーン構成 … Windows の IL2CPP」の行に、`BuildPlayerContent` を先に実行するようにしたことと、足したあとの IL2CPP のビルドは未確認であることを書く

終わったことの確かめ方: コンパイルエラー 0。手順 2 で `result.Error` が空。

### タスク 7: 結果の記録（wiki・CLAUDE.md・計画）

触る: `uta_docs/wiki/architecture/screen-design.md`、`uta_docs/wiki/architecture/repository-layout.md`、`uta_docs/wiki/architecture/unity7-evaluation.md`、`CLAUDE.md`、この計画。

手順:
1. screen-design.md（frontmatter の `updated` を実施日に）:
   - 7.3 の「**未確認**」を、タスク 1 の結果（`TMP_Text` の表示・TMP Settings の既定フォント・Windows の IL2CPP。確かめた日と方法。unity7-evaluation へのリンク）に直す。TMP Settings の節の印 [Uta案。確認待ち] は外す（決めたこと 16 は 2026-11-07 にユーザーが了承済み）
   - 7.2 の「**未確認**: Addressables 4.1.2 …」を、タスク 1・6 の結果（確かめた日と方法。unity7-evaluation へのリンク）に直す
   - 7.4 の表の「Prefab」の行に、部品の一覧（3.4 の 10 個）と「画面の Prefab だけを Addressables の `Screens` に登録する。部品はネスト、`UiRoot` はシーンに置く」を足し、「生成スクリプト」の行に「入口は `PrefabBuildCommands`（`Uta/Prefabs/Build All`）。生成クラスは `IPrefabBuilder` を実装すれば Build All に入る」を足す
   - 実装メモに、実装と計画のズレ（あれば）と、タスク 4 の再生成の差分の結果を書く
2. repository-layout.md 3章の `Uta.Client.Presentation` の行の参照に `Unity.TextMeshPro`（TMP）を足す（`UnityScreenNavigator` は既に書いてある）。`Uta.Client.Editor` の行があれば、`Unity.Addressables.Editor`・`Unity.TextMeshPro`・`UnityScreenNavigator` を足す（無ければ足さない）
3. CLAUDE.md「ビルド・テスト・実行」に 2 行足す（決めたこと 12・19）:
   - 「Unity の EditMode のテスト: Editor を開いたまま `unity command --timeout 300 eval` で `TestRunnerApi` を呼ぶ（コマンドは unity7-evaluation 4章。`unity test` は Editor が開いていると使えない）」
   - 「Prefab の生成（`client/uta`。生成物は手で直さない）: Editor が開いているとき `unity command eval 'return Uta.Client.Editor.Prefabs.PrefabBuildCommands.BuildAll();'`。Editor が閉じているとき用に `-executeMethod Uta.Client.Editor.Prefabs.PrefabBuildCommands.BuildAllBatch` の入口もある。`BuildAll` のたびに `NotoSansJP-Regular SDF.asset` のマテリアルの値が変わるので、`client/uta/Assets/Fonts/NotoSansJP/` の差分はコミットせず `git restore` で戻す」
4. unity7-evaluation.md 4章に、`unity command eval` で Editor のスクリプトの静的メソッドを呼んで Prefab を作れたこと、バッチの `-executeMethod … BuildAllBatch` を足す。3.3 の「UnityScreenNavigator・Supplement は取り込みだけで、使っていない」を「UnityScreenNavigator の `PageContainer` + `AddressableAssetLoader` はスパイクで Push まで確かめた（Presenter 駆動の `ScreenNavigator` は foundation で確かめる）」に直す
5. `python -I .claude/skills/linting-wiki/lint_wiki.py` がエラー 0
6. この計画の frontmatter を `status: done`、`updated` を実施日にする

終わったことの確かめ方: Lint がエラー 0。

## 5. テスト

| ファイル | テスト | 確かめること |
|---|---|---|
| `PrefabContractTests.cs` | `AllPrefabs_SerializedReferences_AreAssigned` | 3.6 |
| | `Parts_AllExist` | 3.6 |
| | `UiRoot_HasLayersInOrder` | 3.6 |
| | `ScreenPrefabs_RootViewIsNamedAfterPrefab` | 3.6（この計画の時点では 0 個で通る） |
| | `ScreenPrefabs_AreRegisteredInScreensGroup` | 3.6（同上） |
| | `ScreensGroup_ContainsOnlyScreenPrefabs` | 3.6 |
| | `TmpSettings_DefaultFont_IsNotoSansJpDynamic` | 3.6 |

実行: Editor を開いたまま `TestRunnerApi` を eval で回す（決めたこと 12。コマンドは unity7-evaluation 4章）。タスク 5 より前は Editor の Test Runner で手動。

Editor・プレイヤーでしか確かめられないもの（自動のテストにしない）:
- Addressables の読み込み（Editor の Play・Windows の IL2CPP）と、TMP の日本語の表示 → タスク 1 のスパイク（コンソールの `[Uta] spike` の行と、目で見る確認）
- 部品の見た目 → タスク 4 の手動の確認
- `SafeAreaFitter`（ノッチのある端末）・`WindowAspectKeeper`（Windows のウィンドウ）→ foundation のタスクで `UiRoot` をシーンに置いてから。Android・iOS の実機は順 7 の範囲外（game-spec の完了条件 7）

## 6. 未決事項

done（2026-11-08）。ユーザーの確認は全件了承済み（2026-11-07）。

- フォントの二重の取り込み（推測）: フォントは Addressables に入れないので、画面の Prefab（`Screens` のバンドル）とシーンの `UiRoot`（プレイヤー）の両方から参照されると、フォントのアセット（動的なので元の ttf を含む。1 本約 5.7 MB）がバンドルとプレイヤーの両方に入る可能性がある。当初はタスク 6 のビルドで大きさを見る予定だったが、ビルドを計画から外した（決めたこと 19）。**画面の Prefab が入ったあと、プレイヤーをビルドするときに `Build/` と `StreamingAssets/aa` の大きさを見る**（この計画では直さない。未確認）
- （foundation との境界・`DialogService` の公開 API・シーンごとの `BusyIndicator` の見つけ方は foundation の計画で決まったので消した。決めたこと 14）
