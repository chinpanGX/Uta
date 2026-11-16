---
title: 実装計画: client の PlayMode のテストの基盤
status: ready
updated: 2026-11-16
spec: ../wiki/decisions/0024-verification-first-reviews.md, ../wiki/architecture/unity7-evaluation.md, ../wiki/architecture/repository-layout.md, ../wiki/architecture/screen-design.md
---

# 実装計画: client の PlayMode のテストの基盤

## この計画で決めたこと

- **置き場**: PlayMode のテストのアセンブリ `Uta.Client.Tests.PlayMode` を `client/uta/Assets/Tests/PlayMode/` に作る。ルート名前空間は EditMode と同じ `Uta.Client.Tests`。asmdef は全プラットフォーム（`includePlatforms` を空）にし、`defineConstraints` の `UNITY_INCLUDE_TESTS` でプレイヤーのビルドから外す（Editor だけの asmdef は Test Runner が EditMode として扱うため）
- **テストの起点は本物の `Bootstrap` シーン**。テストは `SceneManager.LoadScene("Bootstrap")` で読み、`BootstrapEntry` が組むルートのスコープ（本物の `ApiClient`）のまま進める。製品のコードには手を入れない
- **通信の相手はローカルのサーバー**（`http://localhost:5000`。CLAUDE.md「ローカルのサーバー」の手順で上げたもの）。偽の Service に差し替える口は作らない（2026-11-16。ユーザーの判断）。ADR 0024 の「テスト用のサーバーの用意」は、専用のサーバーを作らず、CLAUDE.md のローカルのサーバーを 3.9 の手順でバックグラウンドに上げ下げすることで満たす
- **テストのアセンブリの参照**: `VContainer`（`LifetimeScope` を見つけて破棄するため）・`UnityScreenNavigator`・`UniTask`・uGUI・TMP を参照する。repository-layout.md の「`VContainer` を参照してよいのは `Bootstrap` と `Presentation` だけ」は製品の層の決まりで、テストのアセンブリは既に `Uta.Client.Tests.EditMode` が `VContainer` を参照している。PlayMode も同じ扱いにし、タスク 4 で repository-layout.md のその項に「テストのアセンブリ（`Uta.Client.Tests.EditMode`・`Uta.Client.Tests.PlayMode`）は除く」と書き足す
- **確かめ方のコマンドの例外**: plans/README.md は「コマンドは CLAUDE.md にあるものだけ」とするが、この計画は回し方そのものを作る。3.7・3.9 のコマンドと、unity7-evaluation.md 4章にある `unity command editor.playmode stop`・`unity command console get` を、この計画に限って使う。タスク 4 で、CLAUDE.md「ビルド・テスト・実行」に PlayMode の 1 行（3.9 の末尾の文）を足し、3.7・3.9 のコマンドの全文は unity7-evaluation.md 4章に置く。EditMode のテストと同じ形（CLAUDE.md の行が 4章の全文へリンクする）で、以後の計画は、CLAUDE.md の行とそのリンク先の全文からコマンドを引く
- **カテゴリで分ける**: サーバーが要らないテストは `[Category("Smoke")]`、ローカルのサーバーが要るテストは `[Category("Server")]`。回すコマンドは 2 つ（`Smoke` だけ／全部）
- **回し方**: Editor を開いたまま、EditMode と同じ `unity command eval` + `TestRunnerApi` の非同期の形で `TestMode.PlayMode` を渡す。結果は `client/uta/Temp/uta-playmode-results.xml`（EditMode の結果のファイルと分ける）。回す前に `EditorUtility.RequestScriptReload()` で USN の static を捨てる（ドメインのリロードを切っているため。[unity7-evaluation.md](../wiki/architecture/unity7-evaluation.md) 4章）
- **背面で止まったら `runInBackground` を 1 にする**（2026-11-16。ユーザーの判断）。タスク 1 のスパイクで、Editor が背面のまま PlayMode のテストが終わらないと分かったときだけ、`ProjectSettings.asset` の `runInBackground` を 1 にする。Windows のプレイヤーも背面で止まらなくなる。Android・iOS は無視される
- **最初のテスト**: 3 本。`PlayModeHarnessTests`（フレームが進むこと。Smoke）、`BootToTitleTests`（Bootstrap → Title のページ。Smoke）、`TitleToHomeTests`（Title でスタートを押す → Home のページ。Server）
- **テストの後片付け**: 各テストの `[UnityTearDown]` で、`DontDestroyOnLoad` のシーンの root のうち `LifetimeScope` を持つものを破棄する（ルートのスコープが次のテストに残らないように）。Test Runner 自身の `DontDestroyOnLoad` のオブジェクトは消さない
- 基盤ができたので、client の画面・遷移・通信を触るタスクの動作検証は PlayMode のテストを必須にする（[ADR 0024](../wiki/decisions/0024-verification-first-reviews.md) のとおり）。plans/README.md・スキルの「基盤がなければ」の書き方を、タスク 4 で直す

