using MagicOnion.Server;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using Uta.MasterData;
using Uta.Server.Application.Common;
using Uta.Server.Application.Gacha;
using Uta.Server.Application.Player;
using Uta.Server.Application.Produce;
using Uta.Server.Application.Support;
using Uta.Server.Domain.Gacha;
using Uta.Server.Domain.Idol;
using Uta.Server.Domain.Player;
using Uta.Server.Domain.Produce;
using Uta.Server.Domain.Support;
using Uta.Server.Infrastructure;
using Uta.Shared.Contracts.Common;
using Uta.Shared.Domain.Lesson;
using Uta.Shared.Domain.Produce;
using VolTech.Server.Auth;
using VolTech.Server.Hosting;
using ZLogger;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings:Default が設定されていない（ローカルは dotnet user-secrets、Cloud Run は環境変数 ConnectionStrings__Default）");

if (args.Length > 0 && args[0] == "migrate")
{
    if (args.Length > 1)
    {
        Console.Error.WriteLine("migrate は引数を取らない");
        return 1;
    }

    try
    {
        UtaDatabase.Migrate(connectionString);
        return 0;
    }
    catch (InvalidOperationException exception)
    {
        Console.Error.WriteLine(exception.Message);
        return 1;
    }
}

builder.Services.AddSingleton(MasterDataLoader.Load());
builder.Services.AddSingleton<ICardCatalog>(sp => MasterCardCatalog.Create(sp.GetRequiredService<MemoryDatabase>()));
builder.Services.AddSingleton(sp => MasterProduceCatalog.Create(sp.GetRequiredService<MemoryDatabase>(), sp.GetRequiredService<ICardCatalog>()));
builder.Services.AddSingleton<ProduceEngine>();
builder.Logging.ClearProviders();
builder.Logging.AddZLoggerConsole(options => options.UseJsonFormatter());
builder.WebHost.ConfigureKestrel(kestrel => kestrel.ConfigureEndpointDefaults(listen => listen.Protocols = HttpProtocols.Http2));
builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));
builder.Services.AddScoped<IPlayerResolver, PlayerResolver>();
builder.Services.AddScoped<IPlayerRepository, PlayerRepository>();
builder.Services.AddScoped<IIdolRepository, IdolRepository>();
builder.Services.AddScoped<ISupportCardRepository, SupportCardRepository>();
builder.Services.AddSingleton<IInitialGrantCatalog, MasterInitialGrantCatalog>();
builder.Services.AddSingleton<IRandomSource, SecureRandomSource>();
builder.Services.AddScoped<IGachaRepository, GachaRepository>();
builder.Services.AddScoped<IProduceSessionRepository, ProduceSessionRepository>();
builder.Services.AddSingleton<ProduceViewBuilder>();
builder.Services.AddSingleton<IProduceRewardCatalog, MasterProduceRewardCatalog>();
builder.Services.AddSingleton<IPlayerLevelCatalog, MasterPlayerLevelCatalog>();
builder.Services.AddScoped<ProduceSessionDiscarder>();
builder.Services.AddScoped<StartProduceUseCase>();
builder.Services.AddScoped<GetProduceStateUseCase>();
builder.Services.AddScoped<AbandonProduceUseCase>();
builder.Services.AddScoped<ProduceCompletion>();
builder.Services.AddScoped<ProduceOperationUseCase>();
builder.Services.AddScoped<ItemGranter>();
builder.Services.AddScoped<GetGachasUseCase>();
builder.Services.AddScoped<DrawUseCase>();
builder.Services.AddScoped<ExchangeUseCase>();
builder.Services.AddSingleton<ISupportCatalog, MasterSupportCatalog>();
builder.Services.AddScoped<LevelUpUseCase>();
builder.Services.AddScoped<LimitBreakUseCase>();
builder.Services.AddScoped<SignUpUseCase>();
builder.Services.AddScoped<SignInUseCase>();
builder.Services.AddVoltechServer(options => builder.Configuration.GetSection("Voltech").Bind(options))
    .AddCommonResponse<AppCommonDto, PlayerDiffCollector>();
// 登録した確認が 0 件だと Check が Unknown を返すので、常に Healthy の確認を 1 つ入れる（DB の確認は入れない。11.4）
builder.Services.AddGrpcHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy());

var app = builder.Build();
app.MapMagicOnionService();
app.MapGrpcHealthChecksService();
app.Run();
return 0;

public partial class Program;
