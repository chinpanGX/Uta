using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Uta.Server.Domain.Produce;
using Uta.Server.Infrastructure;
using Uta.Server.Tests.Integration.Support;
using Uta.Shared.Contracts.Player;
using Uta.Shared.Contracts.Produce;
using Uta.Shared.Domain.Lesson;
using Uta.Shared.Domain.Produce;
using VolTech.Server.Transaction;

namespace Uta.Server.Tests.Integration;

[Collection(PostgresCollection.Name)]
public class ProduceOperationTests(PostgresFixture postgres)
{
    private static async Task<(ServerFactory Factory, GrpcChannel Channel, PlayerClient Player)> StartAsync(PostgresFixture postgres)
    {
        var factory = new ServerFactory(postgres, randomSource: new ScriptedRandomSource(111UL, 222UL));
        var channel = factory.CreateGrpcChannel();
        var player = await PlayerClient.CreateAsync(channel, "0123456789abcdef-secret");
        await player.CallAsync(service => service.SignUpAsync(), "req-1");
        await player.CallAsync(service => service.SignInAsync(), "req-2");
        return (factory, channel, player);
    }

    private static Task<ProduceStateResponse> StartProduceAsync(PlayerClient player, string requestId, int scenarioId = 1) =>
        player.CallAsync<IProduceService, ProduceStateResponse>(
            x => x.StartAsync(new StartProduceRequest(1, [], 0, scenarioId)), requestId);

    private static Task<ProduceStateResponse> ChooseAsync(
        PlayerClient player, string requestId, int op, int slot, int option, ParamKind param) =>
        player.CallAsync<IProduceService, ProduceStateResponse>(
            x => x.ChooseWeekActionAsync(new ChooseWeekActionRequest(op, slot, option, param)), requestId);

    private static Task<ProduceStateResponse> GetStateAsync(PlayerClient player, string requestId) =>
        player.CallAsync<IProduceService, ProduceStateResponse>(x => x.GetStateAsync(), requestId);

    private static async Task AssertReasonAsync(Func<Task> call, string reason)
    {
        var exception = await Assert.ThrowsAsync<RpcException>(call);
        RpcAssert.Reason(exception, StatusCode.FailedPrecondition, reason);
    }

    private Task<T?> Scalar<T>(string sql, PlayerClient player) => postgres.QueryScalarAsync<T>(sql, player.PlayerId);

    [Fact]
    public async Task ChooseWeekAction_Lesson_StartsLesson()
    {
        var (factory, channel, player) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;
        await StartProduceAsync(player, "req-3");

        var response = await ChooseAsync(player, "req-4", 0, 0, -1, ParamKind.Singing);

        Assert.Equal(ProducePhase.Lesson, response.Phase);
        Assert.Equal(1, response.OperationNumber);
        Assert.NotNull(response.Lesson);
        Assert.NotEmpty(response.Lesson.Hand);
        Assert.Equal(LessonKind.Normal, response.Lesson.Kind);
        Assert.Equal(ParamKind.Singing, response.Lesson.TargetParam);
        Assert.Equal(25, response.Lesson.ClearTarget);
        Assert.Equal(response.Lesson.DrawPile.Order().ToList(), response.Lesson.DrawPile.ToList());
        Assert.Equal(1, await Scalar<int>("SELECT version FROM produce_sessions WHERE player_id = $1", player));
        Assert.Equal(1, await Scalar<int>("SELECT week FROM produce_sessions WHERE player_id = $1", player));
        Assert.True(await Scalar<int>("SELECT turn FROM produce_sessions WHERE player_id = $1", player) >= 1);
    }

    [Fact]
    public async Task ChooseWeekAction_Lesson_ParamNothing_InvalidArgument()
    {
        var (factory, channel, player) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;
        await StartProduceAsync(player, "req-3");

        await AssertReasonAsync(() => ChooseAsync(player, "req-4", 0, 0, -1, ParamKind.Nothing), "PRODUCE_INVALID_ARGUMENT");

        var state = await GetStateAsync(player, "req-5");
        Assert.Equal(ProducePhase.WeekSelect, state.Phase);
        Assert.Equal(0, state.OperationNumber);
    }

