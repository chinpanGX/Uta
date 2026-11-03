---
title: 実装計画: client/ の画面 1/6 土台・起動・ホーム
status: draft
updated: 2026-11-03
spec: ../wiki/architecture/screen-design.md
---

# 実装計画: client/ の画面 1/6 土台・起動・ホーム

## 1. 目的と前提

[game-spec.md](../wiki/overview/game-spec.md) の順 7（client/ の画面）の 1 本目。画面を作る共通の土台（コードで組む uGUI の部品、基盤の `IScreenNavigator` の使い方、ダイアログ・ブロッカー、テスト用の偽）、マスタの読み取り口（`IGameMaster`）、業務エラーの登録、診断用 IMGUI の置き換え、起動の流れ（画面あり）、ホームを作る。2〜6 本目（ガチャ・所持一覧・編成とプロデュースの開始・週の行動選択・レッスン）は、**3章の公開 API と 3.3 の共通ルールを正**として、この計画の部品を使う。

- 方針の正は [screen-design.md](../wiki/architecture/screen-design.md) 7章（UI はコードで組む。基盤の Presenter 駆動のナビゲーションをそのまま使う。`Replace` は作らない。通信・状態・マスタの扱い。起動の診断モード）。起動の順序の正は同 4章。画面の見た目の叩き台は [screen-wireframes.md](../wiki/overview/screen-wireframes.md)「ホーム」
- 関係する ADR: [0008](../wiki/decisions/0008-shared-domain-and-onion.md)（層）、[0009](../wiki/decisions/0009-client-stack.md)（uGUI + UnityScreenNavigator・VContainer・UniTask・縦画面）、[0006](../wiki/decisions/0006-adopt-unity-7-early.md)（Unity 7）、[0005](../wiki/decisions/0005-foundation-submodule.md)（共通基盤）
- 画面遷移は、基盤 `external/UnityScreenNavigator` の `Runtime/ChinpanGX`（`IScreenNavigator`・`ScreenNavigator`・`IPresenter`・`ILifecycleHandler`・`IScreenWithArgs<TArgs>`・`[AssetAddress]`・`OverlayContainer`。使い方は基盤の README「Presenter-Driven Navigation」）を**そのまま使う**。Uta 側に自前のナビゲーター・Presenter の基底クラスは作らない。基盤（`external/` 以下）は直さない
- **前提 1**: 積み PR。PR #24（クライアントのサインアップ・サインイン。`UtaApiClient`・`PlayerApi`・`PlayerGateway`・`EnterPlayerUseCase`・`PlayerCache`・`ErrorDialogView`（IMGUI）・`StartupPresenter`（診断））のブランチの上に作る
- **前提 2**: 計画 0（[2026-11-03-masterdata-card-text.md](2026-11-03-masterdata-card-text.md)。マスタに `Card.Name`・`CardFace.Description` を足す）が done で、生成された `Uta.MasterData.Card` に `string Name`、`Uta.MasterData.CardFace` に `string Description` があること。タスク 3 の前に確かめ、無ければ止まって報告する
- **この計画で決めないこと**: ガチャ・所持一覧・編成・プロデュースの画面（2〜6 本目）。ホームの 4 つのボタン（プロデュース・再開・ガチャ・アイドル）は、この計画では「準備中です。」のダイアログを出すだけにし、2〜4 本目が `HomePresenter` の対応するメソッドの中身を書き換える（3.7）。モーダル（`ModalBase`・`PushModalAsync`）は部品だけ作り、実際に出して確かめるのは最初に使う計画（2 本目のガチャの結果）
- **Unity 7 の未確認**: UnityScreenNavigator・uGUI（legacy `Text`）・Input System の UI 入力は Unity 7（`7000.0.0a12`）で動作未確認（[unity7-evaluation.md](../wiki/architecture/unity7-evaluation.md) 3.3）。タスク 1 で確かめ、**止まる条件に当たったら止まって報告する**（勝手に別の手段へ変えない）

## 2. 触るファイル

パスは、`client/uta/Assets/` で始まらないものはリポジトリのルートからの相対、それ以外は `client/uta/Assets/` からの相対。`.meta` は Unity が作るので、新しい `.cs`・フォルダには Unity の再コンパイルのあとに生成された `.meta` をコミットする。

| 操作 | パス | 中身 |
|---|---|---|
| 変更 | `Scripts/Presentation/Uta.Client.Presentation.asmdef` | 参照に `UnityScreenNavigator`・`UnityEngine.UI`・`Unity.InputSystem` を足す（3.1） |
| 変更 | `Scripts/Bootstrap/Uta.Client.Bootstrap.asmdef` | 参照に `UnityScreenNavigator` を足す（3.1） |
| 変更 | `Tests/EditMode/Uta.Client.Infrastructure.Tests.EditMode.asmdef` | 参照に `Uta.Client.Presentation`・`UnityScreenNavigator`・`UnityEngine.UI`・`VContainer` を足す（3.1） |
| 作成 | `Scripts/Presentation/Common/UiTheme.cs` | 色・寸法・基準解像度の定数 |
| 作成 | `Scripts/Presentation/Common/UiFonts.cs` | OS フォントの取得（無ければ組み込みフォント） |
| 作成 | `Scripts/Presentation/Common/UiFactory.cs` | uGUI の部品を作る静的関数 |
| 作成 | `Scripts/Presentation/Common/SafeAreaFitter.cs` | `Screen.safeArea` に合わせる |
| 作成 | `Scripts/Presentation/Common/WindowAspectKeeper.cs` | Windows のウィンドウを 9:16 に保つ |
| 作成 | `Scripts/Presentation/Common/BusyIndicator.cs` | 通信中のブロッカー |
| 作成 | `Scripts/Presentation/Common/DialogService.cs` | 確認・お知らせ・選択のダイアログと `DialogResult` |
| 作成 | `Scripts/Presentation/Common/UiRoot.cs` | `Canvas`・`EventSystem`・USN のコンテナ 3 つ・ブロッカー・ダイアログ層・雛形の登録 |
| 作成 | `Scripts/Presentation/Common/PageBase.cs` | ページの基底（`EnsureBuilt`） |
| 作成 | `Scripts/Presentation/Common/ModalBase.cs` | モーダルの基底（`EnsureBuilt`） |
| 作成 | `Scripts/Presentation/Common/PageStack.cs` | ページ・モーダルの枚数を数えて底まで戻す |
| 作成 | `Scripts/Application/Master/MasterItemKind.cs`・`IdolInfo.cs`・`SupportCardInfo.cs`・`SupportAbilityInfo.cs`・`GachaEntryInfo.cs`・`GachaInfo.cs`・`ScenarioWeekInfo.cs`・`ScenarioInfo.cs`・`SupportLevelUpCost.cs`・`CardTextInfo.cs`・`IGameMaster.cs` | マスタの読み取り用の型と `IGameMaster`（3.4。1 ファイル 1 型） |
| 作成 | `Scripts/Application/Player/RefreshPlayerUseCase.cs` | サインインで所持データを取り直す（3.5） |
| 作成 | `Scripts/Infrastructure/MasterData/MasterGameData.cs` | `MemoryDatabase` → `IGameMaster`（3.6） |
| 変更 | `Scripts/Infrastructure/Api/UtaErrors.cs` | 業務エラー 18 個を `ErrorAction.Stay` で足す（3.8） |
| 変更 | `Scripts/Infrastructure/Api/UtaApiClient.cs` | `Create` に `IApiActivityHandler`・`IErrorNavigator` を足す（3.8） |
| 作成 | `Scripts/Presentation/Startup/StartupDiagnostics.cs` | いまの `StartupPresenter` の診断を移す（画面なし。3.7） |
| 変更 | `Scripts/Presentation/Startup/StartupPresenter.cs` | 中身を全部置き換える: 診断は `StartupDiagnostics.cs` に移し、このファイルは `StartupPage` の Presenter にする（3.7） |
| 削除 | `Scripts/Presentation/Startup/StartupView.cs` | IMGUI の診断表示を廃止する |
| 削除 | `Scripts/Presentation/Startup/StartupOptions.cs` | `UtaBootstrap` が診断モードを選ぶので不要 |
| 作成 | `Scripts/Presentation/Startup/IStartupView.cs`・`StartupPage.cs`・`StartupFlow.cs` | 起動中の表示と起動の流れ（3.7） |
| 作成 | `Scripts/Presentation/Home/IHomeView.cs`・`HomePage.cs`・`HomePresenter.cs` | ホーム（3.7） |
| 作成 | `Scripts/Bootstrap/ApiActivityBridge.cs`・`UtaErrorNavigator.cs` | 通信中のブロッカーと、エラーの「タイトルへ」「ホームへ」（3.8） |
| 変更 | `Scripts/Bootstrap/ErrorDialogView.cs` | IMGUI をやめ、`DialogService` を呼ぶ普通のクラスにする（3.8） |
| 変更 | `Scripts/Bootstrap/UtaBootstrap.cs` | 起動の順序、画面の雛形の登録、DI の登録、診断モードの分岐（3.8） |
| 変更 | `client/uta/ProjectSettings/ProjectSettings.asset` | Windows のウィンドウと向き（タスク 2） |
| 作成 | `Tests/EditMode/Fakes/CallLog.cs`・`FakeScreenNavigator.cs`・`FakeDialogService.cs` | テスト用の偽（3.3.8。2〜6 本目も使う） |
| 作成 | `Tests/EditMode/UiFactoryTests.cs`・`BusyIndicatorTests.cs`・`DialogServiceTests.cs`・`PageStackTests.cs`・`MasterGameDataTests.cs`・`RefreshPlayerUseCaseTests.cs`・`StartupFlowTests.cs`・`HomePresenterTests.cs` | EditMode のテスト（5章） |
| 変更 | `Tests/EditMode/UtaErrorsTests.cs` | 登録の数と `Stay` の確認に直す（5章） |
| 変更 | `CLAUDE.md` | 「ビルド・テスト・実行」に EditMode のテストのコマンドを 1 行足す（タスク 6。動いたときだけ） |
| 変更 | `uta_docs/wiki/architecture/screen-design.md` | 4・5章の古い記述、7.2 の部品の表と Presenter の作り方、7.3 の業務エラー、実装メモ（タスク 6） |
| 変更 | `uta_docs/wiki/architecture/repository-layout.md` | 3章の `Uta.Client.Presentation` の参照、126 行の `ErrorDialogView`、277 行付近の USN の確認範囲（タスク 6） |
| 変更 | `uta_docs/wiki/architecture/unity7-evaluation.md` | 2章の表、3.3、4章（タスク 6） |

## 3. 型とシグネチャ

C# は `client/` の `.editorconfig` の範囲（C# 9 の範囲。ファイルスコープ namespace・プライマリコンストラクタ・コレクション式 `[...]`・Index/Range・record は使わない。配列は `new[] { ... }`）。BOM 付き UTF-8、LF、Allman、`var`。名前空間は、フォルダ `Scripts/Presentation/Common` が `Uta.Client.Presentation.Common`、`Startup` が `Uta.Client.Presentation.Startup`、`Home` が `Uta.Client.Presentation.Home`、`Application/Master` が `Uta.Client.Application.Master`、`Application/Player` が `Uta.Client.Application.Player`、`Infrastructure/MasterData/MasterGameData.cs` が `Uta.Client.Infrastructure`（同じフォルダの `MasterCardCatalog` に合わせる）、`Bootstrap` が `Uta.Client.Bootstrap`、テストが `Uta.Client.Infrastructure.Tests`（既存のテストと同じ）。

### 3.1 asmdef の参照

- `Uta.Client.Presentation.asmdef` の `references` を次にする（`overrideReferences: true` のまま）:
  ```
  "Uta.Client.Application", "Uta.Shared.Domain", "UniTask", "VContainer",
  "UnityScreenNavigator", "UnityEngine.UI", "Unity.InputSystem"
  ```
