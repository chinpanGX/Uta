---
title: 実装計画: サーバーのプロデュース 開始・状態・放棄・保存
status: draft
updated: 2026-11-03
spec: ../wiki/architecture/server/produce-server.md, ../wiki/architecture/produce-domain.md, ../wiki/architecture/api-design.md, ../wiki/systems/produce.md
---

# 実装計画: サーバーのプロデュース 開始・状態・放棄・保存

## 1. 目的と前提

順 6（サーバーのプロデュース）の計画 2/3。`StartAsync`・`GetStateAsync`・`AbandonAsync` と、状態をセッションの 1 列に保存して復元する土台（テーブル・MessagePack・乱数ストリーム・マスタの詰め替え）、`SignInAsync` の「進行中のプロデュース」の返却と破棄を作る。操作の RPC（週の行動・カードの選択肢・試験の再挑戦・レッスン）と終了の報酬は [次の計画](2026-11-03-server-produce-play.md)。

- 前提（先にマージされていること）: [ProduceSnapshot の計画](2026-11-03-produce-snapshot.md)（`ProduceSnapshot.From` / `ToState` が使える）
- 仕様・設計の正: [produce-server.md](../wiki/architecture/server/produce-server.md)（テーブル・保存の形・処理の手順・DTO の形）、[produce-domain.md](../wiki/architecture/produce-domain.md) 5〜6章、[api-design.md](../wiki/architecture/api-design.md) 2・5章、[idol-support.md](../wiki/systems/idol-support.md) 2.1（編成の検証）
- 関係する ADR: [0010](../wiki/decisions/0010-database.md)、[0012](../wiki/decisions/0012-migration-dbup.md)、[0008](../wiki/decisions/0008-shared-domain-and-onion.md)
- 受け持つ完了条件: game-spec の 3「次のプロデュースの開始時の性能への反映」（結合テスト 5 で確かめる）
- この計画で**決めないこと**: 操作の RPC、報酬・プレイヤーLv・`produce_results` の完了行（次の計画）。レンタル（`RentalCandidateId` は 0 だけ）。クライアント（順 7）

## 2. 触るファイル

`C` = `shared/Uta.Shared.Contracts`、`S` = `server/Uta.Server`、`T` = `server/Uta.Server.Tests`。Contracts の新しいファイルには `.meta` を付ける（フォルダの `.meta` は `C/Support.meta` をコピーして `guid` を新しい値に、ファイルの `.meta` は `C/Support/ISupportService.cs.meta` をコピーして `guid` を新しい値に。値は `python -c "import uuid;print(uuid.uuid4().hex)"`）。

| 操作 | パス | 中身 | タスク |
|---|---|---|---|
| 作成 | `C/Produce/`（フォルダ）に 4.1 の表のファイル | DTO・サービス IF・エラー | 1 |
| 作成 | `S/Migrations/0006_create_produce_tables.sql` | [produce-server.md](../wiki/architecture/server/produce-server.md) 3章の DDL（そのまま） | 2 |
| 作成 | `S/Domain/Produce/RandomStreamState.cs`、`S/Domain/Produce/SessionRandomStreams.cs` | 4.2 | 2 |
| 作成 | `S/Application/Produce/ProduceVersions.cs`、`ProduceSessionStatus.cs`、`ProduceSessionData.cs`（`ProduceSessionData`・`ProduceSessionRecord`・`ProduceResultRecord`）、`IProduceSessionRepository.cs` | 4.3 | 2 |
| 作成 | `S/Infrastructure/Produce/ProduceStateBlobs.cs`、`S/Infrastructure/Produce/ProduceStateSerializer.cs`、`S/Infrastructure/ProduceSessionRepository.cs` | 4.4 | 2 |
| 作成 | `S/Infrastructure/MasterData/MasterProduceCatalog.cs` | 4.5 | 3 |
| 変更 | `S/Program.cs` | 4.5・4.9 の登録 | 3、4 |
| 作成 | `S/Application/Produce/ProduceView.cs`、`ProduceViewBuilder.cs`、`ProduceSessionDiscarder.cs`、`StartProduceUseCase.cs`、`GetProduceStateUseCase.cs`、`AbandonProduceUseCase.cs` | 4.6 | 4 |
| 変更 | `S/Application/Common/UseCaseError.cs`、`S/Api/UtaServiceAttribute.cs` | 4.7 | 4 |
| 作成 | `S/Api/AppVersionHeader.cs`、`S/Api/ProduceResponseMapper.cs`、`S/Api/ProduceService.cs` | 4.8 | 4 |
| 変更 | `S/Application/Player/SignInUseCase.cs`、`SignInResult.cs`、`S/Api/PlayerService.cs` | 4.10 | 5 |
| 変更 | `T/Integration/Support/PlayerClient.cs` | 4.11 | 4 |
| 作成 | `T/Integration/Support/RpcAssert.cs` | 4.11 | 4 |
| 作成 | `T/Domain/SessionRandomStreamsTests.cs`、`T/Infrastructure/ProduceStateSerializerTests.cs`、`T/MasterData/MasterProduceCatalogTests.cs`、`T/Integration/ProduceStartTests.cs` | 5章 | 2〜5 |
| 変更 | `uta_docs/wiki/architecture/server/produce-server.md` ほか | 実装で決めたことの反映（メインのセッション） | 6 |

## 3. 書式

