using Grpc.Net.Client;
using Uta.Server.Tests.Integration.Support;
using Uta.Shared.Contracts.Gacha;
using Uta.Shared.Contracts.Player;
using Uta.Shared.Contracts.Produce;
using Uta.Shared.Contracts.Support;

namespace Uta.Server.Tests.Integration;

[Collection(PostgresCollection.Name)]
public class CommonResponseTests(PostgresFixture postgres)
{
    private static ulong Normal(int r) => (ulong)(r + 10000);

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

    private static Task<LevelUpResponse> LevelUpAsync(PlayerClient player, string uid, int targetLevel, string requestId) =>
        player.CallAsync<ISupportService, LevelUpResponse>(x => x.LevelUpAsync(new LevelUpRequest(uid, targetLevel)), requestId);

    private static Task<LimitBreakResponse> LimitBreakAsync(PlayerClient player, string uid, string materialUid, string requestId) =>
        player.CallAsync<ISupportService, LimitBreakResponse>(x => x.LimitBreakAsync(new LimitBreakRequest(uid, materialUid)), requestId);

    private Task<T?> Scalar<T>(string sql, PlayerClient player) => postgres.QueryScalarAsync<T>(sql, player.PlayerId);

    private async Task GiveExchangePointsAsync(PlayerClient player)
    {
        await postgres.ExecuteAsync("INSERT INTO gacha_exchange_points (player_id, gacha_id, points) VALUES ($1, 1, 250)", player.PlayerId);
    }

    private async Task<string> AddMaterialAsync(PlayerClient player, int cardId)
    {
        var uid = Ulid.NewUlid().ToString();
        await postgres.ExecuteAsync(
            "INSERT INTO player_support_cards (support_card_uid, player_id, card_id) VALUES ($1, $2, $3)", uid, player.PlayerId, cardId);
        return uid;
    }

    [Fact]
    public async Task Draw_Single_NewCard_ReturnsCommon()
    {
        var (factory, channel, player) = await StartAsync(postgres, Normal(4000));
        await using var _ = factory;
        using var __ = channel;

        var response = await DrawAsync(player, Single(), "req-2");

        var diff = response.Common?.PlayerDiff;
        Assert.NotNull(diff);
        Assert.True(diff.HasBalance);
        Assert.Equal(response.GachaCurrency, diff.GachaCurrency);
        Assert.Equal(await Scalar<int>("SELECT gacha_currency FROM players WHERE player_id = $1", player), diff.GachaCurrency);
        var card = Assert.Single(diff.SupportCards);
        Assert.Equal(response.Results[0].SupportCardUniqueId, card.UniqueId);
        Assert.Equal(response.Results[0].ItemMasterId, card.MasterId);
        Assert.Empty(diff.Idols);
        Assert.Empty(diff.RemovedSupportCardUniqueIds);
    }

    [Fact]
    public async Task Draw_DuplicateIdol_ReturnsIdolWithShards()
    {
        var (factory, channel, player) = await StartAsync(postgres, Normal(0));
        await using var _ = factory;
        using var __ = channel;

        var response = await DrawAsync(player, Single(), "req-2");

        var diff = response.Common?.PlayerDiff;
        Assert.NotNull(diff);
        var idol = Assert.Single(diff.Idols);
        Assert.Equal(response.Results[0].ItemMasterId, idol.MasterId);
        Assert.Equal(await Scalar<int>("SELECT shards FROM player_idols WHERE player_id = $1 AND idol_id = 1", player), idol.Shards);
        Assert.Empty(diff.SupportCards);
    }

    [Fact]
    public async Task Exchange_ReturnsCommon()
    {
        var (factory, channel, player) = await StartAsync(postgres, null);
        await using var _ = factory;
        using var __ = channel;
        await GiveExchangePointsAsync(player);

        var response = await ExchangeAsync(player, new ExchangeRequest(1, 1), "req-2");

        var diff = response.Common?.PlayerDiff;
        Assert.NotNull(diff);
        var idol = Assert.Single(diff.Idols);
        Assert.Equal(response.Result.ItemMasterId, idol.MasterId);
    }