    [Fact]
    public async Task ChooseWeekAction_SlotOutOfRange_InvalidArgument()
    {
        var (factory, channel, player) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;
        await StartProduceAsync(player, "req-3");

        await AssertReasonAsync(() => ChooseAsync(player, "req-4", 0, 99, -1, ParamKind.Singing), "PRODUCE_INVALID_ARGUMENT");
    }

    [Fact]
    public async Task ChooseWeekAction_RestWhenFullHp_InvalidArgument()
    {
        var (factory, channel, player) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;
        await StartProduceAsync(player, "req-3");

        await AssertReasonAsync(() => ChooseAsync(player, "req-4", 0, 1, -1, ParamKind.Nothing), "PRODUCE_INVALID_ARGUMENT");
    }

    [Fact]
    public async Task Operation_StaleNumber_Rejected()
    {
        var (factory, channel, player) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;
        await StartProduceAsync(player, "req-3");

        await AssertReasonAsync(() => ChooseAsync(player, "req-4", 1, 0, -1, ParamKind.Singing), "PRODUCE_STALE_OPERATION");

        await ChooseAsync(player, "req-5", 0, 0, -1, ParamKind.Singing);
        await AssertReasonAsync(() => ChooseAsync(player, "req-6", 0, 0, -1, ParamKind.Singing), "PRODUCE_STALE_OPERATION");
    }

    [Fact]
    public async Task Operation_WrongPhase_UnexpectedPhase()
    {
        var (factory, channel, player) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;
        await StartProduceAsync(player, "req-3");

        await AssertReasonAsync(
            () => player.CallAsync<IProduceService, ProduceStateResponse>(
                x => x.ResolveCardOfferAsync(new ResolveCardOfferRequest(0, CardOfferActionKind.Pick, 0)), "req-4"),
            "PRODUCE_UNEXPECTED_PHASE");
        await AssertReasonAsync(
            () => player.CallAsync<IProduceService, ProduceStateResponse>(
                x => x.DecideExamRetryAsync(new DecideExamRetryRequest(0, false)), "req-5"),
            "PRODUCE_UNEXPECTED_PHASE");
        await AssertReasonAsync(
            () => player.CallAsync<IProduceService, ProduceStateResponse>(
                x => x.ExecuteLessonActionAsync(new ExecuteLessonActionRequest(0, LessonActionKind.Skip, 0, 0)), "req-6"),
            "PRODUCE_UNEXPECTED_PHASE");

        await ChooseAsync(player, "req-7", 0, 0, -1, ParamKind.Singing);
        await AssertReasonAsync(() => ChooseAsync(player, "req-8", 1, 0, -1, ParamKind.Singing), "PRODUCE_UNEXPECTED_PHASE");
    }

    [Fact]
    public async Task Operation_NoSession_NotInProgress()
    {
        var (factory, channel, player) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;

        await AssertReasonAsync(() => ChooseAsync(player, "req-3", 0, 0, -1, ParamKind.Singing), "PRODUCE_NOT_IN_PROGRESS");
    }

    [Fact]
    public async Task Operation_DifferentAppVersion_SessionDiscarded()
    {
        var (factory, channel, player) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;
        await StartProduceAsync(player, "req-3");

        await AssertReasonAsync(
            () => ChooseAsync(player.WithAppVersion("2"), "req-4", 0, 0, -1, ParamKind.Singing), "PRODUCE_SESSION_DISCARDED");

        Assert.Equal(0, await Scalar<int>("SELECT status FROM produce_sessions WHERE player_id = $1", player));
    }

    private static async Task<ProduceStateResponse> StartLessonAsync(PlayerClient player)
    {
        var started = await StartProduceAsync(player, "req-3");
        return await ChooseAsync(player, "req-4", started.OperationNumber, 0, -1, ParamKind.Singing);
    }

