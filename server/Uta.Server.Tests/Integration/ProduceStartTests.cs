using Grpc.Core;
using Grpc.Net.Client;
using Uta.Server.Tests.Integration.Support;
using Uta.Shared.Contracts.Player;
using Uta.Shared.Contracts.Produce;
using Uta.Shared.Contracts.Support;
using Uta.Shared.Domain.Produce;

namespace Uta.Server.Tests.Integration;

[Collection(PostgresCollection.Name)]
public class ProduceStartTests(PostgresFixture postgres)
{
    /// <summary>登録・ログイン済みのプレイヤー。initial の所持IDは SignIn の SupportCards（カード 1〜5）。</summary>
    private static async Task<(ServerFactory Factory, GrpcChannel Channel, PlayerClient Player, SignInResponse SignIn)> StartAsync(
        PostgresFixture postgres, string secret = "0123456789abcdef-secret")
    {
        var factory = new ServerFactory(postgres, randomSource: new ScriptedRandomSource(111UL, 222UL, 333UL, 444UL));
        var channel = factory.CreateGrpcChannel();
        var player = await PlayerClient.CreateAsync(channel, secret);
        await player.CallAsync(service => service.SignUpAsync(), "req-1");
        var signIn = await player.CallAsync(service => service.SignInAsync(), "req-2");
        return (factory, channel, player, signIn);
    }

    private static Task<ProduceStateResponse> StartProduceAsync(
        PlayerClient player, string requestId, int idolId = 1, string[]? uids = null, int rentalCandidateId = 0, int scenarioId = 1) =>
        player.CallAsync<IProduceService, ProduceStateResponse>(
            x => x.StartAsync(new StartProduceRequest(idolId, uids ?? [], rentalCandidateId, scenarioId)), requestId);

    private async Task<string> AddCardAsync(PlayerClient player, int cardId)
    {
        var uid = Ulid.NewUlid().ToString();
        await postgres.ExecuteAsync(
            "INSERT INTO player_support_cards (support_card_uid, player_id, card_id) VALUES ($1, $2, $3)", uid, player.PlayerId, cardId);
        return uid;
    }

    private Task<T?> Scalar<T>(string sql, PlayerClient player) => postgres.QueryScalarAsync<T>(sql, player.PlayerId);

    [Fact]
    public async Task Start_InitialHoldings_ReturnsWeekSelect()
    {
        var (factory, channel, player, _) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;

        var response = await StartProduceAsync(player, "req-3");

        Assert.Equal(ProducePhase.WeekSelect, response.Phase);
        Assert.Equal(1, response.Week);
        Assert.Equal(0, response.OperationNumber);
        Assert.Equal(1, response.ScenarioMasterId);
        Assert.Equal(1, response.IdolMasterId);
        Assert.Equal(30, response.Hp);
        Assert.Equal(30, response.MaxHp);
        Assert.Equal(30, response.Singing);
        Assert.Equal(20, response.Rhythm);
        Assert.Equal(10, response.Expression);
        Assert.Equal(2, response.WeekActions.Length);
        Assert.Equal(0, response.WeekActions[0].SlotIndex);
        Assert.Equal(WeekChoiceKind.Lesson, response.WeekActions[0].Kind);
        Assert.True(response.WeekActions[0].Selectable);
        Assert.Equal(1, response.WeekActions[1].SlotIndex);
        Assert.False(response.WeekActions[1].Selectable);
        Assert.NotEmpty(response.Deck);
        Assert.Equal(response.Deck.Select(x => x.InstanceId).Order().ToList(), response.Deck.Select(x => x.InstanceId).ToList());

        Assert.Equal(1, await Scalar<long>("SELECT count(*) FROM produce_sessions WHERE player_id = $1", player));
        Assert.Equal(0, await Scalar<int>("SELECT status FROM produce_sessions WHERE player_id = $1", player));
        Assert.Equal(1, await Scalar<int>("SELECT app_version FROM produce_sessions WHERE player_id = $1", player));
        Assert.Equal(1, await Scalar<int>("SELECT state_version FROM produce_sessions WHERE player_id = $1", player));
        Assert.Equal(1, await Scalar<int>("SELECT rule_version FROM produce_sessions WHERE player_id = $1", player));
        Assert.Equal(0, await Scalar<int>("SELECT version FROM produce_sessions WHERE player_id = $1", player));
        Assert.Equal(1, await Scalar<int>("SELECT week FROM produce_sessions WHERE player_id = $1", player));
    }

