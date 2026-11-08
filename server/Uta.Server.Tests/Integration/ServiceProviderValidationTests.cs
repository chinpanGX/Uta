using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Uta.Server.Domain.Player;
using Uta.Server.Tests.Integration.Support;

namespace Uta.Server.Tests.Integration;

[Collection(PostgresCollection.Name)]
public class ServiceProviderValidationTests(PostgresFixture postgres)
{
    // 既定（Development）では ASP.NET Core が検査するので、Production で ServerHost の設定が効くことを確かめる
    [Fact]
    public async Task Build_ProductionWithSingletonCapturingScoped_Throws()
    {
        await using var factory = new ServerFactory(postgres, configureServices: services => services.AddSingleton<ScopedCapturingSingleton>());
        await using var production = factory.WithWebHostBuilder(builder => builder.UseEnvironment(Environments.Production));

        var exception = Assert.ThrowsAny<Exception>(() => production.Server);

        var message = exception.ToString();
        Assert.Contains("Cannot consume scoped service", message);
        Assert.Contains(nameof(IPlayerRepository), message);
        Assert.Contains(nameof(ScopedCapturingSingleton), message);
    }

    private sealed class ScopedCapturingSingleton(IPlayerRepository players)
    {
        public IPlayerRepository Players { get; } = players;
    }
}
