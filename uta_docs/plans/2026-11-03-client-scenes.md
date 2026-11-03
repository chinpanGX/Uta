---
title: 実装計画: クライアントのシーン構成（Bootstrap / Title / Home / Produce）
status: ready
updated: 2026-11-03
spec: ../wiki/architecture/screen-design.md, ../wiki/architecture/repository-layout.md, ../wiki/architecture/api-design.md
---

# 実装計画: クライアントのシーン構成

## 1. 目的と前提

[screen-design.md](../wiki/architecture/screen-design.md) 7章で決めたシーン構成を作る。起動を `Bootstrap` シーンの起点にし、`Title` でスタートをタップしたらサインアップ・サインインの通信（[2026-11-03-client-signup-signin.md](2026-11-03-client-signup-signin.md)、done。PR #24 と同じブランチの続き）を行い、成功したら `Home`、そこから `Produce` へ進む。診断用の IMGUI（`StartupPresenter`）をやめ、通常の起動はシーンの uGUI で動かす。

- 関係する ADR: [0006](../wiki/decisions/0006-adopt-unity-7-early.md)、[0008](../wiki/decisions/0008-shared-domain-and-onion.md)、[0009](../wiki/decisions/0009-client-stack.md)
- **ユーザーが決めたこと（2026-11-03）**: シーンは Bootstrap / Title / Home / Produce。Bootstrap が起動の起点。Title のスタートのタップでサインアップ・サインイン。DI は Bootstrap に親、各シーンに子の `LifetimeScope`。中身は最小の uGUI。`IErrorNavigator` を作る
- **この計画で決めないこと**（範囲外）:
  - 画面の作り込み（デザイン、TextMeshPro、日本語、`UnityScreenNavigator` のページ積み）。順 7
  - プロデュースの中身（`Produce` は空）、ホームの「再開」・プロデュース破棄のダイアログ
  - `OpenStoreAsync`（ログだけ）、`DATE_CHANGED` 後のサインインのやり直し
  - Android・iOS の実機での確認

## 2. この計画で決めたこと（細部は Uta案。screen-design.md 7章に書いた）

| 項目 | 決めたこと | 理由 |
|---|---|---|
| 登録の置き場の規則 | **ルート**（`Bootstrap` が作る）: 通信・`PlayerCache`・マスタ・共通エラーハンドラー・**すべてのユースケース**・`ISceneNavigator`・`IErrorNavigator`。**シーンの子**: View・Presenter と、そのシーンだけの診断の補助（`ConnectivityCheck`） | 規則を 1 つにする |
| ルートのスコープの作り方 | `Bootstrap` シーンの `BootstrapEntry`（`MonoBehaviour`）が、マスタを読んだあとコードで `LifetimeScope.Create` する（今の `UtaBootstrap` の中身を移す）。`RuntimeInitializeOnLoadMethod` はやめる | マスタの読み込みが非同期で、その結果（`ICardCatalog`）を登録するため、シーンに置く `LifetimeScope` の `Configure`（同期）では組めない |
| 子のスコープが親を見つける方法 | `SceneNavigator` が `LifetimeScope.EnqueueParent(root)` の中で `SceneManager.LoadSceneAsync` する。`root` は、`SceneNavigator` のコンストラクターで受け取る `LifetimeScope`（VContainer がルートのスコープ自身を `LifetimeScope` として登録している。`LifetimeScope.cs` の `RegisterInstance<LifetimeScope>(this)`） | VContainer の標準の方法。手で `SetRoot` する循環を作らない |
| ナビゲーターと `UtaApiClient` | `UtaApiClient` の登録は `resolver` を使う形にし、`IErrorNavigator` を `resolver.Resolve<IErrorNavigator>()` で解決して `Create` に渡す。**ただし診断（`-uta-quit-after-check`）のときは `null` を渡す**（ナビゲーターを付けない） | 診断で `Title` に戻されて、診断が繰り返されるのを避ける |
| `Title` のエラー時 | `ApiCore` が `ReturnToTitle` を選ぶと、ナビゲーターが `Title` を読み込み直す。古い `TitlePresenter` の続き（`catch` など）が、破棄された `TitleView` に触らないよう、`catch` と後処理の前に `view == null`（`UnityEngine.Object` の null 判定）なら何もせず return する | 古い Presenter が `MissingReferenceException` を出さない |
| 画面の文字 | 英語。組み込みフォント `LegacyRuntime.ttf` の `UnityEngine.UI.Text` | TextMeshPro の初期アセットの取り込みと日本語フォントは画面の計画 |
| 入力 | `EventSystem` に `InputSystemUIInputModule`（`ProjectSettings` の `activeInputHandler: 1` = Input System のみのため。`StandaloneInputModule` は使えない） | |
| `SampleScene` | 削除し、Build Settings・ビルドプロファイル（`Assets/Settings/Build Profiles/Windows.asset`。`m_OverrideGlobalSceneList: 1` で `SampleScene` を参照している）から外して 4 つのシーンにする | テンプレートの未使用のシーン |
| 診断（`-uta-quit-after-check`） | `Title` がタップを待たずに開始し、サインインのあと `ConnectivityCheck`（トークンの取り直し・再認証の再送・サーバーのエラーの受け取り。今の `StartupPresenter` の中身）を行い、`Home` に進まずに終了コードで終わる | バッチモードで誰もタップできない。IL2CPP のプレイヤーでの確認を残す |
| Home に入ったとき `PlayerCache.Current` が null | `Title` へ戻す | 直接 Home に来た場合の保険 |
| `StartGameUseCase` | 作らない。`TitlePresenter` が `SignInUseCase`（端末の認証）→ `EnterPlayerUseCase` を順に呼ぶ | 2 行の合成で、呼び手が 1 つ |

