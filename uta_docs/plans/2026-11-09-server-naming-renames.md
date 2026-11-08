---
title: 実装計画: サーバーだけで閉じる型の改名（naming.md の差の表のサーバーの行）
status: in-progress
updated: 2026-11-09
spec: ../wiki/architecture/naming.md, ../wiki/architecture/server/server-foundation.md, ../wiki/architecture/server/gacha-support-server.md, ../wiki/architecture/server/produce-server.md
---

# 実装計画: サーバーだけで閉じる型の改名

## この計画で決めたこと

- **前提と順番**（2026-11-09。ユーザーの判断）: この計画は、同じブランチ（`claude/plan-server-program-cleanup`）の [Program.cs の整理の計画](2026-11-08-server-program-cleanup.md) と 1 つの計画 PR に入れる。実装も 1 つのセッション・1 つのブランチで、**Program.cs の整理の計画を先に終えてから**この計画を進める。**実装の PR は 2 つの計画で 1 本にする**（1 つのブランチで続けて実装するため。[plans/README.md](README.md) の「実装の PR は計画ごとに分ける」の例外。ユーザーの判断に従う）。理由: Program.cs の整理の計画は、起動の組み立てを `Program.cs` から新しい `Hosting/ServerHost.cs` に移し、その完成形を今の名前（`ProduceViewBuilder`・`UtaDatabase`）で文字どおり書いており、新しく作る `ServiceProviderValidationTests` も `UtaServerFactory` を使う。後から改名すれば、どちらの計画も書いたとおりに実装できる。よって、この計画の行番号・コードは **Program.cs の整理の実装が終わった後**の形を前提にする（`ServerHost.cs` の完成形は整理の計画の 3.1。整理の後の `Program.cs` は `return ServerHost.Run(args);` と `public partial class Program;` だけで、旧い名前を書いていない）
- **`GachaStatus` の新しい名前は `GachaEntryResult`**（2026-11-09。ユーザーの判断。`GachaExchangePointsResult` と比べ、ガチャ一覧の 1 件分と読め、後で開催期間などを足しても名前が合うほうを選んだ）。naming.md の未決事項からこの項を消す
- **改名する型**（naming.md「今のコードとの差」の行のとおり）:

  | 今 | 新 | ファイル |
  |---|---|---|
  | `GrantOutcome`（enum） | `GrantResultKind` | `Domain/Gacha/GrantOutcome.cs` → `GrantResultKind.cs` |
  | `GachaStatus`（record） | `GachaEntryResult` | `Application/Gacha/GetGachasUseCase.cs` の中のまま |
  | `ProduceView`（record） | `ProduceStateResult` | `Application/Produce/ProduceView.cs` → `ProduceStateResult.cs` |
  | `ProduceEndSummary`（record） | `ProduceEndResult` | `ProduceView.cs` から `Application/Produce/ProduceEndResult.cs` に分ける |
  | `ProduceViewBuilder`（class） | `ProduceStateResultFactory` | `Application/Produce/ProduceViewBuilder.cs` → `ProduceStateResultFactory.cs` |
  | `UtaServiceAttribute`（`[UtaService]`） | `GameServiceFilterAttribute`（`[GameServiceFilter]`） | `Api/UtaServiceAttribute.cs` → `GameServiceFilterAttribute.cs` |
  | `UtaDatabase`（static class） | `DatabaseMigrator` | `Infrastructure/UtaDatabase.cs` → `DatabaseMigrator.cs` |
  | テストの `UtaServerFactory` | `ServerFactory` | `Integration/Support/UtaServerFactory.cs` → `ServerFactory.cs` |
  | テストのメソッド `Services_HaveUtaServiceAttribute` | `Services_HaveGameServiceFilterAttribute` | `Architecture/ArchitectureTests.cs` の中 |

