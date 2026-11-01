using Grpc.Core;
using Grpc.Health.V1;
using MagicOnion.Client;
using Microsoft.Extensions.DependencyInjection;
using Uta.Server.Tests.Integration.Support;
using VolTech.Server.Abstractions;
using VolTech.Server.Auth;

namespace Uta.Server.Tests.Integration;

[Collection(PostgresCollection.Name)]
public class DeviceRegistrationTests(PostgresFixture postgres)
{
    private const string Secret = "0123456789abcdef-secret";

    [Fact]
    public async Task Register_ThenIssueToken_ReturnsIdsAndToken()
    {
        await using var factory = new UtaServerFactory(postgres);
        using var channel = factory.CreateGrpcChannel();
        var client = MagicOnionClient.Create<IDeviceService>(channel);

        var registered = await client.RegisterAsync(Secret);
        var token = await client.IssueTokenAsync(registered.DeviceId, Secret);

        Assert.Equal(26, registered.DeviceId.Length);
        Assert.Equal(26, registered.PlayerId.Length);
        Assert.True(Ulid.TryParse(registered.DeviceId, out _));
        Assert.True(Ulid.TryParse(registered.PlayerId, out _));
        Assert.Matches("^[0-9a-f]{64}$", token.AccessToken);
        Assert.Equal(factory.Time.GetUtcNow().AddHours(1).ToUnixTimeSeconds(), new DateTimeOffset(token.ExpiresAt).ToUnixTimeSeconds());
    }

    [Fact]
    public async Task IssueToken_WrongSecret_ReturnsUnauthenticated()
    {
        await using var factory = new UtaServerFactory(postgres);
        using var channel = factory.CreateGrpcChannel();
        var client = MagicOnionClient.Create<IDeviceService>(channel);
        var registered = await client.RegisterAsync(Secret);

        var exception = await Assert.ThrowsAsync<RpcException>(async () => await client.IssueTokenAsync(registered.DeviceId, Secret + "x"));

        Assert.Equal(StatusCode.Unauthenticated, exception.StatusCode);
    }

    [Fact]
    public async Task Register_TooShortSecret_ReturnsInvalidArgument()
    {
        await using var factory = new UtaServerFactory(postgres);
        using var channel = factory.CreateGrpcChannel();
        var client = MagicOnionClient.Create<IDeviceService>(channel);

        var exception = await Assert.ThrowsAsync<RpcException>(async () => await client.RegisterAsync("short"));

        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
    }

    [Fact]
    public async Task Players_AreNotCreatedUntilRowExists()
    {
        await using var factory = new UtaServerFactory(postgres);
        using var channel = factory.CreateGrpcChannel();
        var client = MagicOnionClient.Create<IDeviceService>(channel);
        var registered = await client.RegisterAsync(Secret);
        var playerId = Ulid.Parse(registered.PlayerId);

        using (var scope = factory.Services.CreateScope())
        {
            var resolver = scope.ServiceProvider.GetRequiredService<IPlayerResolver>();
            Assert.Equal(PlayerStatus.NotCreated, await resolver.ResolveAsync(playerId, TestContext.Current.CancellationToken));
        }

        await postgres.ExecuteAsync("INSERT INTO players (player_id) VALUES ($1)", registered.PlayerId);

        using (var scope = factory.Services.CreateScope())
        {
            var resolver = scope.ServiceProvider.GetRequiredService<IPlayerResolver>();
            Assert.Equal(PlayerStatus.Active, await resolver.ResolveAsync(playerId, TestContext.Current.CancellationToken));
        }
    }

    [Fact]
    public async Task GameDay_ResetsAtNoonJst()
    {
        // FakeTimeProvider は過去に戻せないので、JST 11:59 を初期値にして作り、あとで 12:00 に進める
        await using var factory = new UtaServerFactory(postgres, new DateTimeOffset(2026, 11, 1, 2, 59, 0, TimeSpan.Zero));
        using var channel = factory.CreateGrpcChannel();
        var client = MagicOnionClient.Create<IDeviceService>(channel);

        var before = client.RegisterAsync(Secret);
        await before;
        var beforeHeaders = await before.ResponseHeadersAsync;

        factory.Time.SetUtcNow(new DateTimeOffset(2026, 11, 1, 3, 0, 0, TimeSpan.Zero));
        var after = client.RegisterAsync(Secret);
        await after;
        var afterHeaders = await after.ResponseHeadersAsync;

        Assert.Equal("2026-10-31", beforeHeaders.GetValue("x-game-date"));
        Assert.Equal("2026-11-01", afterHeaders.GetValue("x-game-date"));
    }

    [Fact]
    public async Task Health_Check_ReturnsServing()
    {
        await using var factory = new UtaServerFactory(postgres);
        using var channel = factory.CreateGrpcChannel();
        var health = new Health.HealthClient(channel);

        var response = await health.CheckAsync(new HealthCheckRequest(), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HealthCheckResponse.Types.ServingStatus.Serving, response.Status);
    }

    [Fact]
    public async Task Migration_CreatesPlayersTable()
    {
        var length = await postgres.QueryScalarAsync<int>(
            "SELECT character_maximum_length FROM information_schema.columns WHERE table_name = $1 AND column_name = $2", "players", "player_id");
        var dataType = await postgres.QueryScalarAsync<string>(
            "SELECT data_type FROM information_schema.columns WHERE table_name = $1 AND column_name = $2", "players", "player_id");

        Assert.Equal(26, length);
        Assert.Equal("character", dataType);
    }
}
