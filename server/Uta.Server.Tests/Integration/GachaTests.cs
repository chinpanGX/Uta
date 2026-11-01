using System.Text;
using Grpc.Core;
using Grpc.Net.Client;
using Uta.Server.Tests.Integration.Support;
using Uta.Shared.Contracts.Gacha;
using Uta.Shared.Contracts.Player;
using VolTech.Server.Abstractions;

namespace Uta.Server.Tests.Integration;

[Collection(PostgresCollection.Name)]
public class GachaTests(PostgresFixture postgres)
{
    private static ulong Normal(int r) => (ulong)(r + 10000);

    private static ulong Guaranteed(int r) => (ulong)(r + 2200);

    private static DrawRequest Single(int gachaId = 1, int expectedCost = 250) => new DrawRequest(gachaId, 1, expectedCost);

    private static async Task<(UtaServerFactory Factory, GrpcChannel Channel, PlayerClient Player)> StartAsync(
        PostgresFixture postgres, params ulong[]? values)
    {
        var factory = values is null ? new UtaServerFactory(postgres) : new UtaServerFactory(postgres, randomSource: new ScriptedRandomSource(values));
        var channel = factory.CreateGrpcChannel();
        var player = await PlayerClient.CreateAsync(channel);
        await player.CallAsync(service => service.SignUpAsync(), "req-1");
        return (factory, channel, player);
    }

    private static Task<DrawResponse> DrawAsync(PlayerClient player, DrawRequest request, string requestId) =>
        player.CallAsync<IGachaService, DrawResponse>(x => x.DrawAsync(request), requestId);

    private static Task<ExchangeResponse> ExchangeAsync(PlayerClient player, ExchangeRequest request, string requestId) =>
        player.CallAsync<IGachaService, ExchangeResponse>(x => x.ExchangeAsync(request), requestId);

    private Task<T?> Scalar<T>(string sql, PlayerClient player) => postgres.QueryScalarAsync<T>(sql, player.PlayerId);

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
    public async Task Draw_Single_PaysAndGrantsNewCard()
    {
        var (factory, channel, player) = await StartAsync(postgres, Normal(4000));
        await using var _ = factory;
        using var __ = channel;

        var response = await DrawAsync(player, Single(), "req-2");

        var result = Assert.Single(response.Results);
        Assert.Equal(1, result.Slot);
        Assert.Equal(ItemKind.SupportCard, result.ItemKind);
        Assert.Equal(1, result.ItemId);
        Assert.Equal(GachaOutcome.New, result.Outcome);
        Assert.Equal(26, result.SupportCardUid.Length);
        Assert.Equal(2250, response.GachaCurrency);
        Assert.Equal(1, response.ExchangePoints);
        Assert.Equal(2L, await Scalar<long>("SELECT count(*) FROM player_support_cards WHERE player_id = $1 AND card_id = 1", player));
        Assert.Equal(1L, await Scalar<long>("SELECT count(*) FROM gacha_histories WHERE player_id = $1", player));
        Assert.Equal(1L, await Scalar<long>(
            "SELECT count(*) FROM gacha_histories WHERE player_id = $1 AND slot = 1 AND gacha_table_id = 1 AND item_kind = 2 AND item_id = 1 AND outcome = 1 AND request_id = 'req-2'", player));
        Assert.Equal(1, await Scalar<int>("SELECT points FROM gacha_exchange_points WHERE player_id = $1 AND gacha_id = 1", player));
    }

    [Fact]
    public async Task Draw_Single_WritesOneGameLog()
    {
        const string logSql = "SELECT count(*) FROM game_logs WHERE player_id = $1 AND kind = 'gacha' AND body->>'reason' = 'gacha_draw'";
        var (factory, channel, player) = await StartAsync(postgres, Normal(4000));
        await using var _ = factory;
        using var __ = channel;

        await DrawAsync(player, Single(), "req-2");
        Assert.Equal(1L, await Scalar<long>(logSql, player));

        await DrawAsync(player, Single(), "req-2");
        Assert.Equal(1L, await Scalar<long>(logSql, player));

        await Assert.ThrowsAsync<RpcException>(() => DrawAsync(player, new DrawRequest(1, 2, 250), "req-3"));
        Assert.Equal(1L, await Scalar<long>(logSql, player));
    }