## 3. 触るファイル

`A` = `client/uta/Assets/Scripts/Application`、`P` = `.../Presentation`、`B` = `.../Bootstrap`、`E` = `.../Editor`、`I` = `.../Infrastructure`。新しい `.cs` / `.asmdef` / フォルダ / シーンごとに `.meta`（Unity が作る）。

| 操作 | パス | 中身 | タスク |
|---|---|---|---|
| 作成 | `A/Navigation/ISceneNavigator.cs` | 4.1 | 1 |
| 作成 | `P/Title/TitleView.cs`・`TitlePresenter.cs`・`TitleLifetimeScope.cs`・`ConnectivityCheck.cs` | 4.2 | 1 |
| 作成 | `P/Home/HomeView.cs`・`HomePresenter.cs`・`HomeLifetimeScope.cs` | 4.2 | 1 |
| 作成 | `P/Produce/ProduceView.cs`・`ProducePresenter.cs`・`ProduceLifetimeScope.cs` | 4.2 | 1 |
| 削除 | `P/Startup/StartupPresenter.cs` とその `.meta` | `TitlePresenter`・`ConnectivityCheck` に移す | 1 |
| 変更 | `P/Uta.Client.Presentation.asmdef` | `references` に `UnityEngine.UI` を足す | 1 |
| 作成 | `B/BootstrapEntry.cs`・`SceneNavigator.cs`・`ErrorNavigator.cs` | 4.3 | 1 |
| 削除 | `B/UtaBootstrap.cs` とその `.meta` | `BootstrapEntry` に移す | 1 |
| 変更 | `I/Api/UtaApiClient.cs` | `Create` に `IErrorNavigator errorNavigator` を足す | 1 |
| 作成 | `E/SceneBuilder.cs`・`E/BootstrapPlayMode.cs` | 4.4 | 2 |
| 変更 | `E/Uta.Client.Editor.asmdef`・`E/BuildCommands.cs` | 4.4 | 2 |
| 作成 | `client/uta/Assets/Scenes/Bootstrap.unity`・`Title.unity`・`Home.unity`・`Produce.unity` と `.meta` | `SceneBuilder` が生成 | 2 |
| 削除 | `client/uta/Assets/Scenes/SampleScene.unity` とその `.meta` | | 2 |
| 変更 | `client/uta/ProjectSettings/EditorBuildSettings.asset`・`client/uta/Assets/Settings/Build Profiles/Windows.asset` | シーンの並び | 2 |
| 変更 | wiki | 確かめたこと、古くなる記述（5章 タスク 3） | 3 |

## 4. 型とシグネチャ

C# 9 の範囲（`client/` の `.editorconfig`）。BOM 付き UTF-8、LF。static の可変フィールドを作らない。`Presentation` は `VolTech` と `Infrastructure` を参照しない。`using` の不足はコンパイルエラーに従って足してよい。

### 4.1 Application（タスク 1）

