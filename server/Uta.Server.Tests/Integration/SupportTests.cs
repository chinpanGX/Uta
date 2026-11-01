using System.Text;
using Grpc.Core;
using Grpc.Net.Client;
using Uta.Server.Tests.Integration.Support;
using Uta.Shared.Contracts.Player;
using Uta.Shared.Contracts.Support;
using VolTech.Server.Abstractions;

namespace Uta.Server.Tests.Integration;

[Collection(PostgresCollection.Name)]
public class SupportTests(PostgresFixture postgres)
{
    private const string LevelUpLogSql =
        "SELECT count(*) FROM game_logs WHERE player_id = $1 AND kind = 'support' AND body->>'reason' = 'support_level_up'";

    private const string LimitBreakLogSql =
        "SELECT count(*) FROM game_logs WHERE player_id = $1 AND kind = 'support' AND body->>'reason' = 'support_limit_break'";

    /// <summary>登録・ログイン済みのプレイヤーと、カード 1 の初期の所持ID。</summary>
    private static async Task<(UtaServerFactory Factory, GrpcChannel Channel, PlayerClient Player, string Uid)> StartAsync(
        PostgresFixture postgres)
    {
        var factory = new UtaServerFactory(postgres);
        var channel = factory.CreateGrpcChannel();
        var player = await PlayerClient.CreateAsync(channel);
        await player.CallAsync(service => service.SignUpAsync(), "req-1");
        var signIn = await player.CallAsync(service => service.SignInAsync(), "req-2");
        var uid = signIn.SupportCards.First(x => x.CardId == 1).SupportCardUid;
        return (factory, channel, player, uid);
    }

    private static Task<LevelUpResponse> LevelUpAsync(PlayerClient player, string uid, int targetLevel, string requestId) =>
        player.CallAsync<ISupportService, LevelUpResponse>(x => x.LevelUpAsync(new LevelUpRequest(uid, targetLevel)), requestId);

    private static Task<LimitBreakResponse> LimitBreakAsync(PlayerClient player, string uid, string materialUid, string requestId) =>
        player.CallAsync<ISupportService, LimitBreakResponse>(x => x.LimitBreakAsync(new LimitBreakRequest(uid, materialUid)), requestId);

    private Task<T?> Scalar<T>(string sql, PlayerClient player) => postgres.QueryScalarAsync<T>(sql, player.PlayerId);

    private async Task<string> AddMaterialAsync(PlayerClient player, int cardId)
    {
        var uid = Ulid.NewUlid().ToString();
        await postgres.ExecuteAsync(
            "INSERT INTO player_support_cards (support_card_uid, player_id, card_id) VALUES ($1, $2, $3)", uid, player.PlayerId, cardId);
        return uid;
    }

    private static void AssertReason(RpcException exception, StatusCode code, string reason)
    {
        Assert.Equal(code, exception.StatusCode);
        foreach (var entry in exception.Trailers)
        {
            if (entry.Key == ApiHeaders.ErrorDetailsTrailer && entry.IsBinary)
            {
                Assert.Contains(reason, Encoding.UTF8.GetString(entry.ValueBytes));
                return;
            }
        }

        throw new InvalidOperationException("grpc-status-details-bin が trailers にない");
    }

    [Fact]
    public async Task LevelUp_ToTarget_PaysAndUpdates()
    {
        var (factory, channel, player, uid) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;

        var response = await LevelUpAsync(player, uid, 3, "req-3");

        Assert.Equal(3, response.Level);
        Assert.Equal(10000 - 117, response.SupportPoints);
        Assert.Equal(10000 - 117, response.Gold);
        Assert.Equal(3, await postgres.QueryScalarAsync<int>("SELECT level FROM player_support_cards WHERE support_card_uid = $1", uid));
        Assert.Equal(9883, await Scalar<int>("SELECT support_points FROM players WHERE player_id = $1", player));
    }

    [Fact]
    public async Task LevelUp_ToCap_Succeeds()
    {
        var (factory, channel, player, uid) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;
        await postgres.ExecuteAsync("UPDATE players SET support_points = 100000, gold = 100000 WHERE player_id = $1", player.PlayerId);

        var response = await LevelUpAsync(player, uid, 20, "req-3");

        Assert.Equal(20, response.Level);
    }

