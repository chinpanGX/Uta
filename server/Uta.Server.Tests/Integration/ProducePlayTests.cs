using Grpc.Core;
using Grpc.Net.Client;
using Uta.Server.Domain.Produce;
using Uta.Server.Infrastructure;
using Uta.Server.Infrastructure.Produce;
using Uta.Server.Tests.Integration.Support;
using Uta.Shared.Contracts.Player;
using Uta.Shared.Contracts.Produce;
using Uta.Shared.Domain.Lesson;
using Uta.Shared.Domain.Produce;

namespace Uta.Server.Tests.Integration;

[Collection(PostgresCollection.Name)]
public class ProducePlayTests(PostgresFixture postgres)
{
    private static UtaServerFactory NewFactory(PostgresFixture postgres) =>
        new(postgres, randomSource: new ScriptedRandomSource(12345UL, 23456UL, 34567UL, 45678UL, 56789UL, 67890UL));

    private static async Task<(PlayerClient Player, SignInResponse SignIn)> NewPlayerAsync(GrpcChannel channel, string secret = "0123456789abcdef-secret")
    {
        var player = await PlayerClient.CreateAsync(channel, secret);
        await player.CallAsync(service => service.SignUpAsync(), "req-1");
        var signIn = await player.CallAsync(service => service.SignInAsync(), "req-2");
        return (player, signIn);
    }

    private static Task<ProduceStateResponse> StartProduceAsync(PlayerClient player, string requestId, string[] uids, int scenarioId) =>
        player.CallAsync<IProduceService, ProduceStateResponse>(
            x => x.StartAsync(new StartProduceRequest(1, uids, 0, scenarioId)), requestId);

    private static Task<ProduceStateResponse> GetStateAsync(PlayerClient player, string requestId) =>
        player.CallAsync<IProduceService, ProduceStateResponse>(x => x.GetStateAsync(), requestId);

    private Task<T?> Scalar<T>(string sql, PlayerClient player) => postgres.QueryScalarAsync<T>(sql, player.PlayerId);

    private static (int GachaCurrency, int Gold, int SupportPoints, int Exp) MasterReward(ProduceRank rank)
    {
        var row = MasterDataLoader.Load().EvaluationRankRewardTable.All.Single(x => (int)x.Rank == (int)rank);
        return (row.GachaCurrency, row.Gold, row.SupportPoints, row.Exp);
    }

    private static IReadOnlyList<int> MasterRequiredExps() =>
        MasterDataLoader.Load().PlayerLevelExpTable.All.OrderBy(x => x.Level).Select(x => x.RequiredExp).ToList();

    private static async Task AssertReasonAsync(Func<Task> call, string reason)
    {
        var exception = await Assert.ThrowsAsync<RpcException>(call);
        RpcAssert.Reason(exception, StatusCode.FailedPrecondition, reason);
    }

    /// <summary>最短シナリオ（シナリオ 1・アイドル 1・所持IDは先頭 2 枚）を最後まで進める。</summary>
    private static async Task<(ProduceDriver Driver, ProduceStateResponse Final)> PlayShortestAsync(
        PlayerClient player, SignInResponse signIn, string requestId = "req-3")
    {
        var uids = signIn.SupportCards.Take(2).Select(x => x.UniqueId).ToArray();
        var started = await StartProduceAsync(player, requestId, uids, 1);
        var driver = new ProduceDriver(player);
        var final = await driver.PlayToEndAsync(started);
        return (driver, final);
    }

