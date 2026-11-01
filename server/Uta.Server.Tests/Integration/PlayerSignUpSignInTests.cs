using System.Text;
using Grpc.Core;
using MagicOnion.Client;
using Uta.Server.Tests.Integration.Support;
using Uta.Shared.Contracts.Player;
using VolTech.Server.Abstractions;

namespace Uta.Server.Tests.Integration;

[Collection(PostgresCollection.Name)]
public class PlayerSignUpSignInTests(PostgresFixture postgres)
{
    private const string CountSql = "SELECT count(*) FROM players WHERE player_id = $1";

    [Fact]
    public async Task SignUp_NewPlayer_CreatesRow()
    {
        await using var factory = new UtaServerFactory(postgres);
        using var channel = factory.CreateGrpcChannel();
        var player = await PlayerClient.CreateAsync(channel);

        var response = await player.CallAsync(service => service.SignUpAsync(), "req-1");

        Assert.True(response.Created);
        Assert.Equal(1L, await postgres.QueryScalarAsync<long>(CountSql, player.PlayerId));
    }

    [Fact]
    public async Task SignUp_Twice_DoesNothing()
    {
        await using var factory = new UtaServerFactory(postgres);
        using var channel = factory.CreateGrpcChannel();
        var player = await PlayerClient.CreateAsync(channel);

        await player.CallAsync(service => service.SignUpAsync(), "req-1");
        var second = await player.CallAsync(service => service.SignUpAsync(), "req-2");

        Assert.False(second.Created);
        Assert.Equal(1L, await postgres.QueryScalarAsync<long>(CountSql, player.PlayerId));
    }

    [Fact]
    public async Task SignUp_SameRequestId_ReturnsStoredResponse()
    {
        await using var factory = new UtaServerFactory(postgres);
        using var channel = factory.CreateGrpcChannel();
        var player = await PlayerClient.CreateAsync(channel);

        await player.CallAsync(service => service.SignUpAsync(), "req-1");
        var second = await player.CallAsync(service => service.SignUpAsync(), "req-1");

        Assert.True(second.Created);
        Assert.Equal(1L, await postgres.QueryScalarAsync<long>(CountSql, player.PlayerId));
    }