- **型と一緒に変えるメンバーの名前**（型の名前に合わせる。naming.md 1章・7章）:
  - `ProduceViewBuilder.Build(...)` → `ProduceStateResultFactory.Create(...)`（Factory の作るメソッドは `Create`。`CardCatalogFactory.Create`・`ProduceCatalogFactory.Create` と同じ）
  - 主コンストラクターの引数 `ProduceViewBuilder viewBuilder` → `ProduceStateResultFactory resultFactory`（`StartProduceUseCase`・`GetProduceStateUseCase`・`ProduceOperationUseCase`）
  - `ProduceResponseMapper.ToStateResponse(ProduceView view)` の引数 `view` → `result`
  - サーバーの `GachaResult` のプロパティ `Outcome`（型 `GrantOutcome`）→ `ResultKind`、`GachaHistoryRecord` のプロパティ `Outcome`（`int`）→ `ResultKind`（naming.md の「プロパティ名の `Outcome`」の行のうち、サーバーだけの型の分。1章「`Outcome` は使わない」。DB の列 `outcome` は変えず、`GachaRepository` の INSERT の引数 `outcome = row.ResultKind` で写す。Contracts の `DrawResultDto.Outcome` は対象外）
- **`GachaEntryResult` は client にも使う**: naming.md 1章「サーバーとクライアントで、同じ役割には同じ名前」に従い、client の gacha の計画にある `GachaStatus`（class。`GetGachasAsync` の 1 件分）も、gacha の読み替えで `GachaEntryResult` にする。naming.md の差の表に、その行を足す（3.4）。[client の画面の索引](2026-11-07-client-screens-index.md)の gacha への申し送りにも書いた（2026-11-09。ユーザーの判断。計画を書く段階で直した）。client のコードと gacha・foundation の計画はこの計画では直さない
- **formation の計画との重なり**（2026-11-09。ユーザーの判断。改名を formation に任せる案と比べ、formation は P3 待ちで後になるので、こちらで先に済ませるほうを選んだ）: [formation の計画](2026-11-03-client-screens-formation.md)のタスク 1 も `ProduceView`・`ProduceViewBuilder`・`ProduceEndSummary` の改名を受け持っていた。この 3 つはこの計画で直し、formation の読み替えの表（R の 26 行目の行）に「済んでいれば飛ばす」を書き足した（計画を書く段階で直した）。`ClassOptionView`・`ExamOutcome` の改名は formation のまま
- **変えない名前**:
  - `GachaResult.ToLogJson` のキー `"outcome"`（ゲームログの形。ログを読む側の都合で、型の改名と分ける）
  - 型が Shared.Domain の `ExamOutcome` のプロパティ `ExamOutcome`（`ProduceEndResult.ExamOutcome`・`ProduceResultRecord.ExamOutcome`）。型の改名（`ExamResultKind`）と一緒に Shared.Domain の計画で直す。naming.md の「プロパティ名」の行にその旨を書き足す（3.4）
  - `ProduceEndResult` のプロパティ `Ending`（naming.md の差の表に無い）
  - `ProduceStateResult.None`（静的なフィールドの名前はそのまま）
  - XML ドキュメントのコメントは、旧い型名を書いている箇所だけ新しい名前に書き換える（下の手順に箇所を挙げる）
- **ファイルの改名は `git mv`** で行う（履歴を追えるように）。`.cs` は BOM 付き UTF-8・LF のまま
- **`ProduceView.cs` を 2 つに分ける**: `ProduceEndResult` は `AbandonProduceUseCase`・`ProduceCompletion` が `ProduceStateResult` と別に返すので、自分のファイルに置く。`GachaEntryResult` は `GetGachasUseCase` だけが返すので、今のとおり UseCase のファイルに残す

## ユーザーの確認が要ること

なし（進め方と `GachaStatus` の名前は 2026-11-09 にユーザーが決めた。上の節に移した）。

## 1. 目的と前提

[naming.md](../wiki/architecture/naming.md)「今のコードとの差（改名の対象）」のうち、サーバー（`server/`）だけで閉じる行を直し、表から消す。動き（RPC・DB・ゲームログ）は変えない。

