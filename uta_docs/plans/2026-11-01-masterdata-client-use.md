---
title: 実装計画: クライアントでマスタを使う（マスタ → CardCatalog の詰め替えと、起動時の読み込み・DI 登録）
status: done
updated: 2026-11-01
spec: ../wiki/architecture/repository-layout.md, ../wiki/architecture/screen-design.md, ../wiki/architecture/master/masterdata-pipeline.md, ../wiki/architecture/lesson-domain-types.md
---

# 実装計画: クライアントでマスタを使う

## 1. 目的と前提

[マスタを Unity に取り込む計画](2026-11-01-masterdata-unity.md)（done）で、`MasterDataLoader` が StreamingAssets のバイナリから `MemoryDatabase` を作れるようになったが、誰も呼んでいない。この計画で、(1) マスタの行を `Uta.Shared.Domain` の行の型に詰めて `CardCatalog`（`ICardCatalog` の実装）を作る Infrastructure のクラスを足し、(2) 起動時に読み込んで VContainer に `ICardCatalog` として登録する。

- 仕様の正: 詰め替えの方針は [repository-layout.md](../wiki/architecture/repository-layout.md) 4章（行の値をそのまま行の型に詰める。グループ ID の解決と入れ子は `CardCatalog.Create` が行う）、起動フローは [screen-design.md](../wiki/architecture/screen-design.md) 4章、マスタの読み込みは [masterdata-pipeline.md](../wiki/architecture/master/masterdata-pipeline.md) 6章
- 関係する ADR: [0007](../wiki/decisions/0007-masterdata-with-addons.md)、[0008](../wiki/decisions/0008-shared-domain-and-onion.md)、[0009](../wiki/decisions/0009-client-stack.md)
- 先に確かめたこと（2026-11-01）:
  - **マスタの `Effect` の行と Domain の `EffectRow`・`EffectDef` は同じ形**（`Op`・`Status`・`Trigger`・`Value`・`Arg1`〜`Arg3`・`RefConditionGroupId`・`RefEffectGroupId`・`RefCardId`。空欄は 0）。`Condition`・`Card`・`CardFace`・`CardCost` も、Domain の `CardRow`・`CardFaceRow`・`CardCostRow`・`ConditionRow` と列が 1 対 1。違いは enum の型だけ（`Uta.MasterData.X` と `Uta.Shared.Domain.Lesson.X`。名前と数値は同じで、サーバーの `MasterDataTests.EnumMirrors_MatchDomain` が確かめている）。よって詰め替えは機械的で、**サーバーの `server/Uta.Server/Infrastructure/MasterData/MasterCardCatalog.cs` と同じ形**（`(Domain の enum)(int)x.列` のキャスト）
  - `CardCatalog.Create`（`shared/Uta.Shared.Domain/Lesson/CardCatalog.cs`）は実装済み。問題があれば `InvalidOperationException`（メッセージに問題を全部入れる）
  - 起動の入口は `UtaBootstrap.Start`（`RuntimeInitializeOnLoadMethod`）。VContainer の `LifetimeScope.Create` でスコープを組む。`StartupPresenter` が認証と疎通を行い、`-uta-quit-after-check` なら終了コードで終わる
- **この計画で決めないこと**（範囲外）:
  - サーバー側の変換（別セッションで進行中）
  - カード以外のテーブル（プロデュース・ガチャ・育成アイドルなど）の Domain の型への変換。それを使う計画で足す。このため `MemoryDatabase` 自体は DI に登録しない
  - 画面での表示、`ClientOnly` の列（表示名・アイコン）
  - Addressables への移行、Android・iOS の実機での確認（`MasterDataLoader` と同じく未確認のまま）
  - `UnlockLv` の扱いなど、カードの中身の仕様

## 2. この計画で決めたこと（7章の確認事項は、ユーザーが推奨どおりと決めた。2026-11-01）

