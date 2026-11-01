---
title: 実装計画: プレイヤーのサインアップ・サインイン（サーバー）
status: draft
updated: 2026-11-01
spec: ../wiki/architecture/api-design.md, ../wiki/architecture/server/server-foundation.md, ../wiki/overview/game-spec.md
---

# 実装計画: プレイヤーのサインアップ・サインイン（サーバー）

ブレストで未決事項を決め、設計レビュー（1 回目）の指摘を反映した版。

## 1. 目的と前提

- 何を実装するか: `IPlayerService` の `SignUpAsync`（プレイヤーの作成）と `SignInAsync`（所持データの全件取得）を、サーバーと `Uta.Shared.Contracts` に作り、結合テストで通す。仕様は [api-design.md](../wiki/architecture/api-design.md) 1章「サインアップとサインイン」・2章（エラー）・3章（`players`）。
- 位置づけ: [game-spec.md](../wiki/overview/game-spec.md)「実装計画の単位と順」の **5a**（5 の最初の部分。masterdata を待たずに作れる範囲）。
- 前提: 1a（[2026-11-01-connectivity-check-server.md](2026-11-01-connectivity-check-server.md)）が済んでいる。端末の登録・トークンの発行・`PlayerResolver`・`players` テーブル（`player_id`・`created_at` だけ）・`UtaServerFactory`・`PostgresFixture` がある。
- 関係する ADR: [0002](../wiki/decisions/0002-magiconion.md)、[0008](../wiki/decisions/0008-shared-domain-and-onion.md)、[0010](../wiki/decisions/0010-database.md)、[0012](../wiki/decisions/0012-migration-dbup.md)
- **この計画で決めないこと**:
  - 初期の所持品の付与（マスタと所持テーブルが要る。5 の計画で `SignUpAsync` に足す）
  - バージョン違いのプロデュースの破棄（`produce_sessions` が無い。プロデュースの計画で本実装。この計画の `HasActiveProduce`・`DiscardedProduce` は常に false）
  - 「`SignUpAsync` 以外を未作成なら弾く」共通のフィルター（3 つ目の Uta の RPC の計画で作る）
  - クライアント側（`handledCodes` に `PLAYER_NOT_CREATED` を渡すこと、`ErrorDefinition` への登録、サインアップ・サインインを呼ぶユースケース。1b の後の計画）
  - 育成アイドル・サポートカードの所持一覧（それぞれのテーブルの計画で `SignInResponse` に `[Key]` を後ろへ足す）

## 2. 触るファイル

| 操作 | パス | 中身 |
|---|---|---|
| 作成 | `shared/Uta.Shared.Contracts/Player/IPlayerService.cs` | サービス IF |
| 作成 | `shared/Uta.Shared.Contracts/Player/SignUpResponse.cs`・`SignInResponse.cs` | DTO |
| 作成 | `shared/Uta.Shared.Contracts/Player/PlayerErrors.cs` | `ErrorCode` の定数 |
| 作成 | 上の 4 ファイルと `Player/` フォルダの `.meta` | 4章タスク 1 の手順で作る |
| 削除 | `shared/Uta.Shared.Contracts/ContractsAssemblyMarker.cs` とその `.meta` | 印のコメントの指示どおり（サービス IF・DTO を足したら消す） |
| 作成 | `server/Uta.Server/Migrations/0002_add_player_columns.sql` | `players` に列を足す |
| 作成 | `server/Uta.Server/Application/Player/PlayerSnapshot.cs`・`IPlayerRepository.cs`・`SignUpUseCase.cs`・`SignInUseCase.cs` | Application |
| 作成 | `server/Uta.Server/Infrastructure/PlayerRepository.cs` | Dapper の実装 |
| 作成 | `server/Uta.Server/Api/PlayerService.cs` | MagicOnion のサービス実装 |
| 変更 | `server/Uta.Server/Program.cs` | DI の登録（3 行） |
| 変更 | `server/Uta.Server.Tests/Architecture/ArchitectureTests.cs` | Api・Application に型があるテストを 2 件足す |
| 作成 | `server/Uta.Server.Tests/Integration/Support/PlayerClient.cs` | テストの支援（登録・トークン・ヘッダー） |
| 作成 | `server/Uta.Server.Tests/Integration/PlayerSignUpSignInTests.cs` | 結合テスト |
| 変更 | `uta_docs/wiki/architecture/api-design.md`・`repository-layout.md` | 実装後のズレがあれば反映（完了時） |

`Uta.Shared.Contracts.csproj` は変えない（`VolTech.Server.Abstractions` 経由で MagicOnion.Abstractions と MessagePack.Annotations が使える。基盤の `IDeviceService` と同じ形）。

## 3. 型とシグネチャ

