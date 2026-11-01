---
title: 実装計画: 通信の疎通（サーバー）
status: ready
updated: 2026-11-01
spec: ../wiki/architecture/repository-layout.md, ../wiki/architecture/server/server-foundation.md, ../wiki/overview/game-spec.md
---

# 実装計画: 通信の疎通（サーバー）

## 1. 目的と前提

[game-spec.md](../wiki/overview/game-spec.md)「実装計画の単位と順」の 1 番目「通信の疎通」のうち、**サーバーと `shared/` の側（1a）**。ローカルのサーバーで、VolTech-MagicOnion の RPC のうち**端末の登録（`IDeviceService.RegisterAsync`）とトークンの発行（`IssueTokenAsync`）**が、結合テストで通るところまで作る。Uta の RPC（`SyncAsync` など）は作らない。中身の決定は [repository-layout.md](../wiki/architecture/repository-layout.md) 12章（手順 1・2・4。手順 3 は済み）が正で、この計画はそれを「どのファイルに何を書くか」に落とす。Unity の側（手順 5・6。1b「通信の疎通（クライアント）」）は別の計画・別のセッションで進め、手順 6 は 1a のマージ後に行う。1 番目の計画を 1a・1b に分けることは、ユーザーの指示（2026-11-01）による。wiki の分割はこの計画の完了時に同じ PR で反映する（7章）。

- 用語: 計画・テストの名前では「端末の登録」（`RegisterAsync`）、「トークンの発行」（`IssueTokenAsync`）と書く。ユーザーの整理（2026-11-01）では、サインアップ＝端末の登録、サインイン＝トークンの発行で、Uta のプレイヤーを作る `SyncAsync` とは別。この計画は `SyncAsync` を作らない。（基盤の `IDeviceService.cs` はサインアップを「登録の後にアプリがプレイヤーを作ること」の意味でも使う。語の定義は glossary.md に未登録で、ユーザーの判断待ち）
- 関係する ADR: [0002](../wiki/decisions/0002-magiconion.md)、[0005](../wiki/decisions/0005-foundation-submodule.md)、[0008](../wiki/decisions/0008-shared-domain-and-onion.md)、[0010](../wiki/decisions/0010-database.md)、[0012](../wiki/decisions/0012-migration-dbup.md)
- 受け持つ完了条件: なし（[game-spec.md](../wiki/overview/game-spec.md) 完了条件 0 は 1b が受け持つ）。この計画の終わりの形は、`dotnet build Uta.slnx` と既存のテスト、アーキテクチャテスト、VolTech-MagicOnion の端末登録の結合テスト（Docker）が通ること
- 範囲外（後の計画に回す）:
  - Uta の RPC・DTO、`players` の所持品の列（5 番目の計画）、`x-app-version` を読む処理（プロデュースの計画）
  - `client/uta/` の下、`shared/` の `package.json`・asmdef（Unity の取り込みは 1b が足す）、`Uta.Shared.Contracts` の印のクラス（Unity の asmdef のためのもの。置くかどうかは 1b が決める）
  - UnityScreenNavigator・Supplement・MasterMemory、Android / iOS の実機ビルド、Cloud Run へのデプロイ、CI
  - 共通基盤（`external/`）の変更。必要とわかったら、実装担当は止まって報告する（3.1 のルールで基盤を先に直す）

### この計画で決めたこと（wiki に書いていなかった細部）

| 項目 | 決めたこと | 理由 |
|---|---|---|
| ZLogger の版 | 2.5.10 | 2026-11-01 時点の最新の安定版。`Microsoft.Extensions.Logging` は ASP.NET の共有フレームワークのものを使う |
| `Grpc.AspNetCore.HealthChecks` の版 | 2.84.0 | VolTech-MagicOnion の `Grpc.AspNetCore.Server` と同じにする（2.1 の「版は基盤に合わせる」）。ZLogger 2.5.10・`Grpc.AspNetCore.HealthChecks` 2.84.0・`Microsoft.AspNetCore.Mvc.Testing` 10.0.14 は nuget.org で実在を確認済み（2026-11-01） |
| Dapper | `Directory.Packages.props` に 2.1.89 を足し、`PlayerResolver` は Dapper で書く | [ADR 0010](../wiki/decisions/0010-database.md)「アクセスは Npgsql + Dapper」。SQL は Infrastructure のリポジトリに置く（[server-foundation.md](../wiki/architecture/server/server-foundation.md)「SQL を書く層」） |
| アーキテクチャテストの形 | 3.1 の依存の表の「禁止」の列から、層ごとに 1 ルール（`NotHaveDependencyOnAny`）を書く（3.1 の箇条書き「上の『禁止』を、アーキテクチャテストのルール 1 つずつにする」の形） | ユーザーの指示（2026-11-01）。3.1 の中に依存の表の前の段落「許可の一覧から書く」と、表の後の箇条書き「禁止を 1 つずつ」があり、後者にそろえる。許可の一覧の形は、禁止の列に無い依存（例: `Domain` から MagicOnion）を検出できないので、層に型が増えたら見直す。完了時に前者の段落を直す（7章） |
| サーバーの待ち受け | `http://localhost:5000`（h2c）。LAN に出すときは環境変数 `ASPNETCORE_URLS=http://0.0.0.0:5000` | ASP.NET の既定のポートで、基盤のガイドの例（`http://localhost:5000`）と同じ。Kestrel の `Protocols` は `Http2` のみ（12章） |
| `migrate` の引数 | `migrate` の後ろに引数があれば、メッセージを出して終了コード 1 | 12章「ほかの引数は取らない」 |
| `Uta.Shared.Contracts` の中身 | `.cs` を置かない（12章「中身は空でよい」）。`dotnet build` が `.cs` 0 件で通るかは、タスク 2 で確かめる | Unity の asmdef 用の印のクラスは 1b が決める |