```csharp
namespace Uta.Client.Application.Navigation
{
    public interface ISceneNavigator
    {
        UniTask GoToTitleAsync();
        UniTask GoToHomeAsync();
        UniTask GoToProduceAsync();
    }
}
```

### 4.2 Presentation（タスク 1）

名前空間は `Uta.Client.Presentation.Title` / `.Home` / `.Produce`。View は `MonoBehaviour`（`public sealed class`）。`[SerializeField] private` のフィールドをシーンに割り当てる（4.4）。購読の解除は、ラムダではなくメソッドを購読して `Dispose` で外す。

```csharp
// TitleView
[SerializeField] private Button startButton;
[SerializeField] private Text statusText;
public event Action Tapped;                       // Awake で startButton.onClick.AddListener(OnClick)。OnClick は Tapped?.Invoke()
public void SetStatus(string text) { statusText.text = text; }
public void SetInteractable(bool value) { startButton.interactable = value; }

// TitlePresenter : IStartable, IDisposable  （Dispose は if (view != null) { view.Tapped -= OnTapped; }）
public TitlePresenter(TitleView view, SignInUseCase deviceSignIn, EnterPlayerUseCase enterPlayer, ConnectivityCheck connectivityCheck, ISceneNavigator navigator, StartupOptions options)
private bool running;

public void Start()
{
    view.Tapped += OnTapped;
    view.SetStatus("Tap to start");
    if (options.QuitAfterCheck) { RunAsync(true).Forget(); }   // 診断: タップを待たない
}
public void Dispose() { if (view != null) { view.Tapped -= OnTapped; } }   // view が破棄済みなら何もしない
private void OnTapped() { RunAsync(false).Forget(); }

private async UniTaskVoid RunAsync(bool diagnostic)
{
    if (running) { return; }
    running = true;                                             // 二重タップを無視
    view.SetInteractable(false);
    view.SetStatus("Connecting...");
    var succeeded = false;
    try
    {
        Log("startup: start");
        var registered = await deviceSignIn.ExecuteAsync();
        Log(registered ? "device: registered" : "device: token issued");
        var entered = await enterPlayer.ExecuteAsync(registered);
        Log("sign-up: " + (!entered.SignedUp ? "skipped" : entered.Created ? "created" : "already exists"));
        var s = entered.Snapshot;
        Log("sign-in: ok level=" + s.Level + " exp=" + s.Exp + " gold=" + s.Gold + " supportPoints=" + s.SupportPoints + " gachaCurrency=" + s.GachaCurrency + " idols=" + s.Idols.Count + " supportCards=" + s.SupportCards.Count + " hasActiveProduce=" + s.HasActiveProduce + " discardedProduce=" + s.DiscardedProduce);

        if (diagnostic)
        {
            succeeded = await connectivityCheck.RunAsync(Log);
        }
        else
        {
            await navigator.GoToHomeAsync();
            return;                                             // シーンが替わる。running は戻さない
        }
    }
    catch (Exception e)
    {
        Log("start: FAILED " + e.GetType().FullName + ": " + e.Message);
        if (view != null) { view.SetStatus("Failed: " + e.Message + "\nTap to retry"); }
    }

    if (diagnostic)
    {
        UnityEngine.Application.Quit(succeeded ? 0 : 1);
        return;
    }

    if (view == null) { return; }                               // ApiCore がタイトルへ戻して、この Title が破棄された
    view.SetInteractable(true);
    running = false;
}

private static void Log(string line) { Debug.Log("[Uta] " + line); }
```

```csharp
// ConnectivityCheck（Presentation/Title。診断用。画面の計画で消す、と XML コメントに書く。public sealed class）
public ConnectivityCheck(SignInUseCase signIn, EnterPlayerUseCase enterPlayer)
/// <returns>全部通れば true。server error delivery が届かなければ false。それ以外の失敗は例外のまま投げる</returns>
public async UniTask<bool> RunAsync(Action<string> log)
{
    await signIn.ReissueAsync();
    log("reissue: ok");

    var before = signIn.TokenInvalidationCount;
    await signIn.InvalidateServerSideTokenAsync();
    await enterPlayer.ExecuteAsync(false);
    if (signIn.TokenInvalidationCount <= before)
    {
        throw new InvalidOperationException("reauth retry: not observed");
    }
    log("reauth retry: ok (invalidations=" + (signIn.TokenInvalidationCount - before) + ")");

    if (await signIn.CanReceiveServerErrorAsync())
    {
        log("server error delivery: ok");
        log("connectivity check: OK");
        return true;
    }

    log("server error delivery: FAILED");
    log("connectivity check: FAILED server error delivery");
    return false;
}

// TitleLifetimeScope : LifetimeScope（public sealed class）
protected override void Configure(IContainerBuilder builder)
{
    builder.RegisterComponentInHierarchy<TitleView>();
    builder.Register<ConnectivityCheck>(Lifetime.Singleton);
    builder.RegisterEntryPoint<TitlePresenter>();
}
```