- 関係する ADR: なし（命名の規則は naming.md が正）
- 設計: [naming.md](../wiki/architecture/naming.md) 1章（`Uta` を付けない・`View` はクライアントだけ・`Outcome` は使わない）・2章（結果の値・結果の種類）・7章（Factory）

### 決めないこと（範囲外）

- **`shared/`（Shared.Domain・Contracts）の改名**（`GachaOutcome`・`ExamOutcome`・`UseCheckResult` などの enum、`ClassOptionView`、`ExamTurnBuilder`、Contracts のプロパティ `Outcome`・`ExamOutcome`）。client の画面の計画（foundation〜lesson）が実装中・予定で、client のコードにも波及するため、client の計画の読み替えと合わせて別の計画で直す。naming.md の該当の行は残す
- Contracts の `GachaStatusDto` の名前（naming.md の差の表に無い）
- DB の列名・ゲームログのキー・MessagePack のキー
- `ServerHost.cs` の登録の並び・寿命（[Program.cs の整理の計画](2026-11-08-server-program-cleanup.md) が決める。この計画は型の名前だけを書き換える）

## 2. 触るファイル

パスは `server/Uta.Server/`（`S/`）・`server/Uta.Server.Tests/`（`T/`）からの相対で書く。

| 操作 | パス | 中身 | タスク |
|---|---|---|---|
| 改名＋変更 | `S/Domain/Gacha/GrantOutcome.cs` → `GrantResultKind.cs` | enum の名前 | 1 |
| 変更 | `S/Domain/Gacha/ItemGrantRules.cs` | 戻り値の型 | 1 |
| 変更 | `S/Application/Gacha/GachaResult.cs` | プロパティ `Outcome` → `ResultKind`、型 | 1 |
| 変更 | `S/Application/Gacha/ItemGranter.cs` | `GrantOutcome` → `GrantResultKind` | 1 |
| 変更 | `S/Application/Gacha/DrawUseCase.cs`・`ExchangeUseCase.cs` | `result.Outcome` → `result.ResultKind` | 1 |
| 変更 | `S/Application/Gacha/GetGachasUseCase.cs` | `GachaStatus` → `GachaEntryResult` | 1 |
| 変更 | `S/Domain/Gacha/GachaHistoryRecord.cs`・`S/Infrastructure/GachaRepository.cs` | プロパティ `Outcome` → `ResultKind` | 1 |
| 変更 | `S/Api/GachaService.cs` | `x.Outcome` → `x.ResultKind` | 1 |
| 変更 | `T/Domain/ItemGrantRulesTests.cs`・`T/MasterData/ContractsEnumsMatchTests.cs` | `GrantOutcome` → `GrantResultKind` | 1 |
| 改名＋変更 | `S/Application/Produce/ProduceView.cs` → `ProduceStateResult.cs` | `ProduceStateResult` だけを残す | 2 |
| 作成 | `S/Application/Produce/ProduceEndResult.cs` | `ProduceEndResult`（`ProduceView.cs` から移す） | 2 |
| 改名＋変更 | `S/Application/Produce/ProduceViewBuilder.cs` → `ProduceStateResultFactory.cs` | クラス名・`Build` → `Create` | 2 |
| 変更 | `S/Application/Produce/StartProduceUseCase.cs`・`GetProduceStateUseCase.cs`・`ProduceOperationUseCase.cs`・`AbandonProduceUseCase.cs`・`ProduceCompletion.cs` | 型名・引数名 | 2 |
| 変更 | `S/Api/ProduceResponseMapper.cs` | 型名・引数名・コメント | 2 |
| 変更 | `S/Hosting/ServerHost.cs`（Program.cs の整理で作る） | `AddServices` の `ProduceViewBuilder` の登録（2）、`Migrate` メソッドの `UtaDatabase.Migrate`（3） | 2・3 |
| 改名＋変更 | `S/Api/UtaServiceAttribute.cs` → `GameServiceFilterAttribute.cs` | クラス名 | 3 |
| 変更 | `S/Api/GachaService.cs`・`ProduceService.cs`・`SupportService.cs`・`PlayerService.cs` | `[UtaService]` → `[GameServiceFilter]` | 3 |
| 改名＋変更 | `S/Infrastructure/UtaDatabase.cs` → `DatabaseMigrator.cs` | クラス名・`typeof` | 3 |
| 改名＋変更 | `T/Integration/Support/UtaServerFactory.cs` → `ServerFactory.cs` | クラス名・コンストラクター名 | 3 |
| 変更 | `T/Integration/Support/PostgresFixture.cs` | `UtaDatabase.Migrate` → `DatabaseMigrator.Migrate` | 3 |
| 変更 | `T/Integration/*.cs`（`UtaServerFactory` を使う全ファイル。`ServiceProviderValidationTests.cs` を含む） | `UtaServerFactory` → `ServerFactory` | 3 |
| 変更 | `T/Architecture/ArchitectureTests.cs` | メソッド名・`GetCustomAttribute<…>` | 3 |
| 変更 | `uta_docs/wiki/architecture/naming.md` | 差の表の行・未決事項・前書き | 4 |
| 変更 | `uta_docs/wiki/architecture/server/server-foundation.md`・`gacha-support-server.md`・`produce-server.md` | 旧い名前 | 4 |