    [Fact]
    public async Task Start_SameRequestId_ReturnsSavedResponse()
    {
        var (factory, channel, player, _) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;

        var first = await StartProduceAsync(player, "req-3");
        var second = await StartProduceAsync(player, "req-3");

        Assert.Equal(first.Phase, second.Phase);
        Assert.Equal(first.Deck.Length, second.Deck.Length);
        Assert.Equal(first.Singing, second.Singing);
        Assert.Equal(1, await Scalar<long>("SELECT count(*) FROM produce_sessions WHERE player_id = $1", player));
    }

    [Fact]
    public async Task Start_WhileInProgress_AlreadyInProgress()
    {
        var (factory, channel, player, _) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;
        await StartProduceAsync(player, "req-3");

        var exception = await Assert.ThrowsAsync<RpcException>(() => StartProduceAsync(player, "req-4"));

        RpcAssert.Reason(exception, StatusCode.FailedPrecondition, "PRODUCE_ALREADY_IN_PROGRESS");
    }

    private async Task AssertInvalidFormationAsync(PlayerClient player, int idolId, string[] uids)
    {
        var exception = await Assert.ThrowsAsync<RpcException>(() => StartProduceAsync(player, "req-bad", idolId, uids));

        RpcAssert.Reason(exception, StatusCode.FailedPrecondition, "PRODUCE_INVALID_FORMATION");
        Assert.Equal(0, await Scalar<long>("SELECT count(*) FROM produce_sessions WHERE player_id = $1", player));
    }

    [Fact]
    public async Task Start_UnownedIdol_Rejected()
    {
        var (factory, channel, player, _) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;

        await AssertInvalidFormationAsync(player, 3, []);
    }

    [Fact]
    public async Task Start_SixSupportCards_Rejected()
    {
        var (factory, channel, player, signIn) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;
        var uids = signIn.SupportCards.Select(x => x.UniqueId).Append(Ulid.NewUlid().ToString()).ToArray();

        await AssertInvalidFormationAsync(player, 1, uids);
    }

    [Fact]
    public async Task Start_MalformedUid_Rejected()
    {
        var (factory, channel, player, _) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;

        await AssertInvalidFormationAsync(player, 1, ["x"]);
    }

    [Fact]
    public async Task Start_OtherPlayersUid_Rejected()
    {
        var (factory, channel, player, _) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;
        var other = await PlayerClient.CreateAsync(channel, "another-secret-0123456789");
        await other.CallAsync(service => service.SignUpAsync(), "req-1");
        var otherSignIn = await other.CallAsync(service => service.SignInAsync(), "req-2");

        await AssertInvalidFormationAsync(player, 1, [otherSignIn.SupportCards[0].UniqueId]);
    }

    [Fact]
    public async Task Start_DuplicateUid_Rejected()
    {
        var (factory, channel, player, signIn) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;
        var uid = signIn.SupportCards[0].UniqueId;

        await AssertInvalidFormationAsync(player, 1, [uid, uid]);
    }

    [Fact]
    public async Task Start_SameCardIdDifferentUid_Rejected()
    {
        var (factory, channel, player, signIn) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;
        var uid = signIn.SupportCards.First(x => x.MasterId == 1).UniqueId;
        var other = await AddCardAsync(player, 1);

        await AssertInvalidFormationAsync(player, 1, [uid, other]);
    }

    [Fact]
    public async Task Start_PlanMismatch_Rejected()
    {
        var (factory, channel, player, _) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;
        // アイドル 1 は感性。カード 7（理論）は並べられない
        var logic = await AddCardAsync(player, 7);

        await AssertInvalidFormationAsync(player, 1, [logic]);
    }

    [Fact]
    public async Task Start_RentalNotZero_InvalidArgument()
    {
        var (factory, channel, player, _) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;

        var exception = await Assert.ThrowsAsync<RpcException>(() => StartProduceAsync(player, "req-3", rentalCandidateId: 1));

        RpcAssert.Reason(exception, StatusCode.FailedPrecondition, "PRODUCE_INVALID_ARGUMENT");
    }