## ユーザーの確認が要ること

なし（通信の相手と、背面で止まったときの扱いは、2026-11-16 にユーザーが決めた。上の節）。

## 1. 目的と前提

- client の画面・遷移・通信を、Editor を開いたまま自動で確かめる PlayMode のテストの基盤を作る。[ADR 0024](../wiki/decisions/0024-verification-first-reviews.md) の「基盤は別の計画で作る」の計画
- 起動の流れ: `Bootstrap` シーンの `BootstrapEntry` がマスタを読み、ルートの `LifetimeScope` を `DontDestroyOnLoad` で作って `Title` シーンへ進む。`TitleEntry` が `TitlePresenter` のページを積む（このときはサーバーに触らない）。スタートを押すと、`DeviceAuthUseCase.SignInAsync` → `PlayerUseCase.EnterAsync` → `Home` シーン（サーバーが要る）
- サーバーの URL は `ServerEndpoint.Resolve(Environment.GetCommandLineArgs())`。Editor の引数には `-uta-server-url` が無いので、既定の `http://localhost:5000` になる
- 1 回の Play の中で `Bootstrap` を読み直すこと（全部を回すと、`BootToTitleTests` と `TitleToHomeTests` がそれぞれ読む）: USN の `PageContainer`・`ModalContainer` は、`Awake` で static の一覧（`Instances`・`InstanceCacheByName`・`InstanceCacheByTransform`）に自分を入れ、`OnDestroy` で外す（USN `Runtime/Core/Page/PageContainer.cs` の `Awake`・`OnDestroy`。パッケージは `Library/PackageCache/com.harumak.unityscreennavigator@*`）。シーンの読み替えで `OnDestroy` が呼ばれるので、同じ Play の中の読み直しは、ゲームの Home → Title（`HomeEntry` の `GoToTitleAsync`）と同じ経路で、残りは出ない見込み。unity7-evaluation.md 4章の「static が Play をまたいで残る」は、Play を出入りしたときのもので、3.7 の手順 2（`RequestScriptReload`）で消す。見込みが外れたときはタスク 3 の止まる条件で止まる
- アカウントのファイルは Editor の `persistentDataPath` の `account-http___localhost_5000.dat`。手で Play するときと同じファイルを使う（テストは消さない）
- 関係する ADR: [0024](../wiki/decisions/0024-verification-first-reviews.md)（動作検証）、[0009](../wiki/decisions/0009-client-stack.md)（クライアントの構成）、[0023](../wiki/decisions/0023-client-linq-with-zlinq.md)（ZLinq の DropIn）
- **決めないこと**（範囲外）: 偽の Service でのテスト、Produce・ガチャ・所持の画面のテスト（その画面を触る計画で足す）、CI での実行、テスト用のサーバーを自動で上げ下げする仕組み、テストのたびにアカウントを作り直すこと

## 2. 触るファイル

すべて `client/uta/` の下（ドキュメントを除く）。`.meta` は Unity が作ったものをそのままコミットする。

| 操作 | パス | 中身 |
|---|---|---|
| 作成 | `Assets/Tests/PlayMode/Uta.Client.Tests.PlayMode.asmdef` | 3.1 |
| 作成 | `Assets/Tests/PlayMode/AssemblyAttributes.cs` | ZLinq の DropIn（3.1） |
| 作成 | `Assets/Tests/PlayMode/Support/TestWait.cs` | 条件を待つ補助（3.2） |
| 作成 | `Assets/Tests/PlayMode/Support/TestScenes.cs` | 起動・後片付けの補助（3.3） |
| 作成 | `Assets/Tests/PlayMode/PlayModeHarnessTests.cs` | フレームが進むこと（3.4） |
| 作成 | `Assets/Tests/PlayMode/BootToTitleTests.cs` | Bootstrap → Title（3.5） |
| 作成 | `Assets/Tests/PlayMode/TitleToHomeTests.cs` | Title → Home（3.6） |
| 変更（条件つき） | `ProjectSettings/ProjectSettings.asset` | `runInBackground: 0` → `1`（タスク 1 で止まったときだけ） |
| 変更 | `CLAUDE.md`（リポジトリのルート） | 「ビルド・テスト・実行」に PlayMode の行（3.9 の末尾） |
| 変更 | `uta_docs/wiki/architecture/unity7-evaluation.md` | 4章に PlayMode のテストの項（3.8） |
| 変更 | `uta_docs/wiki/architecture/repository-layout.md` | アセンブリの表に 1 行（3.8） |
| 変更 | `uta_docs/wiki/architecture/naming.md` | 8章のテストのアセンブリの行（3.8） |
| 変更 | `uta_docs/wiki/architecture/screen-design.md` | 7.7 の確かめ方（3.8） |
| 変更 | `uta_docs/plans/README.md` | 5.1 の client の行と、その下の段落（3.8） |
| 変更 | `.claude/skills/uta-plan/SKILL.md`・`.claude/skills/uta-implement/SKILL.md`・`.claude/skills/requesting-design-review/lenses/readiness.md` | 「基盤があれば／なければ」の書き方（3.8） |

