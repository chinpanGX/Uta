---
title: 実装計画: サーバーのプロデュース 操作・終了・報酬
status: ready
updated: 2026-11-03
spec: ../wiki/architecture/server/produce-server.md, ../wiki/architecture/produce-domain.md, ../wiki/architecture/api-design.md, ../wiki/systems/produce.md
---

# 実装計画: サーバーのプロデュース 操作・終了・報酬

## 1. 目的と前提

順 6 の計画 3/3。操作の RPC 4 つ（`ChooseWeekActionAsync`・`ResolveCardOfferAsync`・`DecideExamRetryAsync`・`ExecuteLessonActionAsync`）、終了のときの報酬・プレイヤーLv・`produce_results`・ゲームログ、そして「最短シナリオを RPC だけで最後まで進める」「途中で中断して再開する」結合テストを作る。

- 前提（先にマージされていること）: [ProduceSnapshot](2026-11-03-produce-snapshot.md)、[開始・状態・放棄・保存](2026-11-03-server-produce-start.md)
- 仕様・設計の正: [produce-server.md](../wiki/architecture/server/produce-server.md) 4・5・6章、[produce-domain.md](../wiki/architecture/produce-domain.md) 5・8章、[produce.md](../wiki/systems/produce.md) 6.1・7章
- 受け持つ完了条件: game-spec の 4（最後まで進めて評価・報酬・プレイヤー経験値を受け取る）、5（途中で中断しても再開できる）
- この計画で**決めないこと**: 相談・ドリンクの RPC（MVP で作らない）、クライアント（順 7）、報酬の値の調整（マスタの値のまま）

## 2. 触るファイル

`C` = `shared/Uta.Shared.Contracts`、`S` = `server/Uta.Server`、`T` = `server/Uta.Server.Tests`。Contracts の新しいファイルの `.meta` は [開始の計画](2026-11-03-server-produce-start.md) 2章と同じ作り方。

| 操作 | パス | 中身 | タスク |
|---|---|---|---|
| 作成 | `S/Domain/Produce/PlayerLevelRules.cs` | 4.1 | 1 |
| 作成 | `S/Application/Produce/ProduceReward.cs`（`ProduceReward`・`IProduceRewardCatalog`・`IPlayerLevelCatalog`） | 4.1 | 1 |
| 作成 | `S/Infrastructure/MasterData/MasterProduceRewardCatalog.cs`、`MasterPlayerLevelCatalog.cs` | 4.1 | 1 |
| 変更 | `S/Application/Player/IPlayerRepository.cs`、`S/Infrastructure/PlayerRepository.cs` | 4.1 | 1 |
| 変更 | `S/Program.cs` | 4.1・4.4 の登録 | 1、2 |
| 作成 | `C/Produce/ChooseWeekActionRequest.cs`、`ResolveCardOfferRequest.cs`、`DecideExamRetryRequest.cs`、`ExecuteLessonActionRequest.cs`、`CardOfferActionKind.cs`、`LessonActionKind.cs`（と `.meta`） | 4.2 | 2 |
| 変更 | `C/Produce/IProduceService.cs` | 4.2 | 2 |
| 作成 | `S/Application/Produce/ProduceCommandNormalizer.cs`、`ProduceCompletion.cs`、`ProduceOperationUseCase.cs` | 4.3 | 2 |
| 変更 | `S/Api/ProduceService.cs` | 4.4 | 2 |
| 作成 | `T/Domain/PlayerLevelRulesTests.cs`、`T/MasterData/MasterProduceRewardCatalogTests.cs` | 5章 | 1 |
| 作成 | `T/Integration/ProduceOperationTests.cs` | 5章 | 2 |
| 作成 | `T/Integration/Support/ProduceDriver.cs` | 4.5 | 2 |
| 変更 | `T/Integration/Support/PlayerClient.cs` | 4.5 | 2 |
| 作成 | `T/Integration/ProducePlayTests.cs` | 5章 | 3 |
| 変更 | wiki と `game-spec.md` | 実装で決めたことの反映（メインのセッション） | 4 |