## 2. 触るファイル

| 操作 | パス | 中身 | タスク |
|---|---|---|---|
| 作成 | `Directory.Build.props` | 3.1 | 1 |
| 作成 | `Directory.Packages.props` | 3.2 | 1 |
| 作成 | `Uta.slnx` | 3.3 | 1（以降のタスクで行を足す） |
| 作成 | `shared/Directory.Build.props` | 3.4 | 1 |
| 作成 | `client/Directory.Build.props` | 中身が `<Project />` だけ | 1 |
| 変更 | `shared/Uta.Shared.Domain.Tests/Uta.Shared.Domain.Tests.csproj` | `xunit.v3` の `Version` を消す | 1 |
| 変更 | `.gitignore` | `bin~/`・`obj~/` を足す | 1 |
| 変更 | `shared/Uta.Shared.Domain/`・`shared/Uta.Shared.Domain.Tests/` の既存の `.cs` | `EnforceCodeStyleInBuild` で出たエラーだけ直す（なければ変更なし） | 1 |
| 作成 | `shared/Uta.Shared.Contracts/Uta.Shared.Contracts.csproj` | 3.5 | 2 |
| 作成 | `server/Uta.Server/Uta.Server.csproj` | 3.6 | 3 |
| 作成 | `server/Uta.Server/Program.cs` | 3.7 | 3 |
| 作成 | `server/Uta.Server/Infrastructure/PlayerResolver.cs` | 3.8 | 3 |
| 作成 | `server/Uta.Server/Infrastructure/UtaDatabase.cs` | 3.9 | 3 |
| 作成 | `server/Uta.Server/Migrations/0001_create_players.sql` | 3.10 | 3 |
| 作成 | `server/Uta.Server/appsettings.json` | 3.11 | 3 |
| 作成 | `server/Uta.Server/Properties/launchSettings.json` | 3.11 | 3 |
| 作成 | `server/compose.yaml`・`server/.env.example` | 3.12 | 3 |
| 作成 | `server/Uta.Server.Tests/Uta.Server.Tests.csproj` | 3.13 | 4 |
| 作成 | `server/Uta.Server.Tests/Architecture/ArchitectureTests.cs` | 5章 | 4 |
| 作成 | `server/Uta.Server.Tests/Integration/Support/PostgresFixture.cs`・`UtaServerFactory.cs` | 3.13 | 4 |
| 作成 | `server/Uta.Server.Tests/Integration/DeviceRegistrationTests.cs` | 5章 | 4 |

## 3. 型とシグネチャ

### 3.1 `Directory.Build.props`（ルート）

```xml
<Project>

  <!-- shared/・server/ の .NET プロジェクトの共通設定（repository-layout.md 2.1）。Nullable・LangVersion は各 .csproj に書く -->
  <PropertyGroup>
    <Deterministic>true</Deterministic>
    <!-- .editorconfig の規約（var・Allman・命名）をビルドの警告にする -->
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>

</Project>
```

### 3.2 `Directory.Packages.props`

`ManagePackageVersionsCentrally` と `CentralPackageTransitivePinningEnabled` を `true` にする。版は次のとおり（**VolTech-MagicOnion の `Directory.Packages.props` と同じ版は同じに保つ。上げるときは基盤が先**）:

