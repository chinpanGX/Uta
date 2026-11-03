---
title: 実装計画: client/ の画面 1/6 土台・起動・ホーム
status: draft
updated: 2026-11-03
spec: ../wiki/architecture/screen-design.md
---

# 実装計画: client/ の画面 1/6 土台・起動・ホーム

## 1. 目的と前提

[game-spec.md](../wiki/overview/game-spec.md) の順 7（client/ の画面）の 1 本目。画面を作る共通の土台（UnityScreenNavigator（以下 USN）をコードで組んだ UI で動かす部品）と、マスタの読み取り口、診断用 IMGUI の置き換え、起動の流れ（画面あり）、ホームを作る。以降の計画（ガチャ・所持一覧・編成とプロデュースの開始・週の行動選択・レッスン）は、この計画の部品を前提にする。

- 方針の正は [screen-design.md](../wiki/architecture/screen-design.md) 7章（UI はコードで組む。Presenter と View の分け方。通信・状態・マスタの扱い。起動の診断モード）。起動の順序の正は同 4章。画面の見た目の叩き台は [screen-wireframes.md](../wiki/overview/screen-wireframes.md)「ホーム」
- 関係する ADR: [0008](../wiki/decisions/0008-shared-domain-and-onion.md)（層）、[0009](../wiki/decisions/0009-client-stack.md)（uGUI + USN・VContainer・UniTask・縦画面）、[0006](../wiki/decisions/0006-adopt-unity-7-early.md)（Unity 7）
- **前提**: 積み PR。PR #24（クライアントのサインアップ・サインイン。`UtaApiClient`・`PlayerApi`・`PlayerGateway`・`EnterPlayerUseCase`・`PlayerCache`・`ErrorDialogView`（IMGUI）・`StartupPresenter`（診断））のブランチの上に作る
- **この計画で決めないこと**: ガチャ・所持一覧・編成・プロデュースの画面（2〜6 本目）。ホームのボタンが開く先は、この計画では「準備中」のダイアログにして、2〜4 本目が 1 つずつ置き換える（4章 タスク 5）
- **Unity 7 の未確認**: USN・uGUI・VContainer・UniTask は Unity 7（`7000.0.0a12`）で動作未確認（[unity7-evaluation.md](../wiki/architecture/unity7-evaluation.md) 3.3、ADR 0009）。この計画の最初のタスク（タスク 1）で確かめ、**確かめられなければ止まって報告する**（計画を直してから続ける。勝手に別の手段へ変えない）

## 2. 触るファイル

パスは `client/uta/Assets/` からの相対。`.meta` は Unity が作るので、新しい `.cs`・フォルダには Unity の再コンパイルのあとに生成された `.meta` をコミットする。

| 操作 | パス | 中身 |
|---|---|---|
| 変更 | `Scripts/Presentation/Uta.Client.Presentation.asmdef` | 参照に `UnityScreenNavigator`・`UnityEngine.UI`・`Unity.InputSystem` を足す |
| 変更 | `Scripts/Bootstrap/Uta.Client.Bootstrap.asmdef` | 参照に `UnityEngine.UI`・`Unity.InputSystem` は足さない（Bootstrap は Presentation の公開 API 越しに使う）。`VolTech.MagicOnion.Client`（`IErrorDialogView`）は既にある |
| 変更 | `Tests/EditMode/Uta.Client.Infrastructure.Tests.EditMode.asmdef` | 参照に `Uta.Client.Presentation`・`UnityEngine.UI`・`UnityScreenNavigator` を足す |
| 作成 | `Scripts/Presentation/Common/UiTheme.cs` | 色・寸法・基準解像度の定数 |
| 作成 | `Scripts/Presentation/Common/UiFonts.cs` | OS フォントの取得 |
| 作成 | `Scripts/Presentation/Common/UiFactory.cs` | uGUI の部品を作る静的関数 |
| 作成 | `Scripts/Presentation/Common/SafeAreaFitter.cs` | `Screen.safeArea` に合わせる |
| 作成 | `Scripts/Presentation/Common/WindowAspectKeeper.cs` | Windows のウィンドウを 9:16 に保つ |
| 作成 | `Scripts/Presentation/Common/BusyIndicator.cs` | 通信中のブロッカー（`Begin`/`End`。通信の開始・終了は Bootstrap の `ApiActivityBridge` が呼ぶ） |
| 作成 | `Scripts/Presentation/Common/DialogService.cs` | 確認・お知らせ・選択のダイアログ |
| 作成 | `Scripts/Presentation/Common/UiRoot.cs` | `Canvas`・`EventSystem`・USN のコンテナ・ブロッカー・ダイアログ層 |
| 作成 | `Scripts/Presentation/Common/IPresenter.cs` | Presenter の共通 IF |
| 作成 | `Scripts/Presentation/Common/Presenter.cs` | `Presenter<TView>` |
| 作成 | `Scripts/Presentation/Common/PageBase.cs` | ページの基底 |
| 作成 | `Scripts/Presentation/Common/ModalBase.cs` | モーダルの基底 |
| 作成 | `Scripts/Presentation/Common/ScreenNavigator.cs` | 画面の出し入れ |
| 作成 | `Scripts/Application/Master/MasterItemKind.cs` ほか 7 ファイル（3章） | マスタの読み取り用の型と `IGameMaster` |
| 作成 | `Scripts/Application/Player/RefreshPlayerUseCase.cs` | サインインで所持データを取り直す |
| 作成 | `Scripts/Infrastructure/MasterData/MasterGameData.cs` | `MemoryDatabase` → `IGameMaster` |
| 作成 | `Scripts/Presentation/Startup/StartupDiagnostics.cs` | いまの `StartupPresenter` の診断を移す（画面なし） |
| 削除 | `Scripts/Presentation/Startup/StartupView.cs` | IMGUI の診断表示を廃止する |
| 作成 | `Scripts/Presentation/Startup/StartupFlow.cs` | 起動の流れ（認証 → サインイン → ホーム） |
| 作成 | `Scripts/Presentation/Startup/StartupPage.cs` | 起動中の表示（`IStartupView`） |
| 変更 | `Scripts/Presentation/Startup/StartupPresenter.cs` | 中身を全部置き換える: 診断の旧 `StartupPresenter` は `StartupDiagnostics.cs` に移し、このファイルは `StartupPage` の Presenter（3.6）にする |
| 削除 | `Scripts/Presentation/Startup/StartupOptions.cs` | `UtaBootstrap` が診断モードを選ぶので不要 |
| 作成 | `Scripts/Presentation/Home/HomePage.cs`・`IHomeView.cs`・`HomePresenter.cs`・`HomeDestinations.cs` | ホーム |
| 変更 | `Scripts/Infrastructure/Api/UtaApiClient.cs` | `Create` に `IApiActivityHandler`・`IErrorNavigator` を足す（3.7） |
| 作成 | `Scripts/Bootstrap/ApiActivityBridge.cs`・`UtaErrorNavigator.cs` | 通信中のブロッカーと、エラーの「タイトルへ」「ホームへ」（3.7） |
| 変更 | `Scripts/Bootstrap/ErrorDialogView.cs` | IMGUI を `DialogService` 呼び出しに置き換える |
| 変更 | `Scripts/Bootstrap/UtaBootstrap.cs` | 通常の起動と診断モードの分岐、登録 |
| 変更 | `ProjectSettings/ProjectSettings.asset` | Windows のウィンドウと向き（4章 タスク 2） |
| 作成 | `Tests/EditMode/*.cs`（5章） | EditMode のテスト |
| 変更 | `CLAUDE.md`（リポジトリのルート） | `unity test` が使えたら、ビルド・テスト・実行にコマンドを 1 行足す（タスク 6） |
| 変更 | `uta_docs/wiki/architecture/unity7-evaluation.md`・`screen-design.md` | 確かめた結果（タスク 6） |