    [Fact]
    public async Task ShortestScenario_PlaysToEnd_GrantsRewards()
    {
        await using var factory = NewFactory(postgres);
        using var channel = factory.CreateGrpcChannel();
        var (player, signIn) = await NewPlayerAsync(channel);

        var (driver, final) = await PlayShortestAsync(player, signIn);

        Assert.Equal(ProducePhase.Finished, final.Phase);
        var result = final.Result;
        Assert.NotNull(result);
        Assert.Equal(ProduceEnding.Completed, result.Ending);
        Assert.InRange((int)result.Rank, (int)ProduceRank.F, (int)ProduceRank.SSPlus);
        Assert.True(result.ExamOutcome is ExamOutcome.FinalPassed or ExamOutcome.FinalFailed);
        Assert.Null(final.ExamResult);
        Assert.Equal(ExamKind.Final, driver.ExamResults[^1].Kind);

        var reward = MasterReward(result.Rank);
        Assert.Equal(reward.GachaCurrency, result.GachaCurrency);
        Assert.Equal(reward.Gold, result.Gold);
        Assert.Equal(reward.SupportPoints, result.SupportPoints);
        Assert.Equal(reward.Exp, result.Exp);

        Assert.Equal(2500 + reward.GachaCurrency, await Scalar<int>("SELECT gacha_currency FROM players WHERE player_id = $1", player));
        Assert.Equal(10000 + reward.Gold, await Scalar<int>("SELECT gold FROM players WHERE player_id = $1", player));
        Assert.Equal(10000 + reward.SupportPoints, await Scalar<int>("SELECT support_points FROM players WHERE player_id = $1", player));
        var (level, exp) = PlayerLevelRules.AddExp(1, 0, reward.Exp, MasterRequiredExps());
        Assert.Equal(level, await Scalar<int>("SELECT level FROM players WHERE player_id = $1", player));
        Assert.Equal(exp, await Scalar<int>("SELECT exp FROM players WHERE player_id = $1", player));
        Assert.Equal(level, result.PlayerLevel);
        Assert.Equal(exp, result.PlayerExp);

        Assert.Equal(1, await Scalar<int>("SELECT status FROM produce_sessions WHERE player_id = $1", player));
        Assert.Equal(1, await Scalar<long>("SELECT count(*) FROM produce_results WHERE player_id = $1", player));
        Assert.Equal(1, await Scalar<int>("SELECT ending FROM produce_results WHERE player_id = $1", player));
        Assert.Equal(result.EvaluationValue, await Scalar<int>("SELECT evaluation_value FROM produce_results WHERE player_id = $1", player));
        Assert.Equal((int)result.Rank, await Scalar<int>("SELECT produce_rank FROM produce_results WHERE player_id = $1", player));
        Assert.Equal(reward.GachaCurrency, await Scalar<int>("SELECT gacha_currency FROM produce_results WHERE player_id = $1", player));
        Assert.Equal(reward.Gold, await Scalar<int>("SELECT gold FROM produce_results WHERE player_id = $1", player));
        Assert.Equal(reward.SupportPoints, await Scalar<int>("SELECT support_points FROM produce_results WHERE player_id = $1", player));
        Assert.Equal(reward.Exp, await Scalar<int>("SELECT exp FROM produce_results WHERE player_id = $1", player));
        Assert.Equal(1, await Scalar<long>(
            "SELECT count(*) FROM game_logs WHERE player_id = $1 AND kind = 'produce' AND body->>'reason' = 'produce_reward'", player));

        Assert.Equal(ProducePhase.Nothing, (await GetStateAsync(player, "req-state")).Phase);
        await AssertReasonAsync(
            () => player.CallAsync<IProduceService, ProduceStateResponse>(
                x => x.ChooseWeekActionAsync(new ChooseWeekActionRequest(final.OperationNumber, 0, -1, ParamKind.Singing)), "req-after"),
            "PRODUCE_NOT_IN_PROGRESS");
        var again = await StartProduceAsync(player, "req-again", [], 1);
        Assert.Equal(ProducePhase.WeekSelect, again.Phase);
    }

    private static void AssertSameLesson(LessonDto expected, LessonDto actual)
    {
        Assert.NotNull(actual);
        Assert.Equal(expected.Turn, actual.Turn);
        Assert.Equal(expected.Hand, actual.Hand);
        Assert.Equal(expected.DrawPile, actual.DrawPile);
        Assert.Equal(expected.StatusValues, actual.StatusValues);
        Assert.Equal(expected.ParamGained, actual.ParamGained);
    }

    private static void AssertSameBasics(ProduceStateResponse expected, ProduceStateResponse actual)
    {
        Assert.Equal(expected.Phase, actual.Phase);
        Assert.Equal(expected.Week, actual.Week);
        Assert.Equal(expected.OperationNumber, actual.OperationNumber);
        Assert.Equal(expected.Hp, actual.Hp);
        Assert.Equal(expected.Singing, actual.Singing);
        Assert.Equal(expected.Deck.Length, actual.Deck.Length);
    }