    [Fact]
    public async Task Draw_Ten_UsesGuaranteedTableForTenthSlot()
    {
        var rs = new[] { 4000, 4936, 5872, 6808, 7744, 3120, 3120, 4000, 4936 };
        var values = rs.Select(Normal).Append(Guaranteed(680)).ToArray();
        var (factory, channel, player) = await StartAsync(postgres, values);
        await using var _ = factory;
        using var __ = channel;

        var response = await DrawAsync(player, new DrawRequest(1, 10, 2500), "req-2");

        Assert.Equal(10, response.Results.Length);
        Assert.Equal(Enumerable.Range(1, 10).ToArray(), response.Results.Select(x => x.Slot).ToArray());
        for (var i = 0; i < 5; i++)
        {
            var x = response.Results[i];
            Assert.Equal(ItemKind.SupportCard, x.ItemKind);
            Assert.Equal(i + 1, x.ItemId);
            Assert.Equal(GachaOutcome.New, x.Outcome);
        }

        Assert.Equal((ItemKind.Idol, 3, GachaOutcome.New), (response.Results[5].ItemKind, response.Results[5].ItemId, response.Results[5].Outcome));
        var dup = response.Results[6];
        Assert.Equal((ItemKind.Idol, 3, GachaOutcome.Duplicate, 25), (dup.ItemKind, dup.ItemId, dup.Outcome, dup.Shards));
        Assert.Equal((ItemKind.SupportCard, 1, GachaOutcome.New), (response.Results[7].ItemKind, response.Results[7].ItemId, response.Results[7].Outcome));
        Assert.NotEmpty(response.Results[7].SupportCardUid);
        Assert.Equal((ItemKind.SupportCard, 2, GachaOutcome.New), (response.Results[8].ItemKind, response.Results[8].ItemId, response.Results[8].Outcome));
        Assert.NotEmpty(response.Results[8].SupportCardUid);
        Assert.Equal((ItemKind.Idol, 4, GachaOutcome.New), (response.Results[9].ItemKind, response.Results[9].ItemId, response.Results[9].Outcome));
        Assert.Equal(0, response.GachaCurrency);
        Assert.Equal(10, response.ExchangePoints);
        Assert.Equal(10L, await Scalar<long>("SELECT count(*) FROM gacha_histories WHERE player_id = $1", player));
        Assert.Equal(25, await Scalar<int>("SELECT shards FROM player_idols WHERE player_id = $1 AND idol_id = 3", player));
        Assert.Equal(3L, await Scalar<long>("SELECT count(*) FROM player_support_cards WHERE player_id = $1 AND card_id = 1", player));
    }

    [Fact]
    public async Task Draw_DuplicateIdol_ConvertsToShards()
    {
        var (factory, channel, player) = await StartAsync(postgres, Normal(0));
        await using var _ = factory;
        using var __ = channel;

        var response = await DrawAsync(player, Single(), "req-2");

        var result = Assert.Single(response.Results);
        Assert.Equal(GachaOutcome.Duplicate, result.Outcome);
        Assert.Equal(5, result.Shards);
        Assert.Equal("", result.SupportCardUid);
        Assert.Equal(5, await Scalar<int>("SELECT shards FROM player_idols WHERE player_id = $1 AND idol_id = 1", player));
    }

    [Fact]
    public async Task Draw_MaxedSupportCard_ConvertsToSupportPoints()
    {
        var (factory, channel, player) = await StartAsync(postgres, Normal(4000));
        await using var _ = factory;
        using var __ = channel;
        await postgres.ExecuteAsync("UPDATE player_support_cards SET limit_break = 4 WHERE player_id = $1 AND card_id = 1", player.PlayerId);

        var response = await DrawAsync(player, Single(), "req-2");

        var result = Assert.Single(response.Results);
        Assert.Equal(GachaOutcome.Duplicate, result.Outcome);
        Assert.Equal(300, result.SupportPoints);
        Assert.Equal(1L, await Scalar<long>("SELECT count(*) FROM player_support_cards WHERE player_id = $1 AND card_id = 1", player));
        Assert.Equal(10300, await Scalar<int>("SELECT support_points FROM players WHERE player_id = $1", player));
    }

    [Fact]
    public async Task Draw_InvalidCount_Rejected()
    {
        var (factory, channel, player) = await StartAsync(postgres, Normal(0));
        await using var _ = factory;
        using var __ = channel;

        var exception = await Assert.ThrowsAsync<RpcException>(() => DrawAsync(player, new DrawRequest(1, 2, 250), "req-2"));

        AssertReason(exception, StatusCode.FailedPrecondition, "GACHA_INVALID_COUNT");
        Assert.Equal(2500, await Scalar<int>("SELECT gacha_currency FROM players WHERE player_id = $1", player));
    }

    [Fact]
    public async Task Draw_UnknownGacha_Rejected()
    {
        var (factory, channel, player) = await StartAsync(postgres, Normal(0));
        await using var _ = factory;
        using var __ = channel;

        var exception = await Assert.ThrowsAsync<RpcException>(() => DrawAsync(player, Single(gachaId: 99), "req-2"));

        AssertReason(exception, StatusCode.FailedPrecondition, "GACHA_NOT_FOUND");
    }