## 3. 書式

[開始の計画](2026-11-03-server-produce-start.md) 3章と同じ。

## 4. 型とシグネチャ

### 4.1 報酬とプレイヤーLv（タスク 1）

`S/Domain/Produce/PlayerLevelRules.cs`（namespace `Uta.Server.Domain.Produce`）:

```csharp
public static class PlayerLevelRules
{
    /// <param name="requiredExps">requiredExps[i] は Lv i+1 → i+2 に要る経験値。最大 Lv は requiredExps.Count + 1</param>
    public static (int Level, int Exp) AddExp(int level, int exp, int gain, IReadOnlyList<int> requiredExps)
    {
        var maxLevel = requiredExps.Count + 1;
        while (gain > 0 && level < maxLevel)
        {
            var remain = requiredExps[level - 1] - exp;
            if (gain >= remain)
            {
                gain -= remain;
                level++;
                exp = 0;
            }
            else
            {
                exp += gain;
                gain = 0;
            }
        }

        return (level, level >= maxLevel ? 0 : exp);
    }
}
```

`S/Application/Produce/ProduceReward.cs`:

```csharp
public sealed record ProduceReward(int GachaCurrency, int Gold, int SupportPoints, int Exp);

public interface IProduceRewardCatalog
{
    /// <summary>ランクの報酬。F〜SSPlus 以外（Nothing）は InvalidOperationException。</summary>
    ProduceReward Get(ProduceRank rank);
}

public interface IPlayerLevelCatalog
{
    /// <summary>Level の昇順（Lv 1 → 2 が先頭）。</summary>
    IReadOnlyList<int> RequiredExps { get; }
}
```

`S/Infrastructure/MasterData/MasterProduceRewardCatalog.cs`: `public sealed class MasterProduceRewardCatalog : IProduceRewardCatalog`、コンストラクタ `(MemoryDatabase db)` で `db.EvaluationRankRewardTable.All` を `Dictionary<ProduceRank, ProduceReward>`（キーは `(ProduceRank)(int)x.Rank`、値は `x.GachaCurrency`・`x.Gold`・`x.SupportPoints`・`x.Exp`）にする。無いランクは `InvalidOperationException($"produce reward not found: {rank}")`。`MasterPlayerLevelCatalog : IPlayerLevelCatalog`: コンストラクタ `(MemoryDatabase db)` で `db.PlayerLevelExpTable.All.OrderBy(x => x.Level).Select(x => x.RequiredExp).ToList()`。

`IPlayerRepository` に追加し `PlayerRepository` で実装:

```csharp
/// <summary>プロデュースの報酬を加え、Lv と経験値を上書きする（1 文）。</summary>
Task GrantProduceRewardAsync(TxContext tx, Ulid playerId, ProduceReward reward, int level, int exp);
```

SQL: `UPDATE players SET gacha_currency = gacha_currency + @gachaCurrency, gold = gold + @gold, support_points = support_points + @supportPoints, level = @level, exp = @exp WHERE player_id = @playerId`。

`Program.cs`: `IProduceRewardCatalog` → `MasterProduceRewardCatalog`、`IPlayerLevelCatalog` → `MasterPlayerLevelCatalog`（どちらも Singleton）。

### 4.2 Contracts（タスク 2）

4 つの Request は [produce-server.md](../wiki/architecture/server/produce-server.md) 6章の表のとおり（形は [開始の計画](2026-11-03-server-produce-start.md) 4.1 と同じ）。`CardOfferActionKind`（`Nothing` 0 / `Pick` 1 / `Reroll` 2 / `Remove` 3 / `Decline` 4）、`LessonActionKind`（`Nothing` 0 / `UseCard` 1 / `UseDrink` 2 / `Skip` 3）。`IProduceService` に追加:

```csharp
UnaryResult<ProduceStateResponse> ChooseWeekActionAsync(ChooseWeekActionRequest request);
UnaryResult<ProduceStateResponse> ResolveCardOfferAsync(ResolveCardOfferRequest request);
UnaryResult<ProduceStateResponse> DecideExamRetryAsync(DecideExamRetryRequest request);
UnaryResult<ProduceStateResponse> ExecuteLessonActionAsync(ExecuteLessonActionRequest request);
```