## 3. 型とシグネチャ

C# は `client/` の `.editorconfig` の範囲（C# 9 の範囲。ファイルスコープ namespace・プライマリコンストラクタ・コレクション式・record は使わない）。BOM 付き UTF-8、LF、Allman。

### 3.1 asmdef の参照

`Uta.Client.Presentation.asmdef` の `references` を次にする（`overrideReferences: true` のまま）:

```
"Uta.Client.Application", "Uta.Shared.Domain", "UniTask", "VContainer",
"UnityScreenNavigator", "UnityEngine.UI", "Unity.InputSystem"
```

### 3.2 `Uta.Client.Presentation.Common`

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
}

public static class UiFonts
{
    /// <summary>OS の日本語フォント。最初の呼び出しで作って使い回す。</summary>
    public static Font Default { get; }   // Font.CreateDynamicFontFromOSFont(new[] { "Yu Gothic UI", "Meiryo UI", "Noto Sans CJK JP", "Noto Sans JP", "Hiragino Sans", "Droid Sans Fallback" }, 32)
}

public static class UiFactory
{
    /// <summary>RectTransform だけを持つ GameObject を parent の子に作り、親いっぱいに広げる（anchorMin 0,0 / anchorMax 1,1 / offset 0 / scale 1）。</summary>
    public static RectTransform CreateRect(string name, Transform parent);

    /// <summary>CreateRect + Image（color。raycastTarget は true）。</summary>
    public static Image CreateImage(Transform parent, string name, Color color);

    /// <summary>Text。font = UiFonts.Default、horizontalOverflow = Wrap、verticalOverflow = Overflow、raycastTarget = false、color 省略時は UiTheme.Text。親いっぱいには広げない（レイアウトグループの子として使う）。</summary>
    public static Text CreateText(Transform parent, string name, string text, int fontSize, TextAnchor anchor = TextAnchor.MiddleLeft, Color? color = null);

    /// <summary>Image（UiTheme.Button）+ Button + 子の Text（label。UiTheme.FontBody。中央）+ LayoutElement（minHeight = preferredHeight = height）。</summary>
    public static Button CreateButton(Transform parent, string name, string label, float height = UiTheme.ButtonHeight);

    /// <summary>Button.interactable を変え、Image の色を UiTheme.Button / UiTheme.ButtonDisabled にする。</summary>
    public static void SetInteractable(Button button, bool interactable);

    /// <summary>VerticalLayoutGroup（spacing、padding を上下左右に同じ値、childControlWidth/Height = true、childForceExpandWidth = true、childForceExpandHeight = false）。</summary>
    public static RectTransform CreateVertical(Transform parent, string name, float spacing = UiTheme.Spacing, float padding = UiTheme.Padding);

    /// <summary>HorizontalLayoutGroup（同じ設定。childForceExpandWidth = true、childForceExpandHeight = true）。</summary>
    public static RectTransform CreateHorizontal(Transform parent, string name, float spacing = UiTheme.Spacing, float padding = 0f);

    /// <summary>縦スクロール。ScrollRect（horizontal = false、movementType = Clamped）+ Viewport（Image + RectMask2D）+ Content（VerticalLayoutGroup + ContentSizeFitter(vertical = PreferredSize)）。戻り値は Content。ScrollRect の GameObject は parent の子として親いっぱいに広げる。</summary>
    public static RectTransform CreateScrollContent(Transform parent, string name);

    /// <summary>Content の子を全部 Destroy する。</summary>
    public static void ClearChildren(Transform parent);
}