    [Fact]
    public async Task Draw_CostMismatch_Rejected()
    {
        var (factory, channel, player) = await StartAsync(postgres, Normal(0));
        await using var _ = factory;
        using var __ = channel;

        var exception = await Assert.ThrowsAsync<RpcException>(() => DrawAsync(player, Single(expectedCost: 100), "req-2"));

        AssertReason(exception, StatusCode.FailedPrecondition, "GACHA_COST_MISMATCH");
        Assert.Equal(2500, await Scalar<int>("SELECT gacha_currency FROM players WHERE player_id = $1", player));
    }

    [Fact]
    public async Task Draw_NotEnoughCurrency_Rejected()
    {
        var (factory, channel, player) = await StartAsync(postgres, Normal(0));
        await using var _ = factory;
        using var __ = channel;
        await postgres.ExecuteAsync("UPDATE players SET gacha_currency = 249 WHERE player_id = $1", player.PlayerId);

        var exception = await Assert.ThrowsAsync<RpcException>(() => DrawAsync(player, Single(), "req-2"));

        AssertReason(exception, StatusCode.FailedPrecondition, "GACHA_NOT_ENOUGH_CURRENCY");
        Assert.Equal(249, await Scalar<int>("SELECT gacha_currency FROM players WHERE player_id = $1", player));
        Assert.Equal(0L, await Scalar<long>("SELECT count(*) FROM gacha_histories WHERE player_id = $1", player));
    }

    [Fact]
    public async Task Draw_SameRequestId_ReturnsStoredResponse()
    {
        var (factory, channel, player) = await StartAsync(postgres, Normal(4000));
        await using var _ = factory;
        using var __ = channel;

        var first = await DrawAsync(player, Single(), "req-2");
        var second = await DrawAsync(player, Single(), "req-2");

        Assert.Equal(first.Results[0].SupportCardUid, second.Results[0].SupportCardUid);
        Assert.Equal(2250, await Scalar<int>("SELECT gacha_currency FROM players WHERE player_id = $1", player));
        Assert.Equal(1L, await Scalar<long>("SELECT count(*) FROM gacha_histories WHERE player_id = $1", player));
    }

    [Fact]
    public async Task Draw_ConcurrentRequests_NeverOverspend()
    {
        var (factory, channel, player) = await StartAsync(postgres, null);
        await using var _ = factory;
        using var __ = channel;

        var tasks = Enumerable.Range(0, 12).Select(async i =>
        {
            try
            {
                await DrawAsync(player, Single(), $"req-c-{i}");
                return (RpcException?)null;
            }
            catch (RpcException exception)
            {
                return exception;
            }
        }).ToList();
        var outcomes = await Task.WhenAll(tasks);

        Assert.Equal(10, outcomes.Count(x => x is null));
        var failures = outcomes.Where(x => x is not null).Select(x => x!).ToList();
        Assert.Equal(2, failures.Count);
        Assert.All(failures, x => AssertReason(x, StatusCode.FailedPrecondition, "GACHA_NOT_ENOUGH_CURRENCY"));
        Assert.Equal(0, await Scalar<int>("SELECT gacha_currency FROM players WHERE player_id = $1", player));
        Assert.Equal(10, await Scalar<int>("SELECT points FROM gacha_exchange_points WHERE player_id = $1 AND gacha_id = 1", player));
    }

    [Fact]
    public async Task Draw_BeforeSignUp_ReturnsPlayerNotCreated()
    {
        await using var factory = new UtaServerFactory(postgres, randomSource: new ScriptedRandomSource(Normal(0)));
        using var channel = factory.CreateGrpcChannel();
        var player = await PlayerClient.CreateAsync(channel);

        var exception = await Assert.ThrowsAsync<RpcException>(() => DrawAsync(player, Single(), "req-1"));

        AssertReason(exception, StatusCode.FailedPrecondition, "PLAYER_NOT_CREATED");
    }

    [Fact]
    public async Task Draw_WithoutRequestId_InvalidArgument()
    {
        var (factory, channel, player) = await StartAsync(postgres, Normal(0));
        await using var _ = factory;
        using var __ = channel;

        var exception = await Assert.ThrowsAsync<RpcException>(
            () => player.CallAsync<IGachaService, DrawResponse>(x => x.DrawAsync(Single()), null));

        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
    }

    private async Task GiveExchangePointsAsync(PlayerClient player)
    {
        await postgres.ExecuteAsync("INSERT INTO gacha_exchange_points (player_id, gacha_id, points) VALUES ($1, 1, 250)", player.PlayerId);
    }