| パッケージ | 版 | 備考 |
|---|---|---|
| `MagicOnion.Abstractions`・`MagicOnion.Server`・`MagicOnion.Client` | 7.11.0 | 基盤と同じ |
| `MessagePack`・`MessagePack.Annotations` | 3.1.12 | 基盤と同じ |
| `Grpc.AspNetCore.Server`・`Grpc.Net.Client`・`Grpc.StatusProto`・`Grpc.AspNetCore.HealthChecks`・`Grpc.HealthCheck` | 2.84.0 | 基盤と同じ（`HealthChecks`・`Grpc.HealthCheck` は Uta が足す。`Grpc.HealthCheck` はテストのクライアント用で、版が 2.84.0 で実在しなければ、版を変えずに止まって報告する） |
| `Microsoft.NET.StringTools` | 17.14.28 | 基盤と同じ（推移的な依存の固定） |
| `Npgsql` | 10.0.3 | 基盤と同じ |
| `Dapper` | 2.1.89 | Uta が決める（1 章。基盤は使わない） |
| `dbup-postgresql` | 7.0.1 | 基盤と同じ |
| `Ulid` | 1.4.1 | 基盤と同じ |
| `Testcontainers.PostgreSql` | 4.16.0 | 基盤と同じ |
| `NetArchTest.Rules` | 1.3.2 | 基盤と同じ |
| `Microsoft.Extensions.TimeProvider.Testing` | 10.11.0 | 基盤と同じ |
| `Microsoft.AspNetCore.Mvc.Testing` | 10.0.14 | 基盤の `Microsoft.AspNetCore.TestHost` と同じ版（実在を確認済み） |
| `xunit.v3` | 4.0.2 | Uta が決める（10.4） |
| `ZLogger` | 2.5.10 | Uta が決める（1 章） |

### 3.3 `Uta.slnx`

[VolTech-MagicOnion.slnx](../../external/VolTech-MagicOnion/VolTech-MagicOnion.slnx) と同じ書式。ソリューションフォルダは `/shared/`・`/server/`・`/external/`。

```xml
<Solution>
  <Folder Name="/shared/">
    <Project Path="shared/Uta.Shared.Domain/Uta.Shared.Domain.csproj" />
    <Project Path="shared/Uta.Shared.Domain.Tests/Uta.Shared.Domain.Tests.csproj" />
  </Folder>
  <Folder Name="/external/">
    <Project Path="external/VolTech-MagicOnion/src/VolTech.Server.Abstractions/VolTech.Server.Abstractions.csproj" />
    <Project Path="external/VolTech-MagicOnion/src/VolTech.Server/VolTech.Server.csproj" />
  </Folder>
</Solution>
```

最終形は、`/shared/` に `Uta.Shared.Contracts`（タスク 2）、`/server/` に `Uta.Server` と `Uta.Server.Tests`（タスク 3・4）が増える。

### 3.4 `shared/Directory.Build.props`

親の設定を取り込み、`dotnet build` の出力を Unity が読まない `~` で終わるフォルダに逃がす（repository-layout.md 2章。`.csproj` の本文ではなくここに書く。NuGet の復元が先に `obj/` を作るのを避けるため）。

```xml
<Project>

  <Import Project="$([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '$(MSBuildThisFileDirectory)../'))" />

  <!-- Unity は ~ で終わるフォルダを読まない。dotnet build の出力が Unity のパッケージに入って型が重複しないようにする -->
  <PropertyGroup>
    <BaseOutputPath>bin~/</BaseOutputPath>
    <BaseIntermediateOutputPath>obj~/</BaseIntermediateOutputPath>
  </PropertyGroup>

</Project>
```

### 3.5 `shared/Uta.Shared.Contracts`

`Uta.Shared.Domain.csproj` と同じ書き方（netstandard2.1・C# 9・`Nullable` 無効・`TreatWarningsAsErrors`・`RootNamespace` は `Uta.Shared.Contracts`）。`ProjectReference` は `..\Uta.Shared.Domain\Uta.Shared.Domain.csproj` と `..\..\external\VolTech-MagicOnion\src\VolTech.Server.Abstractions\VolTech.Server.Abstractions.csproj`。

`.cs` は置かない（1 章）。`.cs` が 0 件でも `dotnet build` が警告 0 で通ることを確かめる。通らなければ（CS2008 など）、印のクラスなどを自分で足さずに、エラーを添えて止まって報告する。

### 3.6 `server/Uta.Server/Uta.Server.csproj`

`Sdk="Microsoft.NET.Sdk.Web"`。`TargetFramework` は `net10.0`、`ImplicitUsings` は `enable`、`Nullable` は `enable`、`UserSecretsId` は `bd1b74a5-dd58-4532-b25b-6173823dfcca`。

- `PackageReference`（版は書かない）: `Npgsql`、`Dapper`、`dbup-postgresql`、`Ulid`、`ZLogger`、`Grpc.AspNetCore.HealthChecks`
- `ProjectReference`: `..\..\external\VolTech-MagicOnion\src\VolTech.Server\VolTech.Server.csproj`、`..\..\external\VolTech-MagicOnion\src\VolTech.Server.Abstractions\VolTech.Server.Abstractions.csproj`、`..\..\shared\Uta.Shared.Domain\Uta.Shared.Domain.csproj`、`..\..\shared\Uta.Shared.Contracts\Uta.Shared.Contracts.csproj`
- `<EmbeddedResource Include="Migrations\*.sql" />`
- `InternalsVisibleTo` は書かない（`Program` は `public partial class` なので、テストから見える）