    [Fact]
    public async Task Lesson_Response_HasUsableHandSameLengthAsHand()
    {
        var (factory, channel, player) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;
        var lesson = await StartLessonAsync(player);

        Assert.NotNull(lesson.Lesson.UsableHand);
        Assert.Equal(lesson.Lesson.Hand.Length, lesson.Lesson.UsableHand.Length);
        var state = await GetStateAsync(player, "req-5");
        Assert.NotNull(state.Lesson.UsableHand);
        Assert.Equal(state.Lesson.Hand.Length, state.Lesson.UsableHand.Length);
    }

    [Fact]
    public async Task WeekSelect_Response_HasNoLesson()
    {
        var (factory, channel, player) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;

        var started = await StartProduceAsync(player, "req-3");

        Assert.Null(started.Lesson);
    }

    [Fact]
    public async Task ExecuteLessonAction_UnknownKind_InvalidArgument()
    {
        var (factory, channel, player) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;
        var lesson = await StartLessonAsync(player);

        await AssertReasonAsync(
            () => player.CallAsync<IProduceService, ProduceStateResponse>(
                x => x.ExecuteLessonActionAsync(new ExecuteLessonActionRequest(lesson.OperationNumber, LessonActionKind.Nothing, 0, 0)), "req-5"),
            "PRODUCE_INVALID_ARGUMENT");
    }

    [Fact]
    public async Task ExecuteLessonAction_UseDrink_InvalidArgument()
    {
        var (factory, channel, player) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;
        var lesson = await StartLessonAsync(player);

        await AssertReasonAsync(
            () => player.CallAsync<IProduceService, ProduceStateResponse>(
                x => x.ExecuteLessonActionAsync(new ExecuteLessonActionRequest(lesson.OperationNumber, LessonActionKind.UseDrink, 0, 0)), "req-5"),
            "PRODUCE_INVALID_ARGUMENT");
    }

    [Fact]
    public async Task ExecuteLessonAction_UseCardNotInHand_InvalidArgument()
    {
        var (factory, channel, player) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;
        var lesson = await StartLessonAsync(player);

        await AssertReasonAsync(
            () => player.CallAsync<IProduceService, ProduceStateResponse>(
                x => x.ExecuteLessonActionAsync(new ExecuteLessonActionRequest(lesson.OperationNumber, LessonActionKind.UseCard, 9999, 0)), "req-5"),
            "PRODUCE_INVALID_ARGUMENT");
    }

    private async Task<(ProduceStateResponse Offer, ServerFactory Factory, GrpcChannel Channel, PlayerClient Player)> StartCardOfferAsync()
    {
        var (factory, channel, player) = await StartAsync(postgres);
        var current = await StartProduceAsync(player, "req-3", scenarioId: 2);
        var driver = new ProduceDriver(player, preferSupply: true);
        for (var i = 0; current.Phase != ProducePhase.CardOffer; i++)
        {
            Assert.True(i < 200, "CardOffer に届かなかった");
            current = await driver.StepAsync(current);
        }

        return (current, factory, channel, player);
    }

    [Fact]
    public async Task ResolveCardOffer_UnknownKind_InvalidArgument()
    {
        var (offer, factory, channel, player) = await StartCardOfferAsync();
        await using var _ = factory;
        using var __ = channel;

        await AssertReasonAsync(
            () => player.CallAsync<IProduceService, ProduceStateResponse>(
                x => x.ResolveCardOfferAsync(new ResolveCardOfferRequest(offer.OperationNumber, CardOfferActionKind.Nothing, 0)), "req-x1"),
            "PRODUCE_INVALID_ARGUMENT");
    }

    [Fact]
    public async Task ResolveCardOffer_RerollWithNoneLeft_InvalidArgument()
    {
        var (offer, factory, channel, player) = await StartCardOfferAsync();
        await using var _ = factory;
        using var __ = channel;

        await AssertReasonAsync(
            () => player.CallAsync<IProduceService, ProduceStateResponse>(
                x => x.ResolveCardOfferAsync(new ResolveCardOfferRequest(offer.OperationNumber, CardOfferActionKind.Reroll, 0)), "req-x1"),
            "PRODUCE_INVALID_ARGUMENT");
    }