    [Fact]
    public async Task Exchange_EnoughPoints_GrantsAndSpends()
    {
        var (factory, channel, player) = await StartAsync(postgres, null);
        await using var _ = factory;
        using var __ = channel;
        await GiveExchangePointsAsync(player);

        var response = await ExchangeAsync(player, new ExchangeRequest(1, 1), "req-2");

        Assert.Equal(0, response.Result.Slot);
        Assert.Equal(ItemKind.Idol, response.Result.ItemKind);
        Assert.Equal(4, response.Result.ItemId);
        Assert.Equal(GachaOutcome.New, response.Result.Outcome);
        Assert.Equal(50, response.ExchangePoints);
        Assert.Equal(1L, await Scalar<long>("SELECT count(*) FROM player_idols WHERE player_id = $1 AND idol_id = 4", player));
        Assert.Equal(1L, await Scalar<long>("SELECT count(*) FROM gacha_histories WHERE player_id = $1", player));
        Assert.Equal(1L, await Scalar<long>("SELECT count(*) FROM gacha_histories WHERE player_id = $1 AND slot = 0 AND gacha_table_id = 0", player));
        Assert.Equal(2500, await Scalar<int>("SELECT gacha_currency FROM players WHERE player_id = $1", player));
    }

    [Fact]
    public async Task Exchange_WritesOneGameLog()
    {
        var (factory, channel, player) = await StartAsync(postgres, null);
        await using var _ = factory;
        using var __ = channel;
        await GiveExchangePointsAsync(player);

        await ExchangeAsync(player, new ExchangeRequest(1, 1), "req-2");

        Assert.Equal(1L, await Scalar<long>(
            "SELECT count(*) FROM game_logs WHERE player_id = $1 AND kind = 'gacha' AND body->>'reason' = 'gacha_exchange'", player));
    }

    [Fact]
    public async Task Exchange_SupportCardItem_AddsRow()
    {
        var (factory, channel, player) = await StartAsync(postgres, null);
        await using var _ = factory;
        using var __ = channel;
        await GiveExchangePointsAsync(player);

        var response = await ExchangeAsync(player, new ExchangeRequest(1, 2), "req-2");

        Assert.Equal(GachaOutcome.New, response.Result.Outcome);
        Assert.NotEmpty(response.Result.SupportCardUid);
        Assert.Equal(50, response.ExchangePoints);
    }

    [Fact]
    public async Task Exchange_NotEnoughPoints_Rejected()
    {
        var (factory, channel, player) = await StartAsync(postgres, null);
        await using var _ = factory;
        using var __ = channel;

        var exception = await Assert.ThrowsAsync<RpcException>(() => ExchangeAsync(player, new ExchangeRequest(1, 1), "req-2"));

        AssertReason(exception, StatusCode.FailedPrecondition, "GACHA_NOT_ENOUGH_POINTS");
    }

    [Fact]
    public async Task Exchange_UnknownItemOrGacha_Rejected()
    {
        var (factory, channel, player) = await StartAsync(postgres, null);
        await using var _ = factory;
        using var __ = channel;

        var unknownOrder = await Assert.ThrowsAsync<RpcException>(() => ExchangeAsync(player, new ExchangeRequest(1, 3), "req-2"));
        var unknownGacha = await Assert.ThrowsAsync<RpcException>(() => ExchangeAsync(player, new ExchangeRequest(99, 1), "req-3"));

        AssertReason(unknownOrder, StatusCode.FailedPrecondition, "GACHA_NOT_FOUND");
        AssertReason(unknownGacha, StatusCode.FailedPrecondition, "GACHA_NOT_FOUND");
    }

    [Fact]
    public async Task GetGachas_ReturnsExchangePoints()
    {
        var (factory, channel, player) = await StartAsync(postgres, Normal(0));
        await using var _ = factory;
        using var __ = channel;

        var before = await player.CallAsync<IGachaService, GetGachasResponse>(x => x.GetGachasAsync(), null);
        await DrawAsync(player, Single(), "req-2");
        var after = await player.CallAsync<IGachaService, GetGachasResponse>(x => x.GetGachasAsync(), null);

        var first = Assert.Single(before.Gachas);
        Assert.Equal(1, first.GachaId);
        Assert.Equal(0, first.ExchangePoints);
        Assert.Equal(1, Assert.Single(after.Gachas).ExchangePoints);
    }

    [Fact]
    public async Task Migration_CreatesGachaTables()
    {
        var count = await postgres.QueryScalarAsync<long>(
            "SELECT count(*) FROM information_schema.tables WHERE table_name = ANY(ARRAY['gacha_exchange_points','gacha_histories'])");

        Assert.Equal(2L, count);
    }
}
