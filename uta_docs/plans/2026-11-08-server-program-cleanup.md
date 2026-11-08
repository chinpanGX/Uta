---
title: 実装計画: サーバーの Program.cs の登録を並べ替え、DI の検査を常に有効にする
status: draft
updated: 2026-11-08
spec: ../wiki/architecture/repository-layout.md, ../wiki/architecture/server/server-foundation.md
---

# 実装計画: サーバーの Program.cs の登録を並べ替え、DI の検査を常に有効にする

## この計画で決めたこと

- **前提**: [PR #51](https://github.com/chinpanGX/Uta/pull/51)（Catalog をやめた。ADR 0020 のサーバー側）は 2026-11-08 に main へマージ済み。計画のブランチは main から切った（積み PR にしない）。この計画は #51 の後の `server/Uta.Server/Program.cs`（`ICardCatalog`・`ProduceCatalog` の Factory 2 行が残り、ほかの Catalog の登録が無い形）を前提に書いた
- **登録の並べ替え**（2026-11-08。ユーザーの判断。案は入れない（今の並びのまま）と比べ、新しい UseCase の置き場所が決まるほうを選んだ）: `migrate` の分岐の後を、ホスト → マスタ → Infrastructure → 機能ごとのアプリケーション（プロデュース・ガチャ・サポートカード・プレイヤー）→ 基盤 → ヘルスチェック の順に並べ、空行と見出しのコメントで区切る。並べ替えで動きは変わらない（同じ型の登録が 2 つ無く、`AddVoltechServer` の中の登録は `TryAdd`。テストの `ConfigureTestServices` は `Program.cs` の登録の後に流れる）。完成形は 3.1
- **`migrate` の分岐の位置は変えない**（接続文字列を読んだ直後、マスタの読み込みより前）。`migrate` はマスタも DI も使わない
- **機能ごとの拡張メソッド（`AddProduce()` など）には分けない**。登録は 30 行ほどで、見出しのコメントで機能の区切りは読める。分けると、1 か所で読めた寿命の組み合わせ（シングルトンがスコープを抱えていないか）が 5 ファイルに散る。機能ごとの登録が 1 機能で 10 行を超えたら見直す
- **`ProduceEngine` はプロデュースの区切りに置く**（Shared.Domain の育成の計算。マスタの区切りは `MemoryDatabase` と、マスタから Shared.Domain の型を組み立てる Factory の 2 行だけにする）。`ItemGranter` はガチャ、`ProduceSessionDiscarder` はプロデュースの区切りに置く（置き場のフォルダ `Application/Gacha/`・`Application/Produce/` に合わせる。サインインからも使うが、持ち主はフォルダで決める）。`IRandomSource`（`SecureRandomSource`）は実装が `Infrastructure/` にあるので Infrastructure の区切りに置く
- **DI の検査を環境によらず有効にする**（2026-11-08。ユーザーの判断。`ValidateOnBuild` だけ（`ServiceProvider` は `ValidateScopes` のときだけスコープの検査をするので、シングルトンがスコープの型を抱える誤りを Production で見逃す）・入れない、と比べて選んだ。起動時に全登録を 1 回組み立てる分、起動が少し遅くなるが、登録は約 30 件で小さい（推測で、計っていない））: `builder.Host.UseDefaultServiceProvider(options => { options.ValidateScopes = true; options.ValidateOnBuild = true; })` をホストの区切りの先頭に足す。ASP.NET Core は Development のときだけ両方を有効にする（既定）。結合テストは `WebApplicationFactory` の既定の Development で動くので、**今の登録は既に両方の検査を通っている**（全結合テストが通っている）。足すことで変わるのは、Production（Cloud Run）でも登録漏れ・寿命の誤りが `builder.Build()` で止まること
- **`ValidateOnBuild` で落ちないことの確かめ**（足す前に確かめたこと。2026-11-08）:
  - `AddVoltechServer`（VolTech-MagicOnion `11d9b06`、`src/VolTech.Server/Hosting/VoltechServerExtensions.cs`）が登録するのは、オプションと検証（`VoltechServerOptionsValidator`）・`TimeProvider.System`・`GameClock`・`ITransactions` → `Transactions` のシングルトンと、`AddGrpc()`・`AddMagicOnion()`。環境で登録を変える分岐は無い（`IsDevelopment` などは基盤の `src/` に無い）
  - `AddCommonResponse<AppCommonDto, PlayerDiffCollector>()` は `CommonResponseRegistration` のインスタンスをシングルトンで登録するだけで、`PlayerDiffCollector` は DI に登録しない（`CommonResponseRegistration.CreateCollector` が `ActivatorUtilities.CreateInstance` でリクエストのスコープから作る）。`ValidateOnBuild` は登録された型しか見ないので、`PlayerDiffCollector` は検査の対象にならず、落ちない。`PlayerDiffCollector` の引数の解決は、今どおり実行時に `CommonResponseTests` が確かめる
  - MagicOnion のサービスとフィルターも DI に登録されない（MagicOnion がリクエストごとに作る）ので、同じく対象外
  - よって、Development で全結合テストが通っていることが、Production で `ValidateOnBuild` が落ちないことの根拠になる。実装では、タスク 2 の全テストと、Production で検査が効くことのテスト（5章）で確かめる
- **検査が Production で効くことをテストで固定する**（5章）。既存の結合テストは Development で動くので、`UseDefaultServiceProvider` の行を消しても全部通ってしまい、この変更を守れない。Production で、スコープの型を抱えたシングルトンを足すと起動が失敗することを 1 件だけ確かめる
- **wiki**: [repository-layout.md](../wiki/architecture/repository-layout.md) 12章「サーバーの骨組み」の `Program.cs` の行に、登録の並び順と DI の検査を書き足す（タスク 3）。[server-foundation.md](../wiki/architecture/server/server-foundation.md) は `Program.cs` の骨組みを 12章に任せているので直さない

## ユーザーの確認が要ること

なし（並べ替えと DI の検査の 2 つは、2026-11-08 にユーザーが「入れる」「両方を常に有効」と決めた。上の [決定] に移した）。

## 1. 目的と前提

`server/Uta.Server/Program.cs` の DI の登録を、読む順に並べ替え、起動時の DI の検査を Production でも効かせる。動き（RPC・`migrate`・ヘルスチェック）は変えない。

- 関係する ADR: [0020](../wiki/decisions/0020-masterdata-referenced-outside-shared-domain.md)（マスタを `MemoryDatabase` のシングルトンで登録する）、[0011](../wiki/decisions/0011-deploy-google-cloud.md)（Cloud Run。Production の環境）
- 設計: [repository-layout.md](../wiki/architecture/repository-layout.md) 12章「サーバーの骨組み」、[server-foundation.md](../wiki/architecture/server/server-foundation.md)（ZLogger・`AddCommonResponse`）

### 決めないこと（範囲外）

- 登録する型・寿命を変えること。`Program.cs` の登録の中身（型・寿命・引数）は 1 つも変えず、並べ替えと検査の 1 行だけにする
- `using` の並び（今のまま。並べ替えで使わなくなる `using` は無い）
- `migrate` の処理と、接続文字列の読み方
- `appsettings.json`・`launchSettings.json`

## 2. 触るファイル

| 操作 | パス | 中身 | タスク |
|---|---|---|---|
| 変更 | `server/Uta.Server/Program.cs` | 登録を 3.1 の順に並べ替える | 1 |
| 変更 | `server/Uta.Server/Program.cs` | ホストの区切りの先頭に `UseDefaultServiceProvider` を足す（3.1） | 2 |
| 作成 | `server/Uta.Server.Tests/Integration/ServiceProviderValidationTests.cs` | Production で DI の検査が効くことのテスト（3.2） | 2 |
| 変更 | `uta_docs/wiki/architecture/repository-layout.md` | 12章「サーバーの骨組み」の `Program.cs` の行（3.3） | 3 |

## 3. 型とシグネチャ

### 3.1 `Program.cs` の完成形（タスク 2 の後）

1〜47 行目（`using`、`builder` と `connectionString`、`migrate` の分岐）と、`var app = builder.Build();` から後は変えない。48〜81 行目（`builder.Services.AddSingleton(MasterDataLoader.Load());` からヘルスチェックの行まで）を次に置き換える。タスク 1 では、`// ホスト` の区切りの `builder.Host.UseDefaultServiceProvider(...)` の文（とその上のコメント 1 行）を除いた形にする。

```csharp
// ホスト
// DI の検査（スコープの寿命・起動時の組み立て）を、Development だけでなく Production でも有効にする
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});
builder.Logging.ClearProviders();
builder.Logging.AddZLoggerConsole(options => options.UseJsonFormatter());
builder.WebHost.ConfigureKestrel(kestrel => kestrel.ConfigureEndpointDefaults(listen => listen.Protocols = HttpProtocols.Http2));

// マスタ（ADR 0020。起動時に 1 回読み、シングルトンで持つ）
builder.Services.AddSingleton(MasterDataLoader.Load());
builder.Services.AddSingleton<ICardCatalog>(sp => CardCatalogFactory.Create(sp.GetRequiredService<MemoryDatabase>()));
builder.Services.AddSingleton(sp => ProduceCatalogFactory.Create(sp.GetRequiredService<MemoryDatabase>(), sp.GetRequiredService<ICardCatalog>()));

// Infrastructure（DB・リポジトリ・乱数）
builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));
builder.Services.AddSingleton<IRandomSource, SecureRandomSource>();
builder.Services.AddScoped<IPlayerResolver, PlayerResolver>();
builder.Services.AddScoped<IPlayerRepository, PlayerRepository>();
builder.Services.AddScoped<IIdolRepository, IdolRepository>();
builder.Services.AddScoped<ISupportCardRepository, SupportCardRepository>();
builder.Services.AddScoped<IGachaRepository, GachaRepository>();
builder.Services.AddScoped<IProduceSessionRepository, ProduceSessionRepository>();

// プロデュース
builder.Services.AddSingleton<ProduceEngine>();
builder.Services.AddSingleton<ProduceViewBuilder>();
builder.Services.AddScoped<ProduceSessionDiscarder>();
builder.Services.AddScoped<StartProduceUseCase>();
builder.Services.AddScoped<GetProduceStateUseCase>();
builder.Services.AddScoped<AbandonProduceUseCase>();
builder.Services.AddScoped<ProduceCompletion>();
builder.Services.AddScoped<ProduceOperationUseCase>();

// ガチャ
builder.Services.AddScoped<ItemGranter>();
builder.Services.AddScoped<GetGachasUseCase>();
builder.Services.AddScoped<DrawUseCase>();
builder.Services.AddScoped<ExchangeUseCase>();

// サポートカード
builder.Services.AddScoped<LevelUpUseCase>();
builder.Services.AddScoped<LimitBreakUseCase>();

// プレイヤー
builder.Services.AddScoped<SignUpUseCase>();
builder.Services.AddScoped<SignInUseCase>();

// 基盤（VolTech-MagicOnion）。PlayerDiffCollector は DI に登録しない（基盤がスコープから作る。server-foundation.md）
builder.Services.AddVoltechServer(options => builder.Configuration.GetSection("Voltech").Bind(options))
    .AddCommonResponse<AppCommonDto, PlayerDiffCollector>();

// ヘルスチェック
// 登録した確認が 0 件だと Check が Unknown を返すので、常に Healthy の確認を 1 つ入れる（DB の確認は入れない。11.4）
builder.Services.AddGrpcHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy());
```

- 区切りの間は空行 1 つ。`var app = builder.Build();` の前も空行 1 つ（今と同じ）
- 登録の行の文字は、今の行と 1 文字も変えない（並びだけを変える）。今の `builder.Services.Add` で始まる 29 行と、`AddVoltechServer` の次の行の `.AddCommonResponse<AppCommonDto, PlayerDiffCollector>();` がすべて残り、増えも減りもしないこと
- `UseDefaultServiceProvider` は `Microsoft.Extensions.Hosting` の `HostingHostBuilderExtensions` の拡張メソッド。`Microsoft.NET.Sdk.Web` の暗黙の `using` に入っているので、`using` は足さない

### 3.2 `ServiceProviderValidationTests`

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
    // 既定（Development）では ASP.NET Core が検査するので、Production で Program.cs の設定が効くことを確かめる
    [Fact]
    public async Task Build_ProductionWithSingletonCapturingScoped_Throws()
    {
        await using var factory = new UtaServerFactory(postgres, configureServices: services => services.AddSingleton<ScopedCapturingSingleton>());
        await using var production = factory.WithWebHostBuilder(builder => builder.UseEnvironment(Environments.Production));

        var exception = Assert.ThrowsAny<Exception>(() => production.Server);

        Assert.Contains("Cannot consume scoped service", exception.ToString());
    }

    private sealed class ScopedCapturingSingleton(IPlayerRepository players)
    {
        public IPlayerRepository Players { get; } = players;
    }
}
```

- `IPlayerRepository` は `Uta.Server.Domain.Player` にある（scoped で登録されている）。スコープの型を引数に取るシングルトンを足すと、`ValidateScopes` と `ValidateOnBuild` が両方有効なときだけ `builder.Build()` が `AggregateException`（中に `Cannot consume scoped service 'Uta.Server.Domain.Player.IPlayerRepository' from singleton ...`）を投げる。`WebApplicationFactory` が包む例外の型は版で変わりうるので、型は `ThrowsAny<Exception>` で受け、`ToString()`（内側の例外の文も含む）に `Cannot consume scoped service` が含まれることで確かめる
- `builder.Build()` は DB に接続しないので、PostgreSQL のコンテナは接続文字列を渡すためだけに使う（`UtaServerFactory` のコンストラクタが要る）
- 失敗したファクトリーの `DisposeAsync` は例外を投げない（ホストが作られていないので止めるものが無い）。`await using` のままでよい

### 3.3 repository-layout.md 12章

「サーバーの骨組み」の 1 つ目の箇条（`- \`Program.cs\`: \`NpgsqlDataSource\` を 1 つ登録し、…ここで足す`）の直後に、次の箇条を足す（既存の箇条は変えない）。