- `Uta.Client.Bootstrap.asmdef` の `references` の最後に `"UnityScreenNavigator"` を足す（Bootstrap が `IScreenNavigator`・`ScreenNavigator`・`PageContainer`・`OverlayContainer` を使うため）。`unity recompile` が `UnityEngine.UI` について CS0012（参照していないアセンブリの型）を出したときだけ `"UnityEngine.UI"` も足す
- `Uta.Client.Infrastructure.Tests.EditMode.asmdef` の `references` の最後に `"Uta.Client.Presentation"`, `"UnityScreenNavigator"`, `"UnityEngine.UI"`, `"VContainer"` を足す
- `UnityScreenNavigator` の asmdef は `versionDefines` で `com.cysharp.unitask` があると `USN_USE_ASYNC_METHODS` を定義し、`Page.Initialize()` などが `UniTask` を返す版になる。`client/uta/Packages/manifest.json` に `com.cysharp.unitask` があるので、この前提で書く（タスク 1 の止まる条件で確かめる）

### 3.2 共通部品（`Uta.Client.Presentation.Common`）

```csharp
public static class UiTheme
{
    public static readonly Vector2 ReferenceResolution = new Vector2(1080f, 1920f);
    public static readonly Color Background = new Color(0.10f, 0.11f, 0.15f, 1f);
    public static readonly Color Panel = new Color(0.18f, 0.20f, 0.27f, 1f);
    public static readonly Color Button = new Color(0.25f, 0.45f, 0.85f, 1f);
    public static readonly Color ButtonDisabled = new Color(0.35f, 0.35f, 0.40f, 1f);
    public static readonly Color Danger = new Color(0.80f, 0.30f, 0.30f, 1f);
    public static readonly Color Text = new Color(1f, 1f, 1f, 1f);
    public static readonly Color TextMuted = new Color(0.70f, 0.72f, 0.78f, 1f);
    public static readonly Color Accent = new Color(1f, 0.78f, 0.25f, 1f);
    public static readonly Color Overlay = new Color(0f, 0f, 0f, 0.6f);
    public const int FontTitle = 56;
    public const int FontBody = 40;
    public const int FontSmall = 32;
    public const float ButtonHeight = 130f;
    public const float Spacing = 24f;
    public const float Padding = 32f;
    public const float DialogWidth = 880f;
    public const float ModalWidth = 900f;
}

public static class UiFonts
{
    /// <summary>
    /// 最初の呼び出しで作って使い回す。候補は順に "Yu Gothic UI", "Meiryo UI", "Noto Sans CJK JP", "Noto Sans JP", "Hiragino Sans", "Droid Sans Fallback"。
    /// Font.GetOSInstalledFontNames() に含まれる候補だけを、この順のまま配列にして Font.CreateDynamicFontFromOSFont(その配列, 32) で作る。
    /// 1 つも含まれなければ Debug.LogWarning("[Uta] no Japanese OS font found") を出し、Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") を使う（英数字だけ出る。操作はできる）。
    /// </summary>
    public static Font Default { get; }
}

public static class UiFactory
{
    /// <summary>RectTransform だけを持つ GameObject を parent の子に作り、親いっぱいに広げる（anchorMin (0,0) / anchorMax (1,1) / offsetMin・offsetMax 0 / localScale 1）。</summary>
    public static RectTransform CreateRect(Transform parent, string name);

    /// <summary>CreateRect + Image（color。raycastTarget = true）。</summary>
    public static Image CreateImage(Transform parent, string name, Color color);

    /// <summary>CreateRect + Text。font = UiFonts.Default、fontSize、alignment = anchor、horizontalOverflow = Wrap、verticalOverflow = Overflow、raycastTarget = false、color は省略時 UiTheme.Text。レイアウトグループの子として使う（大きさはグループが決める）。</summary>
    public static Text CreateText(Transform parent, string name, string text, int fontSize, TextAnchor anchor = TextAnchor.MiddleLeft, Color? color = null);

    /// <summary>CreateImage(UiTheme.Button) + Button（targetGraphic = その Image）+ 子 "Label"（CreateText。label、UiTheme.FontBody、MiddleCenter、親いっぱい）+ SetHeight(height)。</summary>
    public static Button CreateButton(Transform parent, string name, string label, float height = UiTheme.ButtonHeight);

    /// <summary>ボタンの子 "Label" の Text を label にする。</summary>
    public static void SetLabel(Button button, string label);

    /// <summary>Button.interactable を変え、Image の色を UiTheme.Button / UiTheme.ButtonDisabled にする。</summary>
    public static void SetInteractable(Button button, bool interactable);

    /// <summary>CreateRect + VerticalLayoutGroup（spacing、padding を上下左右に同じ値（RectOffset の int に丸める）、childAlignment = UpperCenter、childControlWidth = childControlHeight = true、childForceExpandWidth = true、childForceExpandHeight = false）。</summary>
    public static RectTransform CreateVertical(Transform parent, string name, float spacing = UiTheme.Spacing, float padding = UiTheme.Padding);

    /// <summary>CreateRect + HorizontalLayoutGroup（spacing、padding、childAlignment = MiddleCenter、childControlWidth = childControlHeight = true、childForceExpandWidth = childForceExpandHeight = true）。</summary>
    public static RectTransform CreateHorizontal(Transform parent, string name, float spacing = UiTheme.Spacing, float padding = 0f);

    /// <summary>CreateImage(UiTheme.Panel) + VerticalLayoutGroup（CreateVertical と同じ設定。spacing = UiTheme.Spacing、padding = UiTheme.Padding）。戻り値はその RectTransform。</summary>
    public static RectTransform CreatePanel(Transform parent, string name);

    /// <summary>target の GameObject の LayoutElement（無ければ付ける）の minHeight = preferredHeight = height。</summary>
    public static void SetHeight(Component target, float height);

    /// <summary>target の GameObject の LayoutElement（無ければ付ける）の flexibleHeight = flexible。</summary>
    public static void SetFlexibleHeight(Component target, float flexible);

    /// <summary>
    /// 縦スクロール。"name"（CreateRect + ScrollRect（horizontal = false、vertical = true、movementType = Clamped、scrollSensitivity = 30）+ SetFlexibleHeight(1)）の子に
    /// "Viewport"（CreateImage(new Color(0,0,0,0)) + RectMask2D）、その子に "Content"（anchorMin (0,1)、anchorMax (1,1)、pivot (0.5,1)、offset 0、
    /// VerticalLayoutGroup（spacing = UiTheme.Spacing、padding 0、UpperCenter、childControlWidth/Height = true、childForceExpandWidth = true、childForceExpandHeight = false）+ ContentSizeFitter（verticalFit = PreferredSize））。
    /// ScrollRect.viewport = Viewport、content = Content。戻り値は Content。
    /// </summary>
    public static RectTransform CreateScrollContent(Transform parent, string name);

    /// <summary>
    /// parent の子を最後から順に全部消す。Application.isPlaying なら子を SetActive(false) してから Object.Destroy（レイアウトから即座に外すため）、
    /// そうでなければ Object.DestroyImmediate。
    /// </summary>
    public static void ClearChildren(Transform parent);

    /// <summary>Application.isPlaying なら Object.Destroy、そうでなければ Object.DestroyImmediate。</summary>
    public static void DestroyObject(GameObject target);
}

[RequireComponent(typeof(RectTransform))]
public sealed class SafeAreaFitter : MonoBehaviour
{
    // OnEnable で 1 回合わせ、LateUpdate で Screen.safeArea・Screen.width・Screen.height を前回と比べ、変わったときに合わせる。
    // 合わせ方: anchorMin = safeArea.position / (Screen.width, Screen.height)、anchorMax = (safeArea.position + safeArea.size) / (Screen.width, Screen.height)、offsetMin = offsetMax = 0。
    // Screen.width か Screen.height が 0 のときは何もしない。
}

public sealed class WindowAspectKeeper : MonoBehaviour
{
    // #if UNITY_STANDALONE_WIN && !UNITY_EDITOR のときだけ中身を持つ（ほかのプラットフォーム・Editor では何もしない空のクラス）。
    // Update で Screen.fullScreen が false のとき、Screen.height と Mathf.RoundToInt(Screen.width * 16f / 9f) の差が 1 より大きければ
    // Screen.SetResolution(Screen.width, Mathf.RoundToInt(Screen.width * 16f / 9f), FullScreenMode.Windowed) を 1 回呼ぶ。
    // 呼んだ直後の 1 フレームは判定を休む（反映待ちで連続して呼ばないため）。
}

public sealed class BusyIndicator : MonoBehaviour
{
    /// <summary>UiRoot.Create が呼ぶ。blocker は全面の透明な Image（raycastTarget = true、最初は enabled = false）。null なら数だけ数える（テスト用）。</summary>
    public void Initialize(Image blocker);
    /// <summary>数を 1 増やし、0 → 1 で blocker.enabled = true。</summary>
    public void Begin();
    /// <summary>数が 0 なら何もしない。そうでなければ 1 減らし、1 → 0 で blocker.enabled = false。</summary>
    public void End();
    public bool IsBusy { get; }   // 数 > 0
}

public enum DialogResult { Positive, Negative }

/// <summary>
/// テストで派生させる（FakeDialogService）ので sealed にせず、公開メソッドは virtual。コンストラクターは layer を保存するだけ（null を許す）。
/// USN のモーダルにしない（ページ遷移の最中にも出せるように、USN の遷移の外の素の層に出す）。
/// </summary>
public class DialogService
{
    public DialogService(RectTransform layer);

    /// <summary>お知らせ。ボタンは「OK」1 つ。押されたら閉じて終わる。</summary>
    public virtual UniTask ShowInfoAsync(string message);

    /// <summary>確認。ボタンは上から positiveLabel（Positive）、negativeLabel（Negative）。</summary>
    public virtual UniTask<DialogResult> ConfirmAsync(string message, string positiveLabel = "はい", string negativeLabel = "いいえ");

    /// <summary>labels を上から並べ、押されたボタンの添字（0 始まり）を返す。labels が null か、長さが 0 または 5 以上なら、待ち行列に入れる前に ArgumentException を投げる。</summary>
    public virtual UniTask<int> ChooseAsync(string message, string[] labels);
}
```

`DialogService` の動き:
- 待ち行列: 内部に `UniTask` の末尾を 1 つ持ち、基盤の `TransitionQueue`（`Runtime/ChinpanGX/Internal/TransitionQueue.cs`）と同じ作り（`UniTaskCompletionSource` で前のダイアログが閉じるのを待ってから次を出す。前が例外で終わっても次へ進む）にする。同時に出るのは 1 つだけ
- 順番が来たら、同じフレームのうちに（`UniTask.Yield` を挟まず）次を `layer` の子に作る: "Dialog"（`CreateImage(layer, "Dialog", UiTheme.Overlay)`。全面で下の入力を止める）→ その子 "Panel"（`CreateImage(..., UiTheme.Panel)`、anchorMin = anchorMax = pivot = (0.5, 0.5)、`sizeDelta.x = UiTheme.DialogWidth`、VerticalLayoutGroup（spacing 24、padding 40、childAlignment = UpperCenter、childControlWidth/Height = true、childForceExpandWidth = true、childForceExpandHeight = false）+ ContentSizeFitter（verticalFit = PreferredSize））→ Panel の子に "Message"（`CreateText(message, UiTheme.FontBody, TextAnchor.MiddleCenter)`）、続けてボタン（`CreateButton`。名前は "Button0"・"Button1"…、高さ `UiTheme.ButtonHeight`）を上から順に
- ボタンが押されたら "Dialog" を `UiFactory.DestroyObject` で消し、結果を返す