## 3. 型とシグネチャ

C# は `client/` の `.editorconfig` に合わせて C# 9 の範囲（ブロックの namespace、Allman、`var`、BOM 付き UTF-8・LF）。値が変わる static の状態を持たない（補助は状態を持たない static class）。

### 3.1 asmdef と AssemblyAttributes

`Assets/Tests/PlayMode/Uta.Client.Tests.PlayMode.asmdef`（この内容のとおり）:

```json
{
    "name": "Uta.Client.Tests.PlayMode",
    "rootNamespace": "Uta.Client.Tests",
    "references": [
        "UnityEngine.TestRunner",
        "Uta.Client.Presentation",
        "UnityScreenNavigator",
        "UniTask",
        "VContainer",
        "UnityEngine.UI",
        "Unity.TextMeshPro"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll",
        "ZLinq.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- `UnityEditor.TestRunner` は入れない（Editor だけのアセンブリを参照すると、全プラットフォームの asmdef がプレイヤー向けにコンパイルできない）
- `UnityScreenNavigator`・`UniTask` は、`TitlePage`・`HomePage` の基底（`PageBase` → USN の `Page`）をコンパイラが解決するために入れる。`unity recompile` で `CS0012`（型が参照されていないアセンブリにある）が出たら、出たアセンブリ名を `references` に足してよい（この 1 種類のエラーに限る。足したものは実装メモに書く）

`Assets/Tests/PlayMode/AssemblyAttributes.cs`（EditMode と同じ 1 行）:

```csharp
[assembly: ZLinq.ZLinqDropIn("", ZLinq.DropInGenerateTypes.Everything)]
```

### 3.2 TestWait

`Assets/Tests/PlayMode/Support/TestWait.cs`:

```csharp
using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;

namespace Uta.Client.Tests
{
    /// <summary>PlayMode のテストで、条件が成り立つまでフレームを送る。</summary>
    public static class TestWait
    {
        /// <summary>
        /// 毎フレーム failIf → condition の順に見る。failIf が null 以外の文字列を返したら、その文字列で Assert.Fail。
        /// condition が true になったら終わる。timeoutSeconds（Time.realtimeSinceStartup で測る）を過ぎたら
        /// "timeout " + timeoutSeconds + "s: " + description で Assert.Fail。failIf は null でよい。
        /// </summary>
        public static IEnumerator Until(Func<bool> condition, float timeoutSeconds, string description, Func<string> failIf = null)
        {
            var deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (true)
            {
                if (failIf != null)
                {
                    var failure = failIf();
                    if (failure != null)
                    {
                        Assert.Fail(failure);
                    }
                }

                if (condition())
                {
                    yield break;
                }

                if (Time.realtimeSinceStartup > deadline)
                {
                    Assert.Fail("timeout " + timeoutSeconds + "s: " + description);
                }

                yield return null;
            }
        }
    }
}
```

### 3.3 TestScenes

`Assets/Tests/PlayMode/Support/TestScenes.cs`:

```csharp
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Uta.Client.Presentation.Common.Parts;
using Uta.Client.Presentation.Title;
using VContainer.Unity;

namespace Uta.Client.Tests
{
    /// <summary>起動からタイトルまでの手順と、テストの後片付け。</summary>
    public static class TestScenes
    {
        public const float BootTimeoutSeconds = 30f;
        public const string TitleReadyStatus = "タップしてスタート";

        /// <summary>Bootstrap シーンを Single で読み、Title シーンのタイトルのページが操作できるまで待つ。</summary>
        public static IEnumerator BootToTitle()
        {
            SceneManager.LoadScene("Bootstrap", LoadSceneMode.Single);
            yield return TestWait.Until(IsTitleReady, BootTimeoutSeconds, "Title のページ（status=\"" + TitleReadyStatus + "\"、スタートが押せる）");
        }

        /// <summary>アクティブなシーンが Title で、TitlePage が activeInHierarchy、StatusText が TitleReadyStatus、StartButton が interactable。</summary>
        public static bool IsTitleReady()
        {
            if (SceneManager.GetActiveScene().name != "Title")
            {
                return false;
            }

            var page = Object.FindAnyObjectByType<TitlePage>();
            if (page == null || !page.gameObject.activeInHierarchy)
            {
                return false;
            }

            return StatusText(page) == TitleReadyStatus && StartButton(page).interactable;
        }

        /// <summary>TitlePage の子 Body/Main/StatusText の文字（TitlePrefabTests と同じパス）。</summary>
        public static string StatusText(TitlePage page)
        {
            return page.transform.Find("Body/Main/StatusText").GetComponent<TMP_Text>().text;
        }