### 4.3 Application（タスク 2）

`ProduceCommandNormalizer.cs`:

```csharp
public static class ProduceCommandNormalizer
{
    /// <summary>ChooseWeekAction の使わない引数を正規形にする（produce-domain.md 5章）。それ以外・範囲外の枠・WeekSelect でないときは raw のまま（Apply が拒否する）。</summary>
    public static ProduceCommand Normalize(ProduceEngine engine, ProduceState state, ProduceCommand raw)
    {
        if (raw.Kind != OperationKind.ChooseWeekAction || state.Phase != ProducePhase.WeekSelect) return raw;
        var choices = engine.GetWeekChoices(state);
        var slot = raw.Arg1;
        if (slot < 0 || slot >= choices.Count) return raw;
        return choices[slot].Kind switch
        {
            WeekChoiceKind.Lesson => ProduceCommand.ChooseLesson(slot, (ParamKind)raw.Arg3),
            WeekChoiceKind.Class => ProduceCommand.ChooseClass(slot, raw.Arg2),
            _ => ProduceCommand.ChooseOther(slot),
        };
    }
}
```

`ProduceCompletion.cs`:

```csharp
public sealed class ProduceCompletion(
    IPlayerRepository players, IProduceSessionRepository sessions, IProduceRewardCatalog rewards, IPlayerLevelCatalog levels)
{
    /// <summary>Phase が Finished の state の報酬・Lv・produce_results・ゲームログを 1 回分書く。セッションの status は呼び出し側が更新する。</summary>
    public async Task<ProduceEndSummary> CompleteAsync(TxContext tx, Ulid playerId, Ulid sessionId, ProduceState state)
}
```

手順: `result = state.Result ?? throw new InvalidOperationException("result missing")`。`reward = rewards.Get(result.Rank)`。`player = await players.FindAsync(tx, playerId) ?? throw new InvalidOperationException("player not found")`。`(level, exp) = PlayerLevelRules.AddExp(player.Level, player.Exp, reward.Exp, levels.RequiredExps)`。`await players.GrantProduceRewardAsync(tx, playerId, reward, level, exp)`。`await sessions.InsertResultAsync(tx, new ProduceResultRecord(sessionId, playerId, ProduceSessionStatus.Completed, result.Outcome, result.EvaluationValue, result.Rank, result.FinalScore, result.FinalRank, reward.GachaCurrency, reward.Gold, reward.SupportPoints, reward.Exp))`。ゲームログ: `tx.WriteGameLog(GameLog.Event("produce", "produce_reward").Target(sessionId.ToString()).Field("rank", (int)result.Rank).Field("evaluation_value", result.EvaluationValue).Field("gacha_currency", reward.GachaCurrency).Field("gold", reward.Gold).Field("support_points", reward.SupportPoints).Field("exp", reward.Exp).Field("level_after", level).Field("exp_after", exp));`。`ProduceEndSummary(Completed, result.Outcome, result.EvaluationValue, result.Rank, result.FinalScore, result.FinalRank, reward.GachaCurrency, reward.Gold, reward.SupportPoints, reward.Exp, level, exp)` を返す。

`ProduceOperationUseCase.cs`:

```csharp
public sealed class ProduceOperationUseCase(
    IProduceSessionRepository sessions, ProduceEngine engine, ProduceCatalog catalog,
    ProduceViewBuilder viewBuilder, ProduceCompletion completion)
{
    public async Task<ProduceView> ExecuteAsync(TxContext tx, Ulid playerId, int appVersion, int operationNumber, ProduceCommand command)
}
```

手順（[produce-server.md](../wiki/architecture/server/produce-server.md) 5章）:

1. `var session = await sessions.FindActiveAsync(tx, playerId) ?? throw new UseCaseException(UseCaseError.ProduceNotInProgress, "not in progress");`
2. `!ProduceVersions.IsCurrent(session, appVersion)` または `session.Data == null` → `ProduceSessionDiscarded`（何も書かない）
3. `var state = session.Data.Snapshot.ToState(catalog);` `operationNumber != state.NextOperationNumber` → `ProduceStaleOperation`
4. `var streams = new SessionRandomStreams(session.Data.MasterSeed, session.Data.Streams);` `var lastExam = state.LastExam;` `var normalized = ProduceCommandNormalizer.Normalize(engine, state, command);`
5. `var result = engine.Apply(state, normalized, streams);` `UnexpectedPhase` → `ProduceUnexpectedPhase`、`InvalidArgument` → `ProduceInvalidArgument`
6. `ProduceEndSummary? end = null; var status = ProduceSessionStatus.InProgress;` `state.Phase == ProducePhase.Finished` なら `end = await completion.CompleteAsync(tx, playerId, session.SessionId, state); status = Completed;`
7. `var data = new ProduceSessionData(session.Data.MasterSeed, streams.Export(), ProduceSnapshot.From(state, catalog.Cards));` `if (!await sessions.TryUpdateAsync(tx, session.SessionId, session.Version, status, data)) throw new UseCaseException(UseCaseError.ProduceStaleOperation, "stale");`
8. `var examResult = ReferenceEquals(lastExam, state.LastExam) ? null : state.LastExam;` `return viewBuilder.Build(state, examResult, end);`

（`Finished` のとき `viewBuilder.Build` の `WeekChoices` などは空になる。`ProduceView.State.Phase` は `Finished`。）

### 4.4 Api（タスク 2）

`ProduceService` のコンストラクタに `ProduceOperationUseCase operation` を足し、4 つを実装する。共通: `var playerId = Context.GetAuthContext().PlayerId; var appVersion = AppVersionHeader.Require(Context);` の後、`transactions.RunAsync(Context, async tx => ProduceResponseMapper.ToStateResponse(await operation.ExecuteAsync(tx, playerId, appVersion, request.OperationNumber, command)))`。`command`（`Uta.Shared.Domain.Produce.ProduceCommand` / `OperationKind`）:

- `ChooseWeekActionAsync`: `new ProduceCommand(OperationKind.ChooseWeekAction, request.SlotIndex, request.OptionIndex, (int)request.Param)`
- `ResolveCardOfferAsync`: `request.Kind` が `Pick` → `ProduceCommand.OfferPick(request.OfferIndex)`、`Reroll` → `OfferReroll()`、`Remove` → `OfferRemove(request.OfferIndex)`、`Decline` → `OfferDecline()`、それ以外 → `throw new BusinessException(ProduceErrors.InvalidArgument, "invalid kind")`
- `DecideExamRetryAsync`: `request.Retry ? ProduceCommand.ExamRetry() : ProduceCommand.ExamNoRetry()`
- `ExecuteLessonActionAsync`: `UseCard` → `ProduceCommand.UseCard(request.CardInstanceId)`、`UseDrink` → `ProduceCommand.UseDrink(request.DrinkSlotIndex)`、`Skip` → `ProduceCommand.Skip()`、それ以外 → 上と同じ `BusinessException`

`command` は `RunAsync` の外（`AppVersionHeader.Require` の後）で作る。

`Program.cs`: `ProduceCompletion`・`ProduceOperationUseCase` を Scoped で登録。

### 4.5 テスト支援（タスク 2）

`PlayerClient` に `public PlayerClient OnChannel(GrpcChannel other) => new PlayerClient(other, PlayerId, token, appVersion);` を足す（再起動したサーバーに同じプレイヤーのままつなぐ）。

`T/Integration/Support/ProduceDriver.cs`（namespace `Uta.Server.Tests.Integration.Support`）:

```csharp
/// <summary>応答の Phase を見て、次の RPC を決めて呼ぶ自動プレイ。</summary>
public sealed class ProduceDriver(PlayerClient player, bool skipOnly = false, bool preferSupply = false)
{
    private readonly string idPrefix = Guid.NewGuid().ToString("N")[..12];   // ドライバーを作り直しても x-request-id が重ならない
    public int Operations { get; private set; }            // 受け付けられた操作の数（失敗した UseCard は数えない）
    public IReadOnlyList<ExamResultDto> ExamResults { get; }   // 応答に ExamResult が入っていたもの、順に

    /// <summary>次の 1 操作を呼び、受け付けられた応答を返す。</summary>
    public async Task<ProduceStateResponse> StepAsync(ProduceStateResponse current);

    /// <summary>Phase が Finished になるまで続ける。maxOperations を超えたら InvalidOperationException。</summary>
    public async Task<ProduceStateResponse> PlayToEndAsync(ProduceStateResponse current, int maxOperations = 3000);
}
```

`StepAsync` の決め方（`current.Phase` ごと）。`x-request-id` は `$"{idPrefix}-{++counter}"`（`counter` はドライバーのフィールド。失敗した `UseCard` にも新しい ID を使う）:

- `WeekSelect`: 次の順で最初に当たるものを選ぶ。(0) `preferSupply` なら `Selectable` で `Kind == Supply` の最初の枠 → `(op, slot, -1, ParamKind.Nothing)`。(1) `Selectable` で `Kind == Lesson` の枠 → `ChooseWeekActionRequest(op, slot, -1, ParamKind.Singing)`。(2) `Kind == Class` の枠で、`ClassOptions` に `Selectable` な選択肢があるもの → その最初の `Index` を `OptionIndex` にして `(op, slot, index, ParamKind.Nothing)`。(3) `Selectable` な最初の枠（支給・休む）→ `(op, slot, -1, ParamKind.Nothing)`
- `CardOffer`: `ResolveCardOfferRequest(current.OperationNumber, CardOfferActionKind.Pick, 0)`
- `Lesson` / `Exam`: `skipOnly` でなければ、`current.Lesson.Hand` を先頭から、`ExecuteLessonActionRequest(op, LessonActionKind.UseCard, instanceId, 0)` を呼ぶ。`RpcException`（判定はドライバーの `private static bool HasReason(RpcException e, string reason)`: `e.StatusCode == StatusCode.FailedPrecondition` で、`e.Trailers` の `Key == ApiHeaders.ErrorDetailsTrailer && IsBinary` の項目の `Encoding.UTF8.GetString(ValueBytes)` が `reason` を含むとき true。`RpcAssert.Reason` と同じ読み方で、Assert せず真偽を返す。`reason` は `"PRODUCE_INVALID_ARGUMENT"`）なら次の手札へ（失敗は `Operations` に数えない）。全部だめなら、または `skipOnly` なら `(op, LessonActionKind.Skip, 0, 0)`
- `ExamResult`: `DecideExamRetryRequest(op, false)`
- それ以外（`Finished`・`Nothing`）: `InvalidOperationException`

`PlayToEndAsync(current, maxOperations = 3000)` は `Phase == Finished` まで `StepAsync` を繰り返す（`maxOperations` は `Operations` の数。超えたら `InvalidOperationException`）。各応答で次を `Assert` する（ドライバーが見張る）: `Phase == Exam` なら `Lesson.ExamTurns.Length == Lesson.MaxTurns` かつ `Lesson.NpcScores.Length == 5`、`Phase == Lesson` なら `ExamTurns` と `NpcScores` が空、`Phase` が `Lesson` / `Exam` / `WeekSelect` / `CardOffer` / `ExamResult` なら `Result == null`、`Phase == Finished` なら `Result != null`、`OperationNumber` が前の応答より 1 増えている。

## 5. タスクとテスト

各タスクの終わりに `dotnet build Uta.slnx`（警告 0・エラー 0）、`dotnet test --project server/Uta.Server.Tests`（結合テストは Docker が要る）、`dotnet test --project shared/Uta.Shared.Domain.Tests` が通ること。Docker が使えないときは、結合テストが確認できていないと報告する。

### タスク 1: 報酬とプレイヤーLv

4.1。テスト:

- `PlayerLevelRulesTests`: 経験値が足りない（`AddExp(1, 0, 10, [40, 80])` → `(1, 10)`）、ちょうど届く（`AddExp(1, 30, 10, [40, 80])` → `(2, 0)`）、あふれる（`AddExp(1, 30, 60, [40, 80])` → `(2, 50)`）、複数レベル（`AddExp(1, 0, 130, [40, 80, 120])` → `(3, 10)`）、最大 Lv に届いたらあふれを捨てる（`AddExp(2, 70, 1000, [40, 80])` → `(3, 0)`）、最大 Lv では増えない（`AddExp(3, 0, 50, [40, 80])` → `(3, 0)`）、`gain` が 0（`AddExp(1, 5, 0, [40])` → `(1, 5)`）
- `MasterProduceRewardCatalogTests`（`MasterDataLoader.Load()` を使う）: `ProduceRank.F` → `(50, 1000, 500, 10)`、`SSPlus` → `(800, 20000, 10000, 60)`、`F` から `SSPlus` の 13 ランクがすべて取れる、`Nothing` は `InvalidOperationException`。`MasterPlayerLevelCatalog`: `RequiredExps.Count == 99`、先頭 40、末尾 3960

### タスク 2a: 操作の RPC と終了

4.2〜4.4。ドライバーは使わない。テスト 1〜7・9・10（8 は 2b）。テスト（`T/Integration/ProduceOperationTests.cs`。`[Collection(PostgresCollection.Name)]`。開始は [開始の計画](2026-11-03-server-produce-start.md) のテストと同じ形で、`UtaServerFactory(postgres, randomSource: new ScriptedRandomSource(111UL, 222UL))`、アイドル 1・シナリオ 1・所持IDなし。呼び出しは `player.CallAsync<IProduceService, ProduceStateResponse>(x => x.ChooseWeekActionAsync(...), "req-n")`）:

1. `ChooseWeekAction_Lesson_StartsLesson`: 開始直後に `ChooseWeekActionAsync(0, slot 0, -1, Singing)` → `Phase == Lesson`、`OperationNumber == 1`、`Lesson` が `null` でなく `Hand` が空でない、`Lesson.Kind == Normal`、`Lesson.TargetParam == Singing`、`Lesson.ClearTarget == 25`、`DrawPile` が昇順。DB の `produce_sessions.version` が 1、`week` 1、`turn` が 1 以上
2. `ChooseWeekAction_Lesson_ParamNothing_InvalidArgument`: `Param` が `Nothing` → `PRODUCE_INVALID_ARGUMENT`。そのあと `GetStateAsync` が `Phase == WeekSelect`、`OperationNumber == 0`（拒否は何も保存しない）
3. `ChooseWeekAction_SlotOutOfRange_InvalidArgument`（枠 99）、`ChooseWeekAction_RestWhenFullHp_InvalidArgument`（枠 1）
4. `Operation_StaleNumber_Rejected`: `OperationNumber` を 1 大きくして送る → `PRODUCE_STALE_OPERATION`。1 つ進めたあとに 0 で送る → 同じ
5. `Operation_WrongPhase_UnexpectedPhase`: 週の選択のときに `ResolveCardOfferAsync(0, Pick, 0)`、`DecideExamRetryAsync(0, false)`、`ExecuteLessonActionAsync(0, Skip, 0, 0)` → いずれも `PRODUCE_UNEXPECTED_PHASE`。レッスン中に `ChooseWeekActionAsync` → 同じ
6. `Operation_NoSession_NotInProgress`: 開始前の `ChooseWeekActionAsync` → `PRODUCE_NOT_IN_PROGRESS`
7. `Operation_DifferentAppVersion_SessionDiscarded`: 開始（バージョン 1）→ `WithAppVersion("2")` の操作 → `PRODUCE_SESSION_DISCARDED`、DB の `status` は 0 のまま
### タスク 2b: テスト支援とテスト 8

4.5（`PlayerClient.OnChannel`・`ProduceDriver`）を作り、テスト 8 を `ProduceOperationTests.cs` に足す。