`SignInUseCase`・`EnterPlayerUseCase`・`PlayerCache`・`ISceneNavigator`・`StartupOptions` はルートの登録（4.3）から解決される。

```csharp
// HomeView
[SerializeField] private Text summaryText;  [SerializeField] private Button produceButton;
public event Action ProduceClicked;   // Awake で produceButton.onClick に接続
public void SetSummary(string text)

// HomePresenter : IStartable, IDisposable
public HomePresenter(HomeView view, PlayerCache cache, ISceneNavigator navigator)
Start(): cache.Current が null なら navigator.GoToTitleAsync().Forget() して return。そうでなければ view.SetSummary(下の文字列) し、view.ProduceClicked += OnProduce（OnProduce は navigator.GoToProduceAsync().Forget()）
Dispose(): if (view != null) { view.ProduceClicked -= OnProduce; }   // View が破棄済みなら何もしない
SetSummary(string text) の本体: summaryText.text = text;
Summary: "Lv " + Level + "  Exp " + Exp + "\nGold " + Gold + "\nSupport pt " + SupportPoints + "\nGacha " + GachaCurrency + "\nIdols " + Idols.Count + "  Support cards " + SupportCards.Count
         （DiscardedProduce が true なら末尾に "\n(previous produce was discarded)"）
HomeLifetimeScope: RegisterComponentInHierarchy<HomeView>(); RegisterEntryPoint<HomePresenter>();

// ProduceView
[SerializeField] private Button homeButton;  public event Action HomeClicked;   // Awake で接続
// ProducePresenter : IStartable, IDisposable
public ProducePresenter(ProduceView view, ISceneNavigator navigator)
Start(): view.HomeClicked += OnHome（OnHome は navigator.GoToHomeAsync().Forget()）
Dispose(): if (view != null) { view.HomeClicked -= OnHome; }
ProduceLifetimeScope: RegisterComponentInHierarchy<ProduceView>(); RegisterEntryPoint<ProducePresenter>();
```

`Presentation` の asmdef の `references` に `UnityEngine.UI` を足す（そのほかは変えない）。`StartupView`・`StartupOptions` は残す（`StartupView` はマスタの読み込みの失敗の表示だけに使う。ルートには登録しない）。

### 4.3 Bootstrap と Infrastructure（タスク 1）