        /// <summary>TitlePage の子 Body/Main/StartButton に付いた ButtonView の Button（TitlePage.startButton が指すもの）。</summary>
        public static Button StartButton(TitlePage page)
        {
            return page.transform.Find("Body/Main/StartButton").GetComponent<ButtonView>().Button;
        }

        /// <summary>DontDestroyOnLoad のシーンの root のうち、LifetimeScope を持つものを Destroy し、1 フレーム送る。ほかの root（Test Runner のもの）は触らない。</summary>
        public static IEnumerator DestroyPersistentScopes()
        {
            var probe = new GameObject("TestScenesProbe");
            Object.DontDestroyOnLoad(probe);
            var roots = probe.scene.GetRootGameObjects();
            Object.Destroy(probe);
            foreach (var root in roots)
            {
                if (root != probe && root.GetComponent<LifetimeScope>() != null)
                {
                    Object.Destroy(root);
                }
            }

            yield return null;
        }
    }
}
```

- `Body/Main/StartButton` に `ButtonView` が付いていることは、EditMode の `TitlePrefabTests`（`startButton` の参照がこのパスの子）で確かめてある。`Button` は `ButtonView` の公開のプロパティ `Button` から取る（`Button` の GameObject がどこにあるかに依らない）

### 3.4 PlayModeHarnessTests

`Assets/Tests/PlayMode/PlayModeHarnessTests.cs`。PlayMode の実行そのもの（フレームが進む）を確かめる。

```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Uta.Client.Tests
{
    public sealed class PlayModeHarnessTests
    {
        [UnityTest]
        [Category("Smoke")]
        public IEnumerator Frames_Advance()
        {
            var start = Time.frameCount;
            for (var i = 0; i < 3; i++)
            {
                yield return null;
            }

            Assert.GreaterOrEqual(Time.frameCount, start + 3);
        }
    }
}
```

### 3.5 BootToTitleTests

`Assets/Tests/PlayMode/BootToTitleTests.cs`:

```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Uta.Client.Tests
{
    /// <summary>起動 → タイトルのページ（サーバーに触らない）。</summary>
    public sealed class BootToTitleTests
    {
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return TestScenes.DestroyPersistentScopes();
        }

        [UnityTest]
        [Category("Smoke")]
        public IEnumerator Boot_FromBootstrapScene_ReachesTitlePage()
        {
            yield return TestScenes.BootToTitle();
        }
    }
}
```

- 通る条件は `TestScenes.IsTitleReady` の 4 つ（シーン名・ページ・文字・ボタン）。Unity Test Framework は、テスト中に `Debug.LogError` や例外のログが出るとテストを失敗にする（`LogAssert` の既定）。起動でエラーのログが出ないことも、このテストで確かめている

### 3.6 TitleToHomeTests

`Assets/Tests/PlayMode/TitleToHomeTests.cs`:

```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Uta.Client.Presentation.Home;
using Uta.Client.Presentation.Title;

namespace Uta.Client.Tests
{
    /// <summary>タイトルでスタート → ホームのページ。ローカルのサーバー（http://localhost:5000）が要る。</summary>
    public sealed class TitleToHomeTests
    {
        private const float HomeTimeoutSeconds = 30f;
        private const string ConnectFailedPrefix = "接続に失敗しました";

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return TestScenes.DestroyPersistentScopes();
        }

        [UnityTest]
        [Category("Server")]
        public IEnumerator Start_WithLocalServer_ReachesHomePage()
        {
            yield return TestScenes.BootToTitle();

            var titlePage = Object.FindAnyObjectByType<TitlePage>();
            TestScenes.StartButton(titlePage).onClick.Invoke();

            yield return TestWait.Until(
                IsHomeReady,
                HomeTimeoutSeconds,
                "Home のページ。ローカルのサーバー（http://localhost:5000）が起動しているか確かめる",
                () => ConnectFailed(titlePage));
        }

        /// <summary>アクティブなシーンが Home で、HomePage が activeInHierarchy。</summary>
        private static bool IsHomeReady()
        {
            if (SceneManager.GetActiveScene().name != "Home")
            {
                return false;
            }

            var page = Object.FindAnyObjectByType<HomePage>();
            return page != null && page.gameObject.activeInHierarchy;
        }