### 3.1 Contracts（C# 9・netstandard2.1・`Nullable` なし。`init`・`record`・`required` を使わない。[repository-layout.md](../wiki/architecture/repository-layout.md) 4章）

DTO はコンストラクタと読み取り専用プロパティで書く。MessagePack が読めるよう、コンストラクタに `[SerializationConstructor]` を付ける。

```csharp
// Player/IPlayerService.cs   namespace Uta.Shared.Contracts.Player  （ルート名前空間はアセンブリ名。フォルダに合わせる）
public interface IPlayerService : IService<IPlayerService>
{
    UnaryResult<SignUpResponse> SignUpAsync();
    UnaryResult<SignInResponse> SignInAsync();
}

// Player/SignUpResponse.cs
[MessagePackObject]
public sealed class SignUpResponse
{
    [SerializationConstructor]
    public SignUpResponse(bool created) { Created = created; }
    [Key(0)] public bool Created { get; }   // 今回作ったか。2 回目以降は false
}

// Player/SignInResponse.cs   （7 引数のコンストラクタに [SerializationConstructor]。引数の順は Key の番号）
[MessagePackObject]
public sealed class SignInResponse
{
    [Key(0)] public int Level { get; }
    [Key(1)] public int Exp { get; }              // 今のレベルの中の経験値（produce.md 7章）
    [Key(2)] public int Gold { get; }
    [Key(3)] public int SupportPoints { get; }
    [Key(4)] public int GachaCurrency { get; }
    [Key(5)] public bool HasActiveProduce { get; }   // この計画では常に false
    [Key(6)] public bool DiscardedProduce { get; }   // この計画では常に false
}

// Player/PlayerErrors.cs   using VolTech.Server.Abstractions;
public static class PlayerErrors
{
    public static readonly ErrorCode NotCreated = new ErrorCode("uta", "PLAYER_NOT_CREATED");
}
```

`IPlayerService` に `[SkipAuth]` は付けない（認証あり・冪等性の対象）。Contracts の `.cs` は BOM 付き UTF-8・LF。

### 3.2 DB（`0002_add_player_columns.sql`）

```sql
ALTER TABLE players
    ADD COLUMN level int NOT NULL DEFAULT 1,
    ADD COLUMN exp int NOT NULL DEFAULT 0,
    ADD COLUMN gold int NOT NULL DEFAULT 0,
    ADD COLUMN support_points int NOT NULL DEFAULT 0,
    ADD COLUMN gacha_currency int NOT NULL DEFAULT 0;
```

DbUp は `Migrations/` の SQL をファイル名の順に流す。埋め込みリソースの取り込み方は `0001_create_players.sql` と同じにする（`UtaDatabase.cs` と `Uta.Server.csproj` を見て、`0001` と同じ指定が `0002` にも効くことを確かめる。効かなければ同じ指定を足す）。

### 3.3 Application（`namespace Uta.Server.Application.Player`。`Contracts`・`Infrastructure`・`Api` を参照しない。`TxContext`（`VolTech.Server.Transaction`）は参照してよい）

```csharp
public sealed record PlayerSnapshot(int Level, int Exp, int Gold, int SupportPoints, int GachaCurrency);

public interface IPlayerRepository
{
    /// <summary>行が無ければ作る。作ったら true、既にあれば false。</summary>
    Task<bool> InsertIfAbsentAsync(TxContext tx, Ulid playerId);
    /// <summary>行が無ければ null。</summary>
    Task<PlayerSnapshot?> FindAsync(TxContext tx, Ulid playerId);
}

public sealed class SignUpUseCase(IPlayerRepository players)
{
    public Task<bool> ExecuteAsync(TxContext tx, Ulid playerId) => players.InsertIfAbsentAsync(tx, playerId);
}

public sealed class SignInUseCase(IPlayerRepository players)
{
    public Task<PlayerSnapshot?> ExecuteAsync(TxContext tx, Ulid playerId) => players.FindAsync(tx, playerId);
}
```

トランザクションは Api が開き、`TxContext` を引数でユースケース → リポジトリへ渡す（[server-foundation.md](../wiki/architecture/server/server-foundation.md)「SQL を書く層」）。

### 3.4 Infrastructure（`PlayerRepository : IPlayerRepository`。Dapper。`CommandDefinition` に `tx.Transaction` と `tx.CancellationToken` を渡し、接続は `tx.Connection`）

```sql
-- InsertIfAbsentAsync（ExecuteAsync の戻り値、つまり影響した行数が 1 なら true）
INSERT INTO players (player_id) VALUES (@playerId) ON CONFLICT (player_id) DO NOTHING
-- FindAsync
SELECT level, exp, gold, support_points AS SupportPoints, gacha_currency AS GachaCurrency
FROM players WHERE player_id = @playerId
```