    [Fact]
    public async Task LevelUp_AboveCap_Rejected()
    {
        var (factory, channel, player, uid) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;

        var exception = await Assert.ThrowsAsync<RpcException>(() => LevelUpAsync(player, uid, 21, "req-3"));

        AssertReason(exception, StatusCode.FailedPrecondition, "SUPPORT_INVALID_TARGET_LEVEL");
        Assert.Equal(10000, await Scalar<int>("SELECT support_points FROM players WHERE player_id = $1", player));
        Assert.Equal(10000, await Scalar<int>("SELECT gold FROM players WHERE player_id = $1", player));
    }

    [Fact]
    public async Task LevelUp_NotAboveCurrent_Rejected()
    {
        var (factory, channel, player, uid) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;

        var exception = await Assert.ThrowsAsync<RpcException>(() => LevelUpAsync(player, uid, 1, "req-3"));

        AssertReason(exception, StatusCode.FailedPrecondition, "SUPPORT_INVALID_TARGET_LEVEL");
    }

    [Fact]
    public async Task LevelUp_NotOwned_Rejected()
    {
        var (factory, channel, player, _) = await StartAsync(postgres);
        await using var __ = factory;
        using var ___ = channel;
        var other = await PlayerClient.CreateAsync(channel, "fedcba9876543210-secret2");
        await other.CallAsync(service => service.SignUpAsync(), "req-1");
        var otherSignIn = await other.CallAsync(service => service.SignInAsync(), "req-2");
        var otherUid = otherSignIn.SupportCards.First(x => x.CardId == 1).SupportCardUid;

        var requestNumber = 3;
        foreach (var target in new[] { otherUid, Ulid.NewUlid().ToString(), "not-a-ulid" })
        {
            var exception = await Assert.ThrowsAsync<RpcException>(() => LevelUpAsync(player, target, 2, "req-" + requestNumber++));
            AssertReason(exception, StatusCode.FailedPrecondition, "SUPPORT_NOT_OWNED");
        }
    }

    [Fact]
    public async Task LevelUp_NotEnoughPoints_Rejected()
    {
        var (factory, channel, player, uid) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;
        await postgres.ExecuteAsync("UPDATE players SET support_points = 0 WHERE player_id = $1", player.PlayerId);

        var exception = await Assert.ThrowsAsync<RpcException>(() => LevelUpAsync(player, uid, 2, "req-3"));

        AssertReason(exception, StatusCode.FailedPrecondition, "SUPPORT_NOT_ENOUGH_POINTS");
        Assert.Equal(10000, await Scalar<int>("SELECT gold FROM players WHERE player_id = $1", player));
    }

    [Fact]
    public async Task LevelUp_NotEnoughGold_Rejected()
    {
        var (factory, channel, player, uid) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;
        await postgres.ExecuteAsync("UPDATE players SET gold = 0 WHERE player_id = $1", player.PlayerId);

        var exception = await Assert.ThrowsAsync<RpcException>(() => LevelUpAsync(player, uid, 2, "req-3"));

        AssertReason(exception, StatusCode.FailedPrecondition, "PLAYER_NOT_ENOUGH_GOLD");
        Assert.Equal(10000, await Scalar<int>("SELECT support_points FROM players WHERE player_id = $1", player));
    }

    [Fact]
    public async Task LevelUp_BothShort_ReportsPointsFirst()
    {
        var (factory, channel, player, uid) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;
        await postgres.ExecuteAsync("UPDATE players SET support_points = 0, gold = 0 WHERE player_id = $1", player.PlayerId);

        var exception = await Assert.ThrowsAsync<RpcException>(() => LevelUpAsync(player, uid, 2, "req-3"));

        AssertReason(exception, StatusCode.FailedPrecondition, "SUPPORT_NOT_ENOUGH_POINTS");
    }

    [Fact]
    public async Task LevelUp_SameRequestId_ReturnsStoredResponse()
    {
        var (factory, channel, player, uid) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;

        var first = await LevelUpAsync(player, uid, 3, "req-3");
        var second = await LevelUpAsync(player, uid, 3, "req-3");

        Assert.Equal(3, first.Level);
        Assert.Equal(3, second.Level);
        Assert.Equal(9883, second.SupportPoints);
        Assert.Equal(9883, await Scalar<int>("SELECT support_points FROM players WHERE player_id = $1", player));
    }

