---
title: 実装計画: Android の実機のスモークと IL2CPP の計算の一致（MVP の完了条件 7）
status: draft
updated: 2026-11-16
spec: ../wiki/overview/game-spec.md, ../wiki/architecture/build-ci.md, ../wiki/architecture/lesson-phases.md, ../wiki/architecture/produce-domain.md
---

# 実装計画: Android の実機のスモークと IL2CPP の計算の一致（MVP の完了条件 7）

## この計画で決めたこと

設計の正は [build-ci.md](../wiki/architecture/build-ci.md)（3〜5章）。下はそこに無い、計画を書くときに決めたこと。

1. **最初のタスクをスパイクにする**: 今のアプリをそのまま Android の IL2CPP でビルドし、実機でスモーク（起動 → ログイン → ガチャ 1 回）まで通す。Unity 7 Alpha の Android のビルド、YetAnotherHttpHandler のネイティブ、`adb reverse` での平文の HTTP/2、MasterMemory・VContainer・MessagePack の実機での動作は、どれも確かめていないため。ここで止まれば、計算の一致（タスク 2〜4）の置き場を Windows に替える（[build-ci.md](../wiki/architecture/build-ci.md) 5.3）。タスク 1 の「止まる条件」に当たったら、回避の実装を書かずに止まって報告する
2. **タスク 2（shared の .NET だけ）はタスク 1 を待たない**。Unity を開かずに終わる。タスク 3 以降はタスク 1・2 の後
3. スモークの端末の操作はユーザーが行う（実装担当は端末を操作できない）。実装担当は、ビルド・`adb install`・`adb reverse`・logcat の取り込みまでを行い、操作をユーザーに頼む
4. 実機の Test Runner の結果は、実装担当が結果の XML で確かめる（端末の操作は要らない。端末をつないでおくことだけユーザーに頼む）
5. `LessonReplayExpected` の期待値は、.NET のテストの失敗の出力から写す（3.4。手で書かない）
6. 新しい asmdef `Uta.Client.Tests.SharedDomainReplay` には LINQ を書かないので、ZLinq の `AssemblyAttributes.cs` を置かない（CLAUDE.md の ZLinq の決まりは LINQ を書くファイルのため。参照も [lesson-phases.md](../wiki/architecture/lesson-phases.md) 8章の 3 つに限る）
7. ビルド用の worktree `../Uta-wt/build-android` は、`tools/worktree-pool.sh` の番号の worktree にしない（`build-foundation` と同じく、ビルドだけに使う detached の worktree）。`git worktree add --detach` で作る

8. **実機からローカルのサーバーへは `adb reverse` でつなぐ**（2026-11-16。ユーザーの判断。クラウドの dev 環境は使わない。Claude の推奨どおり）。USB でつなぎ `adb reverse tcp:5000 tcp:5000`。コードは変えない。[repository-layout.md](../wiki/architecture/repository-layout.md) 11.1 の「PC の LAN の IP を使い、ポートを公開する」を置き換えた。採らなかった案: LAN の IP と起動引数（Unity が `-e unity` の引数を `Environment.GetCommandLineArgs` に出すか未確認。Kestrel の公開とファイアウォールが要る）、ビルド時に埋める（IP が変わるたびにビルドし直し）
9. **テストのカードを `shared/Uta.Shared.Domain.Testing` に移し、.NET と実機で共有する**（2026-11-16。ユーザーの「進めて」で、Claude の推奨どおり）。[lesson-phases.md](../wiki/architecture/lesson-phases.md) 8章の「asmdef の参照は `Uta.Shared.Domain` と Unity Test Framework だけ」[2026-11-01。ユーザーの判断] に `Uta.Shared.Domain.Testing` を足す。採らなかった案: Unity 側に写す（約 400 行の二重の定義。写しのずれと IL2CPP の不一致を見分けられない）
10. **Android のビルドは専用の worktree `../Uta-wt/build-android` で回す**（2026-11-16。ユーザーの「進めて」で、Claude の推奨どおり）。採らなかった案: `build-foundation` を使い回す（Windows と行き来するたびに再インポート）
11. **実機の有無は、タスク 1 の手順 5 で確かめる**（2026-11-16。Claude が決めた）。つながらなければ止まる条件に当たり、Windows に代えるか（4.4）をユーザーが決める

## ユーザーの確認が要ること

なし（2026-11-16 に 1〜3 を解決し、上の 8〜10 に移した。実機の有無は 11）

## 1. 目的と前提