| 項目 | 決めたこと | 理由 |
|---|---|---|
| 読む順 | **認証（スコープ内の `StartupPresenter`）より前**に、`UtaBootstrap` がマスタを読む | マスタは同梱で通信が要らず、失敗は端末の中で決まる。壊れたビルドでサーバーへ通信しない。screen-design.md 4章は認証 → マスタの順だったので、**この計画のコミットで先に直した**（実装設計書が先、計画が後: CLAUDE.md の実装前のルール）。7章の回答が違えば、そちらも戻す |
| 読み込みの失敗 | リトライしない。`StartupView` に `master: FAILED <型名>: <メッセージ>` を出し、`Debug.LogError`、スコープは作らない。`-uta-quit-after-check` のときは終了コード 1 で終わる | 同梱のバイナリは再試行しても同じ結果になる。`SchemaMismatch` も同梱では起きず、起きるのはビルドの不整合（Addressables に移るときにアプリの更新を促す扱いを足す）。鍵（スキーマのハッシュ）はメッセージに入らない（`MasterDataLoader` が保証） |
| 成功の表示 | `Debug.Log("[Uta] master: ok cards=<件数>")` だけ。`StartupView` には出さない | `StartupPresenter` に Domain 以外の依存を足さないため。画面の計画で作り直す |
| 登録する型 | `ICardCatalog`（Domain のポート）を `RegisterInstance` で登録する | 使う側（今後のレッスンのユースケース）が `ICardCatalog` を受け取るだけで済む。`MemoryDatabase` は Infrastructure の外に出さない（repository-layout.md 3.1・4章） |
| Bootstrap から呼ぶ入口 | `MasterCardCatalog.LoadFromStreamingAssetsAsync(CancellationToken)`（`UniTask<CardCatalog>`）を Infrastructure に置き、Bootstrap はこれだけを呼ぶ | Bootstrap が `Uta.MasterData`（`MemoryDatabase`）を直接参照しないため。生成物を参照してよいのは Infrastructure だけ |
| 詰め替えの置き場 | `Assets/Scripts/Infrastructure/MasterData/MasterCardCatalog.cs`（名前空間 `Uta.Client.Infrastructure`）。`MasterData/Generated/` の外（生成のたびに入れ替わるため） | サーバーの `MasterCardCatalog` との対称 |

## 3. 触るファイル

`I` = `client/uta/Assets/Scripts/Infrastructure`、`B` = `client/uta/Assets/Scripts/Bootstrap`、`T` = `client/uta/Assets/Tests/EditMode`。

| 操作 | パス | 中身 | タスク |
|---|---|---|---|
| 作成 | `I/MasterData/MasterCardCatalog.cs` と `.meta` | 4.1 | 1 |
| 変更 | `T/Uta.Client.Infrastructure.Tests.EditMode.asmdef` | `references` に `Uta.Shared.Domain` を足す | 1 |
| 作成 | `T/MasterCardCatalogTests.cs` と `.meta` | 5章 | 1 |
| 変更 | `B/Uta.Client.Bootstrap.asmdef` | `references` に `Uta.Shared.Domain` を足す | 2 |
| 変更 | `B/UtaBootstrap.cs` | 4.2 | 2 |
| 変更 | `uta_docs/wiki/`・`CLAUDE.md`（不要なら変えない） | 実装で決めたこと（メインのセッション） | 3 |

## 4. 型とシグネチャ

C# 9 の範囲（`client/` の `.editorconfig`。ブロック形式の namespace、`var`、Allman）。BOM 付き UTF-8、LF。

### 4.1 `MasterCardCatalog`（タスク 1）

```csharp
using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Uta.MasterData;
using DomainLesson = Uta.Shared.Domain.Lesson;

namespace Uta.Client.Infrastructure
{
    /// <summary>マスタの行を Shared.Domain の行の型に詰めて CardCatalog を作る。グループの解決と組み立ては CardCatalog.Create が行う（repository-layout.md 4章）。</summary>
    public static class MasterCardCatalog
    {
        public static DomainLesson.CardCatalog Create(MemoryDatabase db);

        public static async UniTask<DomainLesson.CardCatalog> LoadFromStreamingAssetsAsync(CancellationToken cancellationToken = default);
    }
}
```

**名前の衝突に注意**: `Uta.MasterData` と `Uta.Shared.Domain.Lesson` の両方に `Plan`・`CardType`・`EffectOp` などの enum と、`CardFace`（マスタのクラスと Domain の `CardFace` クラス）がある。`using` を両方入れると曖昧になるので、上のコードブロックの `using` のとおり、エイリアスだけを入れる。`Create` の中の型は次のとおり書く:

- Domain の enum へのキャストは完全修飾（`(Uta.Shared.Domain.Lesson.Plan)(int)x.Plan`）か、ファイル先頭の `using` エイリアス（`using DomainLesson = Uta.Shared.Domain.Lesson;`）を使って `(DomainLesson.Plan)(int)x.Plan` と書く。**エイリアス `DomainLesson` で統一する**。`using Uta.MasterData;` は `MemoryDatabase` のために 1 つだけ入れ、`using Uta.Shared.Domain.Lesson;` は入れない（`CardRow` なども `DomainLesson.CardRow` と書く）
- 行の型は `DomainLesson.CardRow`・`DomainLesson.CardFaceRow`・`DomainLesson.CardCostRow`・`DomainLesson.EffectRow`・`DomainLesson.ConditionRow`

`Create(db)` の 5 つの `Select` は `x` の型を推論させる（`MemoryDatabase` の `All` は `IReadOnlyList<生成クラス>`）。中身は、サーバーの `server/Uta.Server/Infrastructure/MasterData/MasterCardCatalog.cs` の `Create` と**同じ列の順・同じキャスト**で書く（`db.CardTable.All`・`CardFaceTable`・`CardCostTable`・`EffectTable`・`ConditionTable` を `Select` → `ToList`。`foreach`＋`List.Add` でもよい）。最後は `return DomainLesson.CardCatalog.Create(cards, faces, costs, effects, conditions);`。`db` が `null` なら `ArgumentNullException`（`nameof(db)`）。

`LoadFromStreamingAssetsAsync`: `var db = await MasterDataLoader.LoadFromStreamingAssetsAsync(cancellationToken); return Create(db);`。例外は握りつぶさない（`InvalidOperationException`・`UnityWebRequestException`・`OperationCanceledException` はそのまま呼び出し側へ）。

### 4.2 `UtaBootstrap`（タスク 2）

`Start()` を `private static void Start() { RunAsync().Forget(); }` にし、中身を `private static async UniTaskVoid RunAsync()` に移す（`using Cysharp.Threading.Tasks;` を足す）。処理の順:

1. 既存どおり `args`・`serverUrl`・`view` を作り `DontDestroyOnLoad`
2. `CardCatalog cardCatalog;` を宣言し、`try { cardCatalog = await MasterCardCatalog.LoadFromStreamingAssetsAsync(); } catch (Exception e) { … return; }`
   - 成功: `Debug.Log("[Uta] master: ok cards=" + cardCatalog.CardIds.Count);`
   - 失敗（`catch` 内）: `var message = "master: FAILED " + e.GetType().FullName + ": " + e.Message;` → `Debug.LogError("[Uta] " + message);` → `view.SetText(message);` → `-uta-quit-after-check` が `args` にあれば `UnityEngine.Application.Quit(1);` → `return;`（`LifetimeScope` は作らない）
   - `OperationCanceledException` は起きない（トークンを渡さない）ので別扱いしない
3. 既存の `LifetimeScope.Create(builder => { … })` に `builder.RegisterInstance<ICardCatalog>(cardCatalog);` を 1 行足す（`using Uta.Shared.Domain.Lesson;` を足す。`StartupView`・`Uta.Client.Application` との名前衝突はない。`CardCatalog` の型名は `UtaBootstrap.cs` に `using Uta.Client.Infrastructure;` を足して `MasterCardCatalog` を使うので、`Uta.MasterData` の `CardFace` などと混ざらないよう **`using Uta.MasterData;` は入れない**）
4. 残りは既存どおり（`DontDestroyOnLoad(scope.gameObject)`）

`Uta.Client.Bootstrap.asmdef` の `references` に `"Uta.Shared.Domain"` を足す（`ICardCatalog` のため。repository-layout.md 3章で Bootstrap はすべて参照してよい）。`Uta.Client.Infrastructure` は既に参照している。

## 5. タスク

各タスクが終わったとき、`dotnet build Uta.slnx` が通り（`client/` は変えないので結果は変わらない）、`unity recompile --format json` のエラーが 0 になる状態にする。

### タスク 1: `MasterCardCatalog` とテスト