### 3.7 `server/Uta.Server/Program.cs`

トップレベル文。ファイルの全文は次のとおり（`using` と順番もこのまま）:

```csharp
using MagicOnion.Server;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using Uta.Server.Infrastructure;
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

builder.Logging.ClearProviders();
builder.Logging.AddZLoggerConsole(options => options.UseJsonFormatter());
builder.WebHost.ConfigureKestrel(kestrel => kestrel.ConfigureEndpointDefaults(listen => listen.Protocols = HttpProtocols.Http2));
builder.Services.AddSingleton(NpgsqlDataSource.Create(connectionString));
builder.Services.AddScoped<IPlayerResolver, PlayerResolver>();
builder.Services.AddVoltechServer(options => builder.Configuration.GetSection("Voltech").Bind(options));
// 登録した確認が 0 件だと Check が Unknown を返すので、常に Healthy の確認を 1 つ入れる（DB の確認は入れない。11.4）
builder.Services.AddGrpcHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy());

var app = builder.Build();
app.MapMagicOnionService();
app.MapGrpcHealthChecksService();
app.Run();
return 0;

public partial class Program;
```

- 通常の起動では DbUp を流さない（`migrate` を別に流す。11.2）
- ヘルスチェックには DB の確認を入れない（11.4）

### 3.8 `Infrastructure/PlayerResolver.cs`

SQL は Dapper で書く（1 章）。

```csharp
namespace Uta.Server.Infrastructure;

public sealed class PlayerResolver(NpgsqlDataSource dataSource) : IPlayerResolver
{
    public async ValueTask<PlayerStatus> ResolveAsync(Ulid playerId, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var exists = await connection.ExecuteScalarAsync<int?>(new CommandDefinition(
            "SELECT 1 FROM players WHERE player_id = @playerId",
            new { playerId = playerId.ToString() },
            cancellationToken: cancellationToken));
        return exists is null ? PlayerStatus.NotCreated : PlayerStatus.Active;
    }
}
```

`using Dapper;`・`using Npgsql;`・`using VolTech.Server.Auth;` を付ける。`Ulid` は `System.Ulid`（Cysharp）。`player_id` は `char(26)` で、Npgsql は `string` を `text` として送るが、`char(26)` との比較は暗黙の変換で通る（通らなければ止まって報告する）。

### 3.9 `Infrastructure/UtaDatabase.cs`

```csharp
namespace Uta.Server.Infrastructure;

public static class UtaDatabase
{
    /// <summary>VolTech-MagicOnion のテーブル、Uta のテーブルの順に流す。失敗したら InvalidOperationException。</summary>
    public static void Migrate(string connectionString)
    {
        VoltechDatabase.Migrate(connectionString);

        var upgrader = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(
                typeof(UtaDatabase).Assembly,
                name => name.StartsWith("Uta.Server.Migrations.", StringComparison.Ordinal))
            .WithTransactionPerScript()
            .LogToConsole()
            .Build();

        var result = upgrader.PerformUpgrade();
        if (!result.Successful)
        {
            throw new InvalidOperationException($"Uta のマイグレーション {result.ErrorScript?.Name} に失敗した。", result.Error);
        }
    }
}
```

`using DbUp;`・`using VolTech.Server.Transaction;` を付ける。記録は DbUp の既定のテーブル `schemaversions`（VolTech-MagicOnion の `_voltech_migrations` とは別。11.2）。

### 3.10 `Migrations/0001_create_players.sql`

```sql
CREATE TABLE players (
    player_id char(26) PRIMARY KEY,
    created_at timestamptz NOT NULL DEFAULT now()
);
```

### 3.11 設定

`appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Voltech": {
    "GameDay": {
      "ResetHour": 12,
      "TimeZone": "Asia/Tokyo"
    }
  }
}
```

`ConnectionStrings` は書かない。`AccessTokenLifetime` も書かない（既定の 1 時間）。`Properties/launchSettings.json` は、プロファイル `Uta.Server`（`commandName` は `Project`、`applicationUrl` は `http://localhost:5000`、`environmentVariables` に `ASPNETCORE_ENVIRONMENT` = `Development`。`dotnet run` で user-secrets を読ませるため）の 1 つだけ。

### 3.12 `server/compose.yaml`・`server/.env.example`

`compose.yaml`: サービス `postgres` の 1 つ。`image: postgres:18-alpine`、`environment` は `POSTGRES_DB: uta`・`POSTGRES_PASSWORD: ${POSTGRES_PASSWORD:?server/.env に POSTGRES_PASSWORD を書く}`、`ports: ["5432:5432"]`、名前付きボリューム `uta-postgres` を `/var/lib/postgresql` にマウント（PostgreSQL 18 のイメージはこのパス）、`healthcheck` は `pg_isready -U postgres -d uta`（間隔 5 秒・5 回）。`.env.example` は `POSTGRES_PASSWORD=` の 1 行（値は空。`.env` は `.gitignore` 済み）。ローカルの接続文字列は `Host=localhost;Port=5432;Database=uta;Username=postgres;Password=<.env と同じ値>`。

