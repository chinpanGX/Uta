---
title: 実装計画: Android の実機のスモークと IL2CPP の計算の一致（MVP の完了条件 7）
status: draft
updated: 2026-11-16
spec: ../wiki/overview/game-spec.md, ../wiki/architecture/build-ci.md, ../wiki/architecture/lesson-phases.md, ../wiki/architecture/produce-domain.md
---

# 実装計画: Android の実機のスモークと IL2CPP の計算の一致（MVP の完了条件 7）

## この計画で決めたこと

設計の正は [build-ci.md](../wiki/architecture/build-ci.md)（3〜5章）。下はそこに無い、計画を書くときに決めたこと。

1. **タスクの順番: shared（.NET）→ Unity への取り込み（コンパイルだけ）→ Windows の IL2CPP の Test Runner → Android のビルドとスモーク（スパイク）→ Android の Test Runner → 記録**。Test Runner のプレイヤー実行は Unity 7 a12 で一度も動かしていないので、ビルドの時間が短く Library もある Windows（`build-foundation`）で先に仕組みを確かめる。Windows で通れば、Android で止まったときの代え（[build-ci.md](../wiki/architecture/build-ci.md) 5.3）の材料が先にそろう
2. **Android のビルドとスモーク（タスク 4）はスパイクにし、止まる条件を置く**。Unity 7 Alpha の Android のビルド、YetAnotherHttpHandler のネイティブ、`adb reverse` での平文の HTTP/2、MasterMemory・VContainer・MessagePack の実機での動作は、どれも確かめていないため。止まる条件に当たったら、回避の実装を書かずに止まって報告する。Windows に代えるかはユーザーが決める
3. スモークの端末の操作はユーザーが行う（実装担当は端末を操作できない）。実装担当は、ビルド・`adb install`・`adb reverse`・logcat の取り込みまでを行い、操作をユーザーに頼む
4. 実機の Test Runner の結果は、実装担当が結果の XML とログで確かめる。ユーザーには、端末をつなぎ、画面のロックを外したままにしておくことだけ頼む
5. `LessonReplayExpected` の期待値は、.NET のテストの失敗の出力から写す（3.4。手で書かない）
6. 新しい asmdef `Uta.Client.Tests.SharedDomainReplay` には LINQ を書かないので、ZLinq の `AssemblyAttributes.cs` を置かない（CLAUDE.md の ZLinq の決まりは LINQ を書くファイルのため。参照も [lesson-phases.md](../wiki/architecture/lesson-phases.md) 8章の 3 つに限る）
7. ビルド用の worktree `C:/Work/Uta-wt/build-android` は、`tools/worktree-pool.sh` の番号の worktree にしない（`build-foundation` と同じく、ビルドだけに使う detached の worktree）。`git worktree add --detach` で作り、`git worktree lock` を掛ける。`worktree-pool.sh cleanup` は番号でない worktree を、HEAD が `origin/main` に入っていて変更が無ければ消す（`git worktree remove`。Library は `.gitignore` 済みなので変更に数えない）。ロックした worktree は `git worktree remove` が失敗するので、消されない。`build-foundation` も同じ理由でタスク 3 の初めにロックする
8. **実機からローカルのサーバーへは `adb reverse` でつなぐ**（2026-11-16。ユーザーの判断。クラウドの dev 環境は使わない。Claude の推奨どおり）。USB でつなぎ `adb reverse tcp:5000 tcp:5000`。コードは変えない。[repository-layout.md](../wiki/architecture/repository-layout.md) 11.1 の「PC の LAN の IP を使い、ポートを公開する」を置き換えた。採らなかった案: LAN の IP と起動引数（Unity が `-e unity` の引数を `Environment.GetCommandLineArgs` に出すか未確認。Kestrel の公開とファイアウォールが要る）、ビルド時に埋める（IP が変わるたびにビルドし直し）
9. **テストのカードを `shared/Uta.Shared.Domain.Testing` に移し、.NET と実機で共有する**（2026-11-16。ユーザーの「進めて」で、Claude の推奨どおり）。[lesson-phases.md](../wiki/architecture/lesson-phases.md) 8章の「asmdef の参照は `Uta.Shared.Domain` と Unity Test Framework だけ」[2026-11-01。ユーザーの判断] に `Uta.Shared.Domain.Testing` を足す。採らなかった案: Unity 側に写す（約 400 行の二重の定義。写しのずれと IL2CPP の不一致を見分けられない）
10. **Android のビルドは専用の worktree `C:/Work/Uta-wt/build-android` で回す**（2026-11-16。ユーザーの「進めて」で、Claude の推奨どおり）。採らなかった案: `build-foundation` を使い回す（Windows と行き来するたびに再インポート）
11. **IL2CPP の一致はレッスンの再生だけで確かめる。プロデュース（週の進行・`ProduceCatalog.Create`）は含めない**（2026-11-16。ユーザーの判断。Claude の推奨どおり）。プロデュースのテストのマスタ（`TestProduceMaster`）は `masterdata/csv/` をディスクから読むので、実機に持ち込む仕組みが別に要るため。[produce-domain.md](../wiki/architecture/produce-domain.md) 6章の未決を「MVP では含めない」で閉じた
12. **再生のシナリオは今の `LessonReplayTests` と同じ手順・値にする**（[lesson-phases.md](../wiki/architecture/lesson-phases.md) 8章の決定の「固定のシードと操作列」）。カードごとの計算は .NET の単体テストが受け持ち、実機の確認は、整数の演算・乱数・並べ替え・文字列の書式が IL2CPP で同じになるかを見る。どれだけのカードと局面を通ったかは、タスク 1 で期待値の `turn=`・`paramGained=`・`discard=`・`exiled=` の行を実装メモに写して残す（厚みが足りないと判断したら、シナリオを足すのは別の変更）
13. Editor の PlayMode ではこのテストを回さない。`BootstrapPlayMode`（`Assets/Scripts/Editor/BootstrapPlayMode.cs`）が Play を Bootstrap シーンから始めるので、テストの Play にサーバーへの接続が混ざるため。Editor（Mono）で回しても IL2CPP の確認にならない（lesson-phases.md 8章）。Unity ではコンパイルだけを確かめ、実行はプレイヤー（タスク 3・5）で行う
14. `file:` のパッケージの asmdef の `UNITY_INCLUDE_TESTS` の制約が効くように、`manifest.json` の `testables` に `com.uta.shared.domain.testing` を入れる（3.5）。入れても `Uta.Shared.Domain.Testing` がコンパイルされなければ、タスク 2 の止まる条件