[RequireComponent(typeof(RectTransform))]
public sealed class SafeAreaFitter : MonoBehaviour
{
    // LateUpdate で Screen.safeArea と Screen.width/height を前回と比べ、変わったとき（と最初の 1 回）に
    // anchorMin = safeArea.position / (Screen.width, Screen.height)、anchorMax = (safeArea.position + safeArea.size) / (Screen.width, Screen.height)、offsetMin/Max = 0 にする。
}

public sealed class WindowAspectKeeper : MonoBehaviour
{
    // #if UNITY_STANDALONE_WIN && !UNITY_EDITOR のときだけ中身を持つ（ほかのプラットフォーム・Editor では何もしない空のクラス）。
    // Update で Screen.fullScreen が false のとき、Screen.width と Screen.height の比が 9:16 から 1px 以上ずれていたら
    // Screen.SetResolution(Screen.width, Mathf.RoundToInt(Screen.width * 16f / 9f), FullScreenMode.Windowed) を 1 回呼ぶ。
    // 呼んだ直後の 1 フレームは判定を休む（SetResolution の反映待ちで連続して呼ばないため）。
}

public sealed class BusyIndicator : MonoBehaviour
{
    /// <summary>入力を止める。呼び出しの数を数え、0 → 1 で全面の透明な Image(raycastTarget) を有効にする。</summary>
    public void Begin();
    /// <summary>数を 1 減らし、1 → 0 で Image を無効にする。数が 0 のときの End は何もしない。</summary>
    public void End();
    public bool IsBusy { get; }
}

public enum DialogResult { Positive, Negative }

public class DialogService   // テストで派生させるので sealed にせず、公開メソッドは virtual。コンストラクターは引数を保存するだけ（null を許す）
{
    public DialogService(RectTransform layer);

    /// <summary>お知らせ。ボタンは「OK」1 つ。</summary>
    public virtual UniTask ShowInfoAsync(string message);

    /// <summary>確認。Positive が positiveLabel（既定「はい」）、Negative が negativeLabel（既定「いいえ」）。</summary>
    public virtual UniTask<DialogResult> ConfirmAsync(string message, string positiveLabel = "はい", string negativeLabel = "いいえ");

    /// <summary>ラベルの配列から 1 つ選ばせる。戻り値は選んだ添字（0 始まり）。ラベルは 1〜4 個。</summary>
    public virtual UniTask<int> ChooseAsync(string message, string[] labels);
}
// DialogService は UiRoot.DialogLayer に、全面の半透明（UiTheme.Overlay）の Image + 中央の Panel（幅 880、高さは内容）+ message の Text + ボタンを作り、
// 押されたら破棄して結果を返す。同時に 2 つ出す要求は、先のものが閉じてから後のものを出す（内部にキューを持つ）。

public sealed class UiRoot : MonoBehaviour
{
    public PageContainer Pages { get; }
    public ModalContainer Modals { get; }
    public PreloadedAssetLoader Loader { get; }
    public BusyIndicator Busy { get; }
    public DialogService Dialogs { get; }
    public RectTransform TemplateRoot { get; }   // 非アクティブな GameObject。雛形の置き場

    /// <summary>
    /// GameObject "UtaUi"（DontDestroyOnLoad）を作る。構成: Canvas(ScreenSpaceOverlay) + CanvasScaler(ScaleWithScreenSize、referenceResolution = UiTheme.ReferenceResolution、matchWidthOrHeight = 0) + GraphicRaycaster。
    /// EventSystem.current が null のときは "EventSystem" を作り EventSystem + InputSystemUIInputModule を付ける。
    /// 子（この順。後ろが手前）: "Pages"（CreateRect + PageContainer。AssetLoader = Loader）、"Modals"（CreateRect + ModalContainer。AssetLoader = Loader）、
    /// "Dialogs"（CreateRect。DialogService の層）、"Busy"（CreateRect + Image(透明、raycastTarget = true、最初は無効) + BusyIndicator）、"Templates"（非アクティブ）。
    /// WindowAspectKeeper を UtaUi に付ける。
    /// </summary>
    public static UiRoot Create();
}

public interface IPresenter : IDisposable
{
    /// <summary>View を渡して OnBind を呼ぶ。view が Presenter の TView でなければ InvalidCastException。</summary>
    void BindView(object view);
}

public abstract class Presenter<TView> : IPresenter where TView : class
{
    protected TView View { get; private set; }
    public void Bind(TView view);          // View が設定済みなら InvalidOperationException。view が null なら ArgumentNullException。View を設定して OnBind() を呼ぶ
    void IPresenter.BindView(object view); // Bind((TView)view)
    protected virtual void OnBind();
    public virtual void Dispose();
}

public abstract class PageBase : Page
{
    protected RectTransform Body { get; private set; }
    public event Action Closed;            // Cleanup で発火
    public event Action Reappeared;        // DidPopEnter（上のページが閉じられてこのページに戻ったとき）で発火
    public override void DidPopEnter();    // Reappeared?.Invoke()
    public override UniTask Initialize();  // 下記
    public override UniTask Cleanup();     // Closed?.Invoke() して UniTask.CompletedTask
    protected abstract void BuildUi(RectTransform body);
}
// PageBase.Initialize: 自分の RectTransform を親いっぱいに広げ、Image(UiTheme.Background) を付け（背景。下のページを隠す）、
// 子 "Body"（CreateRect + SafeAreaFitter）を Body に設定して BuildUi(Body) を呼ぶ。