```csharp
// I/Api/UtaApiClient.cs: Create の最後の引数に IErrorNavigator errorNavigator を足し、ApiCore のビルダーに .WithErrorNavigator(errorNavigator) を足す（null なら渡さないのと同じ。ビルダーの仕様）

// B/SceneNavigator.cs（public sealed class : ISceneNavigator。using Cysharp.Threading.Tasks; UnityEngine.SceneManagement; VContainer.Unity;）
private readonly LifetimeScope root;
public SceneNavigator(LifetimeScope root) { this.root = root; }   // VContainer がルート自身を注入する
public UniTask GoToTitleAsync() { return LoadAsync("Title"); }    // Home, Produce も同様
private async UniTask LoadAsync(string sceneName)
{
    using (LifetimeScope.EnqueueParent(root))
    {
        await SceneManager.LoadSceneAsync(sceneName);
    }
}

// B/ErrorNavigator.cs（public sealed class : IErrorNavigator。using VolTech.Client;）
public ErrorNavigator(ISceneNavigator scenes)
GoToTitleAsync => scenes.GoToTitleAsync(); GoToHomeAsync => scenes.GoToHomeAsync();
OpenStoreAsync: Debug.LogWarning("[Uta] OpenStore is not implemented"); return UniTask.CompletedTask;

// B/BootstrapEntry.cs（public sealed class BootstrapEntry : MonoBehaviour。UtaBootstrap の RunAsync を移す）
private void Start() { RunAsync().Forget(); }
RunAsync の手順:
 1. args・serverUrl・quitAfterCheck・StartupView（マスタの失敗の表示用。DontDestroyOnLoad）・ErrorDialogView（今のまま）を作る
 2. マスタを読む（失敗時の表示と Quit は今のまま）。成功したら view.SetText(string.Empty)
 3. accountPath を作る（今のまま）。LifetimeScope.Create でルートを組む。登録:
      RegisterInstance(ICardCatalog)、RegisterInstance(new StartupOptions(quitAfterCheck))、
      Register<SceneNavigator>(Lifetime.Singleton).As<ISceneNavigator>()、
      Register<ErrorNavigator>(Lifetime.Singleton).As<IErrorNavigator>()、
      Register<UtaApiClient>(resolver => UtaApiClient.Create(serverUrl, AppInfo.Platform, AppInfo.Version, accountPath, AppInfo.AccountKeySource, new DefaultCommonErrorHandler(errorDialog), quitAfterCheck ? null : resolver.Resolve<IErrorNavigator>()), Lifetime.Singleton)、
      Register<DeviceAuthGateway>(Lifetime.Singleton).As<IDeviceAuthPort>()、Register<PlayerGateway>(Lifetime.Singleton).As<IPlayerPort>()、
      Register<PlayerCache>(Lifetime.Singleton)、Register<SignInUseCase>(Lifetime.Singleton)、Register<EnterPlayerUseCase>(Lifetime.Singleton)
 4. DontDestroyOnLoad(scope.gameObject); await scope.Container.Resolve<ISceneNavigator>().GoToTitleAsync();
```

`B/UtaBootstrap.cs` を削除する（`RuntimeInitializeOnLoadMethod` を残さない）。`B/Uta.Client.Bootstrap.asmdef` の `references` は変えない。足りない参照のコンパイルエラーが出たら、止まって報告する。`BootstrapEntry` 自体は `MonoBehaviour` なので、asmdef 内の同名のファイルであること（`BootstrapEntry.cs`）が必要。

### 4.4 Editor（タスク 2）

```csharp
// E/SceneBuilder.cs（namespace Uta.Client.Editor。public static class）
[MenuItem("Uta/Create Scenes")] public static void CreateScenes()
```
`-batchmode -executeMethod Uta.Client.Editor.SceneBuilder.CreateScenes -quit` からも呼べる。シーンごとに `EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single)` で空のシーンから作り、`EditorSceneManager.MarkSceneDirty(scene)` のあと `EditorSceneManager.SaveScene(scene, "Assets/Scenes/<名前>.unity")`（既にあれば作り直す）。最後に `EditorBuildSettings.scenes` を **Bootstrap, Title, Home, Produce の順**（すべて有効）にし、`AssetDatabase.DeleteAsset("Assets/Scenes/SampleScene.unity")` する。そのあと `Assets/Settings/Build Profiles/Windows.asset` の `m_Scenes` を、`SampleScene` の 1 件から、4 つのシーン（`m_enabled: 1`・`m_path`・`m_guid`。guid は各 `.unity.meta` の `guid`）の並びに**テキストで書き換える**（`SceneBuilder` ではなく、実装担当が編集する）。

共通:
- `Main Camera`: `new GameObject("Main Camera", typeof(Camera))`、`tag = "MainCamera"`、`clearFlags = SolidColor`、`backgroundColor = new Color(0.1f, 0.1f, 0.15f)`
- `EventSystem`（`Title`・`Home`・`Produce`）: `typeof(EventSystem), typeof(InputSystemUIInputModule)` を付けた GameObject。作ったあと `module.AssignDefaultActions()` を呼ぶ（呼ばないと入力のアクションが空でボタンが反応しない）
- UI の GameObject（`Canvas` の下のもの）は `new GameObject(名前, typeof(RectTransform))` で作り、`transform.SetParent(親, false)`（`false` = ワールド座標を保たない。`true` だと `anchoredPosition` がずれる）で親に付けてから `RectTransform` の値を設定する
- `Canvas`: `typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)`、`renderMode = ScreenSpaceOverlay`、`CanvasScaler.uiScaleMode = ScaleWithScreenSize`、`referenceResolution = (1080, 1920)`、`matchWidthOrHeight = 0.5f`
- `Text`: `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")`、`color = Color.white`、`alignment` は下の表、`supportRichText = false`
- `Button`: `Image`（`color = new Color(0.2f, 0.35f, 0.6f, 1f)`）+ `Button`。子の `Text` は `anchorMin = (0,0)`・`anchorMax = (1,1)`・`offsetMin = offsetMax = (0,0)`・中央揃え・フォントサイズ 48
- `RectTransform` は `anchorMin = anchorMax = pivot = アンカー`、`anchoredPosition`、`sizeDelta` を下の表のとおりに設定する
- `[SerializeField] private` への割り当て: `var so = new SerializedObject(view); so.FindProperty("startButton").objectReferenceValue = button; so.ApplyModifiedPropertiesWithoutUndo();`