    [Fact]
    public async Task Resume_MidLesson_GetStateMatches_ThenContinuesOnNewServer()
    {
        await using var factory = NewFactory(postgres);
        using var channel = factory.CreateGrpcChannel();
        var (player, signIn) = await NewPlayerAsync(channel);
        var uids = signIn.SupportCards.Take(2).Select(x => x.UniqueId).ToArray();
        var started = await StartProduceAsync(player, "req-3", uids, 1);
        var current = await player.CallAsync<IProduceService, ProduceStateResponse>(
            x => x.ChooseWeekActionAsync(new ChooseWeekActionRequest(started.OperationNumber, 0, -1, ParamKind.Singing)), "req-4");
        var driver = new ProduceDriver(player);
        while (!(current.Phase == ProducePhase.Lesson && current.Lesson.Turn >= 2))
        {
            Assert.Equal(ProducePhase.Lesson, current.Phase);
            current = await driver.StepAsync(current);
        }

        var state = await GetStateAsync(player, "req-state");

        AssertSameBasics(current, state);
        AssertSameLesson(current.Lesson, state.Lesson);

        // サーバーを作り直す（古い factory は破棄しない）。乱数源は既定
        await using var factory2 = new UtaServerFactory(postgres);
        using var channel2 = factory2.CreateGrpcChannel();
        var resumed = player.OnChannel(channel2);
        var final = await new ProduceDriver(resumed).PlayToEndAsync(state);

        Assert.Equal(ProduceEnding.Completed, final.Result.Ending);
    }

    [Fact]
    public async Task Resume_AtEveryPhase_GetStateMatches()
    {
        await using var factory = NewFactory(postgres);
        using var channel = factory.CreateGrpcChannel();
        var (player, _) = await NewPlayerAsync(channel);
        var current = await StartProduceAsync(player, "req-3", [], 2);
        var driver = new ProduceDriver(player);
        var seen = new HashSet<ProducePhase>();
        var previousPhase = ProducePhase.Nothing;
        var stateCounter = 0;
        for (var i = 0; ; i++)
        {
            Assert.True(i < 3000, "操作が多すぎる");
            if (current.Phase == ProducePhase.Finished)
            {
                break;
            }

            if (current.Phase != previousPhase)
            {
                seen.Add(current.Phase);
                var state = await GetStateAsync(player, $"req-state-{++stateCounter}");
                AssertSameBasics(current, state);
                Assert.Equal(current.WeekActions.Length, state.WeekActions.Length);
                Assert.Equal(current.CardChoiceMasterIds, state.CardChoiceMasterIds);
                if (current.Phase == ProducePhase.ExamResult)
                {
                    Assert.NotNull(state.ExamResult);
                    Assert.Equal(current.ExamResult.Kind, state.ExamResult.Kind);
                    Assert.Equal(current.ExamResult.Attempt, state.ExamResult.Attempt);
                    Assert.Equal(current.ExamResult.Score, state.ExamResult.Score);
                    Assert.Equal(current.ExamResult.Rank, state.ExamResult.Rank);
                    Assert.Equal(current.ExamResult.Passed, state.ExamResult.Passed);
                }

                if (current.Lesson is null)
                {
                    Assert.Null(state.Lesson);
                }
                else
                {
                    AssertSameLesson(current.Lesson, state.Lesson);
                    Assert.Equal(current.Lesson.ExamTurns.Length, state.Lesson.ExamTurns.Length);
                }
            }

            previousPhase = current.Phase;
            current = await driver.StepAsync(current);
        }

        Assert.Contains(ProducePhase.WeekSelect, seen);
        Assert.Contains(ProducePhase.Lesson, seen);
        Assert.Contains(ProducePhase.Exam, seen);
        Assert.Contains(ProducePhase.ExamResult, seen);
    }

    [Fact]
    public async Task Complete_MidExamFailed_StillGrantsRewardByRank()
    {
        await using var factory = NewFactory(postgres);
        using var channel = factory.CreateGrpcChannel();
        var (player, _) = await NewPlayerAsync(channel);
        var started = await StartProduceAsync(player, "req-3", [], 2);

        var final = await new ProduceDriver(player, skipOnly: true).PlayToEndAsync(started);

        var result = final.Result;
        Assert.Equal(ExamOutcome.MidFailed, result.ExamOutcome);
        Assert.Equal(0, result.FinalScore);
        Assert.Equal(0, result.FinalRank);
        var reward = MasterReward(result.Rank);
        Assert.Equal(reward.GachaCurrency, result.GachaCurrency);
        Assert.Equal(reward.Gold, result.Gold);
        Assert.Equal(reward.SupportPoints, result.SupportPoints);
        Assert.Equal(reward.Exp, result.Exp);
        Assert.Equal(1, await Scalar<int>("SELECT exam_outcome FROM produce_results WHERE player_id = $1", player));
    }