public abstract class ModalBase : Modal
{
    protected RectTransform Panel { get; private set; }
    public event Action Closed;
    public override UniTask Initialize();  // 自分の RectTransform を親いっぱいに広げ、中央の Panel（幅 900、高さは内容の VerticalLayoutGroup + ContentSizeFitter、背景 UiTheme.Panel）を作って BuildUi(Panel)
    public override UniTask Cleanup();
    protected abstract void BuildUi(RectTransform panel);
}

public class ScreenNavigator   // テストで派生させるので sealed にせず、公開メソッドは virtual。コンストラクターは引数を保存するだけ（null を許す）
{
    public ScreenNavigator(UiRoot root, IObjectResolver resolver);

    /// <summary>ページを積んで（stack = true）、Presenter を解決・Bind して返す。configure は Bind の前に呼ぶ（Presenter の入力を渡す）。</summary>
    public virtual UniTask<TPresenter> PushPageAsync<TPage, TPresenter>(Action<TPresenter> configure = null)
        where TPage : PageBase where TPresenter : IPresenter;

    /// <summary>今のページを置き換える（Push の stack = false。置き換えた側のページと Presenter は破棄される）。</summary>
    public virtual UniTask<TPresenter> ReplacePageAsync<TPage, TPresenter>(Action<TPresenter> configure = null)
        where TPage : PageBase where TPresenter : IPresenter;

    public virtual UniTask PopPageAsync();            // 1 つ戻る
    public virtual UniTask PopToRootPageAsync();      // 底の 1 つ（ホーム）だけ残す
    public virtual UniTask<TPresenter> PushModalAsync<TModal, TPresenter>(Action<TPresenter> configure = null)
        where TModal : ModalBase where TPresenter : IPresenter;
    public virtual UniTask PopModalAsync();
}
```

`ScreenNavigator` の動き（共通）:
- 雛形のキーは `typeof(TPage).Name`。`UiRoot.Loader.PreloadedObjects` に無ければ、`new GameObject(key, typeof(RectTransform), typeof(TPage))` を作って `SetActive(false)` し、`UiRoot.TemplateRoot` の子にして登録する（`Instantiate` されるのは雛形の複製）
- 出す: `Pages.Push(typeof(TPage), key, playAnimation: false, stack: ...)`（モーダルは `Modals.Push(typeof(TModal), key, playAnimation: false)`）の `AsyncProcessHandle` を `await handle.Task.AsUniTask()` で待つ（`handle.HasError` なら `handle.Exception` を投げる）。アニメーションは付けない（`playAnimation: false`）
- 出し終わったら（`Initialize` で UI が組み上がっている）、`TPresenter` を `IObjectResolver.Resolve` で取り、`onLoad` で受けた page を `BindView(page)` で渡す。`configure` は `BindView` の前
- ページ（モーダル）の `Closed` で Presenter を `Dispose()` する
- 遷移中（`Pages.IsInTransition` / `Modals.IsInTransition`）に別の遷移を始めない: `ScreenNavigator` は実行中フラグを 1 つ持ち、遷移の最中に別の遷移を呼ばれたら `InvalidOperationException("navigation in progress")` を投げる（待たせない）。ボタンの連打は `BusyIndicator` が止めるので、通常は起きない

### 3.3 `Uta.Client.Application.Master`

```csharp
public enum MasterItemKind { Nothing = 0, Idol = 1, SupportCard = 2 }   // Contracts.Gacha.ItemKind・MasterData の ItemKind と同じ数値

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
    public IReadOnlyList<GachaEntryInfo> NormalEntries { get; } public IReadOnlyList<GachaEntryInfo> GuaranteedEntries { get; }
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
public interface IGameMaster
{
    IReadOnlyList<IdolInfo> Idols { get; }                 // IdolId の昇順
    IdolInfo GetIdol(int idolId);                          // 無ければ KeyNotFoundException
    IReadOnlyList<SupportCardInfo> SupportCards { get; }   // SupportCardId の昇順
    SupportCardInfo GetSupportCard(int supportCardId);     // 無ければ KeyNotFoundException
    IReadOnlyList<GachaInfo> Gachas { get; }               // GachaId の昇順
    GachaInfo GetGacha(int gachaId);                       // 無ければ KeyNotFoundException
    IReadOnlyList<ScenarioInfo> Scenarios { get; }         // ScenarioId の昇順
    ScenarioInfo GetScenario(int scenarioId);              // 無ければ KeyNotFoundException
    int GetSupportMaxLevel(Rarity rarity, int limitBreak);   // SupportLevelCap。無ければ KeyNotFoundException
    int GetSupportMaxLimitBreak(Rarity rarity);              // SupportLevelCap のその Rarity の LimitBreak の最大値。行が無ければ KeyNotFoundException
    SupportLevelUpCost GetSupportLevelUpCost(Rarity rarity, int fromLevel, int toLevel);   // fromLevel <= level < toLevel の SupportLevelCost（Rarity, level）の合計。サーバーの MasterSupportCatalog.GetLevelUpCost と同じ。行が無ければ InvalidOperationException
}
```

`Rarity`・`Plan`・`ParamKind` は `Uta.Shared.Domain.Lesson`、`SupportType`・`Difficulty`・`WeekKind` は `Uta.Shared.Domain.Produce` の enum（マスタの enum と数値が同じ。`MasterCardCatalog` のとおり `(Domain の型)(int)マスタの値` で変換する）。

### 3.4 `Uta.Client.Application.Player.RefreshPlayerUseCase`

```csharp
public sealed class RefreshPlayerUseCase
{
    public RefreshPlayerUseCase(IPlayerPort port, PlayerCache cache);
    /// <summary>port.SignInAsync を呼び、成功なら cache.Set して PlayerSnapshot を返す。Failed・PlayerNotCreated は InvalidOperationException("sign-in failed: " + ErrorText)（PlayerNotCreated は ErrorText が null なので "sign-in failed: player not created"）。</summary>
    public async UniTask<PlayerSnapshot> ExecuteAsync();
}
```

### 3.5 `Uta.Client.Infrastructure.MasterData.MasterGameData`

```csharp
public sealed class MasterGameData : IGameMaster
{
    public MasterGameData(MemoryDatabase db);   // db が null なら ArgumentNullException。読み取り用の型は構築時に全部作る
}
```

読み方はサーバーの `MasterGachaCatalog`・`MasterSupportCatalog`（`server/Uta.Server/Infrastructure/MasterData/`）と同じ（`db.IdolTable.All`・`db.SupportCardTable.All`・`db.GachaTable.All`・`db.GachaTableEntryTable.All`（`GachaTableId` で絞り、`Order` の昇順）・`db.ScenarioTable.All`・`db.ScenarioWeekTable.All`・`db.SupportLevelCostTable.All`・`db.SupportLevelCapTable.All`）。ガチャの通常枠は `Gacha.NormalTableId`、確定枠は `Gacha.GuaranteedTableId` のエントリー。

### 3.6 起動と画面（`Uta.Client.Presentation.Startup`／`Home`）

```csharp
public interface IStartupView { void SetMessage(string message); }
public sealed class StartupPage : PageBase, IStartupView { ... }   // 中央に Text 1 つ（UiTheme.FontBody）。BuildUi で作る。SetMessage で文字を変える