| シーン | オブジェクト（アンカー / anchoredPosition / sizeDelta） |
|---|---|
| `Bootstrap` | `Main Camera`、空の GameObject `BootstrapEntry` に `BootstrapEntry` コンポーネント |
| `Title` | `Main Camera`、`EventSystem`、`Canvas`。`Canvas` の下に `StartButton`（(0.5,0.5) / (0,0) / (600,160)。子の `Text` は "TAP TO START"）、`StatusText`（(0.5,0) / (0,120) / (1000,300)。`Text` フォントサイズ 36、`UpperCenter`）。`TitleView` を `Canvas` に付け、`startButton`・`statusText` を割り当てる。空の GameObject `TitleScope` に `TitleLifetimeScope` |
| `Home` | `Main Camera`、`EventSystem`、`Canvas`。下に `SummaryText`（(0.5,1) / (0,-120) / (1000,800)。フォントサイズ 44、`UpperLeft`）、`ProduceButton`（(0.5,0) / (0,200) / (600,160)。子の `Text` は "PRODUCE"）。`HomeView` を `Canvas` に付けて `summaryText`・`produceButton` を割り当て。`HomeScope` に `HomeLifetimeScope` |
| `Produce` | `Main Camera`、`EventSystem`、`Canvas`。下に `Label`（(0.5,1) / (0,-200) / (1000,160)。`Text` "PRODUCE"、フォントサイズ 64、`UpperCenter`）、`HomeButton`（(0.5,0.5) / (0,0) / (600,160)。子の `Text` は "BACK TO HOME"）。`ProduceView` を `Canvas` に付けて `homeButton` を割り当て。`ProduceScope` に `ProduceLifetimeScope` |

```csharp
// E/BootstrapPlayMode.cs
[InitializeOnLoad] static class BootstrapPlayMode
{
    static BootstrapPlayMode()   // Assets/Scenes/Bootstrap.unity の SceneAsset があれば EditorSceneManager.playModeStartScene に設定。無ければ null
}
```

`E/Uta.Client.Editor.asmdef` の `references` を、`Uta.Client.Bootstrap`・`Uta.Client.Presentation`・`UnityEngine.UI`・`Unity.InputSystem`・`VContainer` にする（これで足りない参照が出たら、止まって報告する）。`BuildCommands.BuildWindowsIl2Cpp` の `scenes` は、`EditorBuildSettings.scenes` の有効なものの `path`（`System.Linq`）にする。

## 5. タスク

前提: 作業の場所は `C:\Work\Uta-wt\3`。**Unity の Editor は閉じておく**（バッチモードで開く。同じプロジェクトを同時に開けない）。確認の手順は [2026-11-03-client-signup-signin.md](2026-11-03-client-signup-signin.md) 5章と同じ（コンパイルは `-batchmode -nographics -quit`、テストは `-runTests -testPlatform EditMode`、ログと XML はスクラッチパッド）。**各タスクが終わったとき、Unity のコンパイルが通り、EditMode のテストが通る状態にする。**

### タスク 1: Application のポートと、Presentation・Bootstrap の組み替え

- 触る: 3章のタスク 1 の行。手順: 4.1・4.2・4.3 のとおり。`StartupPresenter` の中身は、`TitlePresenter`（ログの行・二重タップ・失敗の表示）と `ConnectivityCheck`（`reissue` 以降）に分けて移す（4.2 のコードが最終形）
- 境界: 二重タップは無視する。失敗したら「Tap to retry」を出して、もう一度タップできる。`Home` に入った後は `running` を戻さない。破棄された View に触らない
- 確かめ方: コンパイルが通り、既存の EditMode のテスト（39 件）が通る（シーンはまだ無い。実行の確認は タスク 2 のあと）