- 何を実装するか: MVP の完了条件 7（[game-spec.md](../wiki/overview/game-spec.md)「完了条件」）。Android の実機（IL2CPP）で起動 → ログイン → ガチャ 1 回のスモークを通し、IL2CPP で `Uta.Shared.Domain` の計算が .NET と一致することを、Unity Test Runner の実機実行で確かめる。手順と決めごとは [build-ci.md](../wiki/architecture/build-ci.md) 3〜5章、比べ方は [lesson-phases.md](../wiki/architecture/lesson-phases.md) 8章・[produce-domain.md](../wiki/architecture/produce-domain.md) 7章
- 関係する ADR: [ADR 0006](../wiki/decisions/0006-adopt-unity-7-early.md)（Unity 7 を早期に触る）、[ADR 0008](../wiki/decisions/0008-shared-domain-and-onion.md)（Shared.Domain。本番のクライアントはシードを持たない。実機の一致の確認は CI で回さない）
- 前提: ガチャの画面と通信は `main` にある（順 7 の gacha）。画面の残り（formation・week・lesson）は待たない
- **決めないこと（範囲外）**:
  - iOS の実機ビルド、CI、dev 環境（Cloud Run）への接続（[build-ci.md](../wiki/architecture/build-ci.md) 6章）
  - プロデュースの状態（`ProduceStateComparer`）の IL2CPP の一致。決定はレッスンの状態（`LessonState`）だけ（lesson-phases.md 8章）
  - Managed Stripping の段階を変えること・`link.xml`。今の既定のまま。実機で型が落ちたら止まる条件（タスク 1）
  - 署名用のキーストア、ストア向けの AAB
  - 実機での画面の見た目の確認（豆腐・レイアウト）。スモークで目に入った崩れは実装メモに書くだけで、直さない

## 2. 触るファイル

パスはリポジトリのルートから。`client/uta/Assets/` は `Assets/` と略す。

| 操作 | パス | 中身 | タスク |
|---|---|---|---|
| 変更 | `Assets/Scripts/Editor/BuildCommands.cs` | `BuildAndroidIl2Cpp` を足す（3.1） | 1 |
| 変更 | `client/uta/ProjectSettings/ProjectSettings.asset` | Android のパッケージ名、スタンドアロンのスクリプトバックエンド（3.2） | 1 |
| 作成 | `shared/Uta.Shared.Domain.Testing/Uta.Shared.Domain.Testing.csproj` | 3.3 | 2 |
| 作成 | `shared/Uta.Shared.Domain.Testing/package.json` | 3.3 | 2 |
| 作成 | `shared/Uta.Shared.Domain.Testing/Uta.Shared.Domain.Testing.asmdef` | 3.3 | 2 |
| 移動 | `shared/Uta.Shared.Domain.Tests/Lesson/{TestCards,Phase1Cards,Phase2Cards,Phase3Cards,Phase4Cards,SyntheticCards,TestCardCatalog}.cs` → `shared/Uta.Shared.Domain.Testing/Lesson/` | `git mv`。中身は変えない（名前空間 `Uta.Shared.Domain.Tests.Lesson` のまま） | 2 |
| 作成 | `shared/Uta.Shared.Domain.Testing/Lesson/LessonReplayScenario.cs` | 3.4 | 2 |
| 作成 | `shared/Uta.Shared.Domain.Testing/Lesson/LessonReplayExpected.cs` | 3.4 | 2 |
| 変更 | `shared/Uta.Shared.Domain.Tests/Uta.Shared.Domain.Tests.csproj` | `Uta.Shared.Domain.Testing` の ProjectReference を足す | 2 |
| 作成 | `shared/Uta.Shared.Domain.Tests/Lesson/LessonReplayExpectedTests.cs` | 5章 | 2 |
| 変更 | `Uta.slnx` | `/shared/` に `Uta.Shared.Domain.Testing.csproj` を足す | 2 |
| 変更 | `client/uta/Packages/manifest.json` | `com.uta.shared.domain.testing` を足す（3.5） | 3 |
| 作成（Unity が作る） | `shared/Uta.Shared.Domain.Testing/` の `.meta`（フォルダ・`.cs`・`.asmdef`・`.csproj`・`package.json`） | Unity が取り込んだときに作る。全部コミットする | 3 |
| 作成 | `Assets/Tests/SharedDomainReplay/Uta.Client.Tests.SharedDomainReplay.asmdef` | 3.5 | 3 |
| 作成 | `Assets/Tests/SharedDomainReplay/LessonReplayDeviceTests.cs` | 3.5 | 3 |
| 作成（Unity が作る） | `Assets/Tests/SharedDomainReplay.meta` と、フォルダの中の `.meta` | コミットする | 3 |
| 変更 | `uta_docs/wiki/architecture/unity7-evaluation.md`・`build-ci.md`・`repository-layout.md`（8章）、`CLAUDE.md`（ビルド・テスト・実行） | 結果とコマンドの記録（タスク 5） | 5 |