`server/` の `.editorconfig` に従う（C# 14・ファイルスコープ namespace・プライマリコンストラクタ可、Allman、`var`、BOM 付き UTF-8、LF）。`Contracts` と `shared/` は C# 9（ブロック形式の namespace、プライマリコンストラクタ・コレクション式なし）。ほかのサーバーのコードの書き方は、同じ層の既存ファイル（ガチャ・サポートカード）に合わせる。

## 4. 型とシグネチャ

### 4.1 Contracts（タスク 1）

`C/Produce/` に置く。namespace `Uta.Shared.Contracts.Produce`。DTO は `[MessagePackObject]`、`[SerializationConstructor]` のコンストラクタ（引数は下の表の順・camelCase）、`[Key(n)]` は 0 から宣言順、プロパティは `{ get; }`（`LevelUpRequest` と同じ形）。enum は `Uta.Shared.Domain` のものをそのまま使う（`using Uta.Shared.Domain.Produce;` と `using Uta.Shared.Domain.Lesson;`）。

| ファイル | 型 | フィールド（宣言順） |
|---|---|---|
| `IProduceService.cs` | `IProduceService : IService<IProduceService>` | `UnaryResult<ProduceStateResponse> StartAsync(StartProduceRequest request);`、`[NoSideEffects] UnaryResult<ProduceStateResponse> GetStateAsync();`、`UnaryResult<ProduceResultResponse> AbandonAsync();`（`using MagicOnion; using VolTech.Server.Abstractions;`。操作の 4 つは次の計画で足す） |
| `StartProduceRequest.cs` | `StartProduceRequest` | `int idolId`、`string[] supportCardUids`、`int rentalCandidateId`、`int scenarioId` |
| `ProduceEnding.cs` | `enum ProduceEnding` | `Nothing = 0`、`Completed = 1`、`Abandoned = 2`、`Discarded = 3` |
| `ProduceStateResponse.cs` | `ProduceStateResponse` | `int operationNumber`、`ProducePhase phase`、`int scenarioId`、`int idolId`、`int week`、`int singing`、`int rhythm`、`int expression`、`int hp`、`int maxHp`、`int examRetryLeft`、`DeckCardDto[] deck`、`WeekActionDto[] weekActions`、`ClassOptionDto[] classOptions`、`int[] cardOffers`、`LessonDto lesson`、`ExamResultDto examResult`、`ProduceResultResponse result` |
| `DeckCardDto.cs` | `DeckCardDto` | `int instanceId`、`int cardId`、`bool enhanced` |
| `WeekActionDto.cs` | `WeekActionDto` | `int slotIndex`、`WeekChoiceKind kind`、`bool selectable` |
| `ClassOptionDto.cs` | `ClassOptionDto` | `int index`、`int hpCost`、`ParamKind param`、`int paramGain`、`bool givesCard`、`bool selectable` |
| `LessonDto.cs` | `LessonDto` | `LessonKind kind`、`ParamKind targetParam`、`int maxTurns`、`int clearTarget`、`int perfectTarget`、`ExamTurnDto[] examTurns`、`int turn`、`int shield`、`int usesLeft`、`int paramGained`、`int[] hand`、`int[] drawPile`、`int[] discard`、`int[] exiled`、`int[] statusValues`、`ParamBonusDto[] paramBonuses`、`PersistentDto[] persistents`、`int[] lessonEnhanced`、`int[] npcScores` |
| `ExamTurnDto.cs` | `ExamTurnDto` | `ParamKind param`、`int scoreBonusPercent` |
| `ParamBonusDto.cs` | `ParamBonusDto` | `int percent`、`int remainingTurns` |
| `PersistentDto.cs` | `PersistentDto` | `PersistentTrigger trigger`、`int remainingTurns`、`int remainingCount` |
| `ExamResultDto.cs` | `ExamResultDto` | `ExamKind kind`、`int attempt`、`int score`、`int rank`、`int[] npcScores`、`bool passed` |
| `ProduceResultResponse.cs` | `ProduceResultResponse` | `ProduceEnding ending`、`ExamOutcome examOutcome`、`int evaluationValue`、`ProduceRank rank`、`int finalScore`、`int finalRank`、`int gachaCurrency`、`int gold`、`int supportPoints`、`int exp`、`int playerLevel`、`int playerExp` |
| `ProduceErrors.cs` | `static class ProduceErrors` | `public static readonly ErrorCode StaleOperation = new ErrorCode("uta", "PRODUCE_STALE_OPERATION");` と同じ形で `UnexpectedPhase`（`PRODUCE_UNEXPECTED_PHASE`）、`InvalidArgument`（`PRODUCE_INVALID_ARGUMENT`）、`InvalidFormation`（`PRODUCE_INVALID_FORMATION`）、`NotInProgress`（`PRODUCE_NOT_IN_PROGRESS`）、`SessionDiscarded`（`PRODUCE_SESSION_DISCARDED`）、`AlreadyInProgress`（`PRODUCE_ALREADY_IN_PROGRESS`） |

### 4.2 乱数ストリーム（タスク 2）

`S/Domain/Produce/`（namespace `Uta.Server.Domain.Produce`）:

```csharp
public sealed record RandomStreamState(RandomStreamKind Kind, int Week, int Attempt, ulong S0, ulong S1, ulong S2, ulong S3, long DrawCount);

public sealed class SessionRandomStreams : IRandomStreamSource
{
    public SessionRandomStreams(ulong masterSeed, IEnumerable<RandomStreamState> saved);
    public IRandom Get(RandomStreamKey key);                    // 保存済み → その状態の Xoshiro256StarStar（引いた回数も引き継ぐ）。無ければ RandomStreams.Create(masterSeed, key)。同じキーには同じオブジェクト
    public IReadOnlyList<RandomStreamState> Export();           // Get した・読み込んだ全部の現在の状態。key.ToUInt64() の昇順
}
```