### タスク 2: Editor（シーンの生成・Play の開始シーン・ビルド）

- 触る: 3章のタスク 2 の行。手順（番号の順に行う）:
  1. 4.4 のとおりに `SceneBuilder`・`BootstrapPlayMode`・asmdef・`BuildCommands` を書き、コンパイルを通す
  2. シーンを生成する（下のコマンド）
  3. 生成された 4 つの `.unity.meta` の `guid` を読み、`Windows.asset` の `m_Scenes` を 4.4 のとおりに書き換える
  4. もう一度 Unity をバッチで開いて（コンパイルのコマンド）、`Windows.asset`・`EditorBuildSettings.asset` の差分が 4 件の並びのまま変わらないことを `git diff` で確かめる
  - シーンの生成のコマンド: `Unity.exe -batchmode -nographics -projectPath C:\Work\Uta-wt\3\client\uta -executeMethod Uta.Client.Editor.SceneBuilder.CreateScenes -quit -logFile <ログ>` 。生成したら、`.unity` と `.meta` と `EditorBuildSettings.asset`、`Windows.asset` の変更を残す。**`ProjectSettings` のそのほかの差分（`Standalone: 1` の追加など）や `com.unity.dedicated-server` のフォルダはコミットに含めない**（コミットの前にメインのセッションが戻す）
- 確かめ方: 4 つのシーンができ、`EditorBuildSettings` と `Windows.asset` が 4 つを並べ、`SampleScene` が無く、コンパイルと既存のテストが通る

### タスク 3: 設計書の反映（メインのセッション）

実行で確かめたこと・確かめられなかったことを [unity7-evaluation.md](../wiki/architecture/unity7-evaluation.md) に書く。古くなる記述を直す: `masterdata-pipeline.md` と `lesson-domain-types.md` の `UtaBootstrap`（→ `BootstrapEntry`）、`screen-design.md` 5章の「進行中通知とナビゲーターは未接続」と 4章の「診断用の IMGUI に結果を出す」の記述。この計画を `status: done` にし、`python -I .claude/skills/linting-wiki/lint_wiki.py` を流す。

## 6. テストと実行での確認

### 6.1 EditMode のテスト

新しい単体テストは足さない（`TitlePresenter` などは Unity の View に依存し、ロジックは既存のユースケース（`EnterPlayerUseCase` のテスト 9 件）にある）。既存の 39 件が通ることを確かめる。

### 6.2 実行での確認（メインのセッション。できなければ「確認できていない」と書く）

1. `dotnet build Uta.slnx`（警告 0・エラー 0）
2. ローカルのサーバーを起動する（`CLAUDE.md`。Docker が使えるとき）
3. Windows の IL2CPP のプレイヤー（Editor は閉じる）で `Uta.exe -batchmode -nographics -uta-quit-after-check`: `Bootstrap` → `Title`（タップを待たず）→ `startup: start` … `sign-in: ok` … `reauth retry: ok` … `connectivity check: OK`、終了コード 0。シーンの読み込み・VContainer の親子のスコープ・uGUI の生成が IL2CPP で動くことの確認を兼ねる。存在しないポート（`-uta-server-url http://localhost:5999`）では `start: FAILED` で終了コード 1
4. Editor（Play）での操作（タップ・遷移）: Play モードで `Title` の `StartButton` を押し、`Home` に所持データが出て、`PRODUCE` → `Produce` → `BACK TO HOME` で戻ることを確かめる。ボタンは `unity command eval` で `onClick.Invoke()` を呼んで押す。Editor が使えなければ、ユーザーに依頼する（確認できていないと書く）

## 7. 未決事項

なし（ユーザーが決めたことは 1章。実装担当が計画にない設計判断に出会ったら、止まって報告する）。

実装中に確かめることで、結果によっては計画を直す点: (a) `LifetimeScope.EnqueueParent` の中で `SceneManager.LoadSceneAsync` を `await` する形で、子のスコープが親を見つけるか（見つけなければ止まって報告）、(b) `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")` が Unity 7 で取れるか（取れなければ止まって報告。代わりのフォントを選ぶのは設計判断）、(c) `RegisterComponentInHierarchy<TitleView>()` が `Canvas` に付けた `MonoBehaviour` を見つけるか。