## 3. 型とシグネチャ

### 3.1 ガチャ（タスク 1）

```csharp
// S/Domain/Gacha/GrantResultKind.cs
namespace Uta.Server.Domain.Gacha;

/// <summary>GachaOutcome（Contracts）と同じ数値。</summary>
public enum GrantResultKind
{
    New = 1,
    Duplicate = 2,
}
```

- `ItemGrantRules.Decide` の戻り値: `public static GrantResultKind Decide(GrantedItemKind kind, bool idolOwned, bool supportCardMaxed)`
- `GachaResult`: `public sealed record GachaResult(int Slot, GrantedItemKind Kind, int ItemId, GrantResultKind ResultKind, int Shards, int SupportPoints, Ulid? SupportCardUid)`。`ToLogJson` の `["outcome"] = (int)Outcome` は `["outcome"] = (int)ResultKind` にする（キーは変えない）
- `GetGachasUseCase.cs`: `public sealed record GachaEntryResult(int GachaId, int ExchangePoints);`、`ExecuteAsync` の戻り値は `Task<IReadOnlyList<GachaEntryResult>>`、`Select` の中は `new GachaEntryResult(...)`
- `GachaService.cs` の `(GachaOutcome)(int)x.Outcome` → `(GachaOutcome)(int)x.ResultKind`
- `DrawUseCase.cs`・`ExchangeUseCase.cs` の `(int)result.Outcome` → `(int)result.ResultKind`（`GachaHistoryRecord` の位置引数）
- `Domain/Gacha/GachaHistoryRecord.cs`: 引数 `int Outcome` → `int ResultKind`（位置は変えない）
- `Infrastructure/GachaRepository.cs` の `outcome = row.Outcome,` → `outcome = row.ResultKind,`（SQL の列名・パラメーター名 `outcome` は変えない）

### 3.2 プロデュース（タスク 2）

```csharp
// S/Application/Produce/ProduceEndResult.cs
using Uta.Server.Domain.Produce;
using Uta.Shared.Domain.Produce;

namespace Uta.Server.Application.Produce;

public sealed record ProduceEndResult(
    ProduceSessionStatus Ending, ExamOutcome ExamOutcome, int EvaluationValue, ProduceRank Rank, int FinalScore, int FinalRank,
    int GachaCurrency, int Gold, int SupportPoints, int Exp, int PlayerLevel, int PlayerExp);
```