    [Fact]
    public async Task SignIn_AfterSignUp_ReturnsInitialGrant()
    {
        await using var factory = new UtaServerFactory(postgres);
        using var channel = factory.CreateGrpcChannel();
        var player = await PlayerClient.CreateAsync(channel);

        await player.CallAsync(service => service.SignUpAsync(), "req-1");
        var response = await player.CallAsync(service => service.SignInAsync(), "req-2");

        Assert.Equal(1, response.Level);
        Assert.Equal(0, response.Exp);
        Assert.Equal(10000, response.Gold);
        Assert.Equal(10000, response.SupportPoints);
        Assert.Equal(2500, response.GachaCurrency);
        Assert.False(response.HasActiveProduce);
        Assert.False(response.DiscardedProduce);
        Assert.Equal(new[] { 1, 2 }, response.Idols.Select(x => x.IdolId).ToArray());
        Assert.All(response.Idols, x =>
        {
            Assert.Equal(0, x.Training);
            Assert.Equal(0, x.Awakening);
            Assert.Equal(0, x.Shards);
        });
        Assert.Equal(5, response.SupportCards.Length);
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }.ToHashSet(), response.SupportCards.Select(x => x.CardId).ToHashSet());
        Assert.All(response.SupportCards, x =>
        {
            Assert.Equal(1, x.Level);
            Assert.Equal(0, x.LimitBreak);
            Assert.Equal(26, x.SupportCardUid.Length);
        });
        Assert.Equal(5, response.SupportCards.Select(x => x.SupportCardUid).Distinct().Count());
    }

    [Fact]
    public async Task SignUp_Twice_DoesNotGrantAgain()
    {
        await using var factory = new UtaServerFactory(postgres);
        using var channel = factory.CreateGrpcChannel();
        var player = await PlayerClient.CreateAsync(channel);

        await player.CallAsync(service => service.SignUpAsync(), "req-1");
        await player.CallAsync(service => service.SignUpAsync(), "req-2");
        var response = await player.CallAsync(service => service.SignInAsync(), "req-3");

        Assert.Equal(10000, response.Gold);
        Assert.Equal(2500, response.GachaCurrency);
        Assert.Equal(2, response.Idols.Length);
        Assert.Equal(5, response.SupportCards.Length);
    }

    [Fact]
    public async Task SignUp_NewPlayer_CreatesOwnershipRows()
    {
        await using var factory = new UtaServerFactory(postgres);
        using var channel = factory.CreateGrpcChannel();
        var player = await PlayerClient.CreateAsync(channel);

        await player.CallAsync(service => service.SignUpAsync(), "req-1");

        Assert.Equal(2L, await postgres.QueryScalarAsync<long>("SELECT count(*) FROM player_idols WHERE player_id = $1", player.PlayerId));
        Assert.Equal(5L, await postgres.QueryScalarAsync<long>("SELECT count(*) FROM player_support_cards WHERE player_id = $1", player.PlayerId));
    }

    [Fact]
    public async Task SignUp_NewPlayer_WritesOneGameLog()
    {
        const string logSql = "SELECT count(*) FROM game_logs WHERE player_id = $1 AND kind = 'player' AND body->>'reason' = 'sign_up'";
        await using var factory = new UtaServerFactory(postgres);
        using var channel = factory.CreateGrpcChannel();
        var player = await PlayerClient.CreateAsync(channel);

        await player.CallAsync(service => service.SignUpAsync(), "req-1");
        Assert.Equal(1L, await postgres.QueryScalarAsync<long>(logSql, player.PlayerId));

        await player.CallAsync(service => service.SignUpAsync(), "req-2");
        await player.CallAsync(service => service.SignUpAsync(), "req-1");
        Assert.Equal(1L, await postgres.QueryScalarAsync<long>(logSql, player.PlayerId));
    }

    [Fact]
    public async Task Migration_CreatesOwnershipTables()
    {
        var count = await postgres.QueryScalarAsync<long>(
            "SELECT count(*) FROM information_schema.tables WHERE table_name = ANY(ARRAY['player_idols','player_support_cards'])");

        Assert.Equal(2L, count);
    }

    [Fact]
    public async Task SignIn_BeforeSignUp_ReturnsPlayerNotCreated()
    {
        await using var factory = new UtaServerFactory(postgres);
        using var channel = factory.CreateGrpcChannel();
        var player = await PlayerClient.CreateAsync(channel);

        var exception = await Assert.ThrowsAsync<RpcException>(() => player.CallAsync(service => service.SignInAsync(), "req-1"));

        Assert.Equal(StatusCode.FailedPrecondition, exception.StatusCode);
        var details = Encoding.UTF8.GetString(GetDetailsBytes(exception));
        Assert.Contains("PLAYER_NOT_CREATED", details);
        Assert.Contains("uta", details);
        Assert.Equal(0L, await postgres.QueryScalarAsync<long>(CountSql, player.PlayerId));
    }

    [Fact]
    public async Task SignIn_ThenSignUp_ThenSignIn_Works()
    {
        await using var factory = new UtaServerFactory(postgres);
        using var channel = factory.CreateGrpcChannel();
        var player = await PlayerClient.CreateAsync(channel);

        await Assert.ThrowsAsync<RpcException>(() => player.CallAsync(service => service.SignInAsync(), "req-1"));
        await player.CallAsync(service => service.SignUpAsync(), "req-2");
        var response = await player.CallAsync(service => service.SignInAsync(), "req-3");

        Assert.Equal(1, response.Level);
    }

    [Fact]
    public async Task SignIn_WithoutToken_Unauthenticated()
    {
        await using var factory = new UtaServerFactory(postgres);
        using var channel = factory.CreateGrpcChannel();
        var client = MagicOnionClient.Create<IPlayerService>(channel);

        var exception = await Assert.ThrowsAsync<RpcException>(async () => await client.SignInAsync());

        Assert.Equal(StatusCode.Unauthenticated, exception.StatusCode);
    }

    [Fact]
    public async Task SignIn_WithoutRequestId_InvalidArgument()
    {
        await using var factory = new UtaServerFactory(postgres);
        using var channel = factory.CreateGrpcChannel();
        var player = await PlayerClient.CreateAsync(channel);
        await player.CallAsync(service => service.SignUpAsync(), "req-1");

        var exception = await Assert.ThrowsAsync<RpcException>(() => player.CallAsync(service => service.SignInAsync(), null));

        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
    }

    [Fact]
    public async Task Migration_AddsPlayerColumns()
    {
        var count = await postgres.QueryScalarAsync<long>(
            "SELECT count(*) FROM information_schema.columns WHERE table_name = 'players' AND column_name = ANY(ARRAY['level','exp','gold','support_points','gacha_currency'])");

        Assert.Equal(5L, count);
    }

    private static byte[] GetDetailsBytes(RpcException exception)
    {
        foreach (var entry in exception.Trailers)
        {
            if (entry.Key == ApiHeaders.ErrorDetailsTrailer && entry.IsBinary)
            {
                return entry.ValueBytes;
            }
        }

        throw new InvalidOperationException("grpc-status-details-bin が trailers にない");
    }
}