    [Fact]
    public async Task Start_RentalNotZeroAndInvalidFormation_InvalidFormation()
    {
        var (factory, channel, player, _) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;

        var exception = await Assert.ThrowsAsync<RpcException>(() => StartProduceAsync(player, "req-3", idolId: 3, rentalCandidateId: 1));

        RpcAssert.Reason(exception, StatusCode.FailedPrecondition, "PRODUCE_INVALID_FORMATION");
    }

    [Fact]
    public async Task Start_UnknownScenario_InvalidArgument()
    {
        var (factory, channel, player, _) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;

        var exception = await Assert.ThrowsAsync<RpcException>(() => StartProduceAsync(player, "req-3", scenarioId: 99));

        RpcAssert.Reason(exception, StatusCode.FailedPrecondition, "PRODUCE_INVALID_ARGUMENT");
    }

    [Fact]
    public async Task Start_ReflectsSupportCardLevel()
    {
        var (factory, channel, player, signIn) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;
        var uid = signIn.SupportCards.First(x => x.MasterId == 1).UniqueId;

        var first = await StartProduceAsync(player, "req-3", uids: [uid]);
        Assert.Equal(30 + ProduceFormulas.AbilityValue(3, 20, 1, 40), first.Singing);
        await player.CallAsync<IProduceService, ProduceResultResponse>(x => x.AbandonAsync(), "req-4");
        await postgres.ExecuteAsync("UPDATE players SET support_points = 100000, gold = 100000 WHERE player_id = $1", player.PlayerId);
        await player.CallAsync<ISupportService, LevelUpResponse>(x => x.LevelUpAsync(new LevelUpRequest(uid, 20)), "req-5");
        var second = await StartProduceAsync(player, "req-6", uids: [uid]);

        Assert.Equal(30 + ProduceFormulas.AbilityValue(3, 20, 20, 40), second.Singing);
        Assert.True(second.Singing > first.Singing);
    }

    private static Task<ProduceStateResponse> GetStateAsync(PlayerClient player, string requestId = "req-state") =>
        player.CallAsync<IProduceService, ProduceStateResponse>(x => x.GetStateAsync(), requestId);

    private static void AssertNothing(ProduceStateResponse response)
    {
        Assert.Equal(ProducePhase.Nothing, response.Phase);
        Assert.Empty(response.Deck);
        Assert.Empty(response.WeekActions);
        Assert.Null(response.Lesson);
        Assert.Null(response.ExamResult);
        Assert.Null(response.Result);
    }

    [Fact]
    public async Task GetState_NoSession_ReturnsNothing()
    {
        var (factory, channel, player, _) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;

        AssertNothing(await GetStateAsync(player));
    }

    [Fact]
    public async Task GetState_AfterStart_MatchesStartResponse()
    {
        var (factory, channel, player, _) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;
        var started = await StartProduceAsync(player, "req-3");

        var state = await GetStateAsync(player);

        Assert.Equal(started.Phase, state.Phase);
        Assert.Equal(started.Week, state.Week);
        Assert.Equal(started.OperationNumber, state.OperationNumber);
        Assert.Equal(started.Hp, state.Hp);
        Assert.Equal(started.Singing, state.Singing);
        Assert.Equal(started.Deck.Length, state.Deck.Length);
        Assert.Equal(started.WeekActions.Length, state.WeekActions.Length);
    }

    [Fact]
    public async Task GetState_StaleRuleVersion_ReturnsNothingWithoutWriting()
    {
        var (factory, channel, player, _) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;
        await StartProduceAsync(player, "req-3");
        await postgres.ExecuteAsync("UPDATE produce_sessions SET rule_version = 0 WHERE player_id = $1", player.PlayerId);

        AssertNothing(await GetStateAsync(player));

        Assert.Equal(0, await Scalar<int>("SELECT status FROM produce_sessions WHERE player_id = $1", player));
    }

    [Fact]
    public async Task GetState_StaleStateVersion_ReturnsNothingWithoutWriting()
    {
        var (factory, channel, player, _) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;
        await StartProduceAsync(player, "req-3");
        await postgres.ExecuteAsync("UPDATE produce_sessions SET state_version = 0, state = '\\x00'::bytea WHERE player_id = $1", player.PlayerId);

        AssertNothing(await GetStateAsync(player));

        Assert.Equal(0, await Scalar<int>("SELECT status FROM produce_sessions WHERE player_id = $1", player));
    }