8. `ExecuteLessonAction_UnknownKind_InvalidArgument`（`Kind` が `Nothing`）、`ExecuteLessonAction_UseDrink_InvalidArgument` と `UseCardNotInHand` は、開始 → `ChooseWeekActionRequest(op, 0, -1, Singing)` で `Phase == Lesson` にしてから呼ぶ。`ResolveCardOfferAsync` の `Kind` が `Nothing` と `Reroll`（再抽選は残り回数 0 なので `PRODUCE_INVALID_ARGUMENT`。カードの選択肢の局面で。局面はドライバーで作る）、`ExecuteLessonAction_UseDrink_InvalidArgument`、`ExecuteLessonAction_UseCardNotInHand_InvalidArgument`（インスタンス ID 9999）。カードの選択肢の局面は、シナリオ 2 を `ProduceDriver(player, preferSupply: true).StepAsync` で `Phase == CardOffer` になるまで進めて作る（週 2 の支給は選べて、常にカードの選択肢を出すので、必ず現れる）
9. `Operation_SameRequestId_ReturnsSavedResponse`: 同じ `x-request-id` で同じ操作を 2 回 → 同じレスポンスで、`produce_sessions.version` は 1 回分だけ増える
10. `Operation_RejectedKeepsStateAndStreams`: 拒否（`Param` が `Nothing` の `ChooseWeekAction`）の前後で、`produce_sessions.state`（`QueryScalarAsync<byte[]>`）と `version` が同じ。拒否のあとに有効な操作を送ると成功する

### タスク 3: 通しと再開

4.5。テスト（`T/Integration/ProducePlayTests.cs`。`[Collection(PostgresCollection.Name)]`。`UtaServerFactory(postgres, randomSource: new ScriptedRandomSource(12345UL, 23456UL, 34567UL, 45678UL, 56789UL, 67890UL))`。`StartAsync` が 1 回につき 1 個使う。1 テストで `StartAsync` を 6 回以内しか呼ばない）:

1. `ShortestScenario_PlaysToEnd_GrantsRewards`（完了条件 4）: 新規プレイヤー（`SignUpAsync` 済み）で、アイドル 1・シナリオ 1・所持IDは `SignInAsync` の `SupportCards` の先頭 2 枚。`StartAsync` → `ProduceDriver.PlayToEndAsync`。次を確かめる:
   - 最後の応答の `Result.Ending == Completed`、`Result.Rank` が `F`〜`SSPlus`、`Result.ExamOutcome` が `FinalPassed` か `FinalFailed`。最後の応答自体の `ExamResult` は `null`（再挑戦が残るので、試験の直後は `ExamResult` の局面で止まり、最後の応答は `DecideExamRetry` の応答。[produce-server.md](../wiki/architecture/server/produce-server.md) 5章の規則どおり）。代わりに `driver.ExamResults` の最後が `Kind == Final`
   - マスタ（`MasterDataLoader.Load().EvaluationRankRewardTable`）の `Result.Rank` の行と、`Result.GachaCurrency` / `Gold` / `SupportPoints` / `Exp` が一致する
   - `players`: `gacha_currency` が 2500 + 報酬、`gold` が 10000 + 報酬、`support_points` が 10000 + 報酬。`level` / `exp` は `PlayerLevelRules.AddExp(1, 0, 報酬の Exp, マスタの PlayerLevelExp の RequiredExp の昇順)` と一致し、`Result.PlayerLevel` / `PlayerExp` も同じ
   - `produce_sessions.status` が 1。`produce_results` に 1 行（`ending` 1、`evaluation_value` と `produce_rank` が応答と同じ、`gacha_currency` などが報酬）
   - `game_logs` に kind `produce`、reason `produce_reward` が 1 件
   - 終了後の `GetStateAsync` は `Phase == Nothing`。操作の RPC は `PRODUCE_NOT_IN_PROGRESS`。もう一度 `StartAsync` が通る