- 触る: 3章のタスク 1 の行
- 手順: (1) 4.1 のとおり `MasterCardCatalog.cs` を書く (2) テスト用 asmdef の `references` に `"Uta.Shared.Domain"` を足す（`precompiledReferences` は変えない。CS0012 が出たら足さずに止まって報告する） (3) `MasterCardCatalogTests.cs` を下のとおり書く (4) `unity recompile --format json` でエラー 0 (5) `unity test --mode EditMode`（`client/uta` で実行。CLAUDE.md には無いが、前の計画で使い、[masterdata-pipeline.md](../wiki/architecture/master/masterdata-pipeline.md) の実装メモにある。同じプロジェクトを開いた Editor があると使えない。先に `unity close .`）。使えなければユーザーに「Editor の Test Runner → EditMode → Run All を実行して結果を教えてほしい」と依頼する。結果を見ずに「通った」と書かない
- 境界: マスタに問題があると `CardCatalog.Create` が `InvalidOperationException` を投げる。この計画では包まず、そのまま通す（Bootstrap が捕まえて表示する）
- 確かめ方: 下のテストがすべて通る。既存の `MasterDataLoaderTests` 7 件も通る

`MasterCardCatalogTests`（名前空間 `Uta.Client.Infrastructure.Tests`。NUnit）。**書き方の決まり**: (a) 同梱のバイナリは、このファイルに `private static byte[] ReadBundled()`（`MasterDataLoaderTests` と同じ中身）を複製して使う（共有のヘルパーは作らない）。(b) `using` は `System`・`System.IO`・`System.Linq`・`System.Threading`・`System.Collections`・`Cysharp.Threading.Tasks`・`NUnit.Framework`・`UnityEngine.TestTools`・`DomainLesson = Uta.Shared.Domain.Lesson` のエイリアス。`using Uta.MasterData;` は入れず、マスタの型は `Uta.MasterData.X` と完全修飾する。(c) `EnumMirrors_MatchDomain` は `public static readonly object[][] EnumPairs = { new object[] { typeof(Uta.MasterData.Plan), typeof(DomainLesson.Plan) }, … }`（11 組）を `[TestCaseSource(nameof(EnumPairs))]` に渡し、引数は `(Type masterEnum, Type domainEnum)`。(d) サーバーのテストは xunit・C# 14 なので、`Assert.Equal(a, b)` は `Assert.AreEqual(a, b)` に、`CardIds[^1]` は `CardIds[CardIds.Count - 1]` に、`Assert.Single` は `Assert.AreEqual(1, x.Count)` に、`[Fact]` は `[Test]` に書き換える（Index/Range は C# 9 の規約で使えない）。値と検査の中身は変えない。サーバーの `server/Uta.Server.Tests/MasterData/MasterCardCatalogTests.cs`・`MasterDataTests.cs` の値を写す。**値は実装する時点のそのファイルから写す**）:

| テスト名 | 内容 |
|---|---|
| `Create_Mvp_Has36Cards` | `MasterDataLoader.Load(同梱のバイナリ)` → `Create`。`CardIds.Count == 36`、先頭 1、末尾 215、昇順 |
| `Get_F01_HasNormalAndEnhancedFaces` | サーバーの同名テストと同じ検査（Plan.Free・CardType.Active・Rarity.N・コスト Consume 4 / 強化 3・効果 GainParam 9 / 強化 14） |
| `Get_S14_HasConditionedEffect` | カード 114 の 2 番目の効果の条件（Focus・GreaterOrEqual・3） |
| `Get_S13_HasPersistentWithChild` | カード 113 の AddPersistent（OnActiveCardUse）の子が AddStatus・Focus・1 |
| `Get_S04_HasUseCondition` | カード 104 の使用条件（GoodConditionTurns・GreaterOrEqual・2） |
| `Create_Null_ThrowsArgumentNull` | `Create(null)` で `ArgumentNullException` |
| `EnumMirrors_MatchDomain` | `[TestCaseSource]` で `MasterDataTests.EnumPairs` と同じ 11 組（Plan・CardType・Rarity・ParamKind・EffectOp・PersistentTrigger・StatusKind・CostKind・ConditionSubject・Comparator・CardFaceKind）。`Enum.GetNames` が等しく、各名前の数値も等しい。**キャストで詰めているため、ずれると別の値に化ける。これがクライアントの安全網** |
| `LoadFromStreamingAssetsAsync_ReturnsCatalog` | `[UnityTest]`、`UniTask.ToCoroutine(async () => { … })`。`CardIds.Count == 36` |

### タスク 2: 起動時の読み込みと DI 登録