## ユーザーの確認が要ること

なし（2026-11-16 に解決したものは上の 8〜11）

## 1. 目的と前提

- 何を実装するか: MVP の完了条件 7（[game-spec.md](../wiki/overview/game-spec.md)「完了条件」）。Android の実機（IL2CPP）で起動 → ログイン → ガチャ 1 回のスモークを通し、IL2CPP で `Uta.Shared.Domain` の計算が .NET と一致することを、Unity Test Runner のプレイヤー実行で確かめる。手順と決めごとは [build-ci.md](../wiki/architecture/build-ci.md) 3〜5章、比べ方は [lesson-phases.md](../wiki/architecture/lesson-phases.md) 8章・[produce-domain.md](../wiki/architecture/produce-domain.md) 7章
- 関係する ADR: [ADR 0006](../wiki/decisions/0006-adopt-unity-7-early.md)（Unity 7 を早期に触る）、[ADR 0008](../wiki/decisions/0008-shared-domain-and-onion.md)（Shared.Domain。本番のクライアントはシードを持たない。実機の一致の確認は CI で回さない）
- 前提: ガチャの画面と通信は `main` にある（順 7 の gacha）。画面の残り（formation・week・lesson）は待たない
- 実装の worktree は、Library のある固定の worktree（`tools/worktree-pool.sh start`）で、Editor を開いておく。以下「実装の worktree」と書く
- **決めないこと（範囲外）**:
  - iOS の実機ビルド、CI、dev 環境（Cloud Run）への接続（[build-ci.md](../wiki/architecture/build-ci.md) 6章）
  - プロデュースの再生の IL2CPP の一致（決めたこと 11）
  - Managed Stripping の段階を変えること・`link.xml`。今の既定のまま。実機で型が落ちたら止まる条件（タスク 4）
  - 署名用のキーストア、ストア向けの AAB
  - 実機での画面の見た目の確認（豆腐・レイアウト）。スモークで目に入った崩れは実装メモに書くだけで、直さない

## 2. 触るファイル

パスはリポジトリのルートから。`client/uta/Assets/` は `Assets/` と略す。