public sealed class StartupPresenter : Presenter<IStartupView>
{
    protected override void OnBind();            // View.SetMessage("起動中…")
    public void SetMessage(string message);      // View.SetMessage(message)
}

public sealed class StartupFlow : IStartable
{
    public StartupFlow(SignInUseCase signIn, EnterPlayerUseCase enterPlayer, ScreenNavigator navigator, DialogService dialogs);
    public void Start();            // RunAsync(false).Forget()
    public UniTask RestartAsync();  // RunAsync(true)。エラーの「タイトルへ」から呼ばれる
}
```

`RunAsync(bool restart)` の手順:
1. `restart` が false なら `var startup = await navigator.PushPageAsync<StartupPage, StartupPresenter>();`（底のページ）。true（`RestartAsync`）なら `await navigator.PopToRootPageAsync();` のあと `var startup = await navigator.ReplacePageAsync<StartupPage, StartupPresenter>();`
2. 次を `while (true)` で包み、例外が出たら `dialogs.ConfirmAsync("起動に失敗しました: <e.Message>", "再試行", "終了")` を出す。再試行なら `while` の先頭へ、終了なら `UnityEngine.Application.Quit()` して `return`
   1. `startup.SetMessage("起動中…")`
   2. `var registered = await signIn.ExecuteAsync();`（認証。端末の登録をしたら true）
   3. `var entered = await enterPlayer.ExecuteAsync(registered);`
   4. 抜ける
3. `entered.Snapshot.DiscardedProduce` が true なら `await dialogs.ShowInfoAsync("バージョンが変わったため、進行中のプロデュースは破棄されました。")`
4. `await navigator.ReplacePageAsync<HomePage, HomePresenter>();`

```csharp
public sealed class StartupDiagnostics : IStartable     // 旧 StartupPresenter の診断をそのまま移す（IMGUI の view を使わず Debug.Log のみ）
{
    public StartupDiagnostics(SignInUseCase signIn, EnterPlayerUseCase enterPlayer);
    public void Start();   // 旧 RunAsync と同じ手順。最後に UnityEngine.Application.Quit(succeeded ? 0 : 1)
}

public interface IHomeView
{
    void SetProfile(int level, int gachaCurrency);
    void SetResumeVisible(bool visible);
    event Action ProduceClicked;
    event Action ResumeClicked;
    event Action GachaClicked;
    event Action OwnedClicked;
    event Action Reappeared;      // 他のページから戻ってきた
}
public sealed class HomePage : PageBase, IHomeView { ... }

public class HomeDestinations    // ホームから先への遷移。2〜4 本目が 1 メソッドずつ置き換える。テストで派生させるので sealed にせず、メソッドは virtual
{
    public HomeDestinations(DialogService dialogs);
    public virtual UniTask OpenGachaAsync();      // この計画: dialogs.ShowInfoAsync("準備中です。")
    public virtual UniTask OpenOwnedAsync();      // 同上
    public virtual UniTask OpenProduceAsync();    // 同上（編成へ。進行中のときは再開か放棄の確認は 4 本目）
    public virtual UniTask ResumeProduceAsync();  // 同上
}