        /// <summary>タイトルのページが残っていて、文字が ConnectFailedPrefix で始まるなら、その文字を返す。そうでなければ null。</summary>
        private static string ConnectFailed(TitlePage titlePage)
        {
            if (titlePage == null)
            {
                return null;
            }

            var status = TestScenes.StatusText(titlePage);
            return status.StartsWith(ConnectFailedPrefix, System.StringComparison.Ordinal) ? "Title: " + status : null;
        }
    }
}
```

- `titlePage == null` は、Title シーンが破棄された（Home へ進んだ）後の Unity の破棄済みの比較で true になる。そのときは失敗とみなさない
- `TitlePresenter` は失敗を `Debug.Log`（エラーではない）で出すので、`LogAssert` では落ちない。`failIf` で文字を見て早めに落とす

### 3.7 回すコマンド（CLAUDE.md に足すもの）

すべて `client/uta` で実行する（Bash）。Editor は開いたまま。

1. 未保存のシーンが無いことを確かめる（Play に入るときに保存の確認が出て止まるため）:
   ```
   unity command eval 'return UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty;'
   ```
   `True` なら止まり、ユーザーに保存か破棄を頼む
2. 前の Play の USN の static が残っていれば、ドメインを読み直して捨てる:
   ```
   unity command eval 'return UnityScreenNavigator.Runtime.Core.Page.PageContainer.Instances.Count + UnityScreenNavigator.Runtime.Core.Modal.ModalContainer.Instances.Count == 0 ? "uta-clean" : "uta-dirty";'
   ```
   出力に `uta-clean` があれば、残りは無いので手順 3 へ進む（読み直さない）。`uta-dirty` なら、次の 2 行を回す:
   ```
   unity command eval 'UnityEditor.EditorUtility.RequestScriptReload(); return "ok";'
   timeout 180 bash -c 'until unity command eval '"'"'return UnityScreenNavigator.Runtime.Core.Page.PageContainer.Instances.Count + UnityScreenNavigator.Runtime.Core.Modal.ModalContainer.Instances.Count == 0 ? "uta-clean" : "uta-dirty";'"'"' 2>/dev/null | grep -q uta-clean; do sleep 2; done'
   ```
   `uta-dirty` から `uta-clean` に変わるのはドメインの読み直しが終わったときだけなので、2 行目が終わればリロードは済んでいる（`unity status --until-ready` はリロードが始まる前の ready で返ることがあるので使わない）。終了コード 124 なら、1 回目の `eval` をもう一度回し、出力を添えて止まる。1 回目の `eval` がコンパイルのエラーになったときも、出力を添えて止まる
3. PlayMode のテストを始める（`Smoke` だけ。サーバー不要）:
   ```
   unity command --timeout 300 eval 'var path = System.IO.Path.GetFullPath("Temp/uta-playmode-results.xml"); if (System.IO.File.Exists(path)) System.IO.File.Delete(path); var api = UnityEngine.ScriptableObject.CreateInstance<UnityEditor.TestTools.TestRunner.Api.TestRunnerApi>(); var t = typeof(UnityEditor.TestTools.TestRunner.Api.TestRunnerApi).Assembly.GetType("UnityEditor.TestTools.TestRunner.CommandLineTest.ResultsSavingCallbacks"); var cb = (UnityEditor.TestTools.TestRunner.Api.ICallbacks)UnityEngine.ScriptableObject.CreateInstance(t); t.GetField("m_ResultFilePath", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).SetValue(cb, path); api.RegisterCallbacks(cb); var filter = new UnityEditor.TestTools.TestRunner.Api.Filter { testMode = UnityEditor.TestTools.TestRunner.Api.TestMode.PlayMode, categoryNames = new[] { "Smoke" } }; api.Execute(new UnityEditor.TestTools.TestRunner.Api.ExecutionSettings(filter)); return "started";'
   ```
   全部（`Server` を含む。ローカルのサーバーを先に上げる）は、`, categoryNames = new[] { "Smoke" }` を消した同じコマンド。出力が `started` でない（エラー・タイムアウト）ときは、`unity status --until-ready` のあと 1 回だけ回し直し、2 回目も `started` でなければ出力を添えて止まる
4. 結果のファイルができるのを待って読む（最大 600 秒）:
   ```
   timeout 600 bash -c 'until [ -f Temp/uta-playmode-results.xml ]; do sleep 2; done' && grep -o '<test-run[^>]*>' Temp/uta-playmode-results.xml
   ```
   `<test-run … total= passed= failed=>` を見る。失敗は `result="Failed"` の `<test-case>` の `<message>`。終了コード 124 は、ファイルができなかった（タイムアウト）

### 3.9 ローカルのサーバーの上げ下げ（Server のテストの前後）

リポジトリのルートで実行する（Bash）。`server/.env` と user-secrets の接続文字列（CLAUDE.md「ローカルのサーバー」）が既にあることが前提。無いときは止まってユーザーに頼む（シークレットは作らない）。

1. 上げる:
   ```
   docker compose --env-file server/.env -f server/compose.yaml up -d --wait
   dotnet run --project server/Uta.Server -- migrate
   nohup dotnet run --project server/Uta.Server > client/uta/Temp/uta-server.log 2>&1 &
   timeout 180 bash -c 'until grep -q "Now listening on" client/uta/Temp/uta-server.log; do sleep 2; done'
   ```
   最後の行の終了コードが 0 なら準備ができた（ASP.NET Core が起動のログ `Now listening on: http://localhost:5000` を出す）。124 なら `client/uta/Temp/uta-server.log` を添えて止まる