中の入れ物は `SortedDictionary<ulong, (RandomStreamKey Key, Xoshiro256StarStar Random)>`。`Export` は `Random.S0..S3` と `Random.DrawCount` を写す。`using Uta.Shared.Domain.Rng;`。

### 4.3 Application の型（タスク 2）

`S/Application/Produce/`（namespace `Uta.Server.Application.Produce`）:

```csharp
public static class ProduceVersions
{
    public const int StateVersion = 1;
    public const int RuleVersion = 1;
    public static bool IsCurrent(ProduceSessionRecord session, int appVersion) =>
        session.AppVersion == appVersion && session.StateVersion == StateVersion && session.RuleVersion == RuleVersion;
}

public enum ProduceSessionStatus { InProgress = 0, Completed = 1, Abandoned = 2, Discarded = 3 }

public sealed record ProduceSessionData(ulong MasterSeed, IReadOnlyList<RandomStreamState> Streams, ProduceSnapshot Snapshot);

/// <summary>Data は state_version が今と違うとき null（読まない）。</summary>
public sealed record ProduceSessionRecord(Ulid SessionId, int AppVersion, int StateVersion, int RuleVersion, int Version, ProduceSessionData? Data);

public sealed record ProduceResultRecord(
    Ulid SessionId, Ulid PlayerId, ProduceSessionStatus Ending, ExamOutcome ExamOutcome, int EvaluationValue, ProduceRank Rank,
    int FinalScore, int FinalRank, int GachaCurrency, int Gold, int SupportPoints, int Exp)
{
    /// <summary>放棄・破棄の行（数値は 0、ExamOutcome / Rank は Nothing）。</summary>
    public static ProduceResultRecord Empty(Ulid sessionId, Ulid playerId, ProduceSessionStatus ending) =>
        new(sessionId, playerId, ending, ExamOutcome.Nothing, 0, ProduceRank.Nothing, 0, 0, 0, 0, 0, 0);
}

public interface IProduceSessionRepository
{
    /// <summary>進行中のセッション（status = 0）。無ければ null。</summary>
    Task<ProduceSessionRecord?> FindActiveAsync(TxContext tx, Ulid playerId);

    /// <summary>status = 進行中、version = 0 で追加。state_version・rule_version は ProduceVersions の定数、week / turn は data.Snapshot から（Week と Lesson?.Turn ?? 0）。</summary>
    Task InsertAsync(TxContext tx, Ulid sessionId, Ulid playerId, int appVersion, ProduceSessionData data);

    /// <summary>state・status・week・turn を更新し version を 1 増やす（WHERE session_id AND version = expectedVersion）。0 行なら false。</summary>
    Task<bool> TryUpdateAsync(TxContext tx, Ulid sessionId, int expectedVersion, ProduceSessionStatus status, ProduceSessionData data);

    /// <summary>state は変えず status だけ更新し version を 1 増やす。0 行なら false。</summary>
    Task<bool> TrySetStatusAsync(TxContext tx, Ulid sessionId, int expectedVersion, ProduceSessionStatus status);

    Task InsertResultAsync(TxContext tx, ProduceResultRecord result);
}
```

`using` は `Uta.Server.Domain.Produce`、`Uta.Shared.Domain.Produce`、`VolTech.Server.Transaction`。

### 4.4 Infrastructure（タスク 2）

- `ProduceStateBlobs.cs`（namespace `Uta.Server.Infrastructure.Produce`）: [produce-server.md](../wiki/architecture/server/produce-server.md) 3章の `ProduceStateBlob`・`RandomStreamBlob`、`ProduceSnapshot` の 10 個の型それぞれの `…Blob`。すべて `public sealed class`、`[MessagePackObject]`、`[Key(n)]`（0 から宣言順）の `{ get; set; }`。プロパティの名前・型・順は元のスナップショットの型と同じ（produce-domain.md 6.1 の表）。違いは: enum は `int`、入れ子のスナップショットの型は対応する `…Blob`。配列は空の配列で初期化（`= []`）
- `ProduceStateSerializer.cs`: `public static class ProduceStateSerializer { public static byte[] Serialize(ProduceSessionData data); public static ProduceSessionData Deserialize(byte[] bytes); }`。中で `ProduceSessionData` ↔ `ProduceStateBlob` を、型のペアごとの `private static` の変換関数（`ToBlob(X)` / `FromBlob(XBlob)`）で手書きし、`MessagePackSerializer.Serialize(blob)` / `MessagePackSerializer.Deserialize<ProduceStateBlob>(bytes)` を使う（オプションは既定）。`Streams` は `RandomStreamState` ↔ `RandomStreamBlob`（`Kind` は `int` との相互変換）
- `ProduceSessionRepository.cs`（`S/Infrastructure/`）: `IProduceSessionRepository` を実装。SQL は Dapper（`CommandDefinition(sql, parameters, tx.Transaction, cancellationToken: tx.CancellationToken)`。`PlayerRepository` と同じ書き方）。`FindActiveAsync`: `SELECT session_id AS SessionId, app_version AS AppVersion, state_version AS StateVersion, rule_version AS RuleVersion, version AS Version, state AS State FROM produce_sessions WHERE player_id = @playerId AND status = 0`。`state_version == ProduceVersions.StateVersion` のときだけ `ProduceStateSerializer.Deserialize(State)`、違えば `Data = null`。ID は `x.ToString()` で渡し、`Ulid.Parse` で読む。`InsertAsync` / `TryUpdateAsync` / `TrySetStatusAsync` / `InsertResultAsync` は [produce-server.md](../wiki/architecture/server/produce-server.md) 3章の列に対応する `INSERT` / `UPDATE`（更新は `ExecuteAsync` の戻り値 == 1 を返す）。`produce_results.ending` は `(int)Ending`、`exam_outcome` は `(int)ExamOutcome`、`produce_rank` は `(int)Rank`