| 操作 | パス | 中身 | タスク |
|---|---|---|---|
| 作成 | `shared/Uta.Shared.Domain.Testing/Uta.Shared.Domain.Testing.csproj` | 3.3 | 1 |
| 作成 | `shared/Uta.Shared.Domain.Testing/package.json` | 3.3 | 1 |
| 作成 | `shared/Uta.Shared.Domain.Testing/Uta.Shared.Domain.Testing.asmdef` | 3.3 | 1 |
| 移動 | `shared/Uta.Shared.Domain.Tests/Lesson/{TestCards,Phase1Cards,Phase2Cards,Phase3Cards,Phase4Cards,SyntheticCards,TestCardCatalog}.cs` → `shared/Uta.Shared.Domain.Testing/Lesson/` | `git mv`。中身は変えない（名前空間 `Uta.Shared.Domain.Tests.Lesson` のまま） | 1 |
| 作成 | `shared/Uta.Shared.Domain.Testing/Lesson/LessonReplayScenario.cs` | 3.4 | 1 |
| 作成 | `shared/Uta.Shared.Domain.Testing/Lesson/LessonReplayExpected.cs` | 3.4 | 1 |
| 変更 | `shared/Uta.Shared.Domain.Tests/Uta.Shared.Domain.Tests.csproj` | `Uta.Shared.Domain.Testing` の ProjectReference を足す | 1 |
| 作成 | `shared/Uta.Shared.Domain.Tests/Lesson/LessonReplayExpectedTests.cs` | 5章 | 1 |
| 変更 | `Uta.slnx` | `/shared/` に `Uta.Shared.Domain.Testing.csproj` を足す | 1 |
| 変更 | `Assets/Scripts/Editor/BuildCommands.cs` | `BuildAndroidIl2Cpp` を足す（3.1） | 2 |
| 変更 | `client/uta/ProjectSettings/ProjectSettings.asset` | Android のパッケージ名、スタンドアロンのスクリプトバックエンド（3.2） | 2 |
| 変更 | `client/uta/Packages/manifest.json` | `com.uta.shared.domain.testing` と `testables`（3.5） | 2 |
| 作成（Unity が作る） | `shared/Uta.Shared.Domain.Testing/` の `.meta`（フォルダ・`.cs`・`.asmdef`・`.csproj`・`package.json`） | Unity が取り込んだときに作る。全部コミットする | 2 |
| 作成 | `Assets/Tests/SharedDomainReplay/Uta.Client.Tests.SharedDomainReplay.asmdef` | 3.5 | 2 |
| 作成 | `Assets/Tests/SharedDomainReplay/LessonReplayDeviceTests.cs` | 3.5 | 2 |
| 作成（Unity が作る） | `Assets/Tests/SharedDomainReplay.meta` と、フォルダの中の `.meta` | コミットする | 2 |
| 変更 | `uta_docs/wiki/architecture/unity7-evaluation.md`・`build-ci.md`・`repository-layout.md`（7章・8章）、`uta_docs/wiki/overview/game-spec.md`、`CLAUDE.md`（ビルド・テスト・実行） | 結果とコマンドの記録（タスク 6） | 6 |

`ProjectSettings.asset` は Editor が書く（3.2）。手で書かない。

## 3. 型とシグネチャ

### 3.1 `BuildCommands.BuildAndroidIl2Cpp`

`BuildWindowsIl2Cpp` の下に足す。`using` は今のファイルのまま足りる。

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

実装の worktree の `client/uta` で、Editor を開いたまま次を 1 回実行する（Editor が `ProjectSettings.asset` を書く）。

```
unity command eval 'UnityEditor.PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.chinpangx.uta"); UnityEditor.PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, UnityEditor.ScriptingImplementation.IL2CPP); UnityEditor.AssetDatabase.SaveAssets(); return "ok";'
```

コミットするのは、`applicationIdentifier` の `Android:` の行と、`scriptingBackend` に足された `Standalone: 1` の行だけ。ほかの行の差分（Editor が一緒に書き換えたもの）は `git restore -p` で戻す。差分がこの 2 か所でなければ、戻したものを実装メモに書く。

### 3.3 `Uta.Shared.Domain.Testing` のプロジェクト

`Uta.Shared.Domain.Testing.csproj`（`shared/Directory.Build.props` が `bin~/`・`obj~/` を当てる）:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <!-- テストのカード・再生のシナリオと期待値。.NET のテストと Unity のプレイヤーのテストが使う（build-ci.md 5.2）。Unity にソースで入るため、netstandard2.1・C# 9 -->
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