2. 止める:
   ```
   powershell -NoProfile -Command 'Get-NetTCPConnection -LocalPort 5000 -State Listen -ErrorAction SilentlyContinue | ForEach-Object { Stop-Process -Id $_.OwningProcess -Force }'
   powershell -NoProfile -Command '(Get-NetTCPConnection -LocalPort 5000 -State Listen -ErrorAction SilentlyContinue | Measure-Object).Count'
   ```
   PowerShell の式は単一引用符で渡す（二重引用符だと bash が `$_` を展開して壊れる）。ポート 5000 で待ち受けているプロセスを止める。2 行目が `0`（ポート 5000 を誰も待ち受けていない）なら止まった。`0` 以外なら止まる。PostgreSQL のコンテナーは止めない

CLAUDE.md「ビルド・テスト・実行」の「Unity の EditMode のテスト」の行の次に、次の 1 行を足す（文はこのとおり）:

```text
- Unity の PlayMode のテスト（`Assets/Tests/PlayMode`）: **Editor を開いたまま**、`client/uta` で、未保存のシーンが無いことを確かめ → `unity command eval 'UnityEditor.EditorUtility.RequestScriptReload(); return "ok";'` → USN の `PageContainer.Instances`・`ModalContainer.Instances` が残っていれば `RequestScriptReload` して 0 件になるまで `eval` で待つ → EditMode と同じ形の `eval` に `testMode = TestMode.PlayMode` を渡す（結果は `client/uta/Temp/uta-playmode-results.xml`。コマンドの全文は [unity7-evaluation.md](uta_docs/wiki/architecture/unity7-evaluation.md) 4章）。`[Category("Smoke")]` はサーバー不要で `categoryNames = new[] { "Smoke" }` で絞る。`[Category("Server")]` はローカルのサーバー（`http://localhost:5000`）が要る（バックグラウンドでの上げ下げのコマンドは同じ 4章）。client の画面・遷移・通信を触るタスクは、PlayMode のテストで確かめる（[ADR 0024](uta_docs/wiki/decisions/0024-verification-first-reviews.md)）
```

### 3.8 ドキュメントの変更

リンクの書き方は、書き換える先のページからの相対パス（この計画からの相対ではない）。下の「→」の後の文を、そのまま書く。

- `unity7-evaluation.md` 4章: EditMode のテストの項（`TestRunnerApi` の全文がある項）の直後に、項を足す。見出しの文は「**PlayMode のテストも、Editor を開いたまま `TestRunnerApi` で回す**（2026-11-16。計画は `../../plans/2026-11-16-client-playmode-tests.md` へのリンク）」。中身は 3.7 の手順 1〜4 と 3.9 の手順 1・2 のコマンドの全文、カテゴリ（`Smoke`／`Server`）、結果のファイル、タスク 1 のスパイクで分かったこと（背面で進んだか、`runInBackground` を変えたか、かかった秒数）。4章の「Play モードは Editor が前面にないとループが進まない」の項に、タスク 1 の結果を 1 文足す
- `repository-layout.md` のアセンブリの表: `Uta.Client.Tests.EditMode` の行の次に `| \`Uta.Client.Tests.PlayMode\` | \`Assets/Tests/PlayMode\` | asmdef（全プラットフォーム。\`UNITY_INCLUDE_TESTS\` でプレイヤーから外す。ルート名前空間は \`Uta.Client.Tests\`。2026-11-16） | \`Uta.Client.Presentation\`・\`UnityScreenNavigator\`・\`UniTask\`・\`VContainer\`・\`UnityEngine.UI\`・\`Unity.TextMeshPro\` と、テストに要るもの（NUnit・Test Framework・ZLinq。Editor のアセンブリは参照しない） |` を足す（タスク 2 で `references` を足したなら、その名前も書く）。同じページの「`VContainer` を参照してよいのは `Bootstrap` と `Presentation` だけ」の項の末尾に「テストのアセンブリ（`Uta.Client.Tests.EditMode`・`Uta.Client.Tests.PlayMode`）は除く（2026-11-16）」を足す
- `naming.md` 8章: 「クライアントのテストのアセンブリは `Uta.Client.Tests.EditMode`」を「クライアントのテストのアセンブリは `Uta.Client.Tests.EditMode` と `Uta.Client.Tests.PlayMode`（名前空間はどちらも `Uta.Client.Tests`）。PlayMode の補助は `Tests/PlayMode/Support/` に置く（`TestWait`・`TestScenes`）」に書き換える
- `screen-design.md` 7.7: 「確かめ方は計画ごとに「EditMode のテスト／ Play モード／ Windows の IL2CPP のビルド／手動」のどれかを書く」を「確かめ方は計画ごとに「EditMode のテスト／ PlayMode のテスト／ Windows の IL2CPP のビルド／手動」のどれかを書く。画面・遷移・通信は PlayMode のテストで確かめる（ADR 0024 へのリンク `../decisions/0024-verification-first-reviews.md`。基盤は計画へのリンク `../../plans/2026-11-16-client-playmode-tests.md`）」に書き換える
- `plans/README.md` 5.1: client の行を「`unity recompile`、EditMode のテスト、**PlayMode のテスト**（`Assets/Tests/PlayMode`。回し方は CLAUDE.md）。画面・遷移・通信を触るタスクは PlayMode のテストで確かめる」にし、その下の段落「client の PlayMode のテストは、基盤（…）ができてから必須にする。基盤ができるまでは、…と書く。」を「client の PlayMode のテストは、画面・遷移・通信を触るタスクで必須（基盤は [2026-11-16-client-playmode-tests.md](2026-11-16-client-playmode-tests.md)）。サーバーが要るテストは `[Category("Server")]` にし、5.1 の期待する結果に、ローカルのサーバーを上げることを書く。」に書き換える
- `uta-plan/SKILL.md`: 「（基盤がなければ「未確認（PlayMode の基盤なし）」と書く）」を消す
- `uta-implement/SKILL.md`: 「PlayMode のテスト（基盤があれば）」を「PlayMode のテスト」にする
- `lenses/readiness.md`: 「PlayMode のテストを使うか、基盤がないので「未確認」と書いてあるか」を「PlayMode のテストを使うか（サーバーが要るものは `Server` のカテゴリで、ローカルのサーバーを上げる手順があるか）」にする