    [Fact]
    public async Task Operation_SameRequestId_ReturnsSavedResponse()
    {
        var (factory, channel, player) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;
        await StartProduceAsync(player, "req-3");

        var first = await ChooseAsync(player, "req-4", 0, 0, -1, ParamKind.Singing);
        var second = await ChooseAsync(player, "req-4", 0, 0, -1, ParamKind.Singing);

        Assert.Equal(first.Phase, second.Phase);
        Assert.Equal(first.OperationNumber, second.OperationNumber);
        Assert.Equal(first.Lesson.Hand, second.Lesson.Hand);
        Assert.Equal(first.Lesson.DrawPile, second.Lesson.DrawPile);
        Assert.Equal(1, await Scalar<int>("SELECT version FROM produce_sessions WHERE player_id = $1", player));
    }

    [Fact]
    public async Task Operation_RejectedKeepsStateAndStreams()
    {
        var (factory, channel, player) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;
        await StartProduceAsync(player, "req-3");
        var stateBefore = await Scalar<byte[]>("SELECT state FROM produce_sessions WHERE player_id = $1", player);
        var versionBefore = await Scalar<int>("SELECT version FROM produce_sessions WHERE player_id = $1", player);

        await AssertReasonAsync(() => ChooseAsync(player, "req-4", 0, 0, -1, ParamKind.Nothing), "PRODUCE_INVALID_ARGUMENT");

        Assert.Equal(stateBefore, await Scalar<byte[]>("SELECT state FROM produce_sessions WHERE player_id = $1", player));
        Assert.Equal(versionBefore, await Scalar<int>("SELECT version FROM produce_sessions WHERE player_id = $1", player));
        var accepted = await ChooseAsync(player, "req-5", 0, 0, -1, ParamKind.Singing);
        Assert.Equal(ProducePhase.Lesson, accepted.Phase);
    }


    /// <summary>セッションを読んだ直後に別のリクエストが version を進めた状況を作る（読み込みと更新の間の競合）。</summary>
    private sealed class ConflictInjector
    {
        public Func<Task>? AfterFind { get; set; }
    }

    private sealed class ConflictingSessionRepository(IProduceSessionRepository inner, ConflictInjector injector) : IProduceSessionRepository
    {
        public async Task<ProduceSessionRecord?> FindActiveAsync(TxContext tx, Ulid playerId)
        {
            var record = await inner.FindActiveAsync(tx, playerId);
            if (injector.AfterFind is { } hook)
            {
                await hook();
            }

            return record;
        }

        public Task InsertAsync(TxContext tx, Ulid sessionId, Ulid playerId, int appVersion, ProduceSessionData data) =>
            inner.InsertAsync(tx, sessionId, playerId, appVersion, data);

        public Task<bool> TryUpdateAsync(TxContext tx, Ulid sessionId, int expectedVersion, ProduceSessionStatus status, ProduceSessionData data) =>
            inner.TryUpdateAsync(tx, sessionId, expectedVersion, status, data);

        public Task<bool> TrySetStatusAsync(TxContext tx, Ulid sessionId, int expectedVersion, ProduceSessionStatus status) =>
            inner.TrySetStatusAsync(tx, sessionId, expectedVersion, status);

        public Task InsertResultAsync(TxContext tx, ProduceResultRecord result) => inner.InsertResultAsync(tx, result);
    }