パラメーター `playerId` は `playerId.ToString()`（`char(26)`）。`FindAsync` は、列と同じ名前のプロパティを持つ private なクラス `Row`（`Level`・`Exp`・`Gold`・`SupportPoints`・`GachaCurrency` の `int` プロパティ）に `QuerySingleOrDefaultAsync<Row>` で読み（SELECT の別名は `level AS Level` のように全列に付ける）、null でなければ `PlayerSnapshot` に詰め替える。

### 3.5 Api（`namespace Uta.Server.Api`）

```csharp
public sealed class PlayerService(ITransactions transactions, SignUpUseCase signUp, SignInUseCase signIn)
    : ServiceBase<IPlayerService>, IPlayerService
{
    public async UnaryResult<SignUpResponse> SignUpAsync()
    {
        var playerId = Context.GetAuthContext().PlayerId;   // using VolTech.Server.Hosting;
        return await transactions.RunAsync(Context, async tx => new SignUpResponse(await signUp.ExecuteAsync(tx, playerId)));
    }

    public async UnaryResult<SignInResponse> SignInAsync()
    {
        var playerId = Context.GetAuthContext().PlayerId;
        return await transactions.RunAsync(Context, async tx =>
        {
            var player = await signIn.ExecuteAsync(tx, playerId)
                ?? throw new BusinessException(PlayerErrors.NotCreated, "player not created");   // using VolTech.Server.Abstractions;
            return new SignInResponse(player.Level, player.Exp, player.Gold, player.SupportPoints, player.GachaCurrency, false, false);
        });
    }
}
```

- 両方とも `ITransactions.RunAsync` の中で動かす。これで本体と同じトランザクション、`x-request-id` による冪等性（同じ ID の再送は保存済みのレスポンスを返し、本体を呼ばない）が効く。
- `BusinessException` は `RunAsync` の中で投げる。トランザクションはロールバックされ（`last_responses` の保存もされない）、基盤のフィルターが `FAILED_PRECONDITION` と `google.rpc.ErrorInfo`（domain `uta`・reason `PLAYER_NOT_CREATED`）に変換する。
- 冪等性の帰結: `x-request-id` が無い・形が違うと `INVALID_ARGUMENT`。別の RPC で使った ID の再利用も `INVALID_ARGUMENT`。同じ ID の再送は、最初の応答（`SignUp` なら `Created = true`）をそのまま返す。
- `PLAYER_NOT_CREATED` の判定は `players` の行の有無（`FindAsync` の null）。`AuthContext.PlayerCreated` は使わない（`SignInAsync` はどのみち行を読むので、判定を 1 か所にする）。

### 3.6 `Program.cs` の登録

`builder.Services.AddScoped<IPlayerRepository, PlayerRepository>();`・`AddScoped<SignUpUseCase>();`・`AddScoped<SignInUseCase>();` を、`AddScoped<IPlayerResolver, PlayerResolver>()` の次に足す。`PlayerService` は登録しない（`MapMagicOnionService()` が `ServiceBase` の実装を自動で拾う）。

## 4. タスク

各タスクが終わったとき、`dotnet build Uta.slnx`（警告 0・エラー 0）が通る。

1. **Contracts**: 3.1 の 4 ファイルを作り、`ContractsAssemblyMarker.cs` とその `.meta` を削除する。`.meta` は、同じフォルダの既存の `.meta`（`ContractsAssemblyMarker.cs.meta`・`package.json.meta`）の書式を写し、`guid` だけ新しい 32 桁の小文字 16 進（`python -c "import uuid;print(uuid.uuid4().hex)"`）にする。`Player/` フォルダの `.meta` は `folderAsset: yes` の書式（`Uta.Shared.Contracts` の中に他のフォルダの `.meta` が無ければ、Unity の一般的な `DefaultImporter` の書式）。確かめ: `dotnet build Uta.slnx`
2. **マイグレーション**: 3.2 の SQL を作る。確かめ: 既存の結合テストが通る（`dotnet test --project server/Uta.Server.Tests`）
3. **Application・Infrastructure**: 3.3・3.4 を作る。確かめ: `dotnet build Uta.slnx`
4. **Api と登録**: 3.5・3.6 を作る。確かめ: `dotnet build Uta.slnx`。アーキテクチャテストの 2 件（5章）を足して通す
5. **結合テスト**: 5章を作る。確かめ: `dotnet test --project server/Uta.Server.Tests` が全件通る（Docker が要る）

境界: `SignUpAsync` の 2 回目以降（別の `x-request-id`）は何もしない。`SignInAsync` は未作成なら `PLAYER_NOT_CREATED`。認証なし・無効なトークンは `UNAUTHENTICATED`（基盤）。

## 5. テスト

### 支援（`Integration/Support/PlayerClient.cs`）