    [Fact]
    public async Task Complete_LevelUp_AndMaxLevel()
    {
        await using var factory = NewFactory(postgres);
        using var channel = factory.CreateGrpcChannel();
        var (player, signIn) = await NewPlayerAsync(channel);
        await postgres.ExecuteAsync("UPDATE players SET exp = 39 WHERE player_id = $1", player.PlayerId);

        var (_, final) = await PlayShortestAsync(player, signIn);

        var reward = MasterReward(final.Result.Rank);
        Assert.Equal(2, final.Result.PlayerLevel);
        Assert.Equal(39 + reward.Exp - 40, final.Result.PlayerExp);

        var (other, otherSignIn) = await NewPlayerAsync(channel, "another-secret-0123456789");
        await postgres.ExecuteAsync("UPDATE players SET level = 100, exp = 0 WHERE player_id = $1", other.PlayerId);

        var (_, otherFinal) = await PlayShortestAsync(other, otherSignIn);

        Assert.Equal(100, otherFinal.Result.PlayerLevel);
        Assert.Equal(0, otherFinal.Result.PlayerExp);
    }

    [Fact]
    public async Task Operation_RetryExam_ReturnsToExam()
    {
        await using var factory = NewFactory(postgres);
        using var channel = factory.CreateGrpcChannel();
        var (player, _) = await NewPlayerAsync(channel);
        var current = await StartProduceAsync(player, "req-3", [], 1);
        var driver = new ProduceDriver(player);
        for (var i = 0; current.Phase != ProducePhase.ExamResult; i++)
        {
            Assert.True(i < 3000, "ExamResult に届かなかった");
            current = await driver.StepAsync(current);
        }

        var before = current.ExamRetryLeft;
        Assert.Equal(3, before);

        var retried = await player.CallAsync<IProduceService, ProduceStateResponse>(
            x => x.DecideExamRetryAsync(new DecideExamRetryRequest(current.OperationNumber, true)), "req-retry");

        Assert.Equal(ProducePhase.Exam, retried.Phase);
        Assert.Equal(before - 1, retried.ExamRetryLeft);
        Assert.Null(retried.ExamResult);
        var final = await new ProduceDriver(player).PlayToEndAsync(retried);
        Assert.Equal(ProduceEnding.Completed, final.Result.Ending);
    }

    [Fact]
    public async Task Replay_FromSavedOperations_MatchesSavedState()
    {
        await using var factory = NewFactory(postgres);
        using var channel = factory.CreateGrpcChannel();
        var (player, signIn) = await NewPlayerAsync(channel);
        await PlayShortestAsync(player, signIn);

        var bytes = await Scalar<byte[]>("SELECT state FROM produce_sessions WHERE player_id = $1", player);
        Assert.NotNull(bytes);
        var data = ProduceStateSerializer.Deserialize(bytes);
        var master = MasterDataLoader.Load();
        var catalog = MasterProduceCatalog.Create(master, MasterCardCatalog.Create(master));
        var engine = new ProduceEngine(catalog);

        var saved = data.Snapshot.ToState(catalog);

        var supports = data.Snapshot.SupportCards
            .Select(x => new SupportCardInput(x.CardId, x.Level, x.LimitBreak))
            .ToArray();
        var input = new ProduceStartInput(data.Snapshot.ScenarioId, data.Snapshot.IdolId, data.Snapshot.PlayerLevel, supports);
        var streams = new SessionRandomStreams(data.MasterSeed, []);
        var replayed = engine.Start(input, streams);
        foreach (var operation in data.Snapshot.Operations)
        {
            var command = new ProduceCommand(operation.Kind, operation.Arg1, operation.Arg2, operation.Arg3);
            Assert.Equal(ProduceApplyResult.Accepted, engine.Apply(replayed, command, streams));
        }

        Assert.Equal(ProducePhase.Finished, saved.Phase);
        Assert.NotEmpty(data.Snapshot.Operations);
        Assert.Empty(ProduceStateComparer.Diff(saved, replayed));
    }
}