```csharp
public sealed class UiRoot : MonoBehaviour
{
    public PageContainer Pages { get; }
    public ModalContainer Modals { get; }
    public OverlayContainer Overlays { get; }       // ScreenNavigator のコンストラクターが要る。Uta は Overlay に画面を出さない
    public PreloadedAssetLoader Loader { get; }
    public BusyIndicator Busy { get; }
    public DialogService Dialogs { get; }

    /// <summary>
    /// GameObject "UtaUi"（RectTransform 付き。DontDestroyOnLoad）を作る。構成: Canvas（renderMode = ScreenSpaceOverlay）+ CanvasScaler（uiScaleMode = ScaleWithScreenSize、
    /// referenceResolution = UiTheme.ReferenceResolution、screenMatchMode = MatchWidthOrHeight、matchWidthOrHeight = 0）+ GraphicRaycaster + WindowAspectKeeper + UiRoot。
    /// EventSystem.current が null のときは別の GameObject "EventSystem"（DontDestroyOnLoad）に EventSystem + InputSystemUIInputModule を付ける。
    /// 子（この順。後ろが手前）:
    ///   "Pages"（CreateRect + PageContainer。AssetLoader = Loader）、
    ///   "Modals"（CreateRect + ModalContainer。AssetLoader = Loader）、
    ///   "Overlays"（CreateRect + ModalContainer + OverlayContainer。ModalContainer.AssetLoader = Loader）、
    ///   "Busy"（CreateImage(new Color(0,0,0,0))。Image.enabled = false + BusyIndicator。Initialize(その Image)）、
    ///   "Dialogs"（CreateRect。new DialogService(この RectTransform)）、
    ///   "Templates"（CreateRect。SetActive(false)。雛形の置き場）。
    /// Dialogs を Busy より手前にする（ダイアログは通信中でも押せるようにする。ApiCore はエラーハンドラーの間はブロッカーを外している）。
    /// </summary>
    public static UiRoot Create();

    /// <summary>
    /// ページの雛形を登録する。キーは typeof(TPage).Name（Presenter の [AssetAddress(nameof(TPage))] と同じ）。登録済みなら何もしない。
    /// 雛形 = new GameObject(キー, typeof(RectTransform)) に AddComponent&lt;TPage&gt;() し、SetActive(false) して "Templates" の子にし、Loader.PreloadedObjects[キー] に入れる。
    /// 雛形には子を作らない（UI は Instantiate のあと PageBase.EnsureBuilt が組む）。
    /// </summary>
    public void RegisterPage<TPage>() where TPage : PageBase;

    /// <summary>RegisterPage と同じ。TModal : ModalBase。</summary>
    public void RegisterModal<TModal>() where TModal : ModalBase;
}

public abstract class PageBase : Page
{
    protected RectTransform Body { get; private set; }

    /// <summary>
    /// 冪等。2 回目以降は何もしない。自分の GameObject に Image（UiTheme.Background、raycastTarget = true。下のページを隠す）を付け（あれば色だけ変える）、
    /// 子 "Body"（CreateRect + SafeAreaFitter）を作って Body に入れ、BuildUi(Body) を呼ぶ。
    /// </summary>
    public void EnsureBuilt();

    /// <summary>EnsureBuilt() を呼んで UniTask.CompletedTask を返す。</summary>
    public override UniTask Initialize();

    /// <summary>派生クラスが UI を組む。ボタンの onClick は、ここで View のイベントを発火するように付ける。</summary>
    protected abstract void BuildUi(RectTransform body);
}

public abstract class ModalBase : Modal
{
    protected RectTransform Panel { get; private set; }

    /// <summary>
    /// 冪等。子 "Panel"（CreateImage(UiTheme.Panel)、anchorMin = anchorMax = pivot = (0.5, 0.5)、sizeDelta.x = UiTheme.ModalWidth、
    /// VerticalLayoutGroup（spacing 24、padding 40、UpperCenter、childControlWidth/Height = true、childForceExpandWidth = true、childForceExpandHeight = false）
    /// + ContentSizeFitter（verticalFit = PreferredSize））を作って Panel に入れ、BuildUi(Panel) を呼ぶ。背景の暗幕は USN の DefaultModalBackdrop（USN の Resources にある）。
    /// </summary>
    public void EnsureBuilt();
    public override UniTask Initialize();   // EnsureBuilt() して UniTask.CompletedTask
    protected abstract void BuildUi(RectTransform panel);
}

public sealed class PageStack
{
    /// <summary>VContainer が使う。</summary>
    [Inject]
    public PageStack(PageContainer pages, ModalContainer modals);   // this(() => pages.Pages.Count, () => modals.Modals.Count)

    /// <summary>テスト用。</summary>
    public PageStack(Func<int> pageCount, Func<int> modalCount);

    /// <summary>積まれているページの枚数（呼んだ時点）。</summary>
    public int Depth { get; }
    /// <summary>積まれているモーダルの枚数（呼んだ時点）。</summary>
    public int ModalDepth { get; }

    /// <summary>
    /// ホームへ戻る。ModalDepth > 0 なら navigator.PopModalAsync(playAnimation: false, popCount: ModalDepth) を待ち、
    /// そのあと Depth > 1 なら navigator.PopPageAsync(playAnimation: false, popCount: Depth - 1) を待つ。底が 1 枚以下なら何もしない。
    /// </summary>
    public UniTask PopToRootAsync(IScreenNavigator navigator);

    /// <summary>
    /// 全部を閉じる（タイトルへ戻る＝起動のやり直し用）。ModalDepth > 0 なら PopModalAsync(false, ModalDepth)、Depth > 0 なら PopPageAsync(false, Depth)。
    /// </summary>
    public UniTask ClearAsync(IScreenNavigator navigator);
}
```

`PageStack` の枚数は呼んだ時点で数える。基盤の `ScreenNavigator` は遷移を順番待ちにするので、順番待ちの遷移が残っている間に呼ぶと数がずれうる。MVP では通信中はブロッカーで入力が止まり、Presenter は遷移を `await` してから次の操作に進む（3.3.5）ので、この呼び方で足りる。

### 3.3 画面を作る共通ルール（2〜6 本目も従う）

2〜6 本目の計画は、この節を参照して書く（同じことを繰り返して書かない）。

#### 3.3.1 使う `IScreenNavigator`（基盤）のメソッド

`UnityScreenNavigator.IScreenNavigator` のうち、Uta が使うのは次だけ。**`playAnimation` は常に `false` を名前付き引数で渡す**（遷移のアニメーションを作らないため）。

| メソッド | 使う場面 |
|---|---|
| `PushPageAsync<TPresenter>(bool stack = true, bool playAnimation = true)` | 引数の要らないページを積む。`stack: false` は起動のページだけ（次に積むページがそれを消す） |
| `PushPageAsync<TPresenter, TArgs>(TArgs args, bool stack = true, bool playAnimation = true)` | 引数を渡してページを積む。`TPresenter : IScreenWithArgs<TArgs>`、`TArgs : class` |
| `PopPageAsync(IPresenter presenter, bool playAnimation = true)` | 自分（`this`）のページを、上に積まれたものごと閉じる。「戻る」と局面の切り替え |
| `PopPageAsync(bool playAnimation = true, int popCount = 1)` | `PageStack` だけが使う |
| `PushModalAsync<TPresenter>(bool playAnimation = true)`・`PushModalAsync<TPresenter, TArgs>(TArgs args, bool playAnimation = true)` | モーダルを出す |
| `PopModalAsync(IPresenter presenter, bool playAnimation = true)` | 自分（`this`）のモーダルを閉じる |
| `PopModalAsync(bool playAnimation = true, int popCount = 1)` | `PageStack` だけが使う |
| `WaitForPopAsync<TResult>(IPresenter presenter, CancellationToken cancellation = default)` | 出したモーダル（またはページ）が閉じたときの結果を受け取る。結果は相手の Presenter の `CompleteAsync()` が返す。1 つの Presenter に 1 回だけ |

`PushOverlayAsync`・`PopOverlayAsync` は使わない（ダイアログは `DialogService`、ブロッカーは `BusyIndicator`）。`Push…Async` が返す Presenter に対しては、`WaitForPopAsync`・`Pop…Async` に渡す以外のことをしない（テストの偽は `null` を返す。値の受け渡しは Args と `WaitForPopAsync` で行う）。

遷移の型:
- **ホームの上に積む**（ガチャ・所持一覧・編成など）: `await navigator.PushPageAsync<XxxPresenter>(playAnimation: false);`
- **戻る**: `await navigator.PopPageAsync(this, playAnimation: false);`
- **プロデュースの局面の切り替え**（`Replace` は作らない）: 今の局面のページを閉じてから次を積む。`await navigator.PopPageAsync(current, playAnimation: false); await navigator.PushPageAsync<NextPresenter>(playAnimation: false);`（`current` は今の局面の Presenter。間に一瞬ホームが見えるのは受け入れる）
- **ホームへ戻る**: `await pageStack.PopToRootAsync(navigator);`（ホームの上のモーダルとページを全部閉じる。ホームの Presenter は `DidPopEnter` で表示をやり直す）
- **起動のやり直し（タイトルへ）**: `StartupFlow` だけが `pageStack.ClearAsync(navigator)` を使う

#### 3.3.2 Presenter の形

Presenter は `IPresenter`（引数があれば `IScreenWithArgs<TArgs>`）を実装する普通のクラス。基盤の `ScreenNavigator` が Push のたびに VContainer の子スコープを作り、`Scoped` で登録して解決する。**Bootstrap に Presenter を登録しない**。子スコープには、読み込まれたページ（モーダル）の**具象型**と Args の**具象型**だけが入る。親のスコープに登録したもの（ユースケース・`PlayerCache`・`IGameMaster`・`DialogService`・`IScreenNavigator`・`PageStack` など）も受けられる。

```csharp
using Cysharp.Threading.Tasks;
using Uta.Client.Application.Player;
using Uta.Client.Presentation.Common;
using UnityScreenNavigator;
using VContainer;

namespace Uta.Client.Presentation.Home
{
    [AssetAddress(nameof(HomePage))]                       // 雛形のキー。UiRoot.RegisterPage<HomePage>() のキーと同じ
    public sealed class HomePresenter : IPresenter
    {
        private readonly IHomeView view;
        private readonly PlayerCache cache;
        private readonly DialogService dialogs;
        private bool running;                              // 実行中フラグ。Presenter に 1 つ。全部のボタンの処理が共有する

        [Inject]                                           // VContainer はこちらを使う（具象型のページを受ける）
        public HomePresenter(HomePage view, PlayerCache cache, DialogService dialogs)
            : this((IHomeView)view, cache, dialogs)        // キャストが要る（無いと自分自身を呼ぶ）
        {
        }

        public HomePresenter(IHomeView view, PlayerCache cache, DialogService dialogs)   // EditMode のテストが偽の View で使う
        {
            this.view = view;
            this.cache = cache;
            this.dialogs = dialogs;
        }

        public UniTask InitializeAsync()                   // ILifecycleHandler。画面が読み込まれた直後に 1 回
        {
            view.GachaClicked += OnGachaClicked;           // 購読はここで。Dispose で必ず解く
            Refresh();
            return UniTask.CompletedTask;
        }

        public void DidPopEnter()                          // ILifecycleHandler。上のページが閉じてこのページに戻ったとき
        {
            Refresh();
        }

        public void Dispose()                              // 画面が破棄されたとき（子スコープの破棄）
        {
            view.GachaClicked -= OnGachaClicked;
        }

        private void OnGachaClicked()                      // イベントの受け口は同期。公開の UniTask メソッドを Forget で呼ぶだけ
        {
            OpenGachaAsync().Forget();
        }

        public async UniTask OpenGachaAsync()              // テストはこれを await する
        {
            if (running)
            {
                return;
            }

            running = true;
            try
            {
                await dialogs.ShowInfoAsync("準備中です。");
            }
            finally
            {
                running = false;                           // 例外でも必ず戻す
            }
        }

        private void Refresh() { /* cache.Current から view.Set… を呼ぶ */ }
    }
}
```