`ProjectSettings.asset` は Editor が書く（3.2）。手で書かない。

## 3. 型とシグネチャ

### 3.1 `BuildCommands.BuildAndroidIl2Cpp`

`BuildWindowsIl2Cpp` の下に足す。`using UnityEditor;` などは今のファイルのまま足りる。

```csharp
/// <summary>Android の IL2CPP の APK を Build/Android/Uta.apk に作る。-batchmode -buildTarget Android -executeMethod Uta.Client.Editor.BuildCommands.BuildAndroidIl2Cpp で呼ぶ。</summary>
public static void BuildAndroidIl2Cpp()
{
    // Addressables の内容は今のビルドターゲット向けに作られるため、ターゲットが違えば止める
    if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
    {
        Debug.LogError("[Uta] build: active target is " + EditorUserBuildSettings.activeBuildTarget);
        EditorApplication.Exit(1);
        return;
    }

    AddressableAssetSettings.BuildPlayerContent(out var result);
    if (!string.IsNullOrEmpty(result.Error))
    {
        Debug.LogError("[Uta] addressables: " + result.Error);
        EditorApplication.Exit(1);
        return;
    }

    PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
    PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
    EditorUserBuildSettings.buildAppBundle = false;
    var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
    {
        scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
        locationPathName = "Build/Android/Uta.apk",
        target = BuildTarget.Android,
        options = BuildOptions.None,
    });
    EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
}
```

### 3.2 `ProjectSettings.asset`

Editor を開いた worktree で、`client/uta` から次を 1 回実行する（Editor が `ProjectSettings.asset` を書く）。

```
unity command eval 'UnityEditor.PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.chinpangx.uta"); UnityEditor.PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, UnityEditor.ScriptingImplementation.IL2CPP); UnityEditor.AssetDatabase.SaveAssets(); return "ok";'
```

コミットするのは、`ProjectSettings.asset` の `applicationIdentifier` の `Android:` の行と、`scriptingBackend` に足された `Standalone: 1` の行だけ。ほかの行の差分（Editor が一緒に書き換えたもの）は `git restore -p` で戻す。`ProjectSettings.asset` を開いて差分が 2 か所でなければ、実装メモに書く。

### 3.3 `Uta.Shared.Domain.Testing` のプロジェクト

`Uta.Shared.Domain.Testing.csproj`（`shared/Directory.Build.props` が `bin~/`・`obj~/` を当てる）:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <!-- テストのカード・再生のシナリオと期待値。.NET のテストと Unity の実機のテストが使う（build-ci.md 5.2）。Unity にソースで入るため、netstandard2.1・C# 9 -->
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <LangVersion>9.0</LangVersion>
    <Nullable>disable</Nullable>
    <RootNamespace>Uta.Shared.Domain.Testing</RootNamespace>
    <IsPackable>false</IsPackable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Uta.Shared.Domain\Uta.Shared.Domain.csproj" />
  </ItemGroup>