    [Fact]
    public async Task LevelUp_WritesOneGameLog()
    {
        var (factory, channel, player, uid) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;

        await LevelUpAsync(player, uid, 3, "req-3");
        Assert.Equal(1L, await Scalar<long>(LevelUpLogSql, player));

        await Assert.ThrowsAsync<RpcException>(() => LevelUpAsync(player, uid, 21, "req-4"));
        Assert.Equal(1L, await Scalar<long>(LevelUpLogSql, player));
    }

    [Fact]
    public async Task LevelUp_BeforeSignUp_ReturnsPlayerNotCreated()
    {
        await using var factory = new UtaServerFactory(postgres);
        using var channel = factory.CreateGrpcChannel();
        var player = await PlayerClient.CreateAsync(channel);

        var exception = await Assert.ThrowsAsync<RpcException>(() => LevelUpAsync(player, Ulid.NewUlid().ToString(), 2, "req-1"));

        AssertReason(exception, StatusCode.FailedPrecondition, "PLAYER_NOT_CREATED");
    }

    [Fact]
    public async Task LimitBreak_WithMaterial_Unlocks()
    {
        var (factory, channel, player, uid) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;
        var material = await AddMaterialAsync(player, 1);

        var response = await LimitBreakAsync(player, uid, material, "req-3");

        Assert.Equal(1, response.LimitBreak);
        Assert.Equal(25, response.MaxLevel);
        Assert.Equal(0L, await postgres.QueryScalarAsync<long>("SELECT count(*) FROM player_support_cards WHERE support_card_uid = $1", material));
        Assert.Equal(1, await postgres.QueryScalarAsync<int>("SELECT limit_break FROM player_support_cards WHERE support_card_uid = $1", uid));
    }

    [Fact]
    public async Task LimitBreak_ConsumesLevelledMaterialWithoutRefund()
    {
        var (factory, channel, player, uid) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;
        var material = await AddMaterialAsync(player, 1);
        await postgres.ExecuteAsync("UPDATE player_support_cards SET level = 5 WHERE support_card_uid = $1", material);

        await LimitBreakAsync(player, uid, material, "req-3");

        Assert.Equal(10000, await Scalar<int>("SELECT support_points FROM players WHERE player_id = $1", player));
        Assert.Equal(10000, await Scalar<int>("SELECT gold FROM players WHERE player_id = $1", player));
    }

    [Fact]
    public async Task LimitBreak_WritesOneGameLog()
    {
        var (factory, channel, player, uid) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;
        var material = await AddMaterialAsync(player, 1);

        await LimitBreakAsync(player, uid, material, "req-3");

        Assert.Equal(1L, await Scalar<long>(LimitBreakLogSql, player));
    }

    [Fact]
    public async Task LimitBreak_SameUid_RejectedAsMaterial()
    {
        var (factory, channel, player, uid) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;

        var exception = await Assert.ThrowsAsync<RpcException>(() => LimitBreakAsync(player, uid, uid, "req-3"));

        AssertReason(exception, StatusCode.FailedPrecondition, "SUPPORT_INVALID_MATERIAL");
    }

    [Fact]
    public async Task LimitBreak_DifferentCardMaterial_Rejected()
    {
        var (factory, channel, player, uid) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;
        var signIn = await player.CallAsync(service => service.SignInAsync(), "req-x");
        var other = signIn.SupportCards.First(x => x.CardId == 2).SupportCardUid;

        var exception = await Assert.ThrowsAsync<RpcException>(() => LimitBreakAsync(player, uid, other, "req-3"));

        AssertReason(exception, StatusCode.FailedPrecondition, "SUPPORT_INVALID_MATERIAL");
        Assert.Equal(1L, await postgres.QueryScalarAsync<long>("SELECT count(*) FROM player_support_cards WHERE support_card_uid = $1", other));
    }