決まり:
1. **コンストラクターは 2 本**: `[Inject]` を付けた「具象型のページ（モーダル）を受ける」ものと、「`IXxxView` を受ける」もの。前者は `: this((IXxxView)view, ...)` で後者に渡す。Args・ほかの依存は両方で同じ順に受ける
2. **`[AssetAddress(nameof(XxxPage))]`** を必ず付け、`UtaBootstrap.RegisterScreens`（3.8）に `uiRoot.RegisterPage<XxxPage>();`（モーダルは `RegisterModal<XxxModal>()`）を 1 行足す
3. **コンストラクターで View に触らない**（ページの UI はまだ組まれていない）。購読と最初の表示は `InitializeAsync` で行う。`InitializeAsync` で通信してよい（`async UniTask InitializeAsync()`。終わるまで画面は出ない）
4. **購読したイベントは `Dispose` で全部解く**。`Dispose` は `InitializeAsync` の前に呼ばれても安全に書く（未購読の `-=` は害がない）
5. **ボタンの処理**: イベントの受け口（`OnXxxClicked`）は同期で、`XxxAsync().Forget()` を呼ぶだけ。`XxxAsync` は `public async UniTask` で、先頭で `if (running) return; running = true;`、本体を `try`、`finally` で `running = false;`。連打は通信中ならブロッカー、通信の前後（ダイアログを出している間など）は実行中フラグで止める
6. **戻ってきたときの更新**は `DidPopEnter()` で行う（ページの側にイベントを作らない）
7. **結果を返すモーダル**は `public UniTask<object> CompleteAsync()` を書き、呼ぶ側は `var presenter = await navigator.PushModalAsync<XPresenter, XArgs>(args, playAnimation: false); var result = await navigator.WaitForPopAsync<T>(presenter);`。モーダルからの「もう一度」などの呼び戻しは、Args に `Action`／`Func<UniTask>` を持たせる
8. **閉じる**: ページは `navigator.PopPageAsync(this, playAnimation: false)`、モーダルは `navigator.PopModalAsync(this, playAnimation: false)`。閉じる処理も 5 の実行中フラグの中で行い、同じ Presenter で 2 回呼ばない（閉じたあとの Presenter を渡すと、基盤が `InvalidOperationException` を投げる）

#### 3.3.3 ページ（View）の形

```csharp
public interface IHomeView
{
    void SetProfile(int level, int gachaCurrency);
    event Action GachaClicked;
}

public sealed class HomePage : PageBase, IHomeView
{
    private Text profileText;                // BuildUi で作る
    public event Action GachaClicked;

    public void SetProfile(int level, int gachaCurrency)
    {
        EnsureBuilt();                       // View の公開メソッドは必ず先頭で EnsureBuilt() を呼ぶ
        profileText.text = ...;
    }

    protected override void BuildUi(RectTransform body)
    {
        var gacha = UiFactory.CreateButton(body, "Gacha", "ガチャ");
        gacha.onClick.AddListener(() => GachaClicked?.Invoke());
    }
}
```

- `PageBase.Initialize()` と Presenter の `InitializeAsync()` は、どちらが先に呼ばれるかが決まっていない（USN は同じ優先度のライフサイクルを登録順で呼ぶとは限らない）。そのため **View の公開メソッド（`Set…` など）の先頭で `EnsureBuilt()` を呼ぶ**。イベント（C# の `event`）の購読は UI を組む前でもよい
- View はロジックを持たない。文言の組み立て（数値の書式など）は Presenter でしてもページでしてもよいが、計画にどちらでするかを書く
- `IXxxView` に `event Action`（引数があれば `Action<int>` など）と `void SetXxx(...)` だけを置く。戻り値のあるメソッドを置かない

#### 3.3.4 Args

```csharp
public sealed class XxxArgs
{
    public XxxArgs(int gachaId) { GachaId = gachaId; }
    public int GachaId { get; }
}
```
`sealed class`、コンストラクターで全部受け、get だけのプロパティ。Presenter は `IScreenWithArgs<XxxArgs>` を実装し、両方のコンストラクターで `XxxArgs args` を受ける。

#### 3.3.5 通信とエラー

- 通信中のブロッカーは `ApiActivityBridge` が付けるので、**Presenter は通信を個別に包まない**
- `ApiCore` の共通ハンドラーが出すダイアログ（ネットワークの失敗、`DATE_CHANGED`、**`UtaErrors` に `Stay` で登録した業務エラーのうち `handledCodes` に入れなかったもの**）は、Presenter から追加で出さない
- 業務エラーを画面ごとの文言で出したいときは、そのコードをゲートウェイの `handledCodes` に入れ、ユースケースの結果の型（成功／業務エラー／その他）で Presenter に返し、Presenter が `DialogService` で出す。どのコードを `handledCodes` に入れるかは各計画が書く
- **RPC が成功したあとの `RefreshPlayerUseCase` の失敗は、成功として扱う**: ユースケースが例外を握りつぶして「取り直しに失敗した」印を付けて成功を返し、画面は RPC の応答で更新できる範囲を更新する。ダイアログは `ApiCore` が出した分だけ（Presenter は追加で出さない）
- 遷移（`Push…`／`Pop…`）は `await` してから次に進む

#### 3.3.6 文字と書式

- 数値の 3 桁区切りは `value.ToString("N0", CultureInfo.InvariantCulture)`
- フォントサイズは `UiTheme.FontTitle`（画面の見出し）・`FontBody`（本文・ボタン）・`FontSmall`（補足）の 3 つだけを使う

#### 3.3.7 テストの書き方

- Presenter のテストは EditMode で、偽の View（各計画がテストのファイルの中に `private sealed class FakeXxxView : IXxxView` を書く。呼ばれた `Set…` を `CallLog` に文字列で記録し、`RaiseXxx()` でイベントを起こす）、`FakeScreenNavigator`、`FakeDialogService` を、**同じ `CallLog`** で作り、`log.Entries` の並びで確かめる
- 非同期のテストは `[UnityTest] public IEnumerator Xxx() => UniTask.ToCoroutine(async () => { ... });` にし、Presenter の公開の `UniTask` メソッド（`XxxAsync`、`InitializeAsync`）を `await` する。イベントを起こすテスト（購読の確認）は、偽が同期で終わるので、起こした直後に確かめてよい
- 同期のテストは `[Test]`
- 偽の View の記録の書式は `"<メソッド名>:<引数を , でつないだもの>"`（例 `"SetProfile:12,3250"`、bool は `True`/`False`）

#### 3.3.8 テスト用の偽（`Tests/EditMode/Fakes/`）

```csharp
public sealed class CallLog
{
    public List<string> Entries { get; } = new List<string>();
    public void Add(string entry) { Entries.Add(entry); }
}

public sealed class FakeScreenNavigator : IScreenNavigator
{
    public FakeScreenNavigator(CallLog log);
    /// <summary>Push に渡された Args（渡された順）。</summary>
    public List<object> Args { get; }
    /// <summary>WaitForPopAsync が返す値。空なら default(TResult)。</summary>
    public Queue<object> PopResults { get; }
}
```

`FakeScreenNavigator` の記録（`log.Add`）と戻り値。どれも同期で終わる `UniTask` を返す:

| メソッド | 記録 | 戻り値 |
|---|---|---|
| `PushPageAsync<TPresenter>(stack, playAnimation)` | `"PushPage:" + typeof(TPresenter).Name`、`stack` が false なら末尾に `"(nostack)"` | `default(TPresenter)` |
| `PushPageAsync<TPresenter, TArgs>(args, stack, playAnimation)` | 同上。`Args.Add(args)` | `default(TPresenter)` |
| `PopPageAsync(bool playAnimation, int popCount)` | `"PopPage:" + popCount` | — |
| `PopPageAsync(IPresenter presenter, bool playAnimation)` | `"PopPage:" + presenter.GetType().Name`（presenter が null なら `"PopPage:null"`） | — |
| `PushModalAsync<TPresenter>(playAnimation)`・`<TPresenter, TArgs>(args, playAnimation)` | `"PushModal:" + typeof(TPresenter).Name`（Args 付きは `Args.Add(args)`） | `default(TPresenter)` |
| `PopModalAsync(bool, int popCount)` | `"PopModal:" + popCount` | — |
| `PopModalAsync(IPresenter presenter, bool)` | `"PopModal:" + presenter.GetType().Name`（null なら `"PopModal:null"`） | — |
| `PushOverlayAsync…`・`PopOverlayAsync…` | `"PushOverlay:" + 名前`・`"PopOverlay:" + popCount か型名` | `default` |
| `WaitForPopAsync<TResult>(presenter, cancellation)` | `"WaitForPop"` | `PopResults` が空でなければ `(TResult)PopResults.Dequeue()`、空なら `default(TResult)` |

```csharp
public sealed class FakeDialogService : DialogService
{
    public FakeDialogService(CallLog log) : base(null) { ... }
    /// <summary>ConfirmAsync が返す値。空なら DialogResult.Positive。</summary>
    public Queue<DialogResult> ConfirmResults { get; }
    /// <summary>ChooseAsync が返す値。空なら 0。</summary>
    public Queue<int> ChooseResults { get; }
    /// <summary>null でなければ、ShowInfoAsync はこの Task を待ってから終わる（実行中フラグのテスト用）。既定は null。</summary>
    public UniTaskCompletionSource InfoGate { get; set; }

    public override UniTask ShowInfoAsync(string message);                // 記録 "Info:" + message
    public override UniTask<DialogResult> ConfirmAsync(string message, string positiveLabel = "はい", string negativeLabel = "いいえ");   // 記録 "Confirm:" + message
    public override UniTask<int> ChooseAsync(string message, string[] labels);   // 記録 "Choose:" + message + ":" + string.Join("/", labels)
}
```

### 3.4 マスタの読み取り（`Uta.Client.Application.Master`）