- 触る: 3章のタスク 2 の行
- 手順: 4.2 のとおり
- 境界: マスタの読み込みが失敗したときはスコープを作らない（認証に進まない）。成功時の動作（認証・疎通）は変えない
- 確かめ方:
  1. `unity recompile` のエラー 0
  2. 成功の経路（Editor の Play モード）: コンソールに `[Uta] master: ok cards=36` が出て、続けて既存の `sign-in` などの行が出る（サーバーを動かさなければ `connectivity check: FAILED` でよい。マスタの行が出ることを見る）
  3. 失敗の表示（Editor の Play モード）: 一時的に `MasterDataLoader.RelativePath` を存在しないパスに変えて Play し、`StartupView` に `master: FAILED …` が出てスコープが作られない（`sign-in` の行が出ない）ことを確かめて、元に戻す（コミットしない）。Play モードではコマンドライン引数を渡せず `Application.Quit` も効かないので、終了コードはここでは確かめない
  4. 終了コードと IL2CPP（任意。Editor を閉じて行う）: CLAUDE.md の Windows の IL2CPP のプレイヤーのビルド → `Uta.exe -batchmode -nographics -uta-quit-after-check`。サーバーを動かして終了コード 0、動かさなければ 1（マスタの行はログに出る）。失敗の経路（RelativePath を壊したビルド）の終了コード 1 も同じ方法で見られる。やらなかった項目は「確認できていない」と報告する（結果を推測で「通った」と書かない）。これは `CardCatalog.Create`（LINQ・`Dictionary`）の IL2CPP での確認も兼ねる

### タスク 3: wiki への反映（メインのセッション）

- screen-design.md 4章は計画のコミットで直し済み。実装でズレたら直す
- masterdata-pipeline.md 実装メモ、lesson-domain-types.md の「クライアント側の詰め替えは未実装」を実装済みに、repository-layout.md 8章の要検証の行（`CardCatalog` の IL2CPP は未確認と書く）、[game-spec.md](../wiki/overview/game-spec.md) の実装計画の表に 2d の行（この計画。待つもの: 2c）
- 計画を `status: done` にする

## 6. テスト

5章の表のとおり（EditMode のみ。Unity でしか確かめられない）。サーバー・Shared の既存テストは変えない。実行コマンドは `unity test --mode EditMode` と、変更後の `dotnet build Uta.slnx`（警告 0・エラー 0）。

### 確かめられないこと（未確認のまま残す）

- Android・iOS での読み込み。Windows の IL2CPP での `CardCatalog.Create` は、タスク 2 の確かめ方 4（任意）で確かめられる
- Bootstrap の失敗経路の自動テスト（手動で確かめる: タスク 2 の確かめ方 3・4）

## 7. 未決事項

なし。ユーザーが推奨どおりと決めた（2026-11-01）: (1) マスタは認証より前に読む。(2) 読み込みに失敗したらリトライせず、表示して止まる。(3) `UtaBootstrap` が読み込む（Application にポートは足さない）。(4) Bootstrap は `MemoryDatabase`（`Uta.MasterData`）を触らず、`MasterCardCatalog.LoadFromStreamingAssetsAsync` で包む。このため、カード以外の変換を足す計画で `MemoryDatabase` を一度だけ読んで共有する形に直す（今回はカタログを作ったら捨てる）。

## 8. 実装結果（2026-11-01）

- 計画どおりに実装した（Sonnet のサブエージェント）。Infrastructure の `MasterCardCatalog`、`UtaBootstrap` の非同期化と `ICardCatalog` の登録、EditMode テスト（`MasterCardCatalogTests` 18 件）。asmdef は Bootstrap とテストに `Uta.Shared.Domain` を足した
- `unity test --mode EditMode` で 26 件（既存 7 件を含む）すべて通る。コンパイルは、そのバッチ実行で `error CS` 0 件
- 計画との違い: 新しい worktree には `Assets/Packages/`（NuGet の復元物）が無くコンパイルが通らないので、メインの checkout からコピーして通した（gitignore 済み）。`unity recompile` はメインの Editor を相手にしたので使えなかった
- **確認できていない**: Play モードでの `master: ok` の表示と失敗の表示（確かめ方 3）、Windows の IL2CPP と終了コード（確かめ方 4）、Android・iOS
- コードレビュー（requesting-code-review、client）: 判定は「はい」。Critical・Important なし。Minor 6 件は直さない（`ServerEndpoint.Resolve` の例外は try の外で、以前と同じ。キャンセルを FAILED と表示しない分岐は、トークンを渡すときに足す）