    [Fact]
    public async Task GetState_CorruptState_ReturnsNothing()
    {
        var (factory, channel, player, _) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;
        await StartProduceAsync(player, "req-3");
        await postgres.ExecuteAsync("UPDATE produce_sessions SET state = '\\x00'::bytea WHERE player_id = $1", player.PlayerId);

        AssertNothing(await GetStateAsync(player));

        Assert.Equal(0, await Scalar<int>("SELECT status FROM produce_sessions WHERE player_id = $1", player));
    }

    [Fact]
    public async Task GetState_DifferentAppVersion_ReturnsNothing()
    {
        var (factory, channel, player, _) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;
        await StartProduceAsync(player, "req-3");

        AssertNothing(await GetStateAsync(player.WithAppVersion("2")));

        var exception = await Assert.ThrowsAsync<RpcException>(() => GetStateAsync(player.WithAppVersion(null)));
        RpcAssert.Reason(exception, StatusCode.FailedPrecondition, "PRODUCE_INVALID_ARGUMENT");
    }

    [Fact]
    public async Task Abandon_InProgress_RecordsResult()
    {
        var (factory, channel, player, _) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;
        await StartProduceAsync(player, "req-3");
        var gachaCurrency = await Scalar<int>("SELECT gacha_currency FROM players WHERE player_id = $1", player);

        var result = await player.CallAsync<IProduceService, ProduceResultResponse>(x => x.AbandonAsync(), "req-4");

        Assert.Equal(ProduceEnding.Abandoned, result.Ending);
        Assert.Equal(1, result.PlayerLevel);
        Assert.Equal(2, await Scalar<int>("SELECT status FROM produce_sessions WHERE player_id = $1", player));
        Assert.Equal(1, await Scalar<long>("SELECT count(*) FROM produce_results WHERE player_id = $1", player));
        Assert.Equal(2, await Scalar<int>("SELECT ending FROM produce_results WHERE player_id = $1", player));
        Assert.Equal(0, await Scalar<int>("SELECT produce_rank FROM produce_results WHERE player_id = $1", player));
        Assert.Equal(0, await Scalar<int>("SELECT gacha_currency + gold + support_points + exp FROM produce_results WHERE player_id = $1", player));
        Assert.Equal(gachaCurrency, await Scalar<int>("SELECT gacha_currency FROM players WHERE player_id = $1", player));
        AssertNothing(await GetStateAsync(player));
        Assert.Equal(ProducePhase.WeekSelect, (await StartProduceAsync(player, "req-5")).Phase);
    }

    [Fact]
    public async Task Abandon_NoSession_NotInProgress()
    {
        var (factory, channel, player, _) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;

        var exception = await Assert.ThrowsAsync<RpcException>(
            () => player.CallAsync<IProduceService, ProduceResultResponse>(x => x.AbandonAsync(), "req-3"));

        RpcAssert.Reason(exception, StatusCode.FailedPrecondition, "PRODUCE_NOT_IN_PROGRESS");
    }

    [Fact]
    public async Task Abandon_DifferentAppVersion_SessionDiscardedWithoutWriting()
    {
        var (factory, channel, player, _) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;
        await StartProduceAsync(player, "req-3");

        var exception = await Assert.ThrowsAsync<RpcException>(
            () => player.WithAppVersion("2").CallAsync<IProduceService, ProduceResultResponse>(x => x.AbandonAsync(), "req-4"));

        RpcAssert.Reason(exception, StatusCode.FailedPrecondition, "PRODUCE_SESSION_DISCARDED");
        Assert.Equal(0, await Scalar<int>("SELECT status FROM produce_sessions WHERE player_id = $1", player));
        Assert.Equal(0, await Scalar<long>("SELECT count(*) FROM produce_results WHERE player_id = $1", player));
    }

    [Fact]
    public async Task Start_DifferentAppVersionSessionRemains_DiscardsOldAndStarts()
    {
        var (factory, channel, player, _) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;
        await StartProduceAsync(player, "req-3");

        var response = await StartProduceAsync(player.WithAppVersion("2"), "req-4");

        Assert.Equal(ProducePhase.WeekSelect, response.Phase);
        Assert.Equal(1, await Scalar<long>("SELECT count(*) FROM produce_sessions WHERE player_id = $1 AND status = 3", player));
        Assert.Equal(1, await Scalar<long>("SELECT count(*) FROM produce_sessions WHERE player_id = $1 AND status = 0 AND app_version = 2", player));
        Assert.Equal(1, await Scalar<long>("SELECT count(*) FROM produce_results WHERE player_id = $1 AND ending = 3", player));
    }