```csharp
public enum MasterItemKind { Nothing = 0, Idol = 1, SupportCard = 2 }   // Contracts.Gacha.ItemKind・マスタの ItemKind と同じ数値

public sealed class IdolInfo
{
    public IdolInfo(int idolId, string name, Rarity rarity, Plan plan, int maxHp,
        int initialSinging, int initialRhythm, int initialExpression,
        ParamKind firstParam, ParamKind secondParam, ParamKind thirdParam);
    public int IdolId { get; } public string Name { get; } public Rarity Rarity { get; } public Plan Plan { get; } public int MaxHp { get; }
    public int InitialSinging { get; } public int InitialRhythm { get; } public int InitialExpression { get; }
    public ParamKind FirstParam { get; } public ParamKind SecondParam { get; } public ParamKind ThirdParam { get; }
}
public sealed class SupportCardInfo
{
    public SupportCardInfo(int supportCardId, string name, Rarity rarity, SupportType type, Plan plan);
    public int SupportCardId { get; } public string Name { get; } public Rarity Rarity { get; } public SupportType Type { get; } public Plan Plan { get; }
}
public sealed class SupportAbilityInfo
{
    public SupportAbilityInfo(int order, SupportAbilityKind kind, ParamKind param, int valueAtLv1, int valueAtMax);
    public int Order { get; } public SupportAbilityKind Kind { get; } public ParamKind Param { get; }   // Kind が MaxHp のとき Param は ParamKind.Nothing
    public int ValueAtLv1 { get; } public int ValueAtMax { get; }
}
public sealed class GachaEntryInfo
{
    public GachaEntryInfo(MasterItemKind itemKind, int itemId, int weight);
    public MasterItemKind ItemKind { get; } public int ItemId { get; } public int Weight { get; }   // ItemId は育成アイドルなら IdolId、サポートカードなら SupportCardId
}
public sealed class GachaInfo
{
    public GachaInfo(int gachaId, int singleCost, int tenCost, int ceilingPoints,
        IReadOnlyList<GachaEntryInfo> normalEntries, IReadOnlyList<GachaEntryInfo> guaranteedEntries);
    public int GachaId { get; } public int SingleCost { get; } public int TenCost { get; } public int CeilingPoints { get; }
    public IReadOnlyList<GachaEntryInfo> NormalEntries { get; } public IReadOnlyList<GachaEntryInfo> GuaranteedEntries { get; }   // どちらも GachaTableEntry.Order の昇順
}
public sealed class ScenarioWeekInfo
{
    public ScenarioWeekInfo(int week, WeekKind kind, int clearTarget, int perfectTarget);
    public int Week { get; } public WeekKind Kind { get; } public int ClearTarget { get; } public int PerfectTarget { get; }
}
public sealed class ScenarioInfo
{
    public ScenarioInfo(int scenarioId, string name, Difficulty difficulty, int weekCount, int paramCap, int retryCount, IReadOnlyList<ScenarioWeekInfo> weeks);
    public int ScenarioId { get; } public string Name { get; } public Difficulty Difficulty { get; } public int WeekCount { get; } public int ParamCap { get; } public int RetryCount { get; }
    public IReadOnlyList<ScenarioWeekInfo> Weeks { get; }      // Week の昇順
}
public readonly struct SupportLevelUpCost
{
    public SupportLevelUpCost(int supportPoints, int gold);
    public int SupportPoints { get; } public int Gold { get; }
}
public sealed class CardTextInfo
{
    public CardTextInfo(int cardId, string name, string description, string descriptionEnhanced);
    public int CardId { get; }
    public string Name { get; }                  // Card.Name
    public string Description { get; }           // CardFace（Face = Normal）の Description
    public string DescriptionEnhanced { get; }   // CardFace（Face = Enhanced）の Description。Enhanced の行が無ければ Description と同じ
}
public interface IGameMaster
{
    IReadOnlyList<IdolInfo> Idols { get; }                 // IdolId の昇順
    IdolInfo GetIdol(int idolId);                          // 無ければ KeyNotFoundException
    IReadOnlyList<SupportCardInfo> SupportCards { get; }   // SupportCardId の昇順
    SupportCardInfo GetSupportCard(int supportCardId);     // 無ければ KeyNotFoundException
    /// <summary>SupportAbility の行を Order の昇順で。サポートカードが無ければ KeyNotFoundException、行が 0 件なら空のリスト。
    /// 表示する値は ProduceFormulas.AbilityValue(ValueAtLv1, ValueAtMax, level, maxLevel)（Uta.Shared.Domain.Produce）。maxLevel は GetSupportMaxLevel(rarity, GetSupportMaxLimitBreak(rarity))（完凸の最大 Lv）。</summary>
    IReadOnlyList<SupportAbilityInfo> GetSupportAbilities(int supportCardId);
    IReadOnlyList<GachaInfo> Gachas { get; }               // GachaId の昇順
    GachaInfo GetGacha(int gachaId);                       // 無ければ KeyNotFoundException
    IReadOnlyList<ScenarioInfo> Scenarios { get; }         // ScenarioId の昇順
    ScenarioInfo GetScenario(int scenarioId);              // 無ければ KeyNotFoundException
    int GetSupportMaxLevel(Rarity rarity, int limitBreak);   // SupportLevelCap。無ければ KeyNotFoundException
    int GetSupportMaxLimitBreak(Rarity rarity);              // SupportLevelCap のその Rarity の LimitBreak の最大値。行が無ければ KeyNotFoundException
    /// <summary>fromLevel <= level < toLevel の SupportLevelCost（Rarity, level）の合計。サーバーの MasterSupportCatalog.GetLevelUpCost と同じ。fromLevel >= toLevel なら (0, 0)。間の行が無ければ InvalidOperationException。</summary>
    SupportLevelUpCost GetSupportLevelUpCost(Rarity rarity, int fromLevel, int toLevel);
    /// <summary>スキルカードの名前と説明文。カードが無ければ KeyNotFoundException。</summary>
    CardTextInfo GetCardText(int cardId);
}
```

`Rarity`・`Plan`・`ParamKind` は `Uta.Shared.Domain.Lesson`、`SupportType`・`SupportAbilityKind`・`Difficulty`・`WeekKind` は `Uta.Shared.Domain.Produce` の enum（マスタの enum と数値が同じ。`MasterCardCatalog` のとおり `(Domain の型)(int)マスタの値` で変換する）。スキルカードの効果・コスト・種類は、これまでどおり `ICardCatalog`（`Uta.Shared.Domain.Lesson.CardCatalog`）から読む。

### 3.5 `Uta.Client.Application.Player.RefreshPlayerUseCase`

```csharp
public sealed class RefreshPlayerUseCase
{
    public RefreshPlayerUseCase(IPlayerPort port, PlayerCache cache);
    /// <summary>
    /// port.SignInAsync を呼び、Succeeded なら cache.Set(snapshot) して snapshot を返す。
    /// Failed は InvalidOperationException("sign-in failed: " + ErrorText)、PlayerNotCreated は InvalidOperationException("sign-in failed: player not created")。どちらもキャッシュは変えない。
    /// </summary>
    public UniTask<PlayerSnapshot> ExecuteAsync();
}
```

### 3.6 `Uta.Client.Infrastructure.MasterGameData`

```csharp
public sealed class MasterGameData : IGameMaster
{
    public MasterGameData(MemoryDatabase db);   // db が null なら ArgumentNullException。読み取り用の型は構築時に全部作る
}
```

- 読み方はサーバーの `MasterGachaCatalog`・`MasterSupportCatalog`（`server/Uta.Server/Infrastructure/MasterData/`）と同じ。使うテーブル: `db.IdolTable.All`・`db.SupportCardTable.All`・`db.SupportAbilityTable.All`・`db.GachaTable.All`・`db.GachaTableEntryTable.All`（`GachaTableId` で絞り、`Order` の昇順）・`db.ScenarioTable.All`・`db.ScenarioWeekTable.All`・`db.SupportLevelCostTable.All`・`db.SupportLevelCapTable.All`・`db.CardTable.All`・`db.CardFaceTable.All`
- ガチャの通常枠は `Gacha.NormalTableId`、確定枠は `Gacha.GuaranteedTableId` のエントリー。`ItemKind`（マスタ）→ `MasterItemKind` は `(MasterItemKind)(int)`
- `GetSupportAbilities`: `SupportAbilityTable` を `SupportCardId` で絞り `Order` の昇順。`Kind` は `(Uta.Shared.Domain.Produce.SupportAbilityKind)(int)`、`Param` は `(Uta.Shared.Domain.Lesson.ParamKind)(int)`
- `GetCardText`: `CardTable` の `Name`、`CardFaceTable` の `Face == CardFaceKind.Normal` の行の `Description`、`Face == CardFaceKind.Enhanced` の行の `Description`（無ければ Normal と同じ）。Normal の行が無いカードがあれば、構築時に `InvalidOperationException("card face missing: " + cardId)`

### 3.7 起動と画面（`Uta.Client.Presentation.Startup`／`Home`）

```csharp
public interface IStartupView { void SetMessage(string message); }

public sealed class StartupPage : PageBase, IStartupView
{
    // BuildUi: Body の子に "Message"（CreateText。UiTheme.FontBody、TextAnchor.MiddleCenter）を作り、RectTransform を親いっぱいに広げる（anchorMin (0,0)、anchorMax (1,1)、offset 0）。
    // SetMessage: EnsureBuilt() してから Message の text を変える。
}

[AssetAddress(nameof(StartupPage))]
public sealed class StartupPresenter : IPresenter
{
    [Inject] public StartupPresenter(StartupPage view) : this((IStartupView)view) { }
    public StartupPresenter(IStartupView view);
    public UniTask InitializeAsync();   // view.SetMessage("起動中…") して UniTask.CompletedTask
    public void Dispose();              // 何もしない
}

public sealed class StartupFlow : IStartable
{
    public StartupFlow(SignInUseCase signIn, EnterPlayerUseCase enterPlayer, IScreenNavigator navigator, PageStack pageStack, DialogService dialogs);
    /// <summary>VContainer の IStartable。StartAsync().Forget()。</summary>
    public void Start();
    /// <summary>実行番号を 1 進め、その番号で起動の流れを 1 回走らせる。テストはこれを await する。</summary>
    public UniTask StartAsync();
    /// <summary>エラーの「タイトルへ」から呼ばれる。StartAsync().Forget() して、すぐ UniTask.CompletedTask を返す（ApiCore は遷移を待ってから元の呼び出しに失敗を返すので、ここで新しい起動の終わりまで待たない）。</summary>
    public UniTask RestartAsync();
}
```

`StartupFlow` は実行番号 `private int currentRun;` を持つ。`StartAsync()` は `var run = ++currentRun;` として、次の `RunAsync(run)` を `try` で包んで `await` する（`catch (Exception e)` では `run == currentRun` のときだけ `Debug.LogException(e)`）。以下「古ければ終わる」は `if (run != currentRun) return;`。

`RunAsync(int run)` の手順:
1. `await pageStack.ClearAsync(navigator);`（初回は 0 枚なので何もしない。やり直しでは、ホームとその上を全部閉じる）。古ければ終わる
2. `await navigator.PushPageAsync<StartupPresenter>(stack: false, playAnimation: false);`（`stack: false` なので、次にホームを積むとこのページは消え、ホームがスタックの底になる）。古ければ終わる
3. 次を `while (true)` で繰り返す:
   1. `try { var registered = await signIn.ExecuteAsync(); entered = await enterPlayer.ExecuteAsync(registered); }`（`entered` は `EnterPlayerResult`。ループの外で宣言する）。古ければ終わる。成功なら `break`
   2. `catch (Exception e)`: 古ければ終わる。`Debug.LogWarning("[Uta] startup failed: " + e)`。`var answer = await dialogs.ConfirmAsync("起動に失敗しました: " + e.Message, "再試行", "終了");` 古ければ終わる。`answer == DialogResult.Negative` なら `UnityEngine.Application.Quit();` して終わる。`Positive` ならループの先頭へ
4. `await navigator.PushPageAsync<HomePresenter>(playAnimation: false);` 古ければ終わる
5. `entered.Snapshot.DiscardedProduce` が true なら `await dialogs.ShowInfoAsync("バージョンが変わったため、進行中のプロデュースは破棄されました。");`（ホームの上で知らせる。screen-design 4章）

起動中に `ApiCore` の共通ダイアログで「タイトルへ」が選ばれたとき: `UtaErrorNavigator.GoToTitleAsync` → `RestartAsync` で新しい実行が始まり、そのあと元の実行は失敗を受け取るが、番号が古いので何も出さずに終わる。