## 4. タスク

Editor（`client/uta` を開いたもの）が要る。実装担当が Unity のコマンドを回せないとき（Editor が開いていない・`unity` が無い）は、止まって報告する。

### タスク 1: asmdef とスパイク（PlayMode が Editor を開いたまま回るか）

- 触るファイル: `Uta.Client.Tests.PlayMode.asmdef`、`AssemblyAttributes.cs`、`PlayModeHarnessTests.cs`（条件つきで `ProjectSettings.asset`）
- 参照: 3.1・3.4・3.7
- 手順:
  0. ファイルを作る前に、EditMode のテスト（unity7-evaluation.md 4章の全文）を回し、`<test-run>` の `total` を実装メモに書く（5.1 のタスク 1 で比べる）
  1. 3.1 の 2 ファイルと 3.4 のファイルを作る
  2. `unity recompile --format json`（エラー 0 を見る）
  3. **Editor を前面にせず**（ほかのウィンドウが前面のまま）、3.7 の手順 1〜4 を `Smoke` で回す
  4. 結果で分ける:
     - `total="1" passed="1"` → 終わり。かかった秒数を実装メモに書く
     - 600 秒でファイルができない、かつ `unity command eval 'return UnityEngine.Application.isPlaying + " " + UnityEngine.Time.frameCount;'` が `True` で `frameCount` が 10 秒あけて 2 回見ても変わらない（背面で止まっている）→ `unity command editor.playmode stop` → `unity command --timeout 120 eval 'UnityEditor.PlayerSettings.runInBackground = true; UnityEditor.AssetDatabase.SaveAssets(); return UnityEditor.PlayerSettings.runInBackground;'`（`True`）→ `git diff --stat ProjectSettings/ProjectSettings.asset` と `git diff ProjectSettings/ProjectSettings.asset` で、変わったのが `runInBackground: 0` → `1` の 1 行だけと確かめる → 手順 3 からやり直す
- 止まる条件（自分で回避せず、出力を添えて報告する）:
  - `runInBackground` を 1 にした後も、背面で止まる
  - `ProjectSettings.asset` の差分に `runInBackground` 以外の行がある
  - Play には入った（`isPlaying` が `True`）が、Play が終わってもファイルができない（コールバックが Play をまたいで消えたとみられる）
  - `total="0"`（テストが見つからない）
  - Play に入ったときに Editor のダイアログ（保存の確認など）で止まった
- 確かめ方: 5.1 のタスク 1

### タスク 2: 補助と、起動 → タイトルのスモーク

- 触るファイル: `Support/TestWait.cs`、`Support/TestScenes.cs`、`BootToTitleTests.cs`
- 参照: 3.1・3.2・3.3・3.5・3.7
- 手順: 3.2・3.3・3.5 のとおりに作る → `unity recompile --format json` → 3.7 の手順 1〜4 を `Smoke` で回す
- 境界: `CS0012` が出たら 3.1 の注のとおり `references` を足す。それ以外のコンパイルエラーは止まって報告する
- 止まる条件: `Boot_FromBootstrapScene_ReachesTitlePage` が失敗し、メッセージが timeout でもエラーのログでもない（例外）とき。timeout・エラーのログで失敗したときも、`unity command console get --tail 50 --level error --format json` の出力を添えて報告する（製品のコードは直さない）
- 確かめ方: 5.1 のタスク 2

### タスク 3: タイトル → ホーム（ローカルのサーバー）