`Uta.Shared.Domain.Tests.csproj` の、`Uta.Shared.Domain.csproj` の ProjectReference の次の行に足す:

```xml
<ProjectReference Include="..\Uta.Shared.Domain.Testing\Uta.Shared.Domain.Testing.csproj" />
```

`Uta.slnx` の `/shared/` の `Uta.Shared.Domain.csproj` の行の次に足す:

```xml
<Project Path="shared/Uta.Shared.Domain.Testing/Uta.Shared.Domain.Testing.csproj" />
```

`Uta.Server.Tests` は `Uta.Shared.Domain.Tests` を参照しているので、移したカードは推移的に見える。`server/` は変えない。移したカードを参照しているのは、`shared/Uta.Shared.Domain.Tests` の 13 ファイルと `server/Uta.Server.Tests/MasterData/TestCardsMatchMasterTests.cs`（2026-11-16 の `main` で数えた）で、名前空間を変えないのでどれも変えない。

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
    /// 値は LessonReplayTests の Replay_SameSeedAndCommands_SameResult（通常）・Replay_Exam_SameSeedAndCommands_SameResult（試験）と同じ。
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
                var usable = state.Zones.Hand.FirstOrDefault(id => engine.CanUse(state, id) == UseCheckResultKind.Ok);
                var command = usable != 0 ? LessonCommand.UseCard(usable) : LessonCommand.Skip();
                var result = engine.Apply(state, command, random);
                if (result != LessonApplyResultKind.Accepted)
                {
                    throw new InvalidOperationException("再生の操作が受け付けられませんでした: " + result);
                }
            }

            return state;
        }
    }
}
```

`LessonReplayTests` は変えない（途中で保存・復元する再生など、このシナリオに無いものを持つ）。

`LessonReplayExpected.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Text;
using Uta.Shared.Domain.Lesson;

namespace Uta.Shared.Domain.Tests.Lesson
{
    /// <summary>
    /// <see cref="LessonReplayScenario"/> の最後の状態を .NET で再生した値（LessonStateComparer.Describe の行）。
    /// IL2CPP のプレイヤーのテストがこれと比べる。値は LessonReplayExpectedTests の失敗の出力から写す（手で書かない）。
    /// </summary>
    public static class LessonReplayExpected
    {
        // タスク 1 の手順 5 で、new string[0] を new[] { ... }（LessonReplayExpectedTests の失敗の出力）に置き換える
        public static IReadOnlyList<string> Normal { get; } = new string[0];

        public static IReadOnlyList<string> Exam { get; } = new string[0];

        /// <summary>
        /// 期待値の行と状態を項目ごとに比べ、食い違った項目だけを返す。同じなら空。
        /// LessonStateComparer.Diff は状態どうしを比べるので、期待値が文字列のときはこちらを使う。
        /// </summary>
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
            var builder = new StringBuilder();
            foreach (var line in LessonStateComparer.Describe(state))
            {
                builder.Append("            \"").Append(line.Replace("\\", "\\\\").Replace("\"", "\\\"")).Append("\",\n");
            }

            return builder.ToString();
        }
    }
}
```

- `"[" + i + "]"` は `int` の文字列への変換に現在のカルチャを使うが、整数の 10 進の表記はカルチャで変わらない。`LessonStateComparer` の `Text` に合わせたいときも、ここは診断の文言なので変えない
- `LessonStateComparer` は変えない

### 3.5 Unity 側

`client/uta/Packages/manifest.json`:

- `dependencies` の `"com.uta.shared.contracts"` の行の次に足す:

  ```json
  "com.uta.shared.domain.testing": "file:../../../shared/Uta.Shared.Domain.Testing",
  ```

- `dependencies` の閉じ括弧の後ろ（ルートの直下）に `testables` を足す。今の `manifest.json` に `testables` は無い:

  ```json
  "testables": [
    "com.uta.shared.domain.testing"
  ]
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
    /// IL2CPP でビルドしたプレイヤーで再生し、.NET で再生した状態と項目ごとに比べる（lesson-phases.md 8章、build-ci.md 5.2）。
    /// Editor（Mono）では回さない。
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

### 3.6 よく使うパスと値