```csharp
public sealed class PlayerClient
{
    public static async Task<PlayerClient> CreateAsync(GrpcChannel channel, string secret = "0123456789abcdef-secret");  // 端末の登録 → トークンの発行
    public string PlayerId { get; }
    // 同じチャンネルで IPlayerService を呼ぶ。headers に authorization: Bearer <token> と x-request-id を付ける
    public Task<T> CallAsync<T>(Func<IPlayerService, UnaryResult<T>> call, string requestId);
}
```

`MagicOnionClient.Create<IPlayerService>(channel).WithHeaders(new Metadata { { "authorization", "Bearer " + token }, { "x-request-id", requestId } })` で呼ぶ（ヘッダー名は `ApiHeaders` の定数）。`x-request-id` は英数字とハイフンの 1〜64 文字（`RequestIdRules`）。テストでは `"req-1"`、`"req-2"` のように固定の文字列を渡す。`x-app-version`・`x-game-date` は付けない（この計画のサーバーは見ない）。

### 結合テスト（`PlayerSignUpSignInTests`。`[Collection(PostgresCollection.Name)]`、`UtaServerFactory` と `PostgresFixture` の使い方は `DeviceRegistrationTests` と同じ）

| テスト名 | 手順 | 期待 |
|---|---|---|
| `SignUp_NewPlayer_CreatesRow` | 登録 → `SignUpAsync`（`req-1`） | `Created == true`。`SELECT count(*) FROM players WHERE player_id = $1` が 1 |
| `SignUp_Twice_DoesNothing` | `SignUpAsync`（`req-1`）→ `SignUpAsync`（`req-2`） | 2 回目は `Created == false`。行は 1 のまま |
| `SignUp_SameRequestId_ReturnsStoredResponse` | `SignUpAsync`（`req-1`）を 2 回 | 2 回目も `Created == true`（保存済みのレスポンス）。行は 1 |
| `SignIn_AfterSignUp_ReturnsDefaults` | `SignUpAsync` → `SignInAsync`（`req-2`） | `Level == 1`・`Exp == 0`・`Gold == 0`・`SupportPoints == 0`・`GachaCurrency == 0`・`HasActiveProduce == false`・`DiscardedProduce == false` |
| `SignIn_BeforeSignUp_ReturnsPlayerNotCreated` | 登録 → `SignInAsync` | `RpcException`。`StatusCode == FailedPrecondition`。`Trailers` の `grpc-status-details-bin`（`GetValueBytes`）の UTF-8 化に `PLAYER_NOT_CREATED` と `uta` を含む。`players` に行は無い |
| `SignIn_ThenSignUp_ThenSignIn_Works` | `SignInAsync`（失敗）→ `SignUpAsync` → `SignInAsync`（それぞれ別の `x-request-id`） | 最後の `SignInAsync` が成功する（クライアントの経路の確認） |
| `SignIn_WithoutToken_Unauthenticated` | トークンなしで `IPlayerService.SignInAsync` | `StatusCode == Unauthenticated` |
| `SignIn_WithoutRequestId_InvalidArgument` | トークンあり・`x-request-id` なしで `SignInAsync` | `StatusCode == InvalidArgument` |
| `Migration_AddsPlayerColumns` | `information_schema.columns` を `players` で引く | `level`・`exp`・`gold`・`support_points`・`gacha_currency` の 5 列がある |

### アーキテクチャテスト（`ArchitectureTests.cs` に足す。`InfrastructureNamespace_HasTypes` と同じ形）

- `ApiNamespace_HasTypes`: `Assert.NotEmpty(LayerTypes("Api").GetTypes())`
- `ApplicationNamespace_HasTypes`: `Assert.NotEmpty(LayerTypes("Application").GetTypes())`

既存の依存のルール 4 件は、Api・Application に型が入った後も通ること（`Application` は `Uta.Shared.Contracts` を参照しない。`PlayerErrors` は Api だけが使う）。

実行: `dotnet test --project server/Uta.Server.Tests`（Docker が要る）。Unity 側のビルド・インポートの確認は 1b のセッションの仕事（この計画は `.meta` を書くまで）。

## 6. 未決事項

なし（ブレストの決定は下のとおり）。

- 初期付与は入れない（案 A）。`SignInResponse` は `players` の列と 2 値だけ、`GetPlayerAsync` は作らない。`PLAYER_NOT_CREATED` は `SignInAsync` が直接投げる。破棄・再開は常に false。起動の流れは畳まない（以上、2026-11-01 ユーザーの判断）。
- 判定の材料は `players` の行の有無（レビュー指摘への対応。決定 3 の「直接投げる・フィルターを作らない」は変えない）。`TxContext` は引数で渡す。DTO はコンストラクタと読み取り専用プロパティ（repository-layout 4章に従う）。`.meta` はこの計画で書く。