### 3.13 `server/Uta.Server.Tests`

`Uta.Shared.Domain.Tests.csproj` の形（`OutputType` は `Exe`、`net10.0`、`xunit.v3`）に、`ImplicitUsings`・`Nullable` を有効、`<FrameworkReference Include="Microsoft.AspNetCore.App" />`、`<Using Include="Xunit" />` を足す。`PackageReference`: `xunit.v3`、`Microsoft.AspNetCore.Mvc.Testing`、`Microsoft.Extensions.TimeProvider.Testing`、`Testcontainers.PostgreSql`、`MagicOnion.Client`、`Grpc.Net.Client`、`Grpc.HealthCheck`、`NetArchTest.Rules`。`ProjectReference` は `..\Uta.Server\Uta.Server.csproj`。

`Integration/Support/PostgresFixture.cs`（xunit.v3 の `IAsyncLifetime` は `ValueTask`）:

```csharp
namespace Uta.Server.Tests.Integration.Support;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:18-alpine").Build();

    public string ConnectionString => container.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await container.StartAsync();
        UtaDatabase.Migrate(ConnectionString);
        // 2 回目は何もしない
        UtaDatabase.Migrate(ConnectionString);
    }

    public async ValueTask DisposeAsync()
    {
        await container.DisposeAsync();
    }

    /// <summary>1 行 1 列を読む。無い（または NULL の）ときは default。</summary>
    public async Task<T?> QueryScalarAsync<T>(string sql, params object[] parameters)
    {
        await using var dataSource = NpgsqlDataSource.Create(ConnectionString);
        await using var command = dataSource.CreateCommand(sql);
        foreach (var parameter in parameters)
        {
            command.Parameters.AddWithValue(parameter);
        }

        var value = await command.ExecuteScalarAsync();
        return value is null or DBNull ? default : (T)value;
    }

    /// <summary>結果を返さない SQL（INSERT など）を流す。</summary>
    public async Task ExecuteAsync(string sql, params object[] parameters)
    {
        await using var dataSource = NpgsqlDataSource.Create(ConnectionString);
        await using var command = dataSource.CreateCommand(sql);
        foreach (var parameter in parameters)
        {
            command.Parameters.AddWithValue(parameter);
        }

        await command.ExecuteNonQueryAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
```

`Integration/Support/UtaServerFactory.cs`:

```csharp
namespace Uta.Server.Tests.Integration.Support;

public sealed class UtaServerFactory : WebApplicationFactory<Program>
{
    public UtaServerFactory(PostgresFixture postgres, DateTimeOffset? now = null)
    {
        // Program.cs が builder.Configuration から読むので、環境変数で渡す（ConfigureAppConfiguration は間に合わない）。
        // 環境変数は user-secrets より優先されるので、開発機のローカル DB には落ちない。コンストラクタで渡すので、フィクスチャの初期化前には作れない
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", postgres.ConnectionString);
        Time = new FakeTimeProvider(now ?? new DateTimeOffset(2026, 11, 1, 3, 0, 0, TimeSpan.Zero));
    }

    public FakeTimeProvider Time { get; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Time);
        });
    }

    public GrpcChannel CreateGrpcChannel()
    {
        var handler = new ResponseVersionHandler { InnerHandler = Server.CreateHandler() };
        return GrpcChannel.ForAddress(Server.BaseAddress, new GrpcChannelOptions { HttpHandler = handler });
    }

    // TestServer のレスポンスは HTTP/1.1 になるので、gRPC のクライアントが受け付けるよう、リクエストの版に合わせる
    private sealed class ResponseVersionHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);
            response.Version = request.Version;
            return response;
        }
    }
}
```

`PostgresFixture` には `using Npgsql;`、`using Testcontainers.PostgreSql;`、`using Uta.Server.Infrastructure;` を付ける（`QueryScalarAsync` は基盤の `tests/VolTech.Server.Tests/Support/PostgresFixture.cs` と同じ形）。`UtaServerFactory` の必要な `using`（`Microsoft.AspNetCore.Hosting`、`Microsoft.AspNetCore.Mvc.Testing`、`Microsoft.AspNetCore.TestHost`、`Microsoft.Extensions.DependencyInjection`、`Microsoft.Extensions.DependencyInjection.Extensions`、`Microsoft.Extensions.Logging`、`Microsoft.Extensions.Time.Testing`、`Grpc.Net.Client`、`Testcontainers.PostgreSql`、`Uta.Server.Infrastructure`）に足りないものは、コンパイルエラーを見て足す。