| 名前 | 値 |
|---|---|
| Unity | `"/c/Program Files/Unity/Hub/Editor/7000.0.0a12/Editor/Unity.exe"` |
| `ADB` | `"/c/Program Files/Unity/Hub/Editor/7000.0.0a12/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe"` |
| Windows のビルドの worktree | `C:/Work/Uta-wt/build-foundation`（既にある） |
| Android のビルドの worktree | `C:/Work/Uta-wt/build-android`（タスク 4 で作る） |
| ログ・結果の置き場 | セッションのスクラッチパッド（絶対パス） |

## 4. タスク

### タスク 1: shared のテスト用プロジェクトと期待値（.NET だけ）

触るファイル: 2章のタスク 1 の行。Unity を開かない。

手順:

1. `shared/Uta.Shared.Domain.Testing/` を作り、3.3 の 3 ファイルを置く
2. `git mv` で 7 ファイルを `shared/Uta.Shared.Domain.Testing/Lesson/` に移す（中身は変えない）。3.4 の 2 ファイルを置く
3. `Uta.Shared.Domain.Tests.csproj`・`Uta.slnx` を 3.3 のとおり直す。5章の `LessonReplayExpectedTests.cs` を置く
4. `dotnet build Uta.slnx` が警告 0・エラー 0 になることを確かめる。移したカードが C# 9 で通らない書き方を含んでいたら、止まって報告する（今のファイルは C# 9 の範囲と見込んでいる）
5. `dotnet test --project shared/Uta.Shared.Domain.Tests` を実行する。`LessonReplayExpectedTests` の `Normal_MatchesScenario`・`Exam_MatchesScenario` の 2 件だけが落ち、失敗の出力に `ToCSharpLines` の行が出る。それを `LessonReplayExpected` の `Normal`・`Exam` に写す（`new string[0]` を `new[] { <写した行> }` に置き換える）。期待値の `turn=`・`paramGained=`・`discard=`・`exiled=` の行（通常・試験の両方）を実装メモに写す（決めたこと 12）
6. もう一度 `dotnet test --project shared/Uta.Shared.Domain.Tests` と `dotnet test --project server/Uta.Server.Tests` を実行し、全部が通ることを確かめる（サーバーの結合テストは Docker が要る。Docker が動いていなければ、Docker Desktop の起動をユーザーに頼む）

境界: 期待値の行数は `LessonStateComparer.Describe` の行数（状態しだい）。手で数を合わせない。

終わったことの確かめ方: 手順 4・6。

### タスク 2: Unity に取り込み、コンパイルを通す

触るファイル: 2章のタスク 2 の行。タスク 1 の後。実装の worktree で行う。

手順:

1. `BuildCommands.cs` に 3.1 を足す
2. 3.2 の `eval` を実行し、`ProjectSettings.asset` の差分を 2 か所にする
3. `manifest.json` を 3.5 のとおり直し、`client/uta` で `unity command package resolve`。Unity が `shared/Uta.Shared.Domain.Testing/` の `.meta` を作ったことを `git status` で確かめる
4. `Assets/Tests/SharedDomainReplay/` に 3.5 の 2 ファイルを置く
5. `unity recompile --format json` がエラー 0 になることを確かめる。`client/uta/Library/ScriptAssemblies/` に `Uta.Shared.Domain.Testing.dll` と `Uta.Client.Tests.SharedDomainReplay.dll` があることを確かめる（`ls`）
6. EditMode の全件を CLAUDE.md「ビルド・テスト・実行」のコマンドで回し、失敗 0 を確かめる
7. `.meta` を含めてコミットする

止まる条件: 手順 5 で `Uta.Shared.Domain.Testing` がコンパイルされない（`testables` を入れても `UNITY_INCLUDE_TESTS` が効かない）。このときは、`defineConstraints` を外す・別の置き場にするなどを自分で選ばず、報告する。

終わったことの確かめ方: 手順 5・6。

### タスク 3: Windows の IL2CPP のプレイヤーで Test Runner を回す

触るファイル: なし（実行だけ）。タスク 2 の後。Test Runner のプレイヤー実行の仕組みを、Android より先に確かめる（決めたこと 1）。

手順:

1. `git -C C:/Work/Uta-wt/build-foundation worktree lock --reason "ビルド専用。Library を消さない（build-ci.md 1章）" C:/Work/Uta-wt/build-foundation`（既にロック済みなら飛ばす）
2. `git -C C:/Work/Uta-wt/build-foundation checkout --detach <実装の worktree の HEAD のコミット>`。差分があって切り替えられなければ、`git -C C:/Work/Uta-wt/build-foundation status --short` を実装メモに写して止まる（消さない）
3. 実装の worktree の `client/uta/Assets/Packages/` を、`C:/Work/Uta-wt/build-foundation/client/uta/Assets/Packages/` へコピーし直す（`rm -rf` してから `cp -r`）
4. 実行する（`run_in_background`）:
   `<Unity> -batchmode -projectPath C:/Work/Uta-wt/build-foundation/client/uta -buildTarget StandaloneWindows64 -runTests -testPlatform StandaloneWindows64 -assemblyNames Uta.Client.Tests.SharedDomainReplay -testResults <スクラッチパッド>/windows-replay.xml -logFile <スクラッチパッド>/windows-replay.log`
5. 5.2 の「通ったとみなすもの」（Windows）を確かめる

止まる条件: 手順 4 が、テストの結果ではなく Test Runner の仕組み（プレイヤーのビルド・起動・結果の受け取り）で失敗する。または、30 分たっても結果のファイルができず、ログが 5 分以上増えない。ログの該当箇所を添えて報告する。テストが失敗した（食い違った項目がある）ときは、`<message>` の `[i] 期待 … / 実際 …` を実装メモに写して止まる（コードを直さない。直し方はユーザーと決める）。

### タスク 4: Android のビルドとスモーク（スパイク）

触るファイル: なし（実行だけ）。タスク 2 の後（タスク 3 と前後してよいが、同じ Unity を 2 つ同時に回さない）。

手順:

1. ユーザーに、端末を USB でつなぎ USB デバッグを許可するよう頼む。`"$ADB" devices -l` に端末が `device` で出るのを確かめる。端末の条件は arm64・Android 8.0（API 26）以上（`"$ADB" shell getprop ro.product.cpu.abi` が `arm64-v8a`、`ro.build.version.sdk` が 26 以上）。機種名（`ro.product.model`）と Android の版（`ro.build.version.release`）を実装メモに書く
2. ビルド用の worktree を作る（初回だけ。既にあれば 3 へ）: 実装の worktree で `git worktree add --detach C:/Work/Uta-wt/build-android HEAD` → `git worktree lock --reason "ビルド専用。Library を消さない（build-ci.md 1章）" C:/Work/Uta-wt/build-android`
3. `git -C C:/Work/Uta-wt/build-android checkout --detach <実装の worktree の HEAD のコミット>`（差分があればタスク 3 の手順 2 と同じに扱う）。実装の worktree の `client/uta/Assets/Packages/` を `C:/Work/Uta-wt/build-android/client/uta/Assets/Packages/` にコピーし直す
4. ビルドする（`run_in_background`。初回は Library の作成と Android のインポートで長い）:
   `<Unity> -batchmode -nographics -projectPath C:/Work/Uta-wt/build-android/client/uta -buildTarget Android -executeMethod Uta.Client.Editor.BuildCommands.BuildAndroidIl2Cpp -logFile <スクラッチパッド>/android-build.log`
   終了コード 0 と `C:/Work/Uta-wt/build-android/client/uta/Build/Android/Uta.apk` ができたことを確かめる。ビルドのあと `git -C C:/Work/Uta-wt/build-android status --short` に出たファイル（Addressables が作る `Assets/AddressableAssetsData/Android/` など）は、消さずに実装メモに書く（ビルド用の worktree のものはコミットしない）
5. ローカルのサーバーを起動する（CLAUDE.md「ビルド・テスト・実行」のローカルのサーバーの手順。`migrate` を含む）
6. `"$ADB" install -r C:/Work/Uta-wt/build-android/client/uta/Build/Android/Uta.apk`、`"$ADB" shell pm clear com.chinpangx.uta`、`"$ADB" reverse tcp:5000 tcp:5000`、`"$ADB" logcat -c`。logcat を絞らずに `"$ADB" logcat > <スクラッチパッド>/android-logcat.txt` で背面で取り始める（クラッシュの行は `Unity` のタグに出ないため）
7. ユーザーに、[build-ci.md](../wiki/architecture/build-ci.md) 5.1 の手順 4〜6 を頼み、終わったら知らせてもらう。手順 5 の終了は `"$ADB" shell am force-stop com.chinpangx.uta` を実装担当が実行する
8. logcat を止め、5.1 の「通ったとみなすもの」を確かめる

止まる条件（当たったら、回避の実装を書かずに止まり、ログの該当箇所を添えて報告する。代えるかはユーザーが決める。[build-ci.md](../wiki/architecture/build-ci.md) 5.3）:

- 手順 1 で端末がつながらない（ユーザーに端末が無いときを含む）。これを先に確かめ、長いビルドの前に止まる
- 手順 4 で、Uta のコードや設定ではなく Unity の Android のビルド（Gradle・`AndroidPlayerBuildProgram`・IL2CPP の arm64・NDK）が失敗する
- 手順 4 で、Uta のコードや設定が原因で失敗し、直すのに計画にない設計判断が要る（Managed Stripping・`link.xml`・パッケージの版を変えるなど）
- 手順 7 で、起動しない・通信できない（`Unavailable`・平文の通信の拒否）・型が見つからない（`MissingMethodException`・`ExecutionEngineException`・VContainer の解決の失敗）・マスタを読めない

境界: アプリのデータを消してから始める（1 回目はサインアップの経路、5.1 の手順 6 がサインインの経路）。ガチャ通貨は初期付与で単発を引ける。

終わったことの確かめ方: 手順 4 の終了コード 0 と APK、手順 8 の条件。

### タスク 5: Android の実機で Test Runner を回す

触るファイル: なし（実行だけ）。タスク 3・4 の後。

手順:

1. 端末が `"$ADB" devices` に `device` で出ていることを確かめ、ユーザーに画面のロックを外したままにしてもらう
2. `build-android` の worktree が、タスク 3 で回したのと同じコミットであることを確かめる（違えばタスク 4 の手順 3）
3. 実行する（`run_in_background`）:
   `<Unity> -batchmode -projectPath C:/Work/Uta-wt/build-android/client/uta -buildTarget Android -runTests -testPlatform Android -assemblyNames Uta.Client.Tests.SharedDomainReplay -testResults <スクラッチパッド>/android-replay.xml -logFile <スクラッチパッド>/android-replay.log`
4. 5.2 の「通ったとみなすもの」（Android）を確かめる

止まる条件: タスク 3 と同じ（仕組みの失敗、30 分たってもファイルができずログが 5 分以上増えない、テストの失敗）。Test Runner はテスト用のプレイヤーを開発ビルドで作り、パッケージ名を `com.UnityTestRunner.UnityTestRunner` に変えて入れる（Test Framework 1.9.0 のソースによる。実行で確かめていない）。インストールや起動で止まったときは、`"$ADB" shell pm list packages | grep -i unitytestrunner` の結果も報告に添える。

### 4.4 Windows に代えるとき

タスク 4 またはタスク 5 の止まる条件に当たり、ユーザーが代えると決めたとき:

1. 計算の一致は、タスク 3 の結果で代える（もう一度回さない）
2. スモークは、CLAUDE.md の `BuildWindowsIl2Cpp` を `build-foundation` で実行して `Uta.exe` を作り、引数なしで起動して、[build-ci.md](../wiki/architecture/build-ci.md) 5.1 の手順 4〜6 をユーザーに頼む（手順 5 の終了はウィンドウを閉じる）。「通ったとみなすもの」の logcat は、`Uta.exe` と同じフォルダの `Uta_Data/` ではなく `%USERPROFILE%/AppData/LocalLow/<会社名>/<製品名>/Player.log` で見る（会社名・製品名は `ProjectSettings.asset` の `companyName`・`productName`）

### タスク 6: 結果を wiki と CLAUDE.md に記録する

触るファイル: 2章のタスク 6 の行。

手順:

1. [unity7-evaluation.md](../wiki/architecture/unity7-evaluation.md) 2章の表に、行を足す: 「Test Runner の Windows の IL2CPP のプレイヤー実行（`SharedDomainReplay`）」（タスク 3）、「Android の IL2CPP のビルドとスモーク」（タスク 4。端末の機種・版）、「Android の Test Runner の実機実行と IL2CPP の計算の一致」（タスク 5）。列は今の表と同じ（項目・結果・方法・日付）。止まる条件に当たったものは、3章に「3.4 <項目>」の節を足し、症状・ログの該当行・止めたことを書く。3.3 の「**Android / iOS** の実機ビルド」の行は、Android を確かめた（または Windows に代えた）ことと日付を書き足す
2. [repository-layout.md](../wiki/architecture/repository-layout.md) 8章の表の、「未確認: Android」または「Android・iOS は未確認」と書いた行（`file:` 取り込み、MasterMemoryAddons のバイナリ、MasterMemory・MessagePack・MagicOnion の版、UnityScreenNavigator・VContainer・UniTask）の「確かめ方」の列の末尾に、次の文字列（リンクは repository-layout.md からの相対）: `**2026-MM-DD に Android（<機種>・Android <版>）の IL2CPP で確かめた**（unity7-evaluation.md へのリンク）`を足す。7章の「これから作るもの」の Android の実機ビルドの括弧に`Android は build-ci.md へのリンク の手順で手で回す`を足す
3. [build-ci.md](../wiki/architecture/build-ci.md): 3章の「確かめていないもの」の箇条を、「確かめたもの（日付。結果は unity7-evaluation.md）」に書き換える。2章・3章・5章のコマンドや手順が実装とずれていたら（引数・パス・待ち時間）、実装で使ったものに直す
4. CLAUDE.md「ビルド・テスト・実行」の Windows の IL2CPP の項目の次に、2 項目を足す: Android の IL2CPP の APK（タスク 4 の手順 4 のコマンド。置き場は `C:/Work/Uta-wt/build-android`）、Test Runner のプレイヤー実行（タスク 3・5 の手順のコマンド）。どちらも詳細は [build-ci.md](../wiki/architecture/build-ci.md) へリンクする
5. [game-spec.md](../wiki/overview/game-spec.md) の「実装計画の単位と順」の 8 の行の先頭に `**done**（日付）` を足し、この計画を `status: done` にする

終わったことの確かめ方: `python -I .claude/skills/linting-wiki/lint_wiki.py` がエラー 0。

## 5. テスト

| テスト | 置き場 | 確かめること | 回し方 |
|---|---|---|---|
| `LessonReplayExpectedTests.Normal_MatchesScenario` | `shared/Uta.Shared.Domain.Tests/Lesson/` | `LessonReplayExpected.Compare(LessonReplayExpected.Normal, LessonReplayScenario.RunNormal())` が空 | `dotnet test --project shared/Uta.Shared.Domain.Tests` |
| `LessonReplayExpectedTests.Exam_MatchesScenario` | 同上 | 試験で同じ | 同上 |
| `LessonReplayExpectedTests.Compare_DifferentLine_ReportsIndex` | 同上 | `Describe(RunNormal())` を写した配列の 1 行目を `"x"` にしたもので、`Compare` が 1 件を返し、`[0]` で始まる | 同上 |
| `LessonReplayExpectedTests.Compare_MissingLine_ReportsNone` | 同上 | `Describe(RunNormal())` を写した配列から末尾の 1 行を除いたもので、`Compare` が 1 件を返し、`期待 (なし)` を含む | 同上 |
| `LessonReplayDeviceTests` の 2 件 | `Assets/Tests/SharedDomainReplay/` | IL2CPP のプレイヤーで再生した状態が期待値と一致 | Test Runner のプレイヤー実行（タスク 3 は Windows、タスク 5 は Android） |

`Compare_*` の 2 件は `LessonReplayExpected.Normal` を使わない（タスク 1 の手順 5 で、期待値が空のときに落ちないようにするため）。

```csharp
using System.Collections.Generic;
using Uta.Shared.Domain.Lesson;
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

        [Fact]
        public void Exam_MatchesScenario()
        {
            var state = LessonReplayScenario.RunExam();
            var differences = LessonReplayExpected.Compare(LessonReplayExpected.Exam, state);

            Assert.True(differences.Count == 0, "LessonReplayExpected.Exam に写す:\n" + LessonReplayExpected.ToCSharpLines(state));
        }

        [Fact]
        public void Compare_DifferentLine_ReportsIndex()
        {
            var state = LessonReplayScenario.RunNormal();
            var expected = new List<string>(LessonStateComparer.Describe(state));
            expected[0] = "x";

            var differences = LessonReplayExpected.Compare(expected, state);

            Assert.Single(differences);
            Assert.StartsWith("[0]", differences[0]);
        }

        [Fact]
        public void Compare_MissingLine_ReportsNone()
        {
            var state = LessonReplayScenario.RunNormal();
            var expected = new List<string>(LessonStateComparer.Describe(state));
            expected.RemoveAt(expected.Count - 1);

            var differences = LessonReplayExpected.Compare(expected, state);

            Assert.Single(differences);
            Assert.Contains("期待 (なし)", differences[0]);
        }
    }
}
```

スモーク（[build-ci.md](../wiki/architecture/build-ci.md) 5.1）は手動。Unity・実機でしか確かめられないもの: タスク 2〜5。

## 6. 未決事項

なし