```csharp
// S/Application/Produce/ProduceStateResult.cs
using Uta.Shared.Domain.Produce;

namespace Uta.Server.Application.Produce;

/// <summary>RPC のレスポンスを作るための材料。State が null は「進行中のセッション無し」。</summary>
public sealed record ProduceStateResult(
    ProduceState? State, IReadOnlyList<WeekChoice> WeekChoices, IReadOnlyList<ClassOptionView> ClassOptions,
    IReadOnlyList<int> NpcScores, ExamReport? ExamResult, ProduceEndResult? End)
{
    public static readonly ProduceStateResult None = new(null, [], [], [], null, null);
}
```

- `ProduceStateResult.cs` の `using` は上のとおり `using Uta.Shared.Domain.Produce;` の 1 行だけにする（今の `ProduceView.cs` の `using Uta.Server.Domain.Produce;` は `ProduceSessionStatus` のためで、`ProduceEndResult.cs` に移る。`ProduceState`・`WeekChoice`・`ClassOptionView`・`ExamReport` はすべて `Uta.Shared.Domain.Produce` にある。2026-11-09 に確かめた）

```csharp
// S/Application/Produce/ProduceStateResultFactory.cs（本体は今の ProduceViewBuilder.Build と同じ）
public sealed class ProduceStateResultFactory(ProduceEngine engine, ProduceCatalog catalog)
{
    public ProduceStateResult Create(ProduceState state, ExamReport? examResult, ProduceEndResult? end)
    {
        // 今の Build の本体のまま。`return new ProduceView(` を `return new ProduceStateResult(` にする
    }
}
```

- `StartProduceUseCase`・`ProduceOperationUseCase`・`GetProduceStateUseCase`: 主コンストラクターの `ProduceViewBuilder viewBuilder` → `ProduceStateResultFactory resultFactory`、`ExecuteAsync` の戻り値 `Task<ProduceView>` → `Task<ProduceStateResult>`、`viewBuilder.Build(` → `resultFactory.Create(`、`ProduceView.None` → `ProduceStateResult.None`。`GetProduceStateUseCase` の XML コメント「`ProduceView.None`」→「`ProduceStateResult.None`」
- `ProduceOperationUseCase`: `ProduceEndSummary? end = null;` → `ProduceEndResult? end = null;`
- `AbandonProduceUseCase.ExecuteAsync` の戻り値と `new ProduceEndSummary(` → `ProduceEndResult`
- `ProduceCompletion.CompleteAsync` の戻り値と `new ProduceEndSummary(` → `ProduceEndResult`
- `ProduceResponseMapper`: XML コメントを `/// <summary>ProduceStateResult / ProduceEndResult を Contracts のレスポンスに変える。</summary>` に、`ToStateResponse(ProduceView view)` を `ToStateResponse(ProduceStateResult result)` にし、本体の `view.` をすべて `result.` にする（`view.State`・`view.ExamResult`・`view.WeekChoices`・`view.ClassOptions`・`view.NpcScores`・`view.End` の 6 種類。`view.End` は 1 行に 2 回出るので、全部で 7 か所）。`ToResultResponse(ProduceEndSummary end)` → `ToResultResponse(ProduceEndResult end)`
- `Hosting/ServerHost.cs` の `AddServices`: `services.AddSingleton<ProduceViewBuilder>();` → `services.AddSingleton<ProduceStateResultFactory>();`（行の位置は変えない。`using Uta.Server.Application.Produce;` は既にあるので足さない）
- `ProduceService.cs` は `ProduceResponseMapper` を呼ぶだけで旧い型名を書いていない（`git grep` で確かめた。2026-11-09）

### 3.3 Api・Infrastructure・テストの基盤（タスク 3）

- `GameServiceFilterAttribute.cs`: `public sealed class UtaServiceAttribute : MagicOnionFilterAttribute` → `public sealed class GameServiceFilterAttribute : MagicOnionFilterAttribute`。ほか（XML コメント・`ToErrorCode`）は変えない
- `[UtaService]` → `[GameServiceFilter]`: `GachaService.cs`（クラス）・`ProduceService.cs`（クラス）・`SupportService.cs`（クラス）・`PlayerService.cs`（`SignInAsync` のメソッド）の 4 か所
- `DatabaseMigrator.cs`: `public static class UtaDatabase` → `public static class DatabaseMigrator`、`typeof(UtaDatabase).Assembly` → `typeof(DatabaseMigrator).Assembly`。例外の文「Uta のマイグレーション …」・スクリプト名の接頭辞 `"Uta.Server.Migrations."` は変えない
- `Hosting/ServerHost.cs` の `Migrate` メソッドの `UtaDatabase.Migrate(connectionString);` → `DatabaseMigrator.Migrate(connectionString);`（`using Uta.Server.Infrastructure;` は既にあるので足さない）
- `PostgresFixture.cs` の `UtaDatabase.Migrate(ConnectionString);` 2 か所 → `DatabaseMigrator.Migrate(ConnectionString);`
- `ServerFactory.cs`: `public sealed class UtaServerFactory : WebApplicationFactory<Program>` → `public sealed class ServerFactory : WebApplicationFactory<Program>`、コンストラクター `public UtaServerFactory(` → `public ServerFactory(`
- `T/Integration/` の全ファイルの `UtaServerFactory` → `ServerFactory`（タプルの型・`new`・戻り値の型。2026-11-09 の時点で `CommonResponseTests`・`DeviceRegistrationTests`・`GachaTests`・`PlayerSignUpSignInTests`・`ProduceOperationTests`・`ProducePlayTests`・`ProduceStartTests`・`SupportTests`。Program.cs の整理で足す `ServiceProviderValidationTests` も含む）
- `ArchitectureTests.cs`: `public void Services_HaveUtaServiceAttribute()` → `public void Services_HaveGameServiceFilterAttribute()`、`t.GetCustomAttribute<UtaServiceAttribute>()` → `t.GetCustomAttribute<GameServiceFilterAttribute>()`

### 3.4 wiki（タスク 4）

`uta_docs/wiki/architecture/naming.md`:

- 「今のコードとの差」の表で、次のように直す
  - `| Shared.Domain の \`ExamOutcome\`、サーバーの \`GrantOutcome\` | \`ExamResultKind\`・\`GrantResultKind\` | 2章 |` → `| Shared.Domain の \`ExamOutcome\` | \`ExamResultKind\` | 2章 |`
  - 「プロパティ名の `Outcome`・`ExamOutcome`」の行の根拠の列の末尾に「サーバーの `ProduceEndResult.ExamOutcome`・`ProduceResultRecord.ExamOutcome` は、Shared.Domain の `ExamOutcome` の改名と同じ計画で直す」を足す
  - 「計画にある `ProduceProgress`・`ProduceSession`」の行の直後に、次の行を足す: `| 計画にある client の \`GachaStatus\`（class。\`GetGachasAsync\` の 1 件分） | \`GachaEntryResult\`（サーバーと同じ名前） | 1章・2章 |`
  - 次の 4 行を消す: `サーバーの \`GachaStatus\``・`サーバーの \`ProduceView\`・\`ProduceViewBuilder\``・`サーバーの \`ProduceEndSummary\``・`\`UtaServiceAttribute\`・\`UtaDatabase\`…` で始まる行
- 「未決事項」の箇条を `- Bootstrap の \`ErrorDialogView\` の置き場か名前、古い View 4 つの片付け、\`OwnedSelectionRequest\` の新しい名前。触る実装計画（読み替え）で決める` にする（`GachaStatus` の分を消す）
- 前書きの「ADR 0020 の分（Catalog をやめる）は…で済んだ。」の直後に、次の文を足す（同じ段落の続き。リンクは naming.md からの相対パス）

  ```markdown
  サーバーだけで閉じる改名（`GrantResultKind`・`GachaEntryResult`・`ProduceStateResult`・`ProduceStateResultFactory`・`ProduceEndResult`・`GameServiceFilterAttribute`・`DatabaseMigrator`・テストの `ServerFactory`）は[サーバーの改名の計画](../../plans/2026-11-09-server-naming-renames.md)で済んだ。
  ```
- frontmatter の `updated` を実装した日にする

`uta_docs/wiki/architecture/server/server-foundation.md`（25 行目の箇条）: `` `[UtaService]`（`Api/UtaServiceAttribute.cs`） `` → `` `[GameServiceFilter]`（`Api/GameServiceFilterAttribute.cs`） ``、`ArchitectureTests.Services_HaveUtaServiceAttribute` → `ArchitectureTests.Services_HaveGameServiceFilterAttribute`。`updated` を実装した日にする

`uta_docs/wiki/architecture/server/gacha-support-server.md`（83・84 行目）: `` `[UtaService]`(`UtaServiceAttribute`) `` → `` `[GameServiceFilter]`(`GameServiceFilterAttribute`) ``、`` **`[UtaService]`** `` → `` **`[GameServiceFilter]`** ``、`Services_HaveUtaServiceAttribute` → `Services_HaveGameServiceFilterAttribute`。`updated` を実装した日にする

`uta_docs/wiki/architecture/server/produce-server.md`:

- 167 行目: `` `ProduceService`(`[UtaService]`) `` → `` `ProduceService`(`[GameServiceFilter]`) ``、`` (`ProduceView` → `ProduceStateResponse` / `ProduceResultResponse`) `` → `` (`ProduceStateResult` → `ProduceStateResponse`、`ProduceEndResult` → `ProduceResultResponse`) ``
- 168 行目: `` `ProduceView`、`ProduceViewBuilder` `` → `` `ProduceStateResult`、`ProduceEndResult`、`ProduceStateResultFactory` ``
- 182 行目: `` **`[UtaService]`** `` → `` **`[GameServiceFilter]`** ``
- `updated` を実装した日にする

行番号は 2026-11-09 の main のもの。ずれていたら、旧い名前の文字列で探す。

## 4. タスク

各タスクの前提: [Program.cs の整理の計画](2026-11-08-server-program-cleanup.md) のタスク 1〜3 が同じブランチで終わっていること（`server/Uta.Server.Tests/Integration/ServiceProviderValidationTests.cs` と `server/Uta.Server/Hosting/ServerHost.cs` があること）。無ければ止まって報告する。

### タスク 1: ガチャの型を改名する

- 触るファイル: 2章のタスク 1 の行
- 手順: `git mv server/Uta.Server/Domain/Gacha/GrantOutcome.cs server/Uta.Server/Domain/Gacha/GrantResultKind.cs` のあと、3.1 のとおりに書き換える。`GrantOutcome` は `server/` のすべての出現を `GrantResultKind` にする（`GrantResultKind.cs`・`ItemGrantRules.cs`・`GachaResult.cs`・`ItemGranter.cs`・`ItemGrantRulesTests.cs`・`ContractsEnumsMatchTests.cs` の 6 ファイル）
- 確かめ方:
  - `git grep -nw "GrantOutcome\|GachaStatus" -- server` が 0 件
  - `git grep -n "\.Outcome\b" -- server/Uta.Server/Application/Gacha server/Uta.Server/Api/GachaService.cs server/Uta.Server/Infrastructure/GachaRepository.cs` が 0 件、`git grep -nw "Outcome" -- server/Uta.Server/Domain/Gacha/GachaHistoryRecord.cs` が 0 件
  - `dotnet build Uta.slnx`（警告 0・エラー 0）、`dotnet test --project server/Uta.Server.Tests` がすべて通る

### タスク 2: プロデュースの結果の型を改名する

- 触るファイル: 2章のタスク 2 の行
- 手順:
  1. `git mv server/Uta.Server/Application/Produce/ProduceView.cs server/Uta.Server/Application/Produce/ProduceStateResult.cs`、`git mv server/Uta.Server/Application/Produce/ProduceViewBuilder.cs server/Uta.Server/Application/Produce/ProduceStateResultFactory.cs`
  2. `ProduceEndResult.cs` を作る（BOM 付き UTF-8・LF。3.2）。`ProduceStateResult.cs` から `ProduceEndSummary` の宣言を消し、3.2 の形にする
  3. 3.2 の箇条のとおりに、残りのファイルを書き換える
- 確かめ方:
  - `git grep -nw "ProduceView\|ProduceViewBuilder\|ProduceEndSummary\|viewBuilder" -- server` が 0 件（`client/` の `ProduceView` は対象外）
  - `dotnet build Uta.slnx`（警告 0・エラー 0）、`dotnet test --project server/Uta.Server.Tests` がすべて通る

### タスク 3: Api・Infrastructure・テストの基盤の型を改名する

- 触るファイル: 2章のタスク 3 の行
- 手順:
  1. `git mv server/Uta.Server/Api/UtaServiceAttribute.cs server/Uta.Server/Api/GameServiceFilterAttribute.cs`、`git mv server/Uta.Server/Infrastructure/UtaDatabase.cs server/Uta.Server/Infrastructure/DatabaseMigrator.cs`、`git mv server/Uta.Server.Tests/Integration/Support/UtaServerFactory.cs server/Uta.Server.Tests/Integration/Support/ServerFactory.cs`
  2. 3.3 の箇条のとおりに書き換える
- 確かめ方:
  - `git grep -nw "UtaServiceAttribute\|UtaService\|UtaDatabase\|UtaServerFactory\|Services_HaveUtaServiceAttribute" -- server` が 0 件
  - `dotnet build Uta.slnx`（警告 0・エラー 0）
  - `dotnet test --project server/Uta.Server.Tests` がすべて通り、`ArchitectureTests.Services_HaveGameServiceFilterAttribute` と `ServiceProviderValidationTests.Build_ProductionWithSingletonCapturingScoped_Throws` が含まれる

### タスク 4: wiki を直す

- 触るファイル: 2章のタスク 4 の行
- 手順: 3.4 のとおりに直す
- 確かめ方:
  - `git grep -nw "GrantOutcome\|ProduceViewBuilder\|ProduceEndSummary\|UtaServiceAttribute\|UtaService\|UtaDatabase\|UtaServerFactory" -- uta_docs/wiki` が 0 件（`uta_docs/plans/`・`uta_docs/raw/` の旧い名前は記録なので直さない）
  - `git grep -nw "GachaStatus" -- uta_docs/wiki` が、3.4 で naming.md に足した client の行の 1 件だけ
  - `git grep -nw "ProduceView" -- uta_docs/wiki/architecture/server` が 0 件
  - `python -I .claude/skills/linting-wiki/lint_wiki.py` がエラー 0

## 5. テスト

足すテストは無い（名前だけの変更で、動きを変えない）。既存のテストの改名だけ:

| テスト | 変更 |
|---|---|
| `ArchitectureTests.Services_HaveUtaServiceAttribute` | `Services_HaveGameServiceFilterAttribute` に改名。中身は型名だけ |
| `ItemGrantRulesTests.Decide_ByKindAndState`・`ContractsEnumsMatchTests.ItemKind_MatchesMasterAndApplication` | 型名だけ（`GrantResultKind`） |
| 結合テスト（全件） | `ServerFactory` を使う形に。ガチャ・プロデュースの応答が変わらないことを、今の結合テストがそのまま確かめる |

実行するコマンド（各タスクの終わり）:

- `dotnet build Uta.slnx`
- `dotnet test --project server/Uta.Server.Tests`（結合テストは Docker が要る）

## 6. 未決事項

なし。

## 7. 実装メモ

| タスク | コミット | ズレ・メモ |
|---|---|---|
| 1 | d1ded85 | なし。ログのキー `["outcome"]` は計画どおり残した |
| 2 | 756945a | なし |
| 3 | 26895df | なし |
| 4 | 6127f59 | なし |