テストの書き方（`Integration/DeviceRegistrationTests.cs`）:

- `UtaServerFactory` はテストごとに `await using var factory = new UtaServerFactory(postgres);` で作って捨てる（`DisposeAsync` が `WebApplicationFactory` にある）。`postgres`（`PostgresFixture`）はテストクラスのコンストラクタで受ける（`[Collection(PostgresCollection.Name)]`）。接続文字列は `UtaServerFactory` のコンストラクタが環境変数 `ConnectionStrings__Default` に入れる（`Program.cs` が `builder.Build()` の前に読むため）。同じコレクションのテストは直列に動くので、プロセス全体の環境変数でも衝突しない
- MagicOnion のクライアントは `using var channel = factory.CreateGrpcChannel(); var client = MagicOnionClient.Create<IDeviceService>(channel);`（`using MagicOnion.Client;`・`using VolTech.Server.Abstractions;`）
- 基盤の `tests/VolTech.Server.Tests/AuthTests.cs` と同じ書き方にする: 違う秘密の値のテストは、先に `RegisterAsync` で `DeviceId` を得てから `IssueTokenAsync(registered.DeviceId, secret + "x")` を呼ぶ。エラーは `await Assert.ThrowsAsync<RpcException>(async () => await client.IssueTokenAsync(...))` で受ける
- レスポンスヘッダーは `var call = client.RegisterAsync(secret); await call; var headers = await call.ResponseHeadersAsync;`（`UnaryResult<T>` の `ResponseHeadersAsync`）。`x-game-date` は基盤の `ServerTimeFilter` が全 RPC に付ける（`IDeviceService` は `[SkipAuth]` で、認証なしで呼べる）。付かない・`DATE_CHANGED` になるなど、これと違う振る舞いが出たら、テストを変えずに止まって報告する
- xunit.v3 の `xUnit1051`（`CancellationToken` を渡すよう促す警告）が出たら、`TestContext.Current.CancellationToken` を渡して直す（警告を抑止しない）
- 時刻は `factory.Time.SetUtcNow(new DateTimeOffset(2026, 11, 1, 3, 0, 0, TimeSpan.Zero))` で進める
- `players` への INSERT と `information_schema` の確認は、`PostgresFixture` の `ExecuteAsync`・`QueryScalarAsync` を使う

## 4. タスク

実装担当が守ること: 計画にない設計判断に出会ったら止まって報告する。ビルドの「警告 0」は、`.editorconfig` 以外の警告（NuGet の脆弱性など）も含む。出たら、コードや設定で直し、直せなければ止まって報告する（警告を抑止しない）。**コミットはしない**（メインのセッションが、タスクごとにコミットする）。1b（クライアント）が別のセッションで `client/uta/` と `shared/` の `package.json`・asmdef を触るので、このタスクの範囲のファイルだけを触る。

### タスク 1: ソリューションと版の管理（12章の手順 1）

- 触るファイル: 2章のタスク 1 の行
- 手順:
  1. 3.1・3.2・3.3・3.4 のファイルと `client/Directory.Build.props`（`<Project />`）を作る。`.gitignore` に `bin~/` と `obj~/` を足す
  2. `Uta.Shared.Domain.Tests.csproj` の `<PackageReference Include="xunit.v3" Version="4.0.2" />` から `Version` を消す（コメントは残す）
  3. `shared/Uta.Shared.Domain*/` の `bin/`・`obj/` を消してから `dotnet build Uta.slnx` を実行する。`EnforceCodeStyleInBuild` で `Uta.Shared.Domain` がビルドエラーになったら、**`.editorconfig` を緩めず、コードを `.editorconfig` に合わせて直す**（挙動は変えない）
- 境界: `shared/Uta.Shared.Domain/obj/` が作られないこと（`obj~/` に出る）
- 確かめ方: `dotnet build Uta.slnx` が警告 0・エラー 0。`dotnet test --project shared/Uta.Shared.Domain.Tests` がすべて通る（件数を報告する）。`shared/Uta.Shared.Domain/` に `obj/`・`bin/` が無く、`obj~/`・`bin~/` がある

### タスク 2: `Uta.Shared.Contracts`（手順 2）

- 触るファイル: 2章のタスク 2 の行、`Uta.slnx`（`/shared/` に `Uta.Shared.Contracts` を足す）
- 手順: 3.5 のとおりに作り、`Uta.slnx` に足す
- 確かめ方: `dotnet build Uta.slnx` が警告 0・エラー 0（NU1605 などの版の衝突が出ない。**出たら、版を変えずに止まって報告する**）

### タスク 3: `Uta.Server` の骨組み（手順 4 の前半）