    private static Task<SignInResponse> SignInAsync(PlayerClient player, string requestId) =>
        player.CallAsync(service => service.SignInAsync(), requestId);

    [Fact]
    public async Task SignIn_NoSession_NoProduceFlags()
    {
        var (factory, channel, player, signIn) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;

        Assert.False(signIn.HasActiveProduce);
        Assert.False(signIn.DiscardedProduce);
    }

    [Fact]
    public async Task SignIn_WithoutAppVersionHeader_NoSession_Succeeds()
    {
        var (factory, channel, player, _) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;

        var signIn = await SignInAsync(player.WithAppVersion(null), "req-3");

        Assert.False(signIn.HasActiveProduce);
        Assert.False(signIn.DiscardedProduce);
    }

    [Fact]
    public async Task SignIn_ActiveSession_HasActiveProduce()
    {
        var (factory, channel, player, _) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;
        await StartProduceAsync(player, "req-3");

        var signIn = await SignInAsync(player, "req-4");

        Assert.True(signIn.HasActiveProduce);
        Assert.False(signIn.DiscardedProduce);
    }

    [Fact]
    public async Task SignIn_DifferentAppVersion_DiscardsSession()
    {
        var (factory, channel, player, _) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;
        await StartProduceAsync(player, "req-3");

        var signIn = await SignInAsync(player.WithAppVersion("2"), "req-4");

        Assert.False(signIn.HasActiveProduce);
        Assert.True(signIn.DiscardedProduce);
        Assert.Equal(3, await Scalar<int>("SELECT status FROM produce_sessions WHERE player_id = $1", player));
        Assert.Equal(1, await Scalar<long>("SELECT count(*) FROM produce_results WHERE player_id = $1 AND ending = 3", player));
    }

    [Fact]
    public async Task SignIn_DifferentRuleVersion_DiscardsSession()
    {
        var (factory, channel, player, _) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;
        await StartProduceAsync(player, "req-3");
        await postgres.ExecuteAsync("UPDATE produce_sessions SET rule_version = 0 WHERE player_id = $1", player.PlayerId);

        var signIn = await SignInAsync(player, "req-4");

        Assert.False(signIn.HasActiveProduce);
        Assert.True(signIn.DiscardedProduce);
        Assert.Equal(3, await Scalar<int>("SELECT status FROM produce_sessions WHERE player_id = $1", player));
    }

    [Fact]
    public async Task SignIn_DifferentStateVersion_DiscardsSession()
    {
        var (factory, channel, player, _) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;
        await StartProduceAsync(player, "req-3");
        await postgres.ExecuteAsync("UPDATE produce_sessions SET state_version = 0, state = '\\x00'::bytea WHERE player_id = $1", player.PlayerId);

        var signIn = await SignInAsync(player, "req-4");

        Assert.False(signIn.HasActiveProduce);
        Assert.True(signIn.DiscardedProduce);
        Assert.Equal(3, await Scalar<int>("SELECT status FROM produce_sessions WHERE player_id = $1", player));
    }

    [Fact]
    public async Task GetState_CorruptState_ReturnsNothing_AndSignInDiscards()
    {
        var (factory, channel, player, _) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;
        await StartProduceAsync(player, "req-3");
        await postgres.ExecuteAsync("UPDATE produce_sessions SET state = '\\x00'::bytea WHERE player_id = $1", player.PlayerId);

        AssertNothing(await GetStateAsync(player));
        var signIn = await SignInAsync(player, "req-4");

        Assert.False(signIn.HasActiveProduce);
        Assert.True(signIn.DiscardedProduce);
    }

    [Fact]
    public async Task SignIn_ActiveSession_WithoutAppVersionHeader_InvalidArgument()
    {
        var (factory, channel, player, _) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;
        await StartProduceAsync(player, "req-3");

        var exception = await Assert.ThrowsAsync<RpcException>(() => SignInAsync(player.WithAppVersion(null), "req-4"));

        RpcAssert.Reason(exception, StatusCode.FailedPrecondition, "PRODUCE_INVALID_ARGUMENT");
        Assert.Equal(0, await Scalar<int>("SELECT status FROM produce_sessions WHERE player_id = $1", player));
    }
}