    [Fact]
    public async Task LimitBreak_NotOwnedMaterial_Rejected()
    {
        var (factory, channel, player, uid) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;
        var other = await PlayerClient.CreateAsync(channel, "fedcba9876543210-secret2");
        await other.CallAsync(service => service.SignUpAsync(), "req-1");
        var otherSignIn = await other.CallAsync(service => service.SignInAsync(), "req-2");
        var otherUid = otherSignIn.SupportCards.First(x => x.CardId == 1).SupportCardUid;

        var exception1 = await Assert.ThrowsAsync<RpcException>(() => LimitBreakAsync(player, uid, otherUid, "req-3"));
        var exception2 = await Assert.ThrowsAsync<RpcException>(() => LimitBreakAsync(player, uid, Ulid.NewUlid().ToString(), "req-4"));

        AssertReason(exception1, StatusCode.FailedPrecondition, "SUPPORT_INVALID_MATERIAL");
        AssertReason(exception2, StatusCode.FailedPrecondition, "SUPPORT_INVALID_MATERIAL");
        Assert.Equal(1L, await postgres.QueryScalarAsync<long>("SELECT count(*) FROM player_support_cards WHERE support_card_uid = $1", otherUid));
    }

    [Fact]
    public async Task LimitBreak_NotOwnedTarget_Rejected()
    {
        var (factory, channel, player, _) = await StartAsync(postgres);
        await using var __ = factory;
        using var ___ = channel;
        var material = await AddMaterialAsync(player, 1);

        var exception = await Assert.ThrowsAsync<RpcException>(() => LimitBreakAsync(player, Ulid.NewUlid().ToString(), material, "req-3"));

        AssertReason(exception, StatusCode.FailedPrecondition, "SUPPORT_NOT_OWNED");
    }

    [Fact]
    public async Task LimitBreak_AtMax_RejectedAndMaterialKept()
    {
        var (factory, channel, player, uid) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;
        await postgres.ExecuteAsync("UPDATE player_support_cards SET limit_break = 4 WHERE support_card_uid = $1", uid);
        var material = await AddMaterialAsync(player, 1);

        var exception = await Assert.ThrowsAsync<RpcException>(() => LimitBreakAsync(player, uid, material, "req-3"));

        AssertReason(exception, StatusCode.FailedPrecondition, "SUPPORT_MAX_LIMIT_BREAK");
        Assert.Equal(1L, await postgres.QueryScalarAsync<long>("SELECT count(*) FROM player_support_cards WHERE support_card_uid = $1", material));
    }

    [Fact]
    public async Task LimitBreak_FullFlow_ReachesMaxLevel()
    {
        var (factory, channel, player, uid) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;
        await postgres.ExecuteAsync("UPDATE players SET support_points = 100000, gold = 100000 WHERE player_id = $1", player.PlayerId);

        LimitBreakResponse? last = null;
        for (var i = 0; i < 4; i++)
        {
            var material = await AddMaterialAsync(player, 1);
            last = await LimitBreakAsync(player, uid, material, "req-" + (3 + i));
        }

        Assert.Equal(4, last!.LimitBreak);
        Assert.Equal(40, last.MaxLevel);
        var response = await LevelUpAsync(player, uid, 40, "req-7");
        Assert.Equal(40, response.Level);
    }

    [Fact]
    public async Task LimitBreak_ConcurrentSameMaterial_OneSucceeds()
    {
        var (factory, channel, player, uid) = await StartAsync(postgres);
        await using var _ = factory;
        using var __ = channel;
        var material = await AddMaterialAsync(player, 1);

        async Task<RpcException?> TryAsync(string requestId)
        {
            try
            {
                await LimitBreakAsync(player, uid, material, requestId);
                return null;
            }
            catch (RpcException exception)
            {
                return exception;
            }
        }

        var results = await Task.WhenAll(TryAsync("req-a"), TryAsync("req-b"));

        Assert.Single(results, x => x is null);
        var failure = Assert.Single(results, x => x is not null);
        AssertReason(failure!, StatusCode.FailedPrecondition, "SUPPORT_INVALID_MATERIAL");
        Assert.Equal(1, await postgres.QueryScalarAsync<int>("SELECT limit_break FROM player_support_cards WHERE support_card_uid = $1", uid));
    }
}