</Project>
```

`package.json`:

```json
{
  "name": "com.uta.shared.domain.testing",
  "version": "0.1.0",
  "displayName": "Uta Shared Domain Testing",
  "description": "Uta の Shared.Domain のテストのカードと、IL2CPP の計算の一致を確かめる再生のシナリオ・期待値。テストを含むビルドでだけ使う。",
  "unity": "7000.0"
}
```

`Uta.Shared.Domain.Testing.asmdef`:

```json
{
    "name": "Uta.Shared.Domain.Testing",
    "rootNamespace": "Uta.Shared.Domain.Testing",
    "references": [
        "Uta.Shared.Domain"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": true
}
```

`Uta.Shared.Domain.Tests.csproj` の ProjectReference の `ItemGroup` に 1 行足す:

```xml
<ProjectReference Include="..\Uta.Shared.Domain.Testing\Uta.Shared.Domain.Testing.csproj" />
```

`Uta.slnx` の `/shared/` の `Uta.Shared.Domain.csproj` の行の次に足す:

```xml
<Project Path="shared/Uta.Shared.Domain.Testing/Uta.Shared.Domain.Testing.csproj" />
```

`Uta.Server.Tests` は `Uta.Shared.Domain.Tests` を参照しているので、移したカードは推移的に見える。`server/` は変えない。

### 3.4 再生のシナリオと期待値

名前空間はどちらも `Uta.Shared.Domain.Tests.Lesson`（移したカードと同じ。[build-ci.md](../wiki/architecture/build-ci.md) 5.2）。BOM 付き UTF-8・LF・Allman・C# 9。

`LessonReplayScenario.cs`:

```csharp
using System;
using System.Linq;
using Uta.Shared.Domain.Lesson;
using Uta.Shared.Domain.Rng;

namespace Uta.Shared.Domain.Tests.Lesson
{
    /// <summary>
    /// IL2CPP の計算の一致を確かめる再生（lesson-phases.md 8章、build-ci.md 5.2）。
    /// フェーズ 1〜4 の 36 枚を 1 枚ずつ入れたデッキで、使えるカードがあれば手札の先頭から使い、なければスキップする。
    /// </summary>
    public static class LessonReplayScenario
    {
        public const int NormalHp = 30;
        public const ulong NormalSeed = 1;
        public const int ExamHp = 40;
        public const ulong ExamSeed = 2;

        /// <summary>通常のレッスン（リズム・7 ターン・クリア 1000・パーフェクト 2000）。HP 30、シード 1、attempt 0。</summary>
        public static LessonState RunNormal()
        {
            return Run(new LessonSetup(LessonKind.Normal, ParamKind.Rhythm, 7, 1000, 2000), NormalHp, NormalSeed);
        }

        /// <summary>中間試験（7 ターン。ボーナス 100・120・150・200・100・150・200%、歌・リズム・表現の順に繰り返す。早期終了 300）。HP 40、シード 2、attempt 0。</summary>
        public static LessonState RunExam()
        {
            var percents = new[] { 100, 120, 150, 200, 100, 150, 200 };
            var kinds = new[] { ParamKind.Singing, ParamKind.Rhythm, ParamKind.Expression };
            var turns = percents.Select((p, i) => new ExamTurn(kinds[i % 3], p)).ToArray();
            var setup = new LessonSetup(LessonKind.MidExam, ParamKind.Nothing, 7, 0, 0, new ExamSetup(turns, 300));
            return Run(setup, ExamHp, ExamSeed);
        }

        private static LessonState Run(LessonSetup setup, int hp, ulong seed)
        {
            var engine = new LessonEngine(new TestCardCatalog());
            var cardIds = Phase1Cards.AllIds
                .Concat(Phase2Cards.AllIds)
                .Concat(Phase3Cards.AllIds)
                .Concat(Phase4Cards.AllIds)
                .ToArray();
            var deck = cardIds.Select((cardId, index) => new CardInstance(index + 1, cardId, false)).ToList();
            var state = new LessonState(setup, deck, hp, hp);
            var random = RandomStreams.Create(seed, new RandomStreamKey(RandomStreamKind.Lesson, 3, 0));

            engine.Start(state, random);
            while (!state.IsFinished)
            {
                var usable = state.Zones.Hand.FirstOrDefault(id => engine.CanUse(state, id) == UseCheckResult.Ok);
                var command = usable != 0 ? LessonCommand.UseCard(usable) : LessonCommand.Skip();
                var result = engine.Apply(state, command, random);
                if (result != LessonApplyResult.Accepted)
                {
                    throw new InvalidOperationException("再生の操作が受け付けられませんでした: " + result);
                }
            }

            return state;
        }
    }
}
```

- 値（セットアップ・HP・シード・`RandomStreamKey` の週 3・attempt 0）は今の `LessonReplayTests` の `Replay_SameSeedAndCommands_SameResult`（通常）と `Replay_Exam_SameSeedAndCommands_SameResult`（試験）と同じ。`LessonReplayTests` は変えない

`LessonReplayExpected.cs`:

```csharp
using System;
using System.Collections.Generic;
using Uta.Shared.Domain.Lesson;

namespace Uta.Shared.Domain.Tests.Lesson
{
    /// <summary>
    /// <see cref="LessonReplayScenario"/> の最後の状態を .NET で再生した値（LessonStateComparer.Describe の行）。
    /// IL2CPP の実機のテストがこれと比べる。値は LessonReplayExpectedTests の失敗の出力から写す（手で書かない）。
    /// </summary>
    public static class LessonReplayExpected
    {
        // タスク 2 の手順 5 で、new string[0] を new[] { ... }（LessonReplayExpectedTests の失敗の出力）に置き換える
        public static IReadOnlyList<string> Normal { get; } = new string[0];

        public static IReadOnlyList<string> Exam { get; } = new string[0];

        /// <summary>期待値と状態を項目ごとに比べ、食い違った項目だけを返す。同じなら空。</summary>
        public static IReadOnlyList<string> Compare(IReadOnlyList<string> expected, LessonState actual)
        {
            if (expected == null)
            {
                throw new ArgumentNullException(nameof(expected));
            }

            var actualLines = LessonStateComparer.Describe(actual);
            var count = Math.Max(expected.Count, actualLines.Count);
            var differences = new List<string>();
            for (var i = 0; i < count; i++)
            {
                var e = i < expected.Count ? expected[i] : "(なし)";
                var a = i < actualLines.Count ? actualLines[i] : "(なし)";
                if (!string.Equals(e, a, StringComparison.Ordinal))
                {
                    differences.Add("[" + i + "] 期待 " + e + " / 実際 " + a);
                }
            }

            return differences;
        }

        /// <summary>状態を、このファイルに写せる C# の文字列の並びにする（期待値を作り直すとき用）。</summary>
        public static string ToCSharpLines(LessonState state)
        {
            var lines = LessonStateComparer.Describe(state);
            var builder = new System.Text.StringBuilder();
            foreach (var line in lines)
            {
                builder.Append("            \"").Append(line.Replace("\\", "\\\\").Replace("\"", "\\\"")).Append("\",\n");
            }

            return builder.ToString();
        }
    }
}
```

- `Compare` は `LessonStateComparer.Diff`（状態どうし）と同じ比べ方を、期待値が文字列のときに使うもの。`LessonStateComparer` は変えない

### 3.5 Unity 側

`client/uta/Packages/manifest.json` の `"com.uta.shared.contracts"` の行の次に足す:

```json
"com.uta.shared.domain.testing": "file:../../../shared/Uta.Shared.Domain.Testing",
```

`Assets/Tests/SharedDomainReplay/Uta.Client.Tests.SharedDomainReplay.asmdef`（PlayMode のテスト。`includePlatforms` は空）:

```json
{
    "name": "Uta.Client.Tests.SharedDomainReplay",
    "rootNamespace": "Uta.Client.Tests.SharedDomainReplay",
    "references": [
        "UnityEngine.TestRunner",
        "Uta.Shared.Domain",
        "Uta.Shared.Domain.Testing"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

`Assets/Tests/SharedDomainReplay/LessonReplayDeviceTests.cs`（C# 9。LINQ を使わない）:

```csharp
using NUnit.Framework;
using Uta.Shared.Domain.Tests.Lesson;

namespace Uta.Client.Tests.SharedDomainReplay
{
    /// <summary>
    /// IL2CPP でビルドした実行物で再生し、.NET で再生した状態と項目ごとに比べる（lesson-phases.md 8章、build-ci.md 5.2）。
    /// Editor（Mono）で通っても IL2CPP の確認にはならない。
    /// </summary>
    public sealed class LessonReplayDeviceTests
    {
        [Test]
        public void NormalLesson_SameAsDotNet()
        {
            var differences = LessonReplayExpected.Compare(LessonReplayExpected.Normal, LessonReplayScenario.RunNormal());

            Assert.That(differences, Is.Empty, string.Join("\n", differences));
        }

        [Test]
        public void ExamLesson_SameAsDotNet()
        {
            var differences = LessonReplayExpected.Compare(LessonReplayExpected.Exam, LessonReplayScenario.RunExam());

            Assert.That(differences, Is.Empty, string.Join("\n", differences));
        }
    }
}
```

## 4. タスク

### タスク 1: Android のビルドとスモーク（スパイク）

触るファイル: `BuildCommands.cs`、`ProjectSettings.asset`。

手順:

1. Editor を開いた worktree（実装の worktree）で 3.1 を足し、`client/uta` で `unity recompile --format json` がエラー 0 になることを確かめる。3.2 の `eval` を実行し、差分を 2 か所にする。コミットする
2. ビルド用の worktree を作る（初回だけ。既にあれば 3 へ）: リポジトリのルートで `git worktree add --detach ../Uta-wt/build-android HEAD` → 実装の worktree の `client/uta/Assets/Packages/` を `../Uta-wt/build-android/client/uta/Assets/Packages/` にコピーする（`cp -r`）
3. ビルド用の worktree を実装のコミットにそろえる: `git -C ../Uta-wt/build-android checkout --detach <実装の worktree の HEAD のコミット>`
4. ビルドする（`run_in_background`。初回は Library の作成と Android のインポートで長い）:
   `"/c/Program Files/Unity/Hub/Editor/7000.0.0a12/Editor/Unity.exe" -batchmode -nographics -projectPath ../Uta-wt/build-android/client/uta -buildTarget Android -executeMethod Uta.Client.Editor.BuildCommands.BuildAndroidIl2Cpp -logFile <スクラッチパッド>/android-build.log`
   終了コード 0 と `../Uta-wt/build-android/client/uta/Build/Android/Uta.apk` ができたことを確かめる
5. ユーザーに、端末を USB でつなぎ USB デバッグを許可するよう頼む。`"$ADB" devices -l` に端末が `device` で出るのを確かめる（`ADB` は [build-ci.md](../wiki/architecture/build-ci.md) 3章）
6. ローカルのサーバーを起動する（CLAUDE.md「ビルド・テスト・実行」のローカルのサーバーの手順。`migrate` を含む）
7. `"$ADB" install -r .../Uta.apk`、`"$ADB" shell pm clear com.chinpangx.uta`、`"$ADB" reverse tcp:5000 tcp:5000`、`"$ADB" logcat -c`。logcat を `"$ADB" logcat -s Unity > <スクラッチパッド>/android-logcat.txt` で背面で取り始める
8. ユーザーに、[build-ci.md](../wiki/architecture/build-ci.md) 5.1 の手順 4・5（起動 → タイトル → ホーム → ガチャ → 単発 1 回 → 結果 → 戻る、終了して起動し直してホーム）を頼み、終わったら知らせてもらう
9. logcat を止め、5.1 の「通ったとみなすもの」を確かめる。端末の機種名・Android の版（`"$ADB" shell getprop ro.product.model`・`ro.build.version.release`）を実装メモに書く

止まる条件（当たったら、回避の実装を書かずに止まり、ログの該当箇所を添えて報告する。代えるかはユーザーが決める。[build-ci.md](../wiki/architecture/build-ci.md) 5.3）:

- 手順 4 で、Uta のコードや設定ではなく Unity の Android のビルド（Gradle・`AndroidPlayerBuildProgram`・IL2CPP の arm64・NDK）が失敗する
- 手順 4 で、Uta のコードや設定が原因で失敗し、直すのに計画にない設計判断が要る（Managed Stripping・`link.xml`・パッケージの版を変えるなど）
- 手順 8 で、起動しない・通信できない（`Unavailable`・平文の通信の拒否）・型が見つからない（`MissingMethodException`・`ExecutionEngineException`・VContainer の解決の失敗）・マスタを読めない
- 手順 5 で端末がつながらない（ユーザーに端末が無いときを含む）

境界: アプリのデータを消してから始める（1 回目はサインアップの経路、5.1 の手順 5 がサインインの経路）。ガチャ通貨が足りない状態にはならない（初期付与で引ける）。

終わったことの確かめ方: 手順 4 の終了コード 0 と APK、手順 9 の条件。結果は実装メモに書く（wiki への記録はタスク 5）。

### タスク 2: shared のテスト用プロジェクトと期待値（.NET だけ）

触るファイル: 2章のタスク 2 の行。Unity を開かない。

手順:

1. `shared/Uta.Shared.Domain.Testing/` を作り、3.3 の 3 ファイルを置く
2. `git mv` で 7 ファイルを `shared/Uta.Shared.Domain.Testing/Lesson/` に移す（中身は変えない）。3.4 の 2 ファイルを置く（`LessonReplayExpected` の `Normal`・`Exam` は 3.4 のとおり `new string[0]`）
3. `Uta.Shared.Domain.Tests.csproj`・`Uta.slnx` を 3.3 のとおり直す。5章の `LessonReplayExpectedTests.cs` を置く
4. `dotnet build Uta.slnx` が警告 0・エラー 0 になることを確かめる。移したカードが C# 9 で通らない書き方を含んでいたら、止まって報告する（今のファイルは C# 9 の範囲と見込んでいる）
5. `dotnet test --project shared/Uta.Shared.Domain.Tests` を実行する。`LessonReplayExpectedTests` の 2 件が落ち、失敗の出力に `ToCSharpLines` の行が出る。それを `LessonReplayExpected` の `Normal`・`Exam` の `new[] { ... }` の中に写す（`new string[0]` を置き換える）
6. もう一度 `dotnet test --project shared/Uta.Shared.Domain.Tests` と `dotnet test --project server/Uta.Server.Tests` を実行し、全部が通ることを確かめる（サーバーのテストは結合テストに Docker が要る。Docker が動いていなければ、Docker Desktop の起動をユーザーに頼む）

境界: 期待値の行数は、`LessonStateComparer.Describe` の行数（状態しだい）。手で数を合わせない。

終わったことの確かめ方: 手順 4・6。

### タスク 3: Unity に取り込み、Editor で通す

触るファイル: 2章のタスク 3 の行。タスク 1・2 の後。

手順:

1. `manifest.json` に 3.5 の行を足し、`client/uta` で `unity command package resolve`。Unity が `shared/Uta.Shared.Domain.Testing/` の `.meta` を作ったことを確かめる（`git status` に出る）
2. `Assets/Tests/SharedDomainReplay/` に 3.5 の 2 ファイルを置き、`unity recompile --format json` がエラー 0 になることを確かめる
3. Editor の PlayMode でこの asmdef のテストを回す。CLAUDE.md「ビルド・テスト・実行」の EditMode のコマンドの `Filter` を `new UnityEditor.TestTools.TestRunner.Api.Filter { testMode = UnityEditor.TestTools.TestRunner.Api.TestMode.PlayMode, assemblyNames = new[] { "Uta.Client.Tests.SharedDomainReplay" } }` に替えて実行し、`Temp/uta-test-results.xml` で 2 件が `Passed` であることを確かめる。2 分たってもファイルができなければ、Editor が背面で Play が進んでいない（[unity7-evaluation.md](../wiki/architecture/unity7-evaluation.md) 4章）ので、ユーザーに Editor を前面に出してもらう
4. EditMode の全件も CLAUDE.md のコマンドで回し、増減が無い（失敗 0）ことを確かめる
5. `.meta` を含めてコミットする

境界: PlayMode の確認は Mono なので、IL2CPP の一致の確認にはならない（テストのコードと asmdef の確認だけ）。

終わったことの確かめ方: 手順 2〜4。

### タスク 4: 実機の Test Runner で IL2CPP の一致を確かめる

触るファイル: なし（実行だけ）。タスク 3 の後。端末をつないでおく。

手順:

1. タスク 1 の手順 3 のとおり、`build-android` の worktree をタスク 3 のコミットにそろえる（`Assets/Packages/` はタスク 1 でコピー済み）
2. 端末が `"$ADB" devices` に `device` で出ていることを確かめる
3. 実行する（`run_in_background`）:
   `"/c/Program Files/Unity/Hub/Editor/7000.0.0a12/Editor/Unity.exe" -batchmode -projectPath ../Uta-wt/build-android/client/uta -buildTarget Android -runTests -testPlatform Android -assemblyNames Uta.Client.Tests.SharedDomainReplay -testResults <スクラッチパッドの絶対パス>/android-replay.xml -logFile <スクラッチパッド>/android-replay.log`
4. 結果の XML の `<test-run>` が `total="2" passed="2" failed="0"` であることを確かめる。失敗があれば、`<message>` の食い違った項目（`[i] 期待 … / 実際 …`）を実装メモに写して止まる（コードを直さない。IL2CPP の不一致は報告して、直し方はユーザーと決める）

止まる条件: 手順 3 が、テストの結果ではなく Test Runner の仕組み（プレイヤーのビルド・インストール・結果の受け取り）で失敗する、または 15 分たっても結果のファイルができない。このときは、ログの該当箇所を添えて報告する。ユーザーが Windows に代えると決めたら、4.4 の手順で行う。

### 4.4 Windows に代えるとき（タスク 1 の止まる条件、またはタスク 4 の止まる条件に当たり、ユーザーが代えると決めたとき）

1. `git -C ../Uta-wt/build-foundation checkout --detach <タスク 3 のコミット>`。実装の worktree の `client/uta/Assets/Packages/` を、`build-foundation` の同じ場所へコピーし直す
2. `"/c/Program Files/Unity/Hub/Editor/7000.0.0a12/Editor/Unity.exe" -batchmode -projectPath ../Uta-wt/build-foundation/client/uta -buildTarget StandaloneWindows64 -runTests -testPlatform StandaloneWindows64 -assemblyNames Uta.Client.Tests.SharedDomainReplay -testResults <スクラッチパッドの絶対パス>/windows-replay.xml -logFile <スクラッチパッド>/windows-replay.log`。スタンドアロンのスクリプトバックエンドはタスク 1 で IL2CPP にしてある（ログに `il2cpp` のビルドが出ることを確かめる）。結果はタスク 4 の手順 4 と同じに見る
3. スモークは、CLAUDE.md の `BuildWindowsIl2Cpp` で作った `Uta.exe` を引数なしで起動し、[build-ci.md](../wiki/architecture/build-ci.md) 5.1 の手順 4・5 をユーザーに頼む

### タスク 5: 結果を wiki と CLAUDE.md に記録する

触るファイル: 2章のタスク 5 の行。

手順:

1. [unity7-evaluation.md](../wiki/architecture/unity7-evaluation.md) 2章の表に、Android の IL2CPP のビルド・スモーク・IL2CPP の計算の一致の行を足す（結果・方法・日付・端末）。3.3 の「Android / iOS の実機ビルド」を、Android は済み（または代えた）と直す。止まる条件に当たったものは 3章に節を足す
2. [repository-layout.md](../wiki/architecture/repository-layout.md) 8章の表の「未確認: Android」を、確かめた結果に直す（`file:` 取り込み・MasterMemory のバイナリ・UniTask・VContainer の行）
3. [build-ci.md](../wiki/architecture/build-ci.md) の 3章の「確かめていないもの」を結果に直し、ずれ（コマンド・手順の違い）を直す
4. CLAUDE.md「ビルド・テスト・実行」に、Android の IL2CPP のビルド（タスク 1 の手順 4 のコマンド）と、実機の Test Runner（タスク 4 の手順 3 のコマンド）を足す。詳細は build-ci.md へのリンクにする
5. [game-spec.md](../wiki/overview/game-spec.md) の「実装計画の単位と順」の 8 の行を **done** にし、この計画を `status: done` にする

終わったことの確かめ方: `python -I .claude/skills/linting-wiki/lint_wiki.py` がエラー 0。

## 5. テスト

| テスト | 置き場 | 確かめること | 回し方 |
|---|---|---|---|
| `LessonReplayExpectedTests.Normal_MatchesScenario` | `shared/Uta.Shared.Domain.Tests/Lesson/` | `LessonReplayExpected.Compare(LessonReplayExpected.Normal, LessonReplayScenario.RunNormal())` が空 | `dotnet test --project shared/Uta.Shared.Domain.Tests` |
| `LessonReplayExpectedTests.Exam_MatchesScenario` | 同上 | 試験で同じ | 同上 |
| `LessonReplayExpectedTests.Compare_DifferentLine_ReportsIndex` | 同上 | 期待値の 1 行を変えた配列（`Normal` を写して 1 行目を `"x"` にしたもの）で、`Compare` が 1 件を返し、`[0]` で始まる | 同上 |
| `LessonReplayExpectedTests.Compare_MissingLine_ReportsNone` | 同上 | 期待値の末尾の 1 行を除いた配列で、`Compare` が 1 件を返し、`/ 実際 ` の後が最後の行で、`期待 (なし)` を含む | 同上 |
| `LessonReplayDeviceTests` の 2 件 | `Assets/Tests/SharedDomainReplay/` | 実機（IL2CPP）で再生した状態が期待値と一致 | Editor の PlayMode（タスク 3。Mono）と、実機の Test Runner（タスク 4） |

`LessonReplayExpectedTests` の上の 2 件は、失敗したとき期待値を写せるよう、メッセージに `ToCSharpLines` の出力を入れる:

```csharp
using Xunit;

namespace Uta.Shared.Domain.Tests.Lesson
{
    /// <summary>IL2CPP の一致の期待値が、今のルールで .NET が再生した値と同じであること（build-ci.md 5.2）。</summary>
    public sealed class LessonReplayExpectedTests
    {
        [Fact]
        public void Normal_MatchesScenario()
        {
            var state = LessonReplayScenario.RunNormal();
            var differences = LessonReplayExpected.Compare(LessonReplayExpected.Normal, state);

            Assert.True(differences.Count == 0, "LessonReplayExpected.Normal に写す:\n" + LessonReplayExpected.ToCSharpLines(state));
        }

        // Exam_MatchesScenario は RunExam・Exam で同じ形
        // Compare_DifferentLine_ReportsIndex・Compare_MissingLine_ReportsNone は上の表のとおり
    }
}
```

`Compare_*` の 2 件は、`LessonReplayExpected.Normal` を `new List<string>(LessonReplayExpected.Normal)` で写してから変える（期待値の本体を書き換えない）。

スモーク（5.1）は手動。Unity・実機でしか確かめられないもの: タスク 1・3・4。

## 6. 未決事項

なし