- 触るファイル: 2章のタスク 3 の行、`Uta.slnx`（`/server/` に `Uta.Server` を足す）
- 手順: 3.6〜3.12 のとおりに作り、`Uta.slnx` に足す。続けて手で動かして確かめる:
  1. `server/.env` を作る（`.env.example` を写し、`POSTGRES_PASSWORD` にローカル用の乱数を入れる。**値をコミットせず、返答にも書かない**）
  2. `docker compose -f server/compose.yaml up -d` で起動し、healthy になるまで待つ
  3. `dotnet user-secrets set "ConnectionStrings:Default" "<3.12 の形の接続文字列>" --project server/Uta.Server`
  4. `dotnet run --project server/Uta.Server -- migrate` を 2 回流す。どちらも終了コード 0
  5. `dotnet run --project server/Uta.Server` を起動し（Bash のバックグラウンドで）、ポート 5000 で待ち受けていること（起動ログの `Now listening on: http://localhost:5000`）を確かめる
- 境界: 接続文字列が無ければ、起動もマイグレーションも例外で止まる。`migrate` の後ろに引数があれば終了コード 1
- 確かめ方: `dotnet build Uta.slnx` が警告 0・エラー 0。上の手順 4・5。`git status` に `server/.env` と接続文字列が出ない

### タスク 4: `Uta.Server.Tests`（手順 4 の後半）

- 触るファイル: 2章のタスク 4 の行、`Uta.slnx`（`/server/` に `Uta.Server.Tests` を足す）
- 前提: Docker が動いている
- 手順: 3.13 のとおりにプロジェクトとサポートのクラスを作り、5章のテストを書く
- 境界: NetArchTest が .NET 10・xunit.v3 で動かない、または `WebApplicationFactory<Program>` が起動しないときは、**自分で別の方法を選ばず、エラーを添えて止まって報告する**（8章の「使えなければ…に替える」は報告を受けてメインのセッションが決める）
- 確かめ方: `dotnet test --project server/Uta.Server.Tests` が全件通る。`dotnet build Uta.slnx` が警告 0・エラー 0

## 5. テスト

実行するコマンド（CLAUDE.md のものと、この計画で足すもの。足したものは完了時に CLAUDE.md の「ビルド・テスト・実行」へ書く）:

- `dotnet build Uta.slnx`
- `dotnet test --project shared/Uta.Shared.Domain.Tests`
- `dotnet test --project server/Uta.Server.Tests`（Docker が要る）

足すテスト（`server/Uta.Server.Tests`）:

`Architecture/ArchitectureTests.cs`（`Types.InAssembly(typeof(PlayerResolver).Assembly)`、層の絞りは `ResideInNamespaceStartingWith("Uta.Server.<層>")`）。[repository-layout.md](../wiki/architecture/repository-layout.md) 3.1 の依存の表の「禁止」の列から、層ごとに 1 ルールを書く（`NotHaveDependencyOnAny` の `GetResult().IsSuccessful` が `true`）:

- `Api_DoesNotDependOnInfrastructure`: `Api` は `Uta.Server.Infrastructure` に依存しない
- `Application_DoesNotDependOnOuterLayers`: `Application` は `Uta.Server.Api`・`Uta.Server.Infrastructure`・`Uta.Shared.Contracts` に依存しない
- `Domain_DoesNotDependOnOuterLayersOrDataAccess`: `Domain` は `Uta.Server.Api`・`Uta.Server.Application`・`Uta.Server.Infrastructure`・`Uta.Shared.Contracts`・`Npgsql`・`Dapper` に依存しない
- `Infrastructure_DoesNotDependOnApi`: `Infrastructure` は `Uta.Server.Api` に依存しない
- `InfrastructureNamespace_HasTypes`: `Uta.Server.Infrastructure` の型が 1 つ以上ある（空の集合で通ってしまうのを防ぐ）
- `Rule_DetectsViolation`: `Infrastructure` に `NotHaveDependencyOn("Npgsql")` を当てると `IsSuccessful` が `false`（`PlayerResolver` は Npgsql に依存する。ルールが違反を検出できる証明）

今は `Api`・`Application`・`Domain` に型が無いので、上の 3 つは空の集合で通る。各層に最初の型が入る計画が、その層の「型が 1 つ以上ある」テストを足す（`InfrastructureNamespace_HasTypes` と同じ形）。

`Integration/DeviceRegistrationTests.cs`（`[Collection(PostgresCollection.Name)]`。秘密の値は 16 文字以上の表示できる ASCII、例 `0123456789abcdef-secret`。3.13 の書き方に従う）:

- `Register_ThenIssueToken_ReturnsIdsAndToken`: 骨組み全体（Kestrel 相当・MagicOnion・DB）を通す。`RegisterAsync` の `DeviceId`・`PlayerId` が 26 文字で `Ulid.TryParse` できる。`IssueTokenAsync` の `AccessToken` が小文字の 16 進 64 文字、`ExpiresAt` が `Time.GetUtcNow()` の 1 時間後（秒の単位で一致。`AccessTokenLifetime` が既定の 1 時間であること）
- `IssueToken_WrongSecret_ReturnsUnauthenticated`: 違う秘密の値で `RpcException`（`StatusCode.Unauthenticated`）。基盤のテストと重なるが、Uta のホスト（`Program.cs` の組み立て）を通ってもエラーのステータスが返ることの確認
- `Register_TooShortSecret_ReturnsInvalidArgument`: `"short"` で `StatusCode.InvalidArgument`（上と同じ目的）
- `Players_AreNotCreatedUntilRowExists`: 登録した `PlayerId` を `factory.Services` のスコープから取った `IPlayerResolver` に渡すと `NotCreated`。`PostgresFixture.ExecuteAsync("INSERT INTO players (player_id) VALUES ($1)", playerId)` した後は `Active`（Dapper の `PlayerResolver` を通る）
- `GameDay_ResetsAtNoonJst`: `factory.Time.SetUtcNow` を `2026-11-01T02:59:00Z`（JST 11:59）にして `RegisterAsync` を呼ぶとレスポンスヘッダー `x-game-date` が `2026-10-31`、`2026-11-01T03:00:00Z`（JST 12:00）に進めて呼ぶと `2026-11-01`（`call.ResponseHeadersAsync` の `Metadata.GetValue("x-game-date")`）。`appsettings.json` の `Voltech:GameDay` が読めていることの確認でもある（読めずに落ちたら、`ResetHour` を上書きして通さず、止まって報告する）
- `Health_Check_ReturnsServing`: `new Health.HealthClient(channel).CheckAsync(new HealthCheckRequest())`（`Grpc.Health.V1`）の `Status` が `Serving`（`Grpc.AspNetCore.HealthChecks` の登録が `Program.cs` で効いていることの確認）
- `Migration_CreatesPlayersTable`: `PostgresFixture.QueryScalarAsync<int>("SELECT character_maximum_length FROM information_schema.columns WHERE table_name = $1 AND column_name = $2", "players", "player_id")` が 26、`QueryScalarAsync<string>("SELECT data_type FROM information_schema.columns WHERE table_name = $1 AND column_name = $2", "players", "player_id")` が `character`（`PostgresFixture` が 2 回流しても失敗しないこと自体も、フィクスチャの初期化で確かめる）

`Uta.Shared.Domain.Tests` の既存のテストは、期待値を変えずに通す。

**自動のテストにしないもの**（「Unity 側のテストは必須にしない」10.4。Unity の側は 1b）:

| 確かめること | 方法 | 担当 |
|---|---|---|
| Cloud Run での gRPC のヘルスチェック・h2c（Kestrel の `Http2` のみの設定は `TestServer` を通らないので、タスク 3 の `dotnet run` の起動ログでだけ確かめる） | 範囲外。デプロイする計画で確かめる（未確認のまま残す） | — |

## 6. 未決事項

なし（`requesting-design-review` に出して `ready` にする）。

## 7. 完了したら直す wiki

実装の後（CLAUDE.md「実装前のルール」の 8）、同じ PR で次を直す:

- [repository-layout.md](../wiki/architecture/repository-layout.md) 12章と [game-spec.md](../wiki/overview/game-spec.md) の「実装計画の単位と順」の 1 番目を、1a「通信の疎通（サーバー）」（手順 1・2・4。待つもの: なし）と 1b「通信の疎通（クライアント）」（手順 5・6。手順 6 は 1a のマージ後。完了条件 0 は 1b が受け持つ）に分ける
- 8章の要検証のうち、確かめたもの（NetArchTest が .NET 10・xunit.v3 で動く、版の一致のうちサーバー側）を消し、結果を該当ページ（3.1 など）に書く。サーバー側の版を 2.1 に書き、未決事項「NuGet・UPM の具体の版」のサーバー側を解決にする
- 3.1 の依存の表の前の段落（許可の一覧から書く）を、表の後の箇条書き（禁止を 1 つずつ）に合わせて直す
- 2章のツリーの「（未作成）」の印を実際に合わせる（ツリーは主なファイルだけなので、`.env.example`・`appsettings.json` などは足さない。`server/.gitkeep` は消す。`Uta.slnx`・`Directory.Build.props`・`Directory.Packages.props`・`Uta.Shared.Contracts`・`server/` の 3 つ）。12章の手順 4 の「アーキテクチャテスト 1 件と、端末登録の結合テスト 1 件」を実際の件数に直す
- [server-foundation.md](../wiki/architecture/server/server-foundation.md) の ZLogger の版の記述を、決めた版（2.5.10）に直す
- [glossary.md](../wiki/glossary.md): 「サインアップ」「サインイン」の範囲の定義は、サインアップ・サインインの範囲がユーザーの判断待ちので、この計画では足さない
- CLAUDE.md の「ビルド・テスト・実行」に、動いたコマンドを足す。リポジトリ構成の「未作成」の表記も合わせる