public sealed class HomePresenter : Presenter<IHomeView>
{
    public HomePresenter(PlayerCache cache, HomeDestinations destinations);
    // OnBind: cache.Current.Level と GachaCurrency を SetProfile、cache.Current.HasActiveProduce を SetResumeVisible。
    // 4 つのイベントを購読し、それぞれ destinations の対応するメソッドを呼ぶ（Forget）。Reappeared で SetProfile / SetResumeVisible をやり直す（cache.Current から）。Dispose で購読を解く。
}
```

`HomePage` のレイアウト（`BuildUi(Body)`）: `CreateVertical` の下に、上から ①プレイヤー情報の Text（"プレイヤーLv {level}　ガチャ通貨 {gachaCurrency}"。`SetProfile` で更新）、②「プロデュース中です」の Panel（Image(UiTheme.Panel) の中に Text と「再開」ボタン。`SetResumeVisible(false)` のとき非表示）、③ボタン 3 つ「プロデュース」「ガチャ」「アイドル・サポート」（`CreateButton`）。ボタンは各イベントを `onClick` で発火する。

### 3.7 `Bootstrap`

- `ErrorDialogView : IErrorDialogView`（`Uta.Client.Bootstrap`）: `Initialize(bool autoDismiss, DialogService dialogs)`。`ShowAsync` は、`autoDismiss` なら今までと同じ戻り値（Ok → Ok、RetryOrTitle → Title、Home → Home、Title → Title、それ以外 → Store）を即返す。そうでなければ `ErrorDialogButtons` ごとに `dialogs.ChooseAsync(error.ToString(), labels)` を出し、選ばれた添字を `ErrorDialogButton` に直す: `Ok` → ["OK"] → `Ok`、`RetryOrTitle` → ["再試行", "タイトルへ"] → `Retry`/`Title`、`Home` → ["ホームへ"] → `Home`、`Title` → ["タイトルへ"] → `Title`、それ以外 → ["ストアへ"] → `Store`。旧 `OnGUI`・`OnDestroy` の IMGUI は削除する（`MonoBehaviour` をやめ、普通のクラスにする）
- `UtaApiClient.Create(string serverUrl, string platform, int appVersion, string accountFilePath, string keySource, ICommonErrorHandler errorHandler, IApiActivityHandler activityHandler, IErrorNavigator errorNavigator)`: `ApiCore.CreateBuilder(...)` のあと `.AddActivityHandler(activityHandler).WithErrorNavigator(errorNavigator).Build()` にする（引数が `null` のときはそのハンドラーを付けない）。`IApiActivityHandler`・`IErrorNavigator` は `VolTech.Client` の IF
- `ApiActivityBridge : IApiActivityHandler`（`Uta.Client.Bootstrap`）: コンストラクターで `BusyIndicator` を受ける。`OnActivityStarted()` → `Begin()`、`OnActivityEnded()` → `End()`。**通信中のブロッカーはこれだけで効く**（各画面で包まない）
- `UtaErrorNavigator : IErrorNavigator`（`Uta.Client.Bootstrap`）: コンストラクターで `IObjectResolver` を受ける（循環を避けるため、`StartupFlow`・`ScreenNavigator`・`RefreshPlayerUseCase` は呼ぶときに `Resolve` する）。`GoToTitleAsync()` → `Resolve<StartupFlow>().RestartAsync()`（`DATE_CHANGED`・メンテナンスなどで起動の流れからやり直す。[screen-design.md](../wiki/architecture/screen-design.md) 5章）。`GoToHomeAsync()` → `Resolve<RefreshPlayerUseCase>().ExecuteAsync()` のあと `Resolve<ScreenNavigator>()` で `PopToRootPageAsync()` → `ReplacePageAsync<HomePage, HomePresenter>()`。`OpenStoreAsync()` → 何もしない（Uta はストアへ誘導しない。[repository-layout.md](../wiki/architecture/repository-layout.md) 10.5）
- `UtaBootstrap.RunAsync`: 下の 4章 タスク 4 に書く

## 4. タスク

各タスクの終わりに、`unity recompile --format json`（`client/uta` で実行。コンパイルエラー 0）が通り、EditMode のテストが通る状態にする（テストは 5 章。Editor を開けない環境なら、通ったとは書かず、確かめられていないと報告する）。

### タスク 1: USN・uGUI・Input System の組み込みと確認（スパイク。止まる条件あり）

触る: `Uta.Client.Presentation.asmdef`、`Tests/EditMode/*.asmdef`、`Presentation/Common/UiTheme.cs`・`UiFonts.cs`・`UiFactory.cs`・`SafeAreaFitter.cs`・`IPresenter.cs`・`Presenter.cs`・`PageBase.cs`・`ModalBase.cs`・`UiRoot.cs`（3.2 のとおり）。

手順:
1. 3.1 のとおり asmdef を直し、上のファイルを 3.2 のとおり作る（`BusyIndicator`・`DialogService`・`WindowAspectKeeper`・`ScreenNavigator` はタスク 2・3 で作る。`UiRoot` がそれらを参照するので、タスク 1 の時点では `UiRoot` から `Busy`・`Dialogs` を外し、タスク 2 で足す）
2. `unity recompile --format json`。**次のどれかが起きたら止まって報告する**（直し方を自分で決めない）:
   - `Page.Initialize()` / `Modal.Initialize()` / `Cleanup()` を `public override UniTask` で上書きできない（戻り値が `IEnumerator` の版になっている）
   - `UnityScreenNavigator`・`UnityEngine.UI`・`Unity.InputSystem` の asmdef 名で参照できない
   - USN の公開 API が 3.2 の使い方（`PageContainer.Push(Type, string, bool, bool, ...)`・`AssetLoader` の代入・`AsyncProcessHandle.Task`）と違う
3. 確認用の一時の Play モード検証（コミットしない）: 空のシーンで `UiRoot.Create()` を呼び、`PageBase` を継承した空のページを `Loader` に雛形として登録して `Pages.Push` し、`Initialize` が呼ばれて `Body` が組まれること、`Modals.Push` でモーダルが出ること（`DefaultModalBackdrop` が見つかること）、`Pages.Push(..., stack: false)` で前のページが破棄されることを、`unity command eval`・`unity command editor.playmode start` とコンソール（`unity command console get`）で確かめる。確かめられなかった項目は、タスク 6 で「未確認」として `unity7-evaluation.md` に書く

終わったことの確かめ方: コンパイルエラー 0。止まる条件に当たらない。

### タスク 2: 共通部品の残り（ブロッカー・ダイアログ・ナビゲーション・ウィンドウ）

触る: `BusyIndicator.cs`・`DialogService.cs`・`WindowAspectKeeper.cs`・`ScreenNavigator.cs`・`UiRoot.cs`（`Busy`・`Dialogs` を足す）、`ProjectSettings/ProjectSettings.asset`。

手順:
1. 3.2 のとおり作る。`DialogService` は `UiRoot.Create` が `new DialogService(dialogLayer)` で作る
2. `ProjectSettings/ProjectSettings.asset` を直す（テキストで該当行だけ。Unity を閉じて編集するか、Editor が開いているなら再読み込みを確認する）: `defaultScreenWidth: 540`、`defaultScreenHeight: 960`、`fullscreenMode: 3`、`resizableWindow: 1`、`defaultScreenOrientation: 0`、`allowedAutorotateToPortrait: 1`、`allowedAutorotateToPortraitUpsideDown: 0`、`allowedAutorotateToLandscapeRight: 0`、`allowedAutorotateToLandscapeLeft: 0`
3. `BusyIndicator` は `Begin`/`End` だけを持つ（`RunAsync` は作らない）
4. `UiRoot.Create` の最後で `WindowAspectKeeper` を付ける

境界: `DialogService` は、`ChooseAsync` に 0 個・5 個以上のラベルを渡すと `ArgumentException`。

終わったことの確かめ方: コンパイルエラー 0。5 章のテストのうち `UiFactoryTests`・`BusyIndicatorTests`・`PresenterTests` が通る。

### タスク 3: マスタの読み取り口

触る: `Application/Master/*.cs`、`Application/Player/RefreshPlayerUseCase.cs`、`Infrastructure/MasterData/MasterGameData.cs`。

手順: 3.3・3.4・3.5 のとおり作る。`MasterGameData` はコンストラクターで読み取り用の型を全部作る（`GachaInfo` のエントリーは `Order` の昇順）。`ItemKind`（マスタ）→ `MasterItemKind` は `(MasterItemKind)(int)` で変換する。

境界: `GetSupportLevelUpCost` は `fromLevel >= toLevel` なら `SupportLevelUpCost(0, 0)`。

終わったことの確かめ方: `MasterGameDataTests`・`RefreshPlayerUseCaseTests` が通る。

### タスク 4: 起動の流れとホーム、診断モード、Bootstrap

触る: `Presentation/Startup/*`、`Presentation/Home/*`、`Bootstrap/ErrorDialogView.cs`、`Bootstrap/UtaBootstrap.cs`。

手順:
1. 旧 `StartupPresenter.cs`・`StartupView.cs` を削除し、`StartupDiagnostics.cs` を作る（旧 `RunAsync` の手順を移す。`AddLine` は `Debug.Log("[Uta] " + line)` だけ。`view.SetText` は呼ばない。`StartupOptions` は 2 章の表のとおり削除する）
2. `StartupPage`・`IStartupView`・`StartupPresenter`・`StartupFlow`、`HomePage`・`IHomeView`・`HomePresenter`・`HomeDestinations` を 3.6 のとおり作る
3. `ErrorDialogView` を 3.7 のとおり直す
4. `UtaBootstrap.RunAsync` を次の手順にする:
   1. `serverUrl`・`quitAfterCheck` を今までと同じに読む
   2. マスタを読む: `var db = await MasterDataLoader.LoadFromStreamingAssetsAsync();` → `cardCatalog = MasterCardCatalog.Create(db)`、`gameMaster = new MasterGameData(db)`。失敗したら今までと同じ（`Debug.LogError`、`quitAfterCheck` なら `Application.Quit(1)`）。ただし通常起動では `UiRoot` がまだ無いので、`UiRoot.Create()` を先に作り、失敗は `uiRoot.Dialogs.ShowInfoAsync("マスタを読めませんでした: " + e.Message)` で出して止まる（認証には進まない）
   3. `var uiRoot = UiRoot.Create();`（通常起動・診断モードの両方で作る。診断モードでも `ErrorDialogView` が `DialogService` を要る）
   4. `ErrorDialogView errorDialog = new ErrorDialogView(); errorDialog.Initialize(quitAfterCheck, uiRoot.Dialogs);`
   5. VContainer のスコープ（`UtaApiClient` の登録の lambda は `resolver => UtaApiClient.Create(..., new DefaultCommonErrorHandler(errorDialog), new ApiActivityBridge(uiRoot.Busy), new UtaErrorNavigator(resolver))`）: 今までの登録（`ICardCatalog`、`UtaApiClient`、`DeviceAuthGateway`→`IDeviceAuthPort`、`PlayerGateway`→`IPlayerPort`、`PlayerCache`、`SignInUseCase`、`EnterPlayerUseCase`）に加えて、`RegisterInstance(uiRoot)`、`RegisterInstance(uiRoot.Dialogs)`、`RegisterInstance<IGameMaster>(gameMaster)`、`Register<ScreenNavigator>(Singleton)`、`Register<RefreshPlayerUseCase>(Singleton)`、`Register<HomeDestinations>(Singleton)`、`Register<StartupPresenter>(Transient)`、`Register<HomePresenter>(Transient)`、`quitAfterCheck` なら `RegisterEntryPoint<StartupDiagnostics>()`、そうでなければ `RegisterEntryPoint<StartupFlow>()`。`StartupOptions`・`StartupView` の登録は削除
5. `HomeDestinations` は `ScreenNavigator` を渡さない（この計画の実装は「準備中」のダイアログだけ）。2〜4 本目が `ScreenNavigator` の依存を足す

終わったことの確かめ方: コンパイルエラー 0。`HomePresenterTests` が通る。Play モード（5 章の手動）で起動 → ホームが出る。

### タスク 5: 診断モードの確認

手順: `-uta-quit-after-check` の経路（旧 `StartupPresenter` の診断）が動くことを、サーバー（`docker compose ... up` → `dotnet run --project server/Uta.Server -- migrate` → `dotnet run --project server/Uta.Server`）を起動した状態で、Windows の IL2CPP のビルド（CLAUDE.md の「ビルド・テスト・実行」）→ `Uta.exe -batchmode -nographics -uta-quit-after-check`（終了コード 0）で確かめる。Editor を閉じる・サーバーを起動する環境がなければ、確かめていないと報告する。

### タスク 6: 結果の記録

1. `unity test --mode EditMode --output <パス>` が Editor で動いたら、CLAUDE.md の「ビルド・テスト・実行」にそのコマンドを 1 行足す（動かなければ足さず、報告する）
2. `unity7-evaluation.md` の 2 章の表に、確かめたもの（USN・uGUI・Input System の組み込み、コードで組んだ雛形、`stack: false`、モーダルの背景）を 1 行ずつ足す。確かめられなかったものは 3.3 に足す
3. `screen-design.md` の「実装メモ」に、設計とのズレを書く（なければ「なし」）。この計画の `status` を、全部のタスクが終わったとき `done` にする

## 5. テスト

置き場: `Tests/EditMode/`。既存のテスト用 asmdef を使う。実行: Unity の Editor で Test Runner（EditMode）、または `unity test --mode EditMode`（タスク 6 の確認前は Editor の Test Runner を使う）。

| ファイル | テスト | 内容 |
|---|---|---|
| `UiFactoryTests.cs` | `CreateButton_Default_HasButtonTextAndLayoutHeight` | `Button`・子の `Text`（label）・`LayoutElement.preferredHeight == UiTheme.ButtonHeight` |
| | `SetInteractable_False_ChangesColor` | `interactable == false`、`Image.color == UiTheme.ButtonDisabled` |
| | `CreateScrollContent_ReturnsContentUnderViewport` | 戻り値の親が Viewport、`ContentSizeFitter` と `VerticalLayoutGroup` がある |
| | `ClearChildren_RemovesAllChildren` | 子が 0 になる（`Object.DestroyImmediate` でなく、`ClearChildren` は Play モードでない EditMode では `DestroyImmediate`、Play モードでは `Destroy` を使う: `Application.isPlaying` で分ける） |
| `BusyIndicatorTests.cs` | `Begin_Once_IsBusy` | `Begin()` のあと `IsBusy == true`、`End()` で false |
| | `Begin_Nested_StaysBusyUntilAllEnded` | `Begin()` 2 回、`End()` 1 回ではまだ true、もう 1 回で false |
| | `End_WithoutBegin_DoesNothing` | `IsBusy == false` のまま、例外なし |
| `PresenterTests.cs` | `Bind_Twice_Throws` | 2 回目は `InvalidOperationException` |
| | `Bind_Null_Throws` | `ArgumentNullException` |
| | `BindView_WrongType_Throws` | `InvalidCastException` |
| `MasterGameDataTests.cs` | `Constructor_FromStreamingAssets_ReadsAllTables` | `MasterDataLoader.Load(File.ReadAllBytes(<StreamingAssets>/MasterData/masterdata.bytes))` で作る。`Idols.Count == 4`、`SupportCards.Count == 10`、`Gachas.Count == 1`、`Scenarios.Count == 2`（`MasterDataLoaderTests` と同じ読み方） |
| | `GetSupportLevelUpCost_SumsRows` | R の Lv1→Lv3 が `SupportLevelCostTable` の Lv1 と Lv2 の行の合計 |
| | `GetSupportMaxLevel_MatchesCap` | `SupportLevelCapTable` の行と一致 |
| | `GetGacha_NormalEntriesOrdered` | `NormalEntries` の `Weight` の合計が 10000（masterdata の計画 2/2 の決定） |
| | `GetIdol_Unknown_ThrowsKeyNotFound` | |
| `RefreshPlayerUseCaseTests.cs` | `Execute_Success_UpdatesCache` | 偽の `IPlayerPort`（`EnterPlayerUseCaseTests` の `FakePlayerPort` と同じ作り）が返した `PlayerSnapshot` が `PlayerCache.Current` になる |
| | `Execute_Failed_Throws` | `InvalidOperationException`、キャッシュは変わらない |
| `HomePresenterTests.cs` | `Bind_ShowsProfileAndResume` | 偽の `IHomeView`。`SetProfile(level, gachaCurrency)`、`HasActiveProduce` に応じた `SetResumeVisible` |
| | `Clicks_CallDestinations` | 4 つのイベントで `HomeDestinations` の対応するメソッドが呼ばれる（`HomeDestinations` のメソッドは `virtual` なので、テスト用の派生（コンストラクターには `null` の `DialogService` を渡す）で呼び出しを数える） |
| | `Dispose_UnsubscribesEvents` | 破棄後にイベントを起こしても呼ばれない |

Play モード・手動（Editor、サーバー起動済み）:
- 初回: 起動 → 「起動中…」→ ホーム。プレイヤーLv とガチャ通貨が出る。2 回目（再起動）も同じ
- 「ガチャ」「アイドル・サポート」「プロデュース」を押すと「準備中です。」が出て、OK で閉じる
- 通信中（サインインの間）は入力が効かない（`ApiActivityBridge`。サーバーを遅くする手段がなければ、確かめていないと報告する）
- サーバーを止めて起動 → 通信エラーのダイアログ（`ErrorDialogView` → `DialogService`）が出る
- 画面を縦長にしたとき、背景が全面に出てボタンが safe area の中にある
- Windows のウィンドウの比率補正（`WindowAspectKeeper`）はスタンドアロンのビルドでだけ確かめる（Editor では効かない）。確かめられなければ、そう報告する

## 6. 未決事項

なし