### 4.5 マスタの詰め替えと登録（タスク 3）

`S/Infrastructure/MasterData/MasterProduceCatalog.cs`:

```csharp
public static class MasterProduceCatalog
{
    public static ProduceCatalog Create(Uta.MasterData.MemoryDatabase db, ICardCatalog cards);
}
```

`ProduceMasterRows`（`shared/Uta.Shared.Domain/Produce/ProduceMasterRows.cs`）の 16 個のプロパティそれぞれに、対応するマスタの表（`db.ScenarioTable.All` のように `db.<行の型名から Row を除いた名前>Table.All`）の行を、`ProduceRows.cs` の行の型（`ScenarioRow` など）に詰める。コンストラクタ引数の順は CSV の列の順で、生成クラス（`S/Infrastructure/MasterData/Generated/**`）の同名のプロパティを順に渡す。enum は `(Domain の enum)(int)x.Prop`（`MasterCardCatalog` と同じ。Domain の enum は `Uta.Shared.Domain.Produce` と `Uta.Shared.Domain.Lesson`）。**列の対応は `shared/Uta.Shared.Domain.Tests/Produce/TestProduceMaster.cs` の `LoadRows` と同じ**（そちらは CSV を読むが、引数の順と enum は同じ）。最後に `ProduceCatalog.Create(rows, cards)`。

`Program.cs` に次を足す（`builder.Services.AddSingleton(MasterDataLoader.Load());` の後）:

```csharp
builder.Services.AddSingleton<ICardCatalog>(sp => MasterCardCatalog.Create(sp.GetRequiredService<MemoryDatabase>()));
builder.Services.AddSingleton(sp => MasterProduceCatalog.Create(sp.GetRequiredService<MemoryDatabase>(), sp.GetRequiredService<ICardCatalog>()));
builder.Services.AddSingleton<ProduceEngine>();
```

（`MemoryDatabase` は `Uta.MasterData`、`ICardCatalog` は `Uta.Shared.Domain.Lesson`、`ProduceEngine`・`ProduceCatalog` は `Uta.Shared.Domain.Produce`。`ProduceEngine` のコンストラクタは `ProduceCatalog` 1 つ）

### 4.6 ユースケース（タスク 4）

`S/Application/Produce/`。

`ProduceView.cs`:

```csharp
public sealed record ProduceEndSummary(
    ProduceSessionStatus Ending, ExamOutcome ExamOutcome, int EvaluationValue, ProduceRank Rank, int FinalScore, int FinalRank,
    int GachaCurrency, int Gold, int SupportPoints, int Exp, int PlayerLevel, int PlayerExp);

/// <summary>RPC のレスポンスを作るための材料。State が null は「進行中のセッション無し」。</summary>
public sealed record ProduceView(
    ProduceState? State, IReadOnlyList<WeekChoice> WeekChoices, IReadOnlyList<ClassOptionView> ClassOptions,
    IReadOnlyList<int> NpcScores, ExamReport? ExamResult, ProduceEndSummary? End)
{
    public static readonly ProduceView None = new(null, [], [], [], null, null);
}
```

`ProduceViewBuilder.cs`: `public sealed class ProduceViewBuilder(ProduceEngine engine, ProduceCatalog catalog)`、`public ProduceView Build(ProduceState state, ExamReport? examResult, ProduceEndSummary? end)`:

- `WeekChoices`: `state.Phase == WeekSelect` なら `engine.GetWeekChoices(state)`、そうでなければ空
- `ClassOptions`: `state.Phase == WeekSelect` なら `engine.GetClassOptions(state)`、そうでなければ空
- `NpcScores`: `state.Phase == Exam` なら `ProduceFormulas.NpcScores(catalog.GetScenario(state.Input.ScenarioId).GetWeek(state.Week).RivalBaseScore)`、そうでなければ空
- `ExamResult`: 引数 `examResult` があればそれ、なければ `state.Phase == ExamResult` のとき `state.LastExam`、そうでなければ `null`
- `End`: 引数のまま

`ProduceSessionDiscarder.cs`: `public sealed class ProduceSessionDiscarder(IProduceSessionRepository sessions)`、`public async Task DiscardAsync(TxContext tx, Ulid playerId, ProduceSessionRecord session)`: `if (!await sessions.TrySetStatusAsync(tx, session.SessionId, session.Version, ProduceSessionStatus.Discarded)) throw new UseCaseException(UseCaseError.ProduceStaleOperation, "stale")`; `await sessions.InsertResultAsync(tx, ProduceResultRecord.Empty(session.SessionId, playerId, ProduceSessionStatus.Discarded))`。

`StartProduceUseCase.cs`:

```csharp
public sealed class StartProduceUseCase(
    IProduceSessionRepository sessions, IPlayerRepository players, IIdolRepository idols, ISupportCardRepository supportCards,
    ProduceEngine engine, ProduceCatalog catalog, IRandomSource randomSource, ProduceSessionDiscarder discarder, ProduceViewBuilder viewBuilder)
{
    public async Task<ProduceView> ExecuteAsync(
        TxContext tx, Ulid playerId, int appVersion, int idolId, IReadOnlyList<string> supportCardUids, int rentalCandidateId, int scenarioId)
}
```