- 触るファイル: `TitleToHomeTests.cs`
- 参照: 3.3・3.6・3.7
- 前提: 3.9 の手順 1 でローカルのサーバーを上げる。タスクの終わりに 3.9 の手順 2 で止める
- 参照（追加）: 3.9
- 手順: 3.6 のとおりに作る → `unity recompile --format json` → 3.7 の手順 1〜4 を**絞り込みなし**（全部）で回す
- 止まる条件: `Start_WithLocalServer_ReachesHomePage` が `Title: 接続に失敗しました…` で落ちたとき（サーバー側か、アカウントのファイル `account-http___localhost_5000.dat` の問題。消すかどうかはユーザーに聞く）。2 本目以降のテストが USN の例外（`UnityScreenNavigator` の名前空間の例外）で落ちたとき（同じ Play の中で Bootstrap を読み直すと、USN の static に破棄済みのものが残るとみられる。回避策は書かれていないので報告する）
- 確かめ方: 5.1 のタスク 3

### タスク 4: ドキュメント（進行役が行う）

- 触るファイル: 2 の表のドキュメントの行すべて
- 参照: 3.7・3.8・3.9
- 手順: 3.9 の末尾の CLAUDE.md の 1 行と、3.8 の各項のとおりに書き換える。unity7-evaluation.md には、タスク 1〜3 の実装メモ（秒数・`runInBackground` を変えたか・足した `references`）を反映する
- 確かめ方: 5.1 のタスク 4

## 5. 動作検証

### 5.1 タスクごとの確かめ方

コマンドはすべて `client/uta` で実行する（Lint はリポジトリのルート）。「3.7 Smoke」は 3.7 の手順 1〜4 を `categoryNames = new[] { "Smoke" }` で、「3.7 全部」は絞り込みなしで回すこと。

| タスク | コマンド | 期待する結果 |
|---|---|---|
| 1 | `unity recompile --format json` | エラー 0 |
| 1 | 3.7 Smoke（Editor は背面のまま） | `total="1" passed="1" failed="0"`（`PlayModeHarnessTests.Frames_Advance`） |
| 1 | EditMode のテスト（unity7-evaluation.md 4章の全文。結果は `Temp/uta-test-results.xml`） | 失敗 0。`total` がタスク 1 の手順 0 で書いた値と同じ（PlayMode のテストが EditMode に混ざらない） |
| 2 | `unity recompile --format json` | エラー 0 |
| 2 | 3.7 Smoke | `total="2" passed="2" failed="0"` |
| 3 | `unity recompile --format json` | エラー 0 |
| 3 | 3.7 全部（3.9 の手順 1 でサーバーを上げて） | `total="3" passed="3" failed="0"` |
| 3 | 3.7 Smoke（サーバーを上げたまま） | `total="2" passed="2"`（`Server` が絞り込みで外れる） |
| 4 | `python -I .claude/skills/linting-wiki/lint_wiki.py` | 終了コード 0 |

### 5.2 足すテストケース

| テスト | カテゴリ | 確かめること | タスク |
|---|---|---|---|
| `PlayModeHarnessTests.Frames_Advance` | Smoke | PlayMode でフレームが 3 以上進む | 1 |
| `BootToTitleTests.Boot_FromBootstrapScene_ReachesTitlePage` | Smoke | Bootstrap → Title のページが「タップしてスタート」で押せる。起動でエラーのログが出ない | 2 |
| `TitleToHomeTests.Start_WithLocalServer_ReachesHomePage` | Server | スタート → 端末の認証・サインイン → Home のページ。接続の失敗は早めに落ちる | 3 |

境界のテスト（サーバーが止まっているときの失敗表示など）はこの計画では足さない（範囲外。1章）。

### 5.3 受け入れ

全部のタスクの後に、進行役が回す。

1. `dotnet build Uta.slnx`（リポジトリのルート。警告 0・エラー 0）と `unity recompile --format json`（エラー 0）
2. EditMode のテスト（unity7-evaluation.md 4章の全文。失敗 0）
3. 3.9 の手順 1 でローカルのサーバーを上げて、3.7 全部（`total="3" passed="3"`）。続けてもう一度 3.7 全部を回す（2 回目も `passed="3"`。`RequestScriptReload` で前の Play の static が残らないこと）
4. 3.9 の手順 2 でサーバーを止めて、3.7 Smoke を回す（`total="2" passed="2"`。Smoke がサーバーに依らないこと）
5. `python -I .claude/skills/linting-wiki/lint_wiki.py`（終了コード 0）
6. ユーザーに頼む手順（PlayMode のテストで確かめられない、手の Play との共存）: Editor で `Bootstrap` シーンを開いて Play → タイトルで「タップしてスタート」→ ホームが出る → Play を止める。テストが同じアカウントのファイルを使っても、手の Play でサインインできること

コードレビューは受け入れが通ってから出す（`requesting-code-review`。レイヤーは client）。

## 6. 未決事項

なし。

## 7. 実装メモ

（実装を始めるときに足す）