```markdown
- `Program.cs` の登録の並び（2026-11-08。[計画](../../plans/2026-11-08-server-program-cleanup.md)）: `migrate` の分岐の後に、ホスト（DI の検査・ログ・Kestrel）→ マスタ（`MemoryDatabase` と Shared.Domain の型の Factory）→ Infrastructure（DB・リポジトリ・乱数）→ 機能ごとのアプリケーション（プロデュース・ガチャ・サポートカード・プレイヤー。置き場のフォルダで分ける）→ 基盤（`AddVoltechServer`）→ ヘルスチェック の順に並べ、見出しのコメントで区切る。機能ごとの拡張メソッドには分けない（登録は約 30 行で、寿命の組み合わせを 1 か所で読むため）。DI の検査（`ValidateScopes`・`ValidateOnBuild`）は環境によらず有効にする（ASP.NET Core の既定は Development だけ）。DI に登録しない `PlayerDiffCollector`・MagicOnion のサービスは検査の対象外で、実行時に結合テストが確かめる
```

## 4. タスク

### タスク 1: 登録を並べ替える

- 触るファイル: `server/Uta.Server/Program.cs`
- 手順: 48〜81 行目を、3.1 のコードから `// ホスト` の直後のコメント 1 行と `builder.Host.UseDefaultServiceProvider(...)` の文（6 行）を除いたものに置き換える。ほかの行は変えない
- 境界: 登録の行の数が変わっていないこと（置き換えの前後で `grep -c "builder.Services.Add" server/Uta.Server/Program.cs` が同じ 29）
- 確かめ方: `dotnet build Uta.slnx`（警告 0・エラー 0）、`dotnet test --project server/Uta.Server.Tests`（Docker が要る）がすべて通る