    [Fact]
    public async Task LevelUp_ReturnsCommon()
    {
        var (factory, channel, player) = await StartAsync(postgres, null);
        await using var _ = factory;
        using var __ = channel;
        await postgres.ExecuteAsync("UPDATE players SET support_points = 100000, gold = 100000 WHERE player_id = $1", player.PlayerId);
        var signIn = await player.CallAsync(service => service.SignInAsync(), "req-2");
        var uid = signIn.SupportCards.First(x => x.MasterId == 1).UniqueId;

        var response = await LevelUpAsync(player, uid, 3, "req-3");

        var diff = response.Common?.PlayerDiff;
        Assert.NotNull(diff);
        var card = Assert.Single(diff.SupportCards);
        Assert.Equal(uid, card.UniqueId);
        Assert.Equal(response.Level, card.Level);
        Assert.True(diff.HasBalance);
        Assert.Equal(response.SupportPoints, diff.SupportPoints);
        Assert.Equal(response.Gold, diff.Gold);
    }

    [Fact]
    public async Task LimitBreak_ReturnsCommon_WithRemovedMaterial()
    {
        var (factory, channel, player) = await StartAsync(postgres, null);
        await using var _ = factory;
        using var __ = channel;
        var signIn = await player.CallAsync(service => service.SignInAsync(), "req-2");
        var uid = signIn.SupportCards.First(x => x.MasterId == 1).UniqueId;
        var material = await AddMaterialAsync(player, 1);

        var response = await LimitBreakAsync(player, uid, material, "req-3");

        var diff = response.Common?.PlayerDiff;
        Assert.NotNull(diff);
        Assert.Contains(material, diff.RemovedSupportCardUniqueIds);
        var card = Assert.Single(diff.SupportCards);
        Assert.Equal(uid, card.UniqueId);
        Assert.Equal(response.LimitBreak, card.LimitBreak);
    }

    [Fact]
    public async Task ProduceComplete_ReturnsCommon_WithReward()
    {
        var factory = new UtaServerFactory(postgres, randomSource: new ScriptedRandomSource(12345UL, 23456UL, 34567UL, 45678UL, 56789UL, 67890UL));
        await using var _ = factory;
        using var channel = factory.CreateGrpcChannel();
        var player = await PlayerClient.CreateAsync(channel);
        await player.CallAsync(service => service.SignUpAsync(), "req-1");
        var signIn = await player.CallAsync(service => service.SignInAsync(), "req-2");
        var uids = signIn.SupportCards.Take(2).Select(x => x.UniqueId).ToArray();
        var started = await player.CallAsync<IProduceService, ProduceStateResponse>(
            x => x.StartAsync(new StartProduceRequest(1, uids, 0, 1)), "req-3");

        var final = await new ProduceDriver(player).PlayToEndAsync(started);

        Assert.Null(started.Common);
        var diff = final.Common?.PlayerDiff;
        Assert.NotNull(diff);
        Assert.True(diff.HasBalance);
        Assert.Equal(await Scalar<int>("SELECT gacha_currency FROM players WHERE player_id = $1", player), diff.GachaCurrency);
        Assert.Equal(await Scalar<int>("SELECT gold FROM players WHERE player_id = $1", player), diff.Gold);
        Assert.Equal(await Scalar<int>("SELECT support_points FROM players WHERE player_id = $1", player), diff.SupportPoints);
        Assert.Equal(await Scalar<int>("SELECT level FROM players WHERE player_id = $1", player), diff.Level);
        Assert.Equal(await Scalar<int>("SELECT exp FROM players WHERE player_id = $1", player), diff.Exp);
    }

    [Fact]
    public async Task Draw_SameRequestId_ReturnsSameCommon()
    {
        var (factory, channel, player) = await StartAsync(postgres, Normal(4000));
        await using var _ = factory;
        using var __ = channel;

        var first = await DrawAsync(player, Single(), "req-2");
        var second = await DrawAsync(player, Single(), "req-2");

        var a = first.Common?.PlayerDiff;
        var b = second.Common?.PlayerDiff;
        Assert.NotNull(a);
        Assert.NotNull(b);
        Assert.Equal(a.HasBalance, b.HasBalance);
        Assert.Equal(a.GachaCurrency, b.GachaCurrency);
        Assert.Equal(a.SupportCards.Select(x => x.UniqueId), b.SupportCards.Select(x => x.UniqueId));
        Assert.Equal(a.Idols.Select(x => x.UniqueId), b.Idols.Select(x => x.UniqueId));
        Assert.Equal(a.RemovedSupportCardUniqueIds, b.RemovedSupportCardUniqueIds);
    }
}
