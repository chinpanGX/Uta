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

namespace Uta.Server.Hosting;

/// <summary>
/// サーバーの起動を組み立てる。Program.cs はここを呼ぶだけ。
/// </summary>
internal static class ServerHost
{
    /// <summary>
    /// 最初の引数が migrate ならマイグレーションだけを流し、それ以外は Web ホストを起動する。
    /// </summary>
    /// <returns>終了コード。</returns>
    public static int Run(string[] args)
    {
        // args は加工せずに渡す（WebApplicationFactory が --environment などを args で渡すため）
        var builder = WebApplication.CreateBuilder(args);
        var connectionString = builder.Configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default が設定されていない（ローカルは dotnet user-secrets、Cloud Run は環境変数 ConnectionStrings__Default）");

        // migrate はマスタも DI も使わないので、ホストを組み立てる前に分ける
        if (args.Length > 0 && args[0] == "migrate")
        {
            return Migrate(args, connectionString);
        }

        ConfigureHost(builder);
        AddServices(builder.Services, builder.Configuration, connectionString);

        var app = builder.Build();
        MapEndpoints(app);
        app.Run();
        return 0;
    }

    private static int Migrate(string[] args, string connectionString)
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

    private static void ConfigureHost(WebApplicationBuilder builder)
    {
        // DI の検査（スコープの寿命・起動時の組み立て）を、Development だけでなく Production でも有効にする
        builder.Host.UseDefaultServiceProvider(options =>
        {
            options.ValidateScopes = true;
            options.ValidateOnBuild = true;
        });
        builder.Logging.ClearProviders();
        builder.Logging.AddZLoggerConsole(options => options.UseJsonFormatter());
        builder.WebHost.ConfigureKestrel(kestrel => kestrel.ConfigureEndpointDefaults(listen => listen.Protocols = HttpProtocols.Http2));
    }

    private static void AddServices(IServiceCollection services, IConfiguration configuration, string connectionString)
    {
        // マスタ（ADR 0020。起動時に 1 回読み、シングルトンで持つ）
        services.AddSingleton(MasterDataLoader.Load());
        services.AddSingleton<ICardCatalog>(sp => CardCatalogFactory.Create(sp.GetRequiredService<MemoryDatabase>()));
        services.AddSingleton(sp => ProduceCatalogFactory.Create(sp.GetRequiredService<MemoryDatabase>(), sp.GetRequiredService<ICardCatalog>()));

        // Infrastructure（DB の接続と、IF の実装）
        services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));
        services.AddSingleton<IRandomSource, SecureRandomSource>();
        services.AddScoped<IPlayerResolver, PlayerResolver>();
        services.AddScoped<IPlayerRepository, PlayerRepository>();
        services.AddScoped<IIdolRepository, IdolRepository>();
        services.AddScoped<ISupportCardRepository, SupportCardRepository>();
        services.AddScoped<IGachaRepository, GachaRepository>();
        services.AddScoped<IProduceSessionRepository, ProduceSessionRepository>();

        // プロデュース
        services.AddSingleton<ProduceEngine>();
        services.AddSingleton<ProduceStateResultFactory>();
        services.AddScoped<ProduceSessionDiscarder>();
        services.AddScoped<StartProduceUseCase>();
        services.AddScoped<GetProduceStateUseCase>();
        services.AddScoped<AbandonProduceUseCase>();
        services.AddScoped<ProduceCompletion>();
        services.AddScoped<ProduceOperationUseCase>();

        // ガチャ
        services.AddScoped<ItemGranter>();
        services.AddScoped<GetGachasUseCase>();
        services.AddScoped<DrawUseCase>();
        services.AddScoped<ExchangeUseCase>();

        // サポートカード
        services.AddScoped<LevelUpUseCase>();
        services.AddScoped<LimitBreakUseCase>();

        // プレイヤー
        services.AddScoped<SignUpUseCase>();
        services.AddScoped<SignInUseCase>();

        // 基盤（VolTech-MagicOnion）。PlayerDiffCollector は DI に登録しない（基盤がスコープから作る。server-foundation.md）
        services.AddVoltechServer(options => configuration.GetSection("Voltech").Bind(options))
            .AddCommonResponse<AppCommonDto, PlayerDiffCollector>();

        // ヘルスチェック
        // 登録した確認が 0 件だと Check が Unknown を返すので、常に Healthy の確認を 1 つ入れる（DB の確認は入れない。11.4）
        services.AddGrpcHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy());
    }

    private static void MapEndpoints(WebApplication app)
    {
        app.MapMagicOnionService();
        app.MapGrpcHealthChecksService();
    }
}