```csharp
public sealed class StartupDiagnostics : IStartable     // 旧 StartupPresenter の診断をそのまま移す
{
    public StartupDiagnostics(SignInUseCase signIn, EnterPlayerUseCase enterPlayer);
    public void Start();   // 旧 RunAsync と同じ手順・同じログの行。AddLine は Debug.Log("[Uta] " + line) だけ（画面に出さない）。最後に UnityEngine.Application.Quit(succeeded ? 0 : 1)
}

public interface IHomeView
{
    void SetProfile(int level, int gachaCurrency);
    void SetResumeVisible(bool visible);
    event Action ProduceClicked;
    event Action ResumeClicked;
    event Action GachaClicked;
    event Action OwnedClicked;
}

public sealed class HomePage : PageBase, IHomeView { ... }   // レイアウトは下

[AssetAddress(nameof(HomePage))]
public sealed class HomePresenter : IPresenter
{
    [Inject] public HomePresenter(HomePage view, PlayerCache cache, DialogService dialogs);   // : this((IHomeView)view, cache, dialogs)
    public HomePresenter(IHomeView view, PlayerCache cache, DialogService dialogs);
    public UniTask InitializeAsync();   // 4 つのイベントを購読し、Refresh()
    public void DidPopEnter();          // Refresh()
    public void Dispose();              // 4 つの購読を解く
    public UniTask OpenProduceAsync();  // この計画: 実行中フラグ（3.3.2 の 5）の中で dialogs.ShowInfoAsync("準備中です。")。4 本目が書き換える
    public UniTask ResumeProduceAsync();// 同上。4 本目が書き換える
    public UniTask OpenGachaAsync();    // 同上。2 本目が書き換える
    public UniTask OpenOwnedAsync();    // 同上。3 本目が書き換える
    // Refresh(): var s = cache.Current; view.SetProfile(s.Level, s.GachaCurrency); view.SetResumeVisible(s.HasActiveProduce);
    // ProduceClicked → OpenProduceAsync().Forget()、ResumeClicked → ResumeProduceAsync().Forget()、GachaClicked → OpenGachaAsync().Forget()、OwnedClicked → OpenOwnedAsync().Forget()
}
```

- 2〜4 本目は、`HomePresenter` のコンストラクター（2 本）の最後に依存を足し、対応するメソッドの中身を書き換える。`IScreenNavigator` を足すのは 2 本目（ガチャ）。3・4 本目は足された前提で、自分のユースケースだけを足す（3 本目と 2 本目を並行して進めるときは、3 本目が `IScreenNavigator` を足す差分を 2 本目に合わせて取り込む）
- `HomePage` のレイアウト（`BuildUi(body)`）: `var root = UiFactory.CreateVertical(body, "Root");` の子に上から
  1. "Profile"（`CreateText`。`UiTheme.FontBody`、`MiddleLeft`。`SetHeight(80)`）。`SetProfile` で `"プレイヤーLv " + level + "　ガチャ通貨 " + gachaCurrency.ToString("N0", CultureInfo.InvariantCulture)`（間は全角の空白 1 つ）
  2. "Spacer"（`CreateRect`。`SetFlexibleHeight(1)`。叩き台のキャラ表示の場所。MVP は空）
  3. "ResumePanel"（`CreatePanel`。`SetHeight(260)`）の子に "ResumeText"（`CreateText("プロデュース中", UiTheme.FontBody, TextAnchor.MiddleCenter)`、`SetHeight(70)`）と "Resume"（`CreateButton("再開")`）。`SetResumeVisible(false)` のとき `ResumePanel` を `SetActive(false)`。叩き台の「週 7 / 16」は、`PlayerSnapshot` に週の情報が無いので出さない（[Uta案]。叩き台との差）
  4. "Menu"（`CreateHorizontal`。`SetHeight(UiTheme.ButtonHeight)`）の子に左から "Produce"「プロデュース」、"Gacha"「ガチャ」、"Owned"「アイドル」（`CreateButton`）
  - "Resume"・"Produce"・"Gacha"・"Owned" の `onClick` で、それぞれ `ResumeClicked`・`ProduceClicked`・`GachaClicked`・`OwnedClicked` を発火する

### 3.8 `Bootstrap` と `Infrastructure.Api`

- `UtaErrors.Create()`（`Uta.Client.Infrastructure.Api`）: 次の 19 個を、この順で返す。`PlayerErrors.NotCreated` → `ErrorAction.Title`（今までどおり）。残りの 18 個は `ErrorAction.Stay`: `GachaErrors.NotEnoughCurrency`・`NotEnoughPoints`・`NotFound`・`InvalidCount`・`CostMismatch`、`SupportErrors.InvalidTargetLevel`・`NotEnoughPoints`・`MaxLimitBreak`・`NotOwned`・`InvalidMaterial`、`PlayerErrors.NotEnoughGold`、`ProduceErrors.StaleOperation`・`UnexpectedPhase`・`InvalidArgument`・`InvalidFormation`・`NotInProgress`・`SessionDiscarded`・`AlreadyInProgress`（`Uta.Shared.Contracts` の `Gacha`・`Support`・`Player`・`Produce` 名前空間）。登録が無いコードは基盤が `UNKNOWN_ERROR`（`Title`）に変えてしまうため。`handledCodes` に入れなかったコードは、共通ダイアログを出して画面に残る
- `UtaApiClient.Create(string serverUrl, string platform, int appVersion, string accountFilePath, string keySource, ICommonErrorHandler errorHandler, IApiActivityHandler activityHandler, IErrorNavigator errorNavigator)`: `ApiCore.CreateBuilder(...)` を変数に受け、`activityHandler` が null でなければ `.AddActivityHandler(activityHandler)`、`errorNavigator` が null でなければ `.WithErrorNavigator(errorNavigator)` を呼んでから `.Build()`。`IApiActivityHandler`・`IErrorNavigator` は `VolTech.Client` の IF
- `ErrorDialogView : IErrorDialogView`（`Uta.Client.Bootstrap`。`MonoBehaviour` をやめた普通の `sealed class`）: コンストラクター `ErrorDialogView(bool autoDismiss, DialogService dialogs)`。`ShowAsync(ApiError error, ErrorDialogButtons buttons)` は、まず今までどおり `Debug.LogWarning("[Uta] error dialog: " + error + " buttons=" + buttons)`。`autoDismiss` なら今までと同じ戻り値（Ok → Ok、RetryOrTitle → Title、Home → Home、Title → Title、それ以外 → Store）を即返す。そうでなければ `dialogs.ChooseAsync("エラーが発生しました。\n" + error, labels)` を出し、添字を `ErrorDialogButton` に直す: `Ok` → `new[] { "OK" }` → `Ok`、`RetryOrTitle` → `new[] { "再試行", "タイトルへ" }` → 0: `Retry`／1: `Title`、`Home` → `new[] { "ホームへ" }` → `Home`、`Title` → `new[] { "タイトルへ" }` → `Title`、それ以外 → `new[] { "ストアへ" }` → `Store`。旧 `Initialize`・`OnGUI`・`OnDestroy`・`ButtonAt`・`ButtonText` は削除する。置き場は `Bootstrap` のまま（`Presentation` は VolTech-MagicOnion のクライアントを参照しないため）
- `ApiActivityBridge : IApiActivityHandler`（`Uta.Client.Bootstrap`）: コンストラクターで `BusyIndicator` を受ける。`OnActivityStarted()` → `Begin()`、`OnActivityEnded()` → `End()`
- `UtaErrorNavigator : IErrorNavigator`（`Uta.Client.Bootstrap`）: コンストラクターで `IObjectResolver` を受け、呼ばれたときに解決する。直接注入しないのは循環を避けるため（`UtaApiClient` → `UtaErrorNavigator` → `StartupFlow` → `SignInUseCase` → `DeviceAuthGateway` → `UtaApiClient`）
  - `GoToTitleAsync()`: `resolver.TryResolve<StartupFlow>(out var flow)` が false（診断モード。`StartupFlow` を登録しない）なら `UniTask.CompletedTask`。true なら `flow.RestartAsync()` を返す（`DATE_CHANGED`・メンテナンスなどで起動の流れからやり直す。[screen-design.md](../wiki/architecture/screen-design.md) 5章）
  - `GoToHomeAsync()`（`async`）: `TryResolve<StartupFlow>` が false なら何もしない。true なら `try { await resolver.Resolve<RefreshPlayerUseCase>().ExecuteAsync(); } catch (Exception e) { Debug.LogWarning("[Uta] refresh on go-to-home failed: " + e.Message); }` のあと（取り直しに失敗してもホームへは戻る）、`await resolver.Resolve<PageStack>().PopToRootAsync(resolver.Resolve<IScreenNavigator>());`。ホームの表示は `HomePresenter.DidPopEnter` がやり直す（ホームが既に一番上でページが閉じられなかったときは、表示は古いまま。MVP では受け入れる）
  - `OpenStoreAsync()`: `UniTask.CompletedTask`（Uta はストアへ誘導しない。[repository-layout.md](../wiki/architecture/repository-layout.md) 10.5）
- `UtaBootstrap`: タスク 4 の手順 4 に書く。画面の雛形の登録は `private static void RegisterScreens(UiRoot uiRoot)` にまとめ、この計画では `uiRoot.RegisterPage<StartupPage>(); uiRoot.RegisterPage<HomePage>();` の 2 行。2〜6 本目はここに 1 行ずつ足す

DI の登録（通常起動・診断モードで共通）:

```csharp
builder.RegisterInstance(uiRoot);
builder.RegisterInstance(uiRoot.Dialogs);
builder.RegisterComponent(uiRoot.Pages);        // PageContainer
builder.RegisterComponent(uiRoot.Modals);       // ModalContainer（Overlays の ModalContainer は登録しない）
builder.RegisterComponent(uiRoot.Overlays);     // OverlayContainer
builder.Register<IScreenNavigator, ScreenNavigator>(Lifetime.Singleton);
builder.Register<PageStack>(Lifetime.Singleton);
builder.RegisterInstance<IGameMaster>(gameMaster);
builder.RegisterInstance<ICardCatalog>(cardCatalog);
builder.Register<UtaApiClient>(resolver => UtaApiClient.Create(serverUrl, AppInfo.Platform, AppInfo.Version, accountPath, AppInfo.AccountKeySource,
    new DefaultCommonErrorHandler(errorDialog), new ApiActivityBridge(uiRoot.Busy), new UtaErrorNavigator(resolver)), Lifetime.Singleton);
builder.Register<DeviceAuthGateway>(Lifetime.Singleton).As<IDeviceAuthPort>();
builder.Register<PlayerGateway>(Lifetime.Singleton).As<IPlayerPort>();
builder.Register<PlayerCache>(Lifetime.Singleton);
builder.Register<SignInUseCase>(Lifetime.Singleton);
builder.Register<EnterPlayerUseCase>(Lifetime.Singleton);
builder.Register<RefreshPlayerUseCase>(Lifetime.Singleton);
// 通常起動: RegisterEntryPoint は実装した IF（IStartable）でしか登録しないので、UtaErrorNavigator が型で引けるように AsSelf を付ける
builder.RegisterEntryPoint<StartupFlow>().AsSelf();
// 診断モード（-uta-quit-after-check）: 代わりに
builder.RegisterEntryPoint<StartupDiagnostics>();
```

Presenter（`StartupPresenter`・`HomePresenter`、2〜6 本目のもの）は登録しない（3.3.2）。`StartupOptions`・`StartupView` の登録は削除する。

## 4. タスク

各タスクの終わりに、`unity recompile --format json`（`client/uta` で実行。コンパイルエラー 0）が通り、そのタスクまでの EditMode のテストが通る状態にする。EditMode のテストは、Editor が開いている環境で `unity test --mode EditMode`（タスク 1 で動くかを確かめる。動かなければ Editor の Test Runner の EditMode）で回す。**Editor を開けない環境では、通ったとは書かず、確かめていないと報告する**。

### タスク 1: 参照の追加と共通部品の前半（止まる条件あり）

触る: 3 つの asmdef（3.1）、`Presentation/Common/UiTheme.cs`・`UiFonts.cs`・`UiFactory.cs`・`SafeAreaFitter.cs`・`PageBase.cs`・`ModalBase.cs`・`UiRoot.cs`、`Tests/EditMode/UiFactoryTests.cs`。