    private async Task<(ServerFactory Factory, GrpcChannel Channel, PlayerClient Player, ConflictInjector Injector)> StartConflictableAsync()
    {
        var injector = new ConflictInjector();
        var factory = new ServerFactory(postgres, randomSource: new ScriptedRandomSource(111UL, 222UL), configureServices: services =>
        {
            services.RemoveAll<IProduceSessionRepository>();
            services.AddScoped<IProduceSessionRepository>(sp =>
                new ConflictingSessionRepository(ActivatorUtilities.CreateInstance<ProduceSessionRepository>(sp), injector));
        });
        var channel = factory.CreateGrpcChannel();
        var player = await PlayerClient.CreateAsync(channel, "0123456789abcdef-secret");
        await player.CallAsync(service => service.SignUpAsync(), "req-1");
        await player.CallAsync(service => service.SignInAsync(), "req-2");
        return (factory, channel, player, injector);
    }

    private async Task<(int GachaCurrency, int Gold, int SupportPoints, int Level, int Exp)> PlayerValuesAsync(PlayerClient player) =>
        (await Scalar<int>("SELECT gacha_currency FROM players WHERE player_id = $1", player),
            await Scalar<int>("SELECT gold FROM players WHERE player_id = $1", player),
            await Scalar<int>("SELECT support_points FROM players WHERE player_id = $1", player),
            await Scalar<int>("SELECT level FROM players WHERE player_id = $1", player),
            await Scalar<int>("SELECT exp FROM players WHERE player_id = $1", player));

    private static Func<Task> BumpVersion(PostgresFixture postgres, PlayerClient player) =>
        () => postgres.ExecuteAsync("UPDATE produce_sessions SET version = version + 1 WHERE player_id = $1", player.PlayerId);

    [Fact]
    public async Task Operation_CompletingOperation_OptimisticLockConflict_RollsBackEverything()
    {
        var (factory, channel, player, injector) = await StartConflictableAsync();
        await using var _ = factory;
        using var __ = channel;
        var current = await StartProduceAsync(player, "req-3");
        var driver = new ProduceDriver(player);
        for (var i = 0; current.Phase != ProducePhase.ExamResult; i++)
        {
            Assert.True(i < 3000, "ExamResult に届かなかった");
            current = await driver.StepAsync(current);
        }

        var playerBefore = await PlayerValuesAsync(player);
        var resultsBefore = await Scalar<long>("SELECT count(*) FROM produce_results WHERE player_id = $1", player);
        injector.AfterFind = BumpVersion(postgres, player);

        await AssertReasonAsync(
            () => player.CallAsync<IProduceService, ProduceStateResponse>(
                x => x.DecideExamRetryAsync(new DecideExamRetryRequest(current.OperationNumber, false)), "req-complete"),
            "PRODUCE_STALE_OPERATION");

        Assert.Equal(playerBefore, await PlayerValuesAsync(player));
        Assert.Equal(resultsBefore, await Scalar<long>("SELECT count(*) FROM produce_results WHERE player_id = $1", player));
        Assert.Equal(0, await Scalar<long>(
            "SELECT count(*) FROM game_logs WHERE player_id = $1 AND kind = 'produce' AND body->>'reason' = 'produce_reward'", player));
        Assert.Equal(0, await Scalar<int>("SELECT status FROM produce_sessions WHERE player_id = $1", player));
    }

    [Fact]
    public async Task Abandon_OptimisticLockConflict_RollsBackEverything()
    {
        var (factory, channel, player, injector) = await StartConflictableAsync();
        await using var _ = factory;
        using var __ = channel;
        await StartProduceAsync(player, "req-3");
        var playerBefore = await PlayerValuesAsync(player);
        var resultsBefore = await Scalar<long>("SELECT count(*) FROM produce_results WHERE player_id = $1", player);
        injector.AfterFind = BumpVersion(postgres, player);

        await AssertReasonAsync(
            () => player.CallAsync<IProduceService, ProduceResultResponse>(x => x.AbandonAsync(), "req-abandon"),
            "PRODUCE_STALE_OPERATION");

        Assert.Equal(playerBefore, await PlayerValuesAsync(player));
        Assert.Equal(resultsBefore, await Scalar<long>("SELECT count(*) FROM produce_results WHERE player_id = $1", player));
        Assert.Equal(0, await Scalar<int>("SELECT status FROM produce_sessions WHERE player_id = $1", player));
    }
}