2. `Resume_MidLesson_GetStateMatches_ThenContinuesOnNewServer`（完了条件 5）: 開始 → `ChooseWeekActionRequest(op, 0, -1, Singing)` でレッスンを選ぶ → `ProduceDriver.StepAsync` を、`Phase == Lesson && Lesson.Turn >= 2` になるまで繰り返す（`Phase` が `Lesson` でなくなったら失敗。1 ターン目の `Skip` で 2 ターン目に進む）→ `GetStateAsync` が直前の応答と、`Phase`・`Week`・`OperationNumber`・`Hp`・`Singing`・`Deck.Length`・`Lesson.Turn`・`Lesson.Hand`（並びも）・`Lesson.DrawPile`・`Lesson.StatusValues`・`Lesson.ParamGained` で一致する。そのあと `new UtaServerFactory(postgres)`（乱数源は既定）で新しいサーバーを作り（`factory.Dispose` はしない）、`player.OnChannel(factory2.CreateGrpcChannel())` で同じプレイヤーのまま `ProduceDriver.PlayToEndAsync` で最後まで進められ、`Result.Ending == Completed`
3. `Resume_AtEveryPhase_GetStateMatches`: ドライバーで進めながら、**応答の `Phase` が直前の応答の `Phase` と違うたび**（初回を含む。`Phase == Finished` の応答は除く。終了後の `GetState` は `Nothing` を返すため）に `GetStateAsync` を呼び、直前の応答と同じ。比べる項目: 常に `Phase`・`Week`・`OperationNumber`・`Hp`・`Singing`・`Deck.Length`・`WeekActions.Length`・`CardOffers`（並びも）・`ExamResult` は `Phase == ExamResult` の応答だけ、`null` でないことと `Kind`・`Attempt`・`Score`・`Rank`・`Passed` が一致すること（それ以外の応答の `ExamResult` は比べない）。`Lesson` は、直前の応答の `Lesson` が `null` なら GetState の `Lesson` も `null`、`null` でなければ 2 と同じ項目に加えて `Lesson.ExamTurns.Length`。シナリオ 2（初級・13 週）を使い、アイドル 1。`WeekSelect`・`Lesson`・`Exam`・`ExamResult` が現れること（中間試験の後は必ず `ExamResult` になる）も確かめる。`CardOffer` は現れたときだけ比べ、現れなくてもよい（乱数に頼るため）
4. `Complete_MidExamFailed_StillGrantsRewardByRank`: シナリオ 2・アイドル 1 を `ProduceDriver(player, skipOnly: true)` で最後まで進める。（スコア 0 は NPC の最低 180 に届かず必ず不合格）`Result.ExamOutcome == MidFailed`、`FinalScore == 0`、`FinalRank == 0`、`Result.GachaCurrency` などが `Result.Rank` のマスタの行と一致する（ランクの値は決め打ちしない）。`produce_results.exam_outcome` が 1
5. `Complete_LevelUp_AndMaxLevel`: ア）`UPDATE players SET exp = 39` してから最短シナリオを最後まで → `PlayerLevel == 2`、`PlayerExp == 39 + 報酬の Exp − 40`。イ）別のプレイヤーで `UPDATE players SET level = 100, exp = 0` → 最後まで進めて `PlayerLevel == 100`、`PlayerExp == 0`
6. `Operation_RetryExam_ReturnsToExam`: シナリオ 1 を `ProduceDriver.StepAsync` で `Phase == ExamResult` になるまで進める（最終試験は再挑戦が残っているので、1 回目の試験の後に必ずこの局面になる）。`DecideExamRetryAsync(op, true)` → `Phase == Exam`、`ExamRetryLeft` が 1 減る（初期は 3）、`ExamResult` は `null`。そのあと `ProduceDriver.PlayToEndAsync`（ドライバーは再挑戦しない）で最後まで進み、`Result.Ending == Completed`

### タスク 4: wiki への反映（メインのセッション）

[produce-server.md](../wiki/architecture/server/produce-server.md) の実装メモ、[produce-domain.md](../wiki/architecture/produce-domain.md) の実装メモ、`api-design.md` の実装メモ、`produce.md` の完了条件のチェック、`game-spec.md` の順 6 に done の印とリンク、3 つの計画の `status`。

## 6. 未決事項

なし