手順:
1. 3.1 のとおり asmdef を直す
2. 上の `.cs` を 3.2 のとおり作る。`UiRoot` は、この時点では `Busy`・`Dialogs`（と "Busy"・"Dialogs" の子）を作らない。タスク 2 で足す
3. `UiFactoryTests` を 5 章のとおり作る
4. `unity recompile --format json`。**次のどれかが起きたら止まって報告する**（直し方を自分で決めない）:
   - `Page.Initialize()`／`Modal.Initialize()` を `public override UniTask` で上書きできない（`IEnumerator` の版になっている。`USN_USE_ASYNC_METHODS` が定義されていない）
   - `UnityScreenNavigator`・`UnityEngine.UI`・`Unity.InputSystem` の asmdef 名で参照できない
   - `UnityEngine.UI.Text`・`Font.CreateDynamicFontFromOSFont`・`Font.GetOSInstalledFontNames`・`Resources.GetBuiltinResource` が無い、または obsolete のエラー（CS0619）になる
   - `PageContainer.AssetLoader`・`PreloadedAssetLoader.PreloadedObjects`・`OverlayContainer` が 3.2 の使い方と違う
5. `unity test --mode EditMode` を回す。動いたら、そのことを控えておく（タスク 6 で CLAUDE.md と unity7-evaluation.md 4章に足す）。動かなければ Editor の Test Runner で回し、`unity test` は使えなかったと報告する

終わったことの確かめ方: コンパイルエラー 0。止まる条件に当たらない。`UiFactoryTests` が通る。

### タスク 2: 共通部品の後半（ブロッカー・ダイアログ・ページの数え方・ウィンドウ）とテスト用の偽

触る: `BusyIndicator.cs`・`DialogService.cs`・`WindowAspectKeeper.cs`・`PageStack.cs`・`UiRoot.cs`（`Busy`・`Dialogs` を足す）、`client/uta/ProjectSettings/ProjectSettings.asset`、`Tests/EditMode/Fakes/*.cs`、`Tests/EditMode/BusyIndicatorTests.cs`・`DialogServiceTests.cs`・`PageStackTests.cs`。

手順:
1. 3.2 のとおり作る。`UiRoot.Create` に "Busy"・"Dialogs" の子と `WindowAspectKeeper` を足す
2. `ProjectSettings.asset` を直す（テキストで該当行だけ。Editor が開いているなら閉じてから編集する）: `defaultScreenWidth: 540`、`defaultScreenHeight: 960`、`fullscreenMode: 3`、`resizableWindow: 1`、`defaultScreenOrientation: 0`、`allowedAutorotateToPortrait: 1`、`allowedAutorotateToPortraitUpsideDown: 0`、`allowedAutorotateToLandscapeRight: 0`、`allowedAutorotateToLandscapeLeft: 0`（いまは順に 1024・768・1・0・4・1・1・1・1）
3. 3.3.8 の `CallLog`・`FakeScreenNavigator`・`FakeDialogService` を作る
4. 5 章の `BusyIndicatorTests`・`DialogServiceTests`・`PageStackTests` を作る

境界: `ChooseAsync` に null・0 個・5 個以上のラベルを渡すと `ArgumentException`。`BusyIndicator.End` を `Begin` より多く呼んでも数は 0 より下がらない。`PageStack` は枚数が 0・1 のとき Pop を呼ばない。

終わったことの確かめ方: コンパイルエラー 0。`BusyIndicatorTests`・`DialogServiceTests`・`PageStackTests` が通る。

### タスク 3: マスタの読み取り口・取り直し・業務エラーの登録

前提の確認: 生成された `Scripts/Infrastructure/MasterData/Generated/Lesson/Card.cs` に `public string Name`、`CardFace.cs` に `public string Description` がある（計画 0）。無ければ止まって報告する。

触る: `Application/Master/*.cs`（11 ファイル）、`Application/Player/RefreshPlayerUseCase.cs`、`Infrastructure/MasterData/MasterGameData.cs`、`Infrastructure/Api/UtaErrors.cs`、`Tests/EditMode/MasterGameDataTests.cs`・`RefreshPlayerUseCaseTests.cs`・`UtaErrorsTests.cs`。

手順: 3.4・3.5・3.6・3.8（`UtaErrors`）のとおり作る・直す。`UtaErrorsTests` は 5 章のとおり書き直す（旧 `Create_ContainsPlayerNotCreatedWithTitleAction` は消す）。

境界: `GetSupportLevelUpCost` は `fromLevel >= toLevel` なら `SupportLevelUpCost(0, 0)`。`GetSupportAbilities` は行が 0 件なら空のリスト（例外にしない）。

終わったことの確かめ方: `MasterGameDataTests`・`RefreshPlayerUseCaseTests`・`UtaErrorsTests` が通る。

### タスク 4: 起動の流れとホーム、診断モード、Bootstrap

触る: `Presentation/Startup/*`、`Presentation/Home/*`、`Infrastructure/Api/UtaApiClient.cs`、`Bootstrap/ApiActivityBridge.cs`・`UtaErrorNavigator.cs`・`ErrorDialogView.cs`・`UtaBootstrap.cs`、`Tests/EditMode/StartupFlowTests.cs`・`HomePresenterTests.cs`。

手順:
1. `StartupDiagnostics.cs` を作る（旧 `StartupPresenter.RunAsync` の手順とログの行をそのまま移す。`view.SetText` は呼ばない。`options.QuitAfterCheck` の分岐はやめ、常に最後に `Quit(succeeded ? 0 : 1)`）。`StartupView.cs`・`StartupOptions.cs` を削除し、`StartupPresenter.cs` を 3.7 の内容に置き換える
2. `IStartupView`・`StartupPage`・`StartupFlow`、`IHomeView`・`HomePage`・`HomePresenter` を 3.7 のとおり作る
3. `UtaApiClient.Create`・`ErrorDialogView`・`ApiActivityBridge`・`UtaErrorNavigator` を 3.8 のとおり作る・直す
4. `UtaBootstrap.RunAsync` を次の順にする:
   1. `serverUrl`・`quitAfterCheck` を今までと同じに読む
   2. `var uiRoot = UiRoot.Create(); RegisterScreens(uiRoot);`（通常起動・診断モードの両方。診断モードでも `ErrorDialogView` が `DialogService` を要る）
   3. マスタ: `var db = await MasterDataLoader.LoadFromStreamingAssetsAsync(); cardCatalog = MasterCardCatalog.Create(db); gameMaster = new MasterGameData(db);` を `try` で包む。失敗したら `Debug.LogError("[Uta] master: FAILED " + e.GetType().FullName + ": " + e.Message)`。`quitAfterCheck` なら `UnityEngine.Application.Quit(1)` して終わる。そうでなければ `await uiRoot.Dialogs.ShowInfoAsync("マスタを読めませんでした: " + e.Message);` して終わる（認証に進まない。screen-design 4章 1）。成功したら今までどおり `Debug.Log("[Uta] master: ok cards=" + cardCatalog.CardIds.Count)`（診断の確認で見る行）
   4. `var errorDialog = new ErrorDialogView(quitAfterCheck, uiRoot.Dialogs);`
   5. `var accountPath = ...`（今までと同じ）。`LifetimeScope.Create` で 3.8 の DI の登録をする（`quitAfterCheck` で `StartupFlow` と `StartupDiagnostics` を分ける）。`DontDestroyOnLoad(scope.gameObject)` は今までどおり
5. 5 章の `StartupFlowTests`・`HomePresenterTests` を作る
6. Play モードで確かめる（サーバーを起動した状態。CLAUDE.md「ローカルのサーバー」）: `unity command editor.playmode start` → 数秒後に `unity command console get --tail 50 --level error --format json`（エラーが無いこと）→ `unity command eval 'return UnityEngine.Object.FindFirstObjectByType<Uta.Client.Presentation.Common.UiRoot>().Pages.Pages.Count;'` が `1`（ホームだけ。起動のページが消えている）→ `unity command editor.playmode stop`。画面の見た目は 5 章の手動の確認で見る

止まる条件: `RegisterEntryPoint<StartupFlow>()` の戻り値に `.AsSelf()` が付けられない、または `IObjectResolver.TryResolve<T>(out T)` が無い（VContainer 1.19.0 の API が想定と違う）。

終わったことの確かめ方: コンパイルエラー 0。`StartupFlowTests`・`HomePresenterTests` が通る。手順 6 で `Pages.Count` が 1。

### タスク 5: 診断モードの確認

手順: `-uta-quit-after-check` の経路（`StartupDiagnostics`）が動くことを、サーバー（`docker compose ... up` → `dotnet run --project server/Uta.Server -- migrate` → `dotnet run --project server/Uta.Server`）を起動した状態で、Windows の IL2CPP のビルド（CLAUDE.md の「ビルド・テスト・実行」。Editor を閉じてから）→ `Uta.exe -batchmode -nographics -uta-quit-after-check`（終了コード 0）で確かめる。Editor を閉じる・サーバーを起動する環境がなければ、確かめていないと報告する。

### タスク 6: 結果の記録（wiki・CLAUDE.md）

1. `unity test --mode EditMode` が動いたら（タスク 1）: CLAUDE.md の「ビルド・テスト・実行」の Unity の行のあとに「クライアントの EditMode のテスト（Editor が開いているとき）: `unity test --mode EditMode`（`client/uta` で実行）」を 1 行足し、[unity7-evaluation.md](../wiki/architecture/unity7-evaluation.md) 4章の「`unity` CLI の使い方（動いたもの）」に `unity test --mode EditMode` を足す。動かなければどちらも足さない
2. [unity7-evaluation.md](../wiki/architecture/unity7-evaluation.md):
   - 2章の表に、確かめたものを 1 行ずつ足す（UnityScreenNavigator の基盤の `ScreenNavigator`（Presenter 駆動）で、コードで組んだ雛形（`PreloadedAssetLoader`）のページを積む・`stack: false` のページが次の Push で消える／uGUI の legacy `Text` と OS フォント（Windows）／Input System の UI 入力（`InputSystemUIInputModule`）／`IApiActivityHandler` のブロッカー）。確かめられなかったものは 3.3 に足す。モーダル（`ModalBase`・`DefaultModalBackdrop`）は「この計画では出していない。2 本目で確かめる」と 3.3 に書く
   - 3.3 の 61 行「`ErrorDialogView`（診断用の IMGUI）」を「`ErrorDialogView`（`DialogService` のダイアログ。`-uta-quit-after-check` では自動で閉じる）」に直す（経路が未確認なのは変わらない）
   - 3.3 の 66 行「UnityScreenNavigator・Supplement は取り込みだけで、使っていない」を「UnityScreenNavigator は基盤の `Runtime/ChinpanGX`（`ScreenNavigator`）を画面に使った（2章）。Supplement は使っていない」に直す
   - 4章 78 行「画面は IMGUI の診断用で、画面の計画（7 番目）で作り直す」を「画面は順 7 の計画（`uta_docs/plans/2026-11-03-client-screens-foundation.md`）で uGUI に作り直した。診断は `-uta-quit-after-check` のときだけ（`StartupDiagnostics`）」に直す
3. [screen-design.md](../wiki/architecture/screen-design.md):
   - 4章 手順 4 の最後の文「診断用の IMGUI（`StartupView`・`ErrorDialogView`）に結果を出すところまでで、画面は順 7 [2026-11-03]」を「起動の流れは `StartupFlow`（起動のページ → ホーム）。診断は `-uta-quit-after-check` のときだけ `StartupDiagnostics` がログに出す [2026-11-03]」に直す
   - 5章 1 つ目の括弧「進行中通知とナビゲーターは未接続で、エラーハンドラーは `DefaultCommonErrorHandler` と診断用の `ErrorDialogView`。画面の計画で作り直す。2026-11-03」を「進行中通知は `ApiActivityBridge` → `BusyIndicator`、ナビゲーターは `UtaErrorNavigator`、エラーハンドラーは `DefaultCommonErrorHandler` と `ErrorDialogView`（`DialogService`）。業務エラーは `UtaErrors` に `Stay` で登録する。2026-11-03」に直す
   - 7.2 の表の「共通部品」の役割を「`UiTheme`・`UiFonts`・`UiFactory`・`SafeAreaFitter`・`WindowAspectKeeper`・`UiRoot`（`Canvas`・`PageContainer`・`ModalContainer`・`OverlayContainer`・ブロッカー・ダイアログ層）・`PageBase`・`ModalBase`・`PageStack`・`BusyIndicator`・`DialogService`」に、「画面」の行の `<名前>Presenter`（`Presenter<I<名前>View>`）を「`<名前>Presenter`（基盤の `IPresenter`）」に直す
   - 7.2 の「Presenter の作り方」の項（`ScreenNavigator` が `Bind(view)` を呼ぶ・`configure` コールバック）を消し、「Presenter の形」の項に「書き方の正は `uta_docs/plans/2026-11-03-client-screens-foundation.md` 3.3」を足す
   - 7.3 の「エラー」の項に「業務エラーのコードは全部 `UtaErrors` に `ErrorAction.Stay` で登録する（登録が無いと基盤が `UNKNOWN_ERROR` にしてタイトルへ戻す）」を足す
   - 「実装メモ」に、設計とのズレを書く（なければ「順 7 の 1 本目: なし」）