### タスク 2: DI の検査を常に有効にし、テストを足す

- 触るファイル: `server/Uta.Server/Program.cs`、`server/Uta.Server.Tests/Integration/ServiceProviderValidationTests.cs`（作成）
- 手順:
  1. `Program.cs` の `// ホスト` の行の直後に、3.1 のコメント 1 行と `builder.Host.UseDefaultServiceProvider(...)` の文を足す
  2. 3.2 のテストのファイルを作る
- 境界: 既存の結合テストがすべて通ること（Development で既に検査が効いているので、落ちるなら登録の誤りではなく `UseDefaultServiceProvider` の置き方の誤り。止まって報告する）
- 確かめ方:
  - `dotnet build Uta.slnx`（警告 0・エラー 0）
  - `dotnet test --project server/Uta.Server.Tests` がすべて通り、`Build_ProductionWithSingletonCapturingScoped_Throws` が通る
  - 追加のテストが設定を守っていることを 1 回だけ確かめる: `UseDefaultServiceProvider` の文を一時的に消すと `Build_ProductionWithSingletonCapturingScoped_Throws` が落ちること（Production では既定で検査しないため、`production.Server` が例外を投げない）。確かめたら戻す。結果を実装メモに書く
- 止まる条件: 1 で Production の起動（既存のテストは Development なので、テストでは起きない）や、`WithWebHostBuilder` の `UseEnvironment` が `Program.cs` の環境に届かず、消しても通ってしまうとき。テストの書き方を変えずに止まって報告する

### タスク 3: wiki に書く

- 触るファイル: `uta_docs/wiki/architecture/repository-layout.md`
- 手順: 3.3 の箇条を足す。frontmatter の `updated` を実装した日にする
- 確かめ方: `python -I .claude/skills/linting-wiki/lint_wiki.py` がエラー 0

## 5. テスト

| テスト | 種類 | 確かめること |
|---|---|---|
| `ServiceProviderValidationTests.Build_ProductionWithSingletonCapturingScoped_Throws` | 結合（Docker が要る） | Production でも、スコープの型を抱えたシングルトンの登録で起動が失敗する（`ValidateScopes`・`ValidateOnBuild` が効いている） |
| 既存の結合テスト・アーキテクチャテスト・単体テスト（全件） | 結合・単体 | 並べ替えと検査の追加で動きが変わらない。Development で全登録が検査を通る |

実行するコマンド:

- `dotnet build Uta.slnx`
- `dotnet test --project server/Uta.Server.Tests`

Production の Cloud Run での起動は、この計画では確かめない（デプロイは別。テストで Production の環境名での組み立てまでを確かめる）。

## 6. 未決事項

なし。

## 7. 実装メモ

（実装を始めるときに足す）