手順（上から。投げるのは `UseCaseException(UseCaseError.X, "メッセージ")`）:

1. `rentalCandidateId != 0` → `ProduceInvalidArgument`
2. `catalog.GetScenario(scenarioId)` を `try` で呼び、`KeyNotFoundException` なら `ProduceInvalidArgument`
3. `await idols.ExistsAsync(tx, playerId, idolId)` が false → `ProduceInvalidFormation`（`IdolId` がマスタに無い場合も所持していないので同じ）
4. `supportCardUids.Count > ProduceRules.MaxSupportCards` → `ProduceInvalidFormation`
5. `uids`: 各文字列を `Ulid.TryParse` し、失敗したら `ProduceInvalidFormation`。同じ `Ulid` が 2 回あれば `ProduceInvalidFormation`。各 `uid` を `supportCards.FindAsync(tx, playerId, uid)` で読み、`null` なら `ProduceInvalidFormation`。読んだ `OwnedSupportCard`（`CardId`・`Level`・`LimitBreak`）を枠の順に集める
6. `FormationValidator.Validate(catalog, idolId, cardIds)` が `Ok` でなければ `ProduceInvalidFormation`
7. `var existing = await sessions.FindActiveAsync(tx, playerId);` があるとき、`ProduceVersions.IsCurrent(existing, appVersion)` なら `ProduceAlreadyInProgress`、そうでなければ `await discarder.DiscardAsync(tx, playerId, existing)`
8. `var player = await players.FindAsync(tx, playerId) ?? throw new InvalidOperationException("player not found");` `input = new ProduceStartInput(scenarioId, idolId, player.Level, owned.Select(x => new SupportCardInput(x.CardId, x.Level, x.LimitBreak)).ToList())`
9. `var seed = randomSource.Create().NextUInt64(); var streams = new SessionRandomStreams(seed, []); var state = engine.Start(input, streams);`
10. `var data = new ProduceSessionData(seed, streams.Export(), ProduceSnapshot.From(state, catalog.Cards)); await sessions.InsertAsync(tx, Ulid.NewUlid(), playerId, appVersion, data);`
11. `return viewBuilder.Build(state, null, null);`

`GetProduceStateUseCase.cs`: `GetProduceStateUseCase(IProduceSessionRepository sessions, ProduceCatalog catalog, ProduceViewBuilder viewBuilder)`、`Task<ProduceView> ExecuteAsync(TxContext tx, Ulid playerId, int appVersion)`: `FindActiveAsync` が `null`、または `!ProduceVersions.IsCurrent(...)`、または `Data == null` なら `ProduceView.None`。そうでなければ `viewBuilder.Build(session.Data.Snapshot.ToState(catalog), null, null)`。

`AbandonProduceUseCase.cs`: `AbandonProduceUseCase(IProduceSessionRepository sessions, IPlayerRepository players)`、`Task<ProduceEndSummary> ExecuteAsync(TxContext tx, Ulid playerId, int appVersion)`: `FindActiveAsync` が `null` → `ProduceNotInProgress`、`!IsCurrent` → `ProduceSessionDiscarded`（書かない）。`TrySetStatusAsync(..., Abandoned)` が false → `ProduceStaleOperation`。`InsertResultAsync(ProduceResultRecord.Empty(session.SessionId, playerId, Abandoned))`。`players.FindAsync` で現在の Lv・経験値を読み、`new ProduceEndSummary(Abandoned, ExamOutcome.Nothing, 0, ProduceRank.Nothing, 0, 0, 0, 0, 0, 0, player.Level, player.Exp)` を返す。

### 4.7 エラーの変換（タスク 4）

`UseCaseError` に追加: `ProduceStaleOperation`、`ProduceUnexpectedPhase`、`ProduceInvalidArgument`、`ProduceInvalidFormation`、`ProduceNotInProgress`、`ProduceSessionDiscarded`、`ProduceAlreadyInProgress`。`UtaServiceAttribute.ToErrorCode` にそれぞれ `ProduceErrors.StaleOperation`・`UnexpectedPhase`・`InvalidArgument`・`InvalidFormation`・`NotInProgress`・`SessionDiscarded`・`AlreadyInProgress` を対応させる（`using Uta.Shared.Contracts.Produce;`）。`ToErrorCode` を `internal static` にする。`[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]` に変える（メソッドに付けられるようにする。4.10 で使う）。

### 4.8 Api（タスク 4）