4. [repository-layout.md](../wiki/architecture/repository-layout.md):
   - 3章の表の `Uta.Client.Presentation` の行の参照してよいものに `UnityEngine.UI`・`Unity.InputSystem` を足す
   - 126 行「診断用の `ErrorDialogView`（`IErrorDialogView` の IMGUI 実装）は `Bootstrap` に置く [2026-11-03]: ...」を「`ErrorDialogView`（`IErrorDialogView` の実装。表示は `Presentation` の `DialogService` に任せる）は `Bootstrap` に置く [2026-11-03]: `Presentation` の参照先に VolTech-MagicOnion のクライアントが無いため」に直す
   - 277 行付近の未確認の表の UnityScreenNavigator の「確かめた範囲」に、この計画で Editor の Play モードで確かめた範囲（ページの Push・`stack: false`）を足す
5. `python -I .claude/skills/linting-wiki/lint_wiki.py` を回し、エラーが 0 であること
6. 全部のタスクが終わったら、この計画の `status` を `done` にする

## 5. テスト

置き場: `Tests/EditMode/`。既存のテスト用 asmdef（`Uta.Client.Infrastructure.Tests.EditMode`）を使う。名前空間は `Uta.Client.Infrastructure.Tests`。書き方は 3.3.7。

| ファイル | テスト | 内容 |
|---|---|---|
| `UiFactoryTests.cs` | `CreateButton_Default_HasButtonLabelAndHeight` | `Button`、子 "Label" の `Text.text == label`、`LayoutElement.preferredHeight == UiTheme.ButtonHeight` |
| | `SetInteractable_False_ChangesColor` | `interactable == false`、`Image.color == UiTheme.ButtonDisabled` |
| | `SetLabel_ChangesText` | 子 "Label" の `text` が変わる |
| | `CreateScrollContent_ReturnsContentUnderViewport` | 戻り値の親の名前が "Viewport"、戻り値に `ContentSizeFitter` と `VerticalLayoutGroup` |
| | `ClearChildren_RemovesAllChildren` | EditMode（`Application.isPlaying == false`）で子 3 つが 0 になる |
| `BusyIndicatorTests.cs` | `Begin_Once_IsBusy` | `Initialize(null)` のあと `Begin()` で `IsBusy == true`、`End()` で false |
| | `Begin_Nested_StaysBusyUntilAllEnded` | `Begin()` 2 回、`End()` 1 回ではまだ true、もう 1 回で false |
| | `End_WithoutBegin_DoesNothing` | `IsBusy == false` のまま、例外なし。そのあと `Begin()` 1 回で true |
| | `Begin_WithBlocker_EnablesImage` | `Initialize(image)`（enabled = false）→ `Begin()` で `image.enabled == true`、`End()` で false |
| `DialogServiceTests.cs` | `ConfirmAsync_PositiveClicked_ReturnsPositive` | `new GameObject("Layer", typeof(RectTransform))` の層。`ConfirmAsync("m")` のあと、層の下の `Button` を `GetComponentsInChildren<Button>()` で取り、[0] の `onClick.Invoke()` → 結果が `Positive`、層の子が 0 |
| | `ChooseAsync_SecondClicked_ReturnsOne` | ラベル 3 つ。[1] を押すと 1 |
| | `ChooseAsync_FiveLabels_Throws` | `ArgumentException` |
| | `ShowInfoAsync_Twice_ShowsOneAtATime` | 2 回続けて呼ぶと層の子は 1 つ。OK を押すと 2 つ目が出る（子が 1 つで、"Message" の text が 2 つ目の文言） |
| `PageStackTests.cs` | `PopToRoot_ThreePages_PopsTwo` | 枚数 3・モーダル 0 → `log` が `"PopPage:2"` だけ |
| | `PopToRoot_WithModals_PopsModalsFirst` | 枚数 2・モーダル 1 → `"PopModal:1"`, `"PopPage:1"` |
| | `PopToRoot_OnePage_DoesNothing` | 枚数 1・モーダル 0 → 記録なし |
| | `Clear_PopsAll` | 枚数 3・モーダル 2 → `"PopModal:2"`, `"PopPage:3"` |
| | `Clear_Empty_DoesNothing` | 0・0 → 記録なし |
| `MasterGameDataTests.cs` | `Constructor_FromStreamingAssets_ReadsAllTables` | `MasterDataLoader.Load(File.ReadAllBytes(UnityEngine.Application.streamingAssetsPath + "/" + MasterDataLoader.RelativePath))` で作る（`MasterDataLoaderTests` と同じ読み方）。`Idols.Count == 4`、`SupportCards.Count == 10`、`Gachas.Count == 1`、`Scenarios.Count == 2` |
| | `GetSupportLevelUpCost_SumsRows` | R の Lv1→Lv3 が `SupportLevelCostTable` の (R, 1) と (R, 2) の行の合計 |
| | `GetSupportLevelUpCost_SameLevel_ReturnsZero` | `fromLevel == toLevel` で (0, 0) |
| | `GetSupportMaxLevel_MatchesCap` | `SupportLevelCapTable` の各行と一致 |
| | `GetGacha_NormalEntriesOrderedAndWeighted` | `NormalEntries` が `GachaTableEntry.Order` の昇順で、`Weight` の合計が 10000 |
| | `GetSupportAbilities_OrderedAndMapped` | `SupportCards[0]` の `SupportCardId` で、`SupportAbilityTable` の行（同じ ID、`Order` の昇順）と件数・`Kind`・`Param`・値が一致。件数 1 以上 |
| | `GetCardText_ReturnsNameAndDescriptions` | `CardTable` の最初の行の `CardId` で、`Name` が `Card.Name`、`Description` が Normal の行の `Description`、`DescriptionEnhanced` が Enhanced の行の `Description`（無ければ Normal と同じ） |
| | `GetIdol_Unknown_ThrowsKeyNotFound` | |
| | `GetCardText_Unknown_ThrowsKeyNotFound` | |
| `RefreshPlayerUseCaseTests.cs` | `Execute_Success_UpdatesCache` | 偽の `IPlayerPort`（`EnterPlayerUseCaseTests` の `FakePlayerPort` と同じ作りを、このファイルに private で書く）が返した `PlayerSnapshot` が `PlayerCache.Current` になり、戻り値と同じ |
| | `Execute_Failed_Throws` | `InvalidOperationException`（メッセージが `"sign-in failed: "` で始まる）、キャッシュは変わらない |
| | `Execute_NotCreated_Throws` | メッセージが `"sign-in failed: player not created"` |
| `UtaErrorsTests.cs` | `Create_ContainsAllBusinessErrors` | 長さ 19。`PlayerErrors.NotCreated` が `Title`、3.8 の残り 18 個が全部入っていて `Stay` |
| | `Create_IsAcceptedByErrorPolicyProvider` | `new ErrorPolicyProvider(UtaErrors.Create())` が例外を投げず、`GetDefaultAction(PlayerErrors.NotCreated) == Title`、`GetDefaultAction(GachaErrors.NotEnoughCurrency) == Stay` |
| `StartupFlowTests.cs` | `StartAsync_Success_PushesStartupThenHome` | 偽の `IDeviceAuthPort`（`GetAccountStateAsync` は `Registered`、`EnsureSignedInAsync` は完了済み）と偽の `IPlayerPort`（`SignInAsync` は `Success`）。`PageStack(() => 0, () => 0)`。`log` が `"PushPage:StartupPresenter(nostack)"`, `"PushPage:HomePresenter"` |
| | `StartAsync_Discarded_ShowsInfoAfterHome` | スナップショットの `DiscardedProduce == true` → 上の 2 つのあとに `"Info:バージョンが変わったため、進行中のプロデュースは破棄されました。"` |
| | `StartAsync_SignInFails_ConfirmsThenRetries` | 1 回目の `EnsureSignedInAsync` が `InvalidOperationException("x")`、2 回目は成功。`log` が `"PushPage:StartupPresenter(nostack)"`, `"Confirm:起動に失敗しました: x"`, `"PushPage:HomePresenter"` |
| | `RestartAsync_WhileRunning_OldRunShowsNothing` | 1 回目の `EnsureSignedInAsync` は `UniTaskCompletionSource` の未完了の Task、2 回目以降は完了済み。`var first = flow.StartAsync();` → `await flow.RestartAsync();` → 1 回目の源に `TrySetException(new InvalidOperationException("x"))` → `await first`。`log` に `"Confirm:"` で始まる行が無く、`"PushPage:HomePresenter"` が 1 回だけ |
| `HomePresenterTests.cs` | `InitializeAsync_ShowsProfileAndResume` | 偽の `IHomeView`。`PlayerCache` に Lv 12・ガチャ通貨 3250・`HasActiveProduce = true` → `log` が `"SetProfile:12,3250"`, `"SetResumeVisible:True"` |
| | `Clicks_ShowNotReady` | `InitializeAsync` のあと 4 つのイベントを順に起こす → `"Info:準備中です。"` が 4 回 |
| | `DidPopEnter_RefreshesFromCache` | キャッシュを Lv 13 に差し替えて `DidPopEnter()` → `"SetProfile:13,..."` |
| | `Dispose_UnsubscribesEvents` | `Dispose()` のあとイベントを起こしても `"Info:"` が増えない |
| | `OpenGachaAsync_WhileRunning_Ignored` | `FakeDialogService.InfoGate` に未完了の源を入れ、`OpenGachaAsync()` を 2 回呼ぶ → `"Info:"` は 1 回。源を完了させたあと、もう 1 回呼ぶと 2 回目が出る（実行中フラグが finally で戻る） |

Play モード・手動（Editor、サーバー起動済み）:
- 初回: 起動 → 「起動中…」→ ホーム。プレイヤーLv とガチャ通貨が出る。2 回目（再起動）も同じ
- ホームの「プロデュース」「ガチャ」「アイドル」「再開」（進行中のときだけ見える）を押すと「準備中です。」が出て、OK で閉じる
- 通信中（サインインの間）は入力が効かない（`ApiActivityBridge`。サーバーを遅くする手段がなければ、確かめていないと報告する）
- サーバーを止めて起動 → `StartupFlow` の起動失敗のダイアログ（「起動に失敗しました: …」、再試行／終了）が出る（端末の認証は `ApiCore` を通らないので、`ErrorDialogView` の共通ダイアログは出ない）。サーバーを起動してから「再試行」でホームに進む
- 画面を縦長にしたとき、背景が全面に出てボタンが safe area の中にある。日本語が表示される（Windows の OS フォント）
- Windows のウィンドウの初期サイズ（540×960）と比率補正（`WindowAspectKeeper`）はスタンドアロンのビルドでだけ確かめる（Editor では効かない）。確かめられなければ、そう報告する
- Android・iOS の実機での表示（OS フォント・safe area）は順 7 の別項目（実機ビルド）で確かめる。この計画では未確認

## 6. 未決事項

なし
