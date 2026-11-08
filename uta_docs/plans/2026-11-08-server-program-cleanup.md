---
title: 実装計画: サーバーの起動の組み立てを ServerHost に移し、DI の検査を常に有効にする
status: draft
updated: 2026-11-09
spec: ../wiki/architecture/repository-layout.md, ../wiki/architecture/server/server-foundation.md
---

# 実装計画: サーバーの起動の組み立てを ServerHost に移し、DI の検査を常に有効にする

## この計画で決めたこと

- **前提**: [PR #51](https://github.com/chinpanGX/Uta/pull/51)（Catalog をやめた。ADR 0020 のサーバー側）は 2026-11-08 に main へマージ済み。計画のブランチは main から切った（積み PR にしない）。この計画は #51 の後の `server/Uta.Server/Program.cs`（`ICardCatalog`・`ProduceCatalog` の Factory 2 行が残り、ほかの Catalog の登録が無い形）を前提に書いた
- **`Program.cs` は起動の入口だけにする**（2026-11-09。ユーザーの判断）: `Program.cs` は `return ServerHost.Run(args);` と `public partial class Program;` だけにする。起動の組み立て（接続文字列の読み込み・`migrate`・ホストの設定・DI の登録・エンドポイント）は、新しく作る `server/Uta.Server/Hosting/ServerHost.cs`（`internal static class ServerHost`。名前空間 `Uta.Server.Hosting`）に閉じる。`Run` の中は、手順ごとの private static メソッド（`Migrate`・`ConfigureHost`・`AddServices`・`MapEndpoints`）を呼ぶ形にし、`Run` を読めば起動の流れが分かるようにする。完成形は 3.1・3.2
  - 名前: naming.md「型の名前に `Uta` を付けない」に従い `ServerHost`（`UtaServerHost` にしない）。役割（サーバーのホストを組み立てて動かす）の名前にした
  - 置き場: 層（`Api`・`Application`・`Domain`・`Infrastructure`）の外の `Hosting/`。起動の組み立てはすべての層を参照するので、どの層にも入らない（repository-layout.md 3章「起動の組み立て…はすべてを参照してよい」に `Hosting/ServerHost.cs` を足す）。アーキテクチャテストが見るのは `Uta.Server.{層}` の名前空間だけ（`ArchitectureTests` の 12 行目）なので、`Uta.Server.Hosting` はどのルールにも当たらない
  - `internal` にする。呼ぶのは同じアセンブリの `Program` だけで、テストは `WebApplicationFactory<Program>` を通して使う
  - 状態を持たない static class（フィールドなし）。CLAUDE.md の static の規則（`client/`・`shared/`・`masterdata/` が対象）にも当たらない形
  - **`WebApplicationFactory<Program>` が引き続き使えること**: `WebApplicationFactory` は、入口（`Program` の `Main`）を別のスレッドで呼び、ホストが組み立てられる（`builder.Build()`）のを `DiagnosticListener` で捕まえて差し替える。入口から別のクラスを経由して `WebApplication.CreateBuilder` → `Build()` を呼んでも捕まえられる、と .NET のソースの知識で判断した（確かめていない）。タスク 1 で、既存の結合テストがすべて通ることで確かめる。通らなければ止まる（タスク 1 の止まる条件）
- **DI の登録の並べ替え**（2026-11-08。ユーザーの判断。案は入れない（今の並びのまま）と比べ、新しい UseCase の置き場所が決まるほうを選んだ）: `AddServices` の中を、マスタ → Infrastructure → 機能ごとのアプリケーション（プロデュース・ガチャ・サポートカード・プレイヤー）→ 基盤 → ヘルスチェック の順に並べ、空行と見出しのコメントで区切る。ホストの設定（DI の検査・ログ・Kestrel）は `IServiceCollection` の外なので `ConfigureHost` に分ける。並べ替えで動きは変わらない（同じ型の登録が 2 つ無く、`AddVoltechServer` が `TryAdd` で入れる型（`TimeProvider`・`GameClock`・`ITransactions`）を Uta は登録しない。テストの `ConfigureTestServices` は Uta の登録の後に流れる）
- **`AddServices` は 1 つのメソッドにし、機能ごとのメソッドや拡張メソッド（`AddProduce()` など）には分けない**。登録は 30 行ほどで、見出しのコメントで機能の区切りは読める。分けると、1 か所で読めた寿命の組み合わせ（シングルトンがスコープを抱えていないか）が散る。機能ごとの登録が 1 機能で 10 行を超えたら見直す
- **区切りへの振り分け**: `ProduceEngine` はプロデュースの区切りに置く（Shared.Domain の育成の計算。マスタの区切りは `MemoryDatabase` と、マスタから Shared.Domain の型を組み立てる Factory の 2 行だけにする）。`ItemGranter` はガチャ、`ProduceSessionDiscarder` はプロデュースの区切りに置く（置き場のフォルダ `Application/Gacha/`・`Application/Produce/` に合わせる。サインインからも使うが、持ち主はフォルダで決める）。`IRandomSource`（`SecureRandomSource`）は、リポジトリと同じく Application の IF を `Infrastructure/` で実装したものなので Infrastructure の区切りに置く（Infrastructure の区切りは、DB の接続と IF の実装だけ。`Infrastructure/` にあっても、マスタの読み込み・Factory はマスタ、`PlayerDiffCollector` は基盤の区切り）
- **`migrate` の順序は変えない**: `Run` の中で、接続文字列を読んだ直後、ホストの設定とマスタの読み込みより前に `migrate` を分ける。`migrate` はマスタも DI も使わない
- **DI の検査を環境によらず有効にする**（2026-11-08。ユーザーの判断。`ValidateOnBuild` だけ（`ServiceProvider` は `ValidateScopes` のときだけスコープの検査をするので、シングルトンがスコープの型を抱える誤りを Production で見逃す）・入れない、と比べて選んだ。起動時に全登録の依存の解決を 1 回調べる分、起動が少し遅くなる。登録の総数はフレームワークを含めて数百だが、`ValidateOnBuild` はインスタンスを作らず依存の解決だけを調べるので、影響は小さい（計っていない））: `builder.Host.UseDefaultServiceProvider(options => { options.ValidateScopes = true; options.ValidateOnBuild = true; })` を `ConfigureHost` の先頭に足す。ASP.NET Core は環境名が Development のときだけ両方を有効にする（既定）。ここで言う Production は ASP.NET Core の環境名で、`ASPNETCORE_ENVIRONMENT` を設定しないときの既定値。Cloud Run の環境（[ADR 0011](../wiki/decisions/0011-deploy-google-cloud.md)・repository-layout.md 11.1 の呼び方では dev。本番は分けない）には `ASPNETCORE_ENVIRONMENT` を設定する記述が wiki に無いので、環境名は Production になる。結合テストは `WebApplicationFactory` の既定の Development で動くので、**今の登録は既に両方の検査を通っている**。今日の登録の誤りはテストで止まるので、足すことで増えるのは次の 3 つ:
  1. 実行時の `ValidateScopes`: ルートの `IServiceProvider` からスコープの型を解決する誤りが、Development 以外でも「黙って抱え込む」から「例外で止まる」に変わる。今の Uta のコードと基盤（`AuthFilter`・`CommonResponseRegistration` はリクエストの `ServiceProvider` を使う）にルートから解決する経路は無く、RPC の経路は結合テストで同じ検査を通っているので、Cloud Run で新たに例外になる経路は無い
  2. 将来、環境で分岐する登録（`builder.Environment.IsDevelopment()` など）を足したとき、Development 以外だけの登録も `builder.Build()` で検査される
  3. テストを回さずにデプロイしたとき、DI に登録した型の依存の漏れ・寿命の誤りが起動で止まる（要求の途中で落ちない）。DI に登録しない MagicOnion のサービスが使う型（新しい UseCase の `AddScoped` の書き忘れなど）は、今どおり最初の要求で落ちる
- **`ValidateOnBuild` で落ちないことの確かめ**（足す前に確かめたこと。2026-11-08）:
  - `AddVoltechServer`（VolTech-MagicOnion `11d9b06`、`src/VolTech.Server/Hosting/VoltechServerExtensions.cs`）が登録するのは、オプションと検証（`VoltechServerOptionsValidator`）・`TimeProvider.System`・`GameClock`・`ITransactions` → `Transactions` のシングルトンと、`AddGrpc()`・`AddMagicOnion()`。環境で登録を変える分岐は無い（`IsDevelopment` などは基盤の `src/` に無い）
  - `AddCommonResponse<AppCommonDto, PlayerDiffCollector>()` は `CommonResponseRegistration` のインスタンスをシングルトンで登録するだけで、`PlayerDiffCollector` は DI に登録しない（`CommonResponseRegistration.CreateCollector` が `ActivatorUtilities.CreateInstance` でリクエストのスコープから作る）。`ValidateOnBuild` は登録された型しか見ないので、`PlayerDiffCollector` は検査の対象にならず、落ちない。`PlayerDiffCollector` の引数の解決は、今どおり実行時に `CommonResponseTests` が確かめる
  - MagicOnion のサービスとフィルターも DI に登録されない（MagicOnion がリクエストごとに作る）ので、同じく対象外
  - よって、`UtaServerFactory` が Development で `builder.Build()` を通っていること（`ValidateOnBuild` は登録された全記述子を見るので、テストが叩く RPC の範囲には関係ない）が、Production で `ValidateOnBuild` が落ちないことの根拠になる。成り立つ前提は、テストが変える登録が Production の記述子の解決を変えないこと。`UtaServerFactory.ConfigureWebHost` が変えるのは 3 つ: `TimeProvider`・`IRandomSource` はインスタンスで置き換えるだけ、`ConfigureLogging(logging => logging.ClearProviders())` はログのプロバイダー（ZLogger のコンソール）の記述子を外す。よって ZLogger のプロバイダーの登録だけは結合テストで検査されないので、タスク 2 で環境名 Production のまま `dotnet run` で起動し、`builder.Build()` を通ることを確かめる
- **検査が Production で効くことをテストで固定する**（5章）。既存の結合テストは Development で動くので、`UseDefaultServiceProvider` の行を消しても全部通ってしまい、この変更を守れない。Production で、スコープの型を抱えたシングルトンを足すと起動が失敗することを 1 件だけ確かめる
- **wiki**: repository-layout.md の 2章（フォルダの木）・3章（起動の組み立て）・12章（サーバーの骨組み）と、server-foundation.md の `Program.cs` と書いた 3 か所を、`Hosting/ServerHost.cs` に合わせて直す（タスク 3。文は 3.4）
- **後に続く計画**（2026-11-09。ユーザーの判断）: 同じブランチの [サーバーの改名の計画](2026-11-09-server-naming-renames.md) を、この計画の実装の後に、同じセッション・同じブランチで実装する。この計画は今の名前（`ProduceViewBuilder`・`UtaDatabase`・`UtaServerFactory`）のまま書き、改名は後の計画が行う。この計画で `ProduceViewBuilder` の登録と `UtaDatabase.Migrate` の呼び出しは `Program.cs` から `Hosting/ServerHost.cs` に移る

## ユーザーの確認が要ること

なし（並べ替えと DI の検査は 2026-11-08 に、`Program.cs` を起動の入口だけにすることは 2026-11-09 に、ユーザーが決めた。上の [決定] に移した）。

## 1. 目的と前提

`server/Uta.Server/Program.cs` を起動の入口だけにし、起動の組み立てを `Hosting/ServerHost.cs` に移して、手順ごとのメソッドと、読む順に並べた DI の登録にする。起動時の DI の検査を、環境名が Production（Cloud Run の dev 環境）でも効かせる。動き（RPC・`migrate`・ヘルスチェック）は変えない。例外は、スコープの型をルートから解決する誤りの失敗の仕方で、Development 以外でも例外で止まるようになる（今のコードにその経路は無い。「この計画で決めたこと」）。

- 関係する ADR: [0020](../wiki/decisions/0020-masterdata-referenced-outside-shared-domain.md)（マスタを `MemoryDatabase` のシングルトンで登録する）、[0011](../wiki/decisions/0011-deploy-google-cloud.md)（Cloud Run。環境は dev の 1 つで、ASP.NET Core の環境名は Production）
- 設計: [repository-layout.md](../wiki/architecture/repository-layout.md) 3章・12章「サーバーの骨組み」、[server-foundation.md](../wiki/architecture/server/server-foundation.md)（ZLogger・`AddCommonResponse`）、[naming.md](../wiki/architecture/naming.md)（型の名前に `Uta` を付けない）

### 決めないこと（範囲外）

- 登録する型・寿命を変えること。登録の中身（型・寿命・引数）は 1 つも変えず、置き場所と並びと検査の 1 文だけにする
- `migrate` の処理と、接続文字列の読み方（文はそのまま `ServerHost` に移す）
- `appsettings.json`・`launchSettings.json`
- Cloud Run に `ASPNETCORE_ENVIRONMENT` を設定するかどうか（今は wiki に記述が無く、設定しない形。repository-layout.md 11.4 に書き足すかは、この計画では決めない）
- ADR 0020 の本文（「今の `Program.cs` の形」と書いてあるが、ADR は書き換えない）

## 2. 触るファイル

| 操作 | パス | 中身 | タスク |
|---|---|---|---|
| 作成 | `server/Uta.Server/Hosting/ServerHost.cs` | 起動の組み立て（3.1。タスク 1 は `UseDefaultServiceProvider` の文を除く） | 1 |
| 変更 | `server/Uta.Server/Program.cs` | 入口だけにする（3.2） | 1 |
| 変更 | `server/Uta.Server/Hosting/ServerHost.cs` | `ConfigureHost` の先頭に `UseDefaultServiceProvider` を足す | 2 |
| 作成 | `server/Uta.Server.Tests/Integration/ServiceProviderValidationTests.cs` | Production で DI の検査が効くことのテスト（3.3） | 2 |
| 変更 | `uta_docs/wiki/architecture/repository-layout.md` | 2章のフォルダの木、3章の起動の組み立て、12章「サーバーの骨組み」（3.4） | 3 |
| 変更 | `uta_docs/wiki/architecture/server/server-foundation.md` | `Program.cs` と書いた 3 か所（3.4） | 3 |

## 3. 型とシグネチャ

### 3.1 `Hosting/ServerHost.cs` の完成形（タスク 2 の後）

BOM 付き UTF-8・LF。`server/` なのでファイルスコープの名前空間を使う。`using` は今の `Program.cs` の 1〜22 行目をそのまま写す（並びも同じ。`Program.cs` からは消える）。`IConfiguration`・`IServiceCollection`・`WebApplication` は `Microsoft.NET.Sdk.Web` の暗黙の `using`（`ImplicitUsings` は `Uta.Server.csproj` で有効）に入っているので足さない。

```csharp
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
        services.AddSingleton<ProduceViewBuilder>();
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
```

- 登録の行は、今の `Program.cs` の 48〜81 行目の行から、先頭の `builder.Services.` を `services.` に、`AddVoltechServer` の引数の `builder.Configuration` を `configuration` に変えただけにする（ほかの文字は変えない）。今の `builder.Services.Add` で始まる 29 行が `services.Add` で始まる 29 行になり、`.AddCommonResponse<AppCommonDto, PlayerDiffCollector>();` の 1 行と合わせて、増えも減りもしないこと
- `migrate` の分岐の中身・接続文字列の例外の文は、今の `Program.cs` 25〜46 行目と同じ文字にする（`Migrate` に移すので、`return` の値と `Console.Error.WriteLine` の文は変えない）
- `UseDefaultServiceProvider` は `Microsoft.Extensions.Hosting` の `HostingHostBuilderExtensions` の拡張メソッドで、暗黙の `using` に入っている
- 名前空間 `Uta.Server.Hosting` の中で `using VolTech.Server.Hosting;` を書いてよい（型の名前は `VoltechServerExtensions` などで、`ServerHost` と重ならない）

### 3.2 `Program.cs` の完成形

BOM 付き UTF-8・LF。今の 89 行をすべて次に置き換える。

```csharp
using Uta.Server.Hosting;

return ServerHost.Run(args);

public partial class Program;
```

- `public partial class Program;` は、結合テストの `WebApplicationFactory<Program>`（`UtaServerFactory`）が型として使うので残す

### 3.3 `ServiceProviderValidationTests`

```csharp
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
        await using var factory = new UtaServerFactory(postgres, configureServices: services => services.AddSingleton<ScopedCapturingSingleton>());
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
```

- `IPlayerRepository` は `Uta.Server.Domain.Player` にある（scoped で登録されている）。スコープの型を引数に取るシングルトンを足すと、`ValidateScopes` と `ValidateOnBuild` が両方有効なときだけ `builder.Build()` が `AggregateException`（中に `Cannot consume scoped service 'Uta.Server.Domain.Player.IPlayerRepository' from singleton ...`）を投げる。`WebApplicationFactory` が包む例外の型は版で変わりうるので、型は `ThrowsAny<Exception>` で受け、`ToString()`（内側の例外の文も含む）に `Cannot consume scoped service` と、自分の型の名前 `IPlayerRepository`・`ScopedCapturingSingleton` が含まれることで確かめる（文言は版で変わりうるが、変わればテストが落ちて気づける。型の名前で、何が検出されたかも読める）
- `builder.Build()` は DB に接続しないので、PostgreSQL のコンテナは接続文字列を渡すためだけに使う（`UtaServerFactory` のコンストラクタが要る）
- 失敗したファクトリーの `DisposeAsync` は例外を投げない（ホストが作られていないので止めるものが無い）。`await using` のままでよい

### 3.4 wiki の文

**repository-layout.md**

1. 2章のフォルダの木: `│     ├─ Api/                #   MagicOnion のサービス実装` の行の直前に、次の 1 行を足す（列の位置は前後の行に揃える）

   ```text
   │     ├─ Hosting/            #   起動の組み立て（ServerHost。Program.cs は ServerHost.Run を呼ぶだけ）
   ```

2. 3章: `- 起動の組み立て（\`Program.cs\`・\`AddVoltechServer()\`）はすべてを参照してよい` を、次に置き換える

   ```markdown
   - 起動の組み立て（`Program.cs`・`Hosting/ServerHost.cs`・`AddVoltechServer()`）はすべてを参照してよい。`Hosting/` は層ではないので、アーキテクチャテストの対象にしない
   ```

3. 12章「サーバーの骨組み」の 1 つ目の箇条（`- \`Program.cs\`: \`NpgsqlDataSource\` を 1 つ登録し、…ここで足す`）の先頭の `` `Program.cs`: `` を `` `Program.cs`・`Hosting/ServerHost.cs`（2026-11-09 に、組み立てを `ServerHost` に移した）: `` に置き換え（残りの文は変えない）、その直後に次の 3 つの箇条を足す

   ```markdown
   - `Program.cs` は起動の入口だけ（`return ServerHost.Run(args);` と、テストの `WebApplicationFactory<Program>` 用の `public partial class Program;`）（2026-11-09。[計画](../../plans/2026-11-08-server-program-cleanup.md)）。`ServerHost.Run` は、接続文字列を読む → `migrate` なら `Migrate` だけを流して終わる → `ConfigureHost`（DI の検査・ログ・Kestrel）→ `AddServices`（DI の登録）→ `Build` → `MapEndpoints` → `Run` の順に、手順ごとの private static メソッドを呼ぶ
   - `AddServices` の登録の並び（2026-11-08）: マスタ（`MemoryDatabase` と Shared.Domain の型の Factory）→ Infrastructure（DB の接続と、Domain・Application・基盤の IF を `Infrastructure/` で実装したもの。リポジトリ・`IPlayerResolver`・`IRandomSource`）→ 機能ごとのアプリケーション（プロデュース・ガチャ・サポートカード・プレイヤー。`Application/` の置き場のフォルダで分け、Shared.Domain の計算（`ProduceEngine`）は使う機能の区切りに置く）→ 基盤（`AddVoltechServer`）→ ヘルスチェック の順に並べ、見出しのコメントで区切る。1 つのメソッドにし、機能ごとのメソッドや拡張メソッドには分けない（登録は約 30 行で、寿命の組み合わせを 1 か所で読むため）
   - DI の検査（2026-11-08）: `ValidateScopes`・`ValidateOnBuild` は環境名によらず有効にする（ASP.NET Core の既定は Development だけ）。DI に登録しない `PlayerDiffCollector`・MagicOnion のサービスとフィルターは検査の対象外で、実行時に結合テストが確かめる
   ```

**server-foundation.md**

4. 9 行目の `` `Program.cs`・マイグレーション・設定の骨組みは `` を `` `Program.cs`・`Hosting/ServerHost.cs`・マイグレーション・設定の骨組みは `` に置き換える
5. 23 行目の `` `Program.cs` で `AddZLoggerConsole` を構成し `` を `` `Hosting/ServerHost.cs` の `ConfigureHost` で `AddZLoggerConsole` を構成し `` に置き換える
6. 101 行目の `` `Program.cs` で `AddVoltechServer(...).AddCommonResponse<AppCommonDto, PlayerDiffCollector>()` を呼ぶ `` を `` `Hosting/ServerHost.cs` の `AddServices` で `AddVoltechServer(...).AddCommonResponse<AppCommonDto, PlayerDiffCollector>()` を呼ぶ `` に置き換える

## 4. タスク

### タスク 1: 起動の組み立てを ServerHost に移す

- 触るファイル: `server/Uta.Server/Hosting/ServerHost.cs`（作成）、`server/Uta.Server/Program.cs`
- 手順:
  1. `Hosting/ServerHost.cs` を、3.1 のコードから `ConfigureHost` の先頭のコメント 1 行と `builder.Host.UseDefaultServiceProvider(...)` の文 5 行（計 6 行）を除いたもので作る
  2. `Program.cs` を 3.2 に置き換える
- 境界: 登録の行の数が変わっていないこと（`git show HEAD:server/Uta.Server/Program.cs | grep -c "builder.Services.Add"` と `grep -c "services.Add" server/Uta.Server/Hosting/ServerHost.cs` が同じ 29）
- 確かめ方: `dotnet build Uta.slnx`（警告 0・エラー 0）、`dotnet test --project server/Uta.Server.Tests`（Docker が要る）がすべて通る
- 止まる条件: 結合テストが `WebApplicationFactory` のホストの組み立てで失敗するとき（例: 入口がホストを組み立てずに終わった、という例外）。入口から `ServerHost` を経由すると `WebApplicationFactory` がホストを捕まえられない、ということなので、書き方を変えずに止まって報告する（計画を直す）

### タスク 2: DI の検査を常に有効にし、テストを足す

- 触るファイル: `server/Uta.Server/Hosting/ServerHost.cs`、`server/Uta.Server.Tests/Integration/ServiceProviderValidationTests.cs`（作成）
- 手順:
  1. `ConfigureHost` の先頭（`builder.Logging.ClearProviders();` の前）に、3.1 のコメント 1 行と `builder.Host.UseDefaultServiceProvider(...)` の文を足す
  2. 3.3 のテストのファイルを作る
- 境界: 既存の結合テストがすべて通ること（Development で既に検査が効いているので、落ちるなら登録の誤りではなく `UseDefaultServiceProvider` の置き方の誤り。止まって報告する）
- 確かめ方:
  - `dotnet build Uta.slnx`（警告 0・エラー 0）
  - `dotnet test --project server/Uta.Server.Tests` がすべて通り、`Build_ProductionWithSingletonCapturingScoped_Throws` が通る
  - 追加のテストが設定を守っていることを 1 回だけ確かめる: `UseDefaultServiceProvider` の文を一時的に消すと `Build_ProductionWithSingletonCapturingScoped_Throws` が落ちること（Production では既定で検査しないため、`production.Server` が例外を投げない）。確かめたら戻す。結果を実装メモに書く
  - 環境名 Production でローカルに起動できることを 1 回だけ確かめる（ZLogger のプロバイダーを含む全登録が検査を通る。DB には接続しないので、接続文字列はダミーでよい）。Bash で次を実行し、出力に `Now listening on` が出ることを確かめる（20 秒で `timeout` が止める。終了コード 124 は想定どおり）。出なかったら出力を実装メモに書いて止まる:

    ```bash
    dotnet build server/Uta.Server
    ASPNETCORE_ENVIRONMENT=Production ConnectionStrings__Default='Host=localhost;Database=uta' timeout 20 dotnet run --project server/Uta.Server --no-launch-profile --no-build
    taskkill //IM Uta.Server.exe //F
    ```

    `--no-launch-profile` は `launchSettings.json` の Development を使わないため。ポート 5000 を別のプロセスが使っていると `Failed to bind` で落ちるので、そのときは先に `taskkill //IM Uta.Server.exe //F` を流してからやり直す。Windows では `timeout` が止めても `Uta.Server.exe` が残ることがあるので、最後の `taskkill` で止める（残っていなければ「見つからない」と出るだけ）。2026-11-09 に、この計画の前の `Program.cs` で回し、`"Hosting environment: Production"` と `Now listening on: http://localhost:5000` が出ることを確かめた

- 止まる条件: `WithWebHostBuilder` の `UseEnvironment` が `ServerHost` の環境に届かず、`UseDefaultServiceProvider` の文を消しても `Build_ProductionWithSingletonCapturingScoped_Throws` が通ってしまうとき。テストの書き方を変えずに止まって報告する

### タスク 3: wiki に書く

- 触るファイル: `uta_docs/wiki/architecture/repository-layout.md`、`uta_docs/wiki/architecture/server/server-foundation.md`
- 手順: 3.4 の 1〜6 を行う。2 つのページの frontmatter の `updated` を実装した日にする
- 確かめ方: `python -I .claude/skills/linting-wiki/lint_wiki.py` がエラー 0。`grep -n "Program.cs" uta_docs/wiki/architecture/repository-layout.md uta_docs/wiki/architecture/server/server-foundation.md` で出る行が、3.4 で書いた文（`Program.cs` と `Hosting/ServerHost.cs` を並べたもの、入口だけと書いたもの）だけであること

## 5. テスト

| テスト | 種類 | 確かめること |
|---|---|---|
| `ServiceProviderValidationTests.Build_ProductionWithSingletonCapturingScoped_Throws` | 結合（Docker が要る） | Production でも、スコープの型を抱えたシングルトンの登録で起動が失敗する（`ValidateScopes`・`ValidateOnBuild` が効いている） |
| 既存の結合テスト・アーキテクチャテスト・単体テスト（全件） | 結合・単体 | 組み立てを `ServerHost` に移しても `WebApplicationFactory<Program>` が使え、並べ替えと検査の追加で動きが変わらない。Development で全登録が検査を通る |

実行するコマンド:

- `dotnet build Uta.slnx`
- `dotnet test --project server/Uta.Server.Tests`

`migrate` は、結合テストのフィクスチャ（`PostgresFixture`）が `UtaDatabase.Migrate` を直接呼ぶだけなので、`ServerHost.Migrate` の経路（`dotnet run --project server/Uta.Server -- migrate`）はテストで通らない。文を移すだけなので、この計画では確かめない。Cloud Run（dev）での起動も確かめない（デプロイは別。テストで環境名 Production での組み立てまでを確かめる）。

## 6. 未決事項

なし。

## 7. 実装メモ

（実装を始めるときに足す）