- `AppVersionHeader.cs`（namespace `Uta.Server.Api`）: `public static class AppVersionHeader`。`public static int? Read(ServiceContext context)`: `context.CallContext.RequestHeaders.GetValue(ApiHeaders.AppVersion)` を `int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var v)` で読み、読めなければ `null`。`public static int Require(ServiceContext context)`: `Read(context) ?? throw new BusinessException(ProduceErrors.InvalidArgument, "x-app-version is required")`。`using VolTech.Server; using VolTech.Server.Abstractions; using MagicOnion.Server;`
- `ProduceResponseMapper.cs`: `public static class ProduceResponseMapper`。`public static ProduceStateResponse ToStateResponse(ProduceView view)`、`public static ProduceResultResponse ToResultResponse(ProduceEndSummary end)`。
  - `view.State == null`: すべて 0・空配列・`null`、`Phase = ProducePhase.Nothing`（空配列はすべて `new T[0]` / `Array.Empty<T>()`）
  - それ以外: `OperationNumber = state.NextOperationNumber`、`Phase`、`ScenarioId`・`IdolId` は `state.Input`、`Week`、`Singing` / `Rhythm` / `Expression` は `state.GetParam(...)`、`Hp` は `state.Lesson?.Hp ?? state.Hp`、`MaxHp = state.MaxHp`、`ExamRetryLeft`、`Deck` は `state.Deck` を `InstanceId` の昇順に `DeckCardDto`、`WeekActions` は `view.WeekChoices` を `WeekActionDto(SlotIndex, Kind, Selectable)`、`ClassOptions` は `view.ClassOptions` を `ClassOptionDto(Index, HpCost, Param, ParamGain, GivesCard, Selectable)`、`CardOffers = state.OfferCards.ToArray()`、`Lesson = state.Lesson == null ? null : ToLessonDto(state.Lesson, view.NpcScores)`、`ExamResult` は `view.ExamResult` を `ExamResultDto(Kind, Attempt, Score, Rank, NpcScores.ToArray(), Passed)`（`null` なら `null`）、`Result = view.End == null ? null : ToResultResponse(view.End)`
  - `ToLessonDto`: `Kind`・`TargetParam`・`MaxTurns`・`ClearTarget`・`PerfectTarget` は `lesson.Setup`、`ExamTurns` は `lesson.Setup.Exam` が `null` なら空、あれば `ExamTurnDto(Param, ScoreBonusPercent)`、`Turn`・`Shield`・`UsesLeft`・`ParamGained`、`Hand = lesson.Zones.Hand.ToArray()`（並びのまま）、`DrawPile` / `Discard` / `Exiled` は昇順に並べた配列、`StatusValues` は `i = 0..StatusSet.KindCount - 1` の `lesson.Statuses.Get((StatusKind)i)`、`ParamBonuses` は `ParamBonusDto(Percent, RemainingTurns)`、`Persistents` は `PersistentDto(Trigger, RemainingTurns, RemainingCount)`、`LessonEnhanced = lesson.LessonEnhanced.ToArray()`、`NpcScores = npcScores.ToArray()`
  - `ToResultResponse`: `ProduceEnding` は `(ProduceEnding)(int)end.Ending`。ほかは同名のフィールド（`PlayerLevel` / `PlayerExp`）
- `ProduceService.cs`: `[UtaService] public sealed class ProduceService(ITransactions transactions, StartProduceUseCase start, GetProduceStateUseCase getState, AbandonProduceUseCase abandon) : ServiceBase<IProduceService>, IProduceService`。`GachaService` と同じ形（`#pragma warning disable CS8644`、`Context.GetAuthContext().PlayerId`、`transactions.RunAsync(Context, async tx => ...)`）。3 つの RPC とも先頭で `var appVersion = AppVersionHeader.Require(Context);`。`StartAsync` は `start.ExecuteAsync(tx, playerId, appVersion, request.IdolId, request.SupportCardUids ?? [], request.RentalCandidateId, request.ScenarioId)` → `ProduceResponseMapper.ToStateResponse`、`GetStateAsync` は `getState.ExecuteAsync` → `ToStateResponse`、`AbandonAsync` は `abandon.ExecuteAsync` → `ToResultResponse`

### 4.9 DI の登録（タスク 4）

`Program.cs`: `IProduceSessionRepository` → `ProduceSessionRepository`（Scoped）、`ProduceViewBuilder`（Singleton）、`ProduceSessionDiscarder`・`StartProduceUseCase`・`GetProduceStateUseCase`・`AbandonProduceUseCase`（Scoped）。

### 4.10 SignIn（タスク 5）

- `SignInResult` に `bool HasActiveProduce, bool DiscardedProduce` を末尾に足す
- `SignInUseCase`: コンストラクタに `IProduceSessionRepository sessions, ProduceSessionDiscarder discarder` を足す。`ExecuteAsync(TxContext tx, Ulid playerId, int? appVersion)`: `player` が `null` なら `null`（今のまま）。`var session = await sessions.FindActiveAsync(tx, playerId)`。`session == null` なら `(false, false)`。あるとき `appVersion == null` なら `UseCaseException(UseCaseError.ProduceInvalidArgument, "x-app-version is required")`。`ProduceVersions.IsCurrent(session, appVersion.Value)` なら `(true, false)`、そうでなければ `await discarder.DiscardAsync(tx, playerId, session)` して `(false, true)`
- `PlayerService.SignInAsync`: `[UtaService]` を**メソッドに**付ける（`PlayerService` クラスには付けない。`SignUpAsync` を弾かないため）。`var appVersion = AppVersionHeader.Read(Context);` を渡し、`SignInResponse` の 6・7 番目の引数（今は `false, false`）を `result.HasActiveProduce, result.DiscardedProduce` にする。`ArchitectureTests.Services_HaveUtaServiceAttribute` は `PlayerService` を除外済みなので変えない
- `UtaServiceAttribute` がメソッドに付けられること（`MagicOnionFilterAttribute` のメソッド対象）は、`dotnet build` と既存の `PlayerSignUpSignInTests` が通ることで確かめる。通らなければ止まって報告する

### 4.11 テスト支援（タスク 4）

`RpcAssert.cs`（namespace `Uta.Server.Tests.Integration.Support`）: `public static class RpcAssert { public static void Reason(RpcException exception, StatusCode code, string reason); }`。中身は `SupportTests.AssertReason`（`private static`）と同じ（`SupportTests` は変えない）。`ProduceStartTests` はこれを使う。

`PlayerClient`: `x-app-version` を付ける。`private readonly string? appVersion;` を持ち、`CreateAsync(GrpcChannel channel, string secret = "0123456789abcdef-secret", string? appVersion = "1")`。2 つの `CallAsync` で、`appVersion` が `null` でなければ `headers.Add(ApiHeaders.AppVersion, appVersion)`。`public PlayerClient WithAppVersion(string? version) => new PlayerClient(channel, PlayerId, token, version);`（コンストラクタに `appVersion` 引数を足す）。

## 5. タスクとテスト

各タスクの終わりに `dotnet build Uta.slnx`（警告 0・エラー 0）と `dotnet test --project server/Uta.Server.Tests`（結合テストは Docker が要る）と `dotnet test --project shared/Uta.Shared.Domain.Tests` が通ること。Docker が使えないときは、結合テストが確認できていないと報告する。

### タスク 1: Contracts

4.1 のファイルを作る。確かめ方: ビルドが通る。

### タスク 2: 保存の土台

マイグレーション、4.2・4.3・4.4。テスト（`T/Domain/SessionRandomStreamsTests.cs`、`T/Infrastructure/ProduceStateSerializerTests.cs`）:

1. `SessionRandomStreams_ExportThenRestore_ContinuesSequence`: マスターシード 7、キー `(Week, 1, 0)` と `(Lesson, 1, 0)` から 5 個ずつ `NextUInt64()` を引き、`Export()` → 新しい `SessionRandomStreams(7, exported)` → 次の 5 個ずつが、続けて引いた場合（もう 1 つの `SessionRandomStreams(7, [])` で 10 個引く）の後半と一致する
2. `SessionRandomStreams_SameKey_SameObject`、`SessionRandomStreams_Export_OrderedByKey`（キー `(Lesson, 3, 0)`、`(Week, 1, 0)`、`(Scenario, 0, 0)` の順に `Get` しても、`Export` は `ToUInt64()` の昇順）、`SessionRandomStreams_Export_IncludesLoadedStreamsNotRequested`
3. `Serializer_RoundTrip_SameState`（`[Theory]`: シナリオ 2 のアイドル 1〜4、シード 1・2・3）: `catalog = TestProduceMaster.Create()`（`Uta.Shared.Domain.Tests.Produce`。テストプロジェクトは `Uta.Shared.Domain.Tests` を参照している）、`SessionRandomStreams` を乱数源にして `ProduceBot.Next` → `Apply` で最後まで進め、**10 回の操作ごとと最後に**、`ProduceSnapshot.From` → `ProduceStateSerializer.Serialize` → `Deserialize` → `ToState(catalog)` が元の状態と `ProduceStateComparer.Diff` が空
4. `Serializer_ResumeWithStreams_SameFinalState`（同じ組み合わせ）: 同じシードで A（途中を保存せず最後まで）と B（**10 回の操作ごとに** `Serialize(new ProduceSessionData(seed, streams.Export(), snapshot))` → `Deserialize` → `new SessionRandomStreams(data.MasterSeed, data.Streams)` と `ToState` で作り直して続ける）を走らせ、最後の状態が一致し `Operations` の件数も同じ
5. `Serializer_NullableParts_RoundTrip`: 開始直後（`Lesson` などが `null`）の往復で、`null` が `null` のまま
6. `Migration_CreatesTables`: `PostgresFixture` で `SELECT count(*) FROM information_schema.tables WHERE table_name IN ('produce_sessions','produce_results')` が 2（`PostgresFixture` は `[Collection(PostgresCollection.Name)]` で受け取る）

確かめ方: 上のテストと既存のテストが通る。

### タスク 3: マスタの詰め替え

4.5。テスト（`T/MasterData/MasterProduceCatalogTests.cs`。`MasterDataLoader.Load()` と `MasterCardCatalog.Create` を使う。`MasterCardCatalogTests` の書き方に合わせる）:

1. `Create_FromMaster_Succeeds`: 例外なく作れ、`GetScenario(1).WeekCount == 3`、`GetScenario(2).WeekCount == 13`、`GetIdol(1)` が取れる
2. `BotPlay_FromMaster_ReachesFinished`（`[Theory]`: シナリオ 1 のアイドル 1・2、シナリオ 2 のアイドル 1〜4）: `ProduceEngine` と `TestRandomStreamSource(1)` で `ProduceBot.Run` を最後まで回し、`Phase == Finished`

確かめ方: `dotnet test --project server/Uta.Server.Tests` が通る。

### タスク 4: 開始・状態・放棄

4.6〜4.9、4.11。テスト（`T/Integration/ProduceStartTests.cs`。`[Collection(PostgresCollection.Name)]`。`SupportTests` の `StartAsync` と同じ形で、`UtaServerFactory(postgres, randomSource: new ScriptedRandomSource(111UL, 222UL, 333UL, 444UL))` を使い、`SignUpAsync` のあと `player.CallAsync<IProduceService, ProduceStateResponse>(x => x.StartAsync(new StartProduceRequest(...)), "req-n")`。エラーの確認は `Assert.ThrowsAsync<RpcException>` で例外を受けて `RpcAssert.Reason(exception, StatusCode.FailedPrecondition, "PRODUCE_…")`）:

1. `Start_InitialHoldings_ReturnsWeekSelect`: アイドル 1、所持IDなし、`rentalCandidateId` 0、シナリオ 1。`Phase == WeekSelect`、`Week == 1`、`OperationNumber == 0`、`ScenarioId == 1`、`IdolId == 1`、`Hp == MaxHp == 30`、`Singing == 30`・`Rhythm == 20`・`Expression == 10`、`WeekActions` は 2 件（枠 0 がレッスンで選べる、枠 1 が休むで選べない）、`Deck` が空でなく `InstanceId` の昇順。DB: `produce_sessions` に 1 行（`status` 0、`app_version` 1、`state_version` 1、`rule_version` 1、`version` 0、`week` 1）
2. `Start_SameRequestId_ReturnsSavedResponse`: 同じ `x-request-id` で 2 回呼ぶと同じレスポンスで、`produce_sessions` は 1 行のまま
3. `Start_WhileInProgress_AlreadyInProgress`: 別の `x-request-id` で 2 回目は `PRODUCE_ALREADY_IN_PROGRESS`
4. `Start_InvalidFormation_Rejected`（`[Theory]` か複数の `[Fact]`。いずれも `FAILED_PRECONDITION` + `PRODUCE_INVALID_FORMATION`）: 未所持のアイドル（3）、所持IDが 6 個（`SignInAsync` の `SupportCards` の 5 枚 + `Ulid.NewUlid().ToString()`）、形が違う文字列（`"x"`）、他人の所持ID（別の `PlayerClient`（別の `secret`）のカードの所持ID）、同じ所持ID が 2 回、同じカード ID の別の所持ID（`player_support_cards` に `card_id` 1 の行を `SupportTests.AddMaterialAsync` と同じ SQL で足し、元の所持IDと並べる）、プランの不一致（アイドル 1 は感性。`card_id` 7（理論）の行を足して並べる）
5. `Start_ReflectsSupportCardLevel`: アイドル 1 で `card_id` 1 の所持ID を 1 枚つけて開始 → `Singing == 30 + ProduceFormulas.AbilityValue(3, 20, 1, 40)`。`AbandonAsync` → `ISupportService.LevelUpAsync` で目標 20 → 再び開始 → `Singing == 30 + ProduceFormulas.AbilityValue(3, 20, 20, 40)` で、前より大きい（`ScriptedRandomSource` の値は 2 個目を使う）
6. `Start_RentalNotZero_InvalidArgument`、`Start_UnknownScenario_InvalidArgument`（99）
7. `GetState_NoSession_ReturnsNothing`: `Phase == Nothing`、`Deck` と `WeekActions` が空、`Lesson` / `ExamResult` / `Result` が `null`
8. `GetState_AfterStart_MatchesStartResponse`: `Phase`・`Week`・`OperationNumber`・`Hp`・`Singing`・`Deck.Length`・`WeekActions.Length` が開始のレスポンスと同じ
9. `GetState_StaleVersions_ReturnsNothingWithoutWriting`: 開始後に `UPDATE produce_sessions SET rule_version = 0`（別のテストで `state_version = 0, state = '\x00'::bytea`）→ `GetStateAsync` は `Nothing`、`status` は 0 のまま
10. `GetState_DifferentAppVersion_ReturnsNothing`: `player.WithAppVersion("2")` の `GetStateAsync` は `Nothing`。`WithAppVersion(null)` の `GetStateAsync` は `PRODUCE_INVALID_ARGUMENT`
11. `Abandon_InProgress_RecordsResult`: 開始 → `AbandonAsync` → `Ending == Abandoned`、`PlayerLevel == 1`。DB: `produce_sessions.status` 2、`produce_results` に 1 行（`ending` 2、`produce_rank` 0、`gacha_currency` など 0）。`players.gacha_currency` は変わらない。そのあと `GetStateAsync` は `Nothing`、`StartAsync` がまた通る
12. `Abandon_NoSession_NotInProgress`、`Abandon_DifferentAppVersion_SessionDiscardedWithoutWriting`（`WithAppVersion("2")` の `AbandonAsync` は `PRODUCE_SESSION_DISCARDED`、DB の `status` は 0 のまま）
13. `Start_DifferentAppVersionSessionRemains_DiscardsOldAndStarts`: 開始（バージョン 1）→ `WithAppVersion("2")` で `StartAsync`（別の `x-request-id`）→ 成功。古い行は `status` 3 で、`produce_results` に `ending` 3 の行、新しい行が `status` 0

### タスク 5: SignIn

4.10。テスト（`ProduceStartTests.cs` に足す）:

1. `SignIn_NoSession_NoProduceFlags`（`HasActiveProduce` / `DiscardedProduce` が false）、`SignIn_WithoutAppVersionHeader_NoSession_Succeeds`（`WithAppVersion(null)`）
2. `SignIn_ActiveSession_HasActiveProduce`: 開始後の `SignInAsync` が `HasActiveProduce == true`、`DiscardedProduce == false`
3. `SignIn_DifferentAppVersion_DiscardsSession`: 開始（バージョン 1）→ `WithAppVersion("2")` の `SignInAsync` が `HasActiveProduce == false`、`DiscardedProduce == true`。DB: `status` 3、`produce_results` に `ending` 3 の行
4. `SignIn_DifferentRuleVersion_DiscardsSession`（`rule_version = 0` に更新）、`SignIn_DifferentStateVersion_DiscardsSession`（`state_version = 0, state = '\x00'::bytea`）
5. `SignIn_ActiveSession_WithoutAppVersionHeader_InvalidArgument`: `WithAppVersion(null)` の `SignInAsync` が `PRODUCE_INVALID_ARGUMENT`
6. 既存の `PlayerSignUpSignInTests` が通る

### タスク 6: wiki への反映（メインのセッション）

[produce-server.md](../wiki/architecture/server/produce-server.md) の実装メモ、計画の `status`、`game-spec.md` の表。

## 6. 未決事項

なし
