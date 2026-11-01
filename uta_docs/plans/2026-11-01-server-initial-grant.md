---
title: 実装計画: プレイヤーの初期付与と所持テーブル（サーバー）
status: ready
updated: 2026-11-01
spec: ../wiki/architecture/api-design.md, ../wiki/architecture/server/gacha-support-server.md, ../wiki/systems/idol-support.md, ../wiki/overview/game-spec.md
---

# 実装計画: プレイヤーの初期付与と所持テーブル（サーバー）

## 1. 目的と前提

- 何を実装するか: `SignUpAsync` が、プレイヤーを作ったときに初期の所持品（R の育成アイドル全員・初期サポートカード・ガチャ通貨・ゴールド・サポート強化pt）を同じトランザクションで付与する。所持テーブル `player_idols`・`player_support_cards` を作り、`SignInAsync` が所持の育成アイドル・サポートカードも返す。仕様は [idol-support.md](../wiki/systems/idol-support.md) 4章 IDL-05、[game-spec.md](../wiki/overview/game-spec.md)「初期の所持品」、DTO は [api-design.md](../wiki/architecture/api-design.md) 1章。
- 位置づけ: [game-spec.md](../wiki/overview/game-spec.md)「実装計画の単位と順」の**順 5 の 1/3**。続きは [ガチャ](2026-11-01-server-gacha.md)、[サポートカードの強化](2026-11-01-server-support-upgrade.md)。この 3 本は同じブランチ・同じ PR に順に積む。
- 前提: 5a（[2026-11-01-player-signup-signin.md](2026-11-01-player-signup-signin.md)）と 2b（マスタ 31 テーブル。サーバーに `MemoryDatabase` を `Infrastructure/MasterDataLoader.cs` が読む。`Program.cs` が `MemoryDatabase` を Singleton で登録済み）がマージ済み。
- 関係する ADR: [0007](../wiki/decisions/0007-masterdata-with-addons.md)、[0008](../wiki/decisions/0008-shared-domain-and-onion.md)、[0010](../wiki/decisions/0010-database.md)、[0012](../wiki/decisions/0012-migration-dbup.md)
- **この計画で決めないこと**: ガチャ・交換・強化（後の 2 本）。トレーニング・覚醒（MVP で作らない。`training`・`awakening` 列は 0 固定で持つだけ）。プロデュースの破棄・再開（順 6。`HasActiveProduce`・`DiscardedProduce` は常に false のまま）。クライアント側。
- 5a で作ったプレイヤー（初期付与を受けていない）は救わない（開発環境の DB は作り直す。バックフィルは作らない）。

## 2. 触るファイル

| 操作 | パス | 中身 |
|---|---|---|
| 作成 | `server/Uta.Server/Migrations/0003_create_player_idols.sql` | `player_idols` |
| 作成 | `server/Uta.Server/Migrations/0004_create_player_support_cards.sql` | `player_support_cards` |
| 作成 | `shared/Uta.Shared.Contracts/Player/OwnedIdolDto.cs`・`OwnedSupportCardDto.cs` と各 `.meta` | DTO |
| 変更 | `shared/Uta.Shared.Contracts/Player/SignInResponse.cs` | `Idols`・`SupportCards` を `[Key(7)]`・`[Key(8)]` で足す（`PlayerService` の呼び出しと同じタスクで直す） |
| 作成 | `server/Uta.Server/Application/Idol/IIdolRepository.cs`・`OwnedIdol.cs` | ポートと型 |
| 作成 | `server/Uta.Server/Application/Support/ISupportCardRepository.cs`・`OwnedSupportCard.cs` | ポートと型 |
| 作成 | `server/Uta.Server/Application/Player/IInitialGrantCatalog.cs`・`InitialGrant.cs`・`SignInResult.cs` | ポートと型 |
| 作成 | `server/Uta.Server/Application/Common/GameLogJson.cs` | ゲームログの配列の値を作る補助 |
| 変更 | `server/Uta.Server/Application/Player/IPlayerRepository.cs` | `GrantCurrenciesAsync` を足す |
| 変更 | `server/Uta.Server/Application/Player/SignUpUseCase.cs`・`SignInUseCase.cs` | 付与・所持の取得 |
| 作成 | `server/Uta.Server/Infrastructure/IdolRepository.cs`・`SupportCardRepository.cs` | Dapper 実装 |
| 変更 | `server/Uta.Server/Infrastructure/PlayerRepository.cs` | `GrantCurrenciesAsync` |
| 作成 | `server/Uta.Server/Infrastructure/MasterData/MasterInitialGrantCatalog.cs` | マスタから `InitialGrant` を作る |
| 変更 | `server/Uta.Server/Api/PlayerService.cs` | `SignInResponse` の組み立て |
| 変更 | `server/Uta.Server/Program.cs` | 登録 |
| 変更 | `server/Uta.Server.Tests/Integration/PlayerSignUpSignInTests.cs` | 期待値の更新とテストの追加 |
| 作成 | `server/Uta.Server.Tests/MasterData/MasterInitialGrantCatalogTests.cs` | 単体テスト（DB なし） |

`Uta.Server.csproj` は変えない（`Migrations\*.sql` は埋め込み済み）。

## 3. 型とシグネチャ

### 3.1 DB

```sql
-- 0003_create_player_idols.sql
CREATE TABLE player_idols (
    player_id char(26) NOT NULL REFERENCES players (player_id),
    idol_id int NOT NULL,
    training int NOT NULL DEFAULT 0,
    awakening int NOT NULL DEFAULT 0,
    shards int NOT NULL DEFAULT 0,
    created_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (player_id, idol_id)
);

-- 0004_create_player_support_cards.sql
CREATE TABLE player_support_cards (
    support_card_uid char(26) PRIMARY KEY,
    player_id char(26) NOT NULL REFERENCES players (player_id),
    card_id int NOT NULL,
    level int NOT NULL DEFAULT 1,
    limit_break int NOT NULL DEFAULT 0,
    acquired_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX ix_player_support_cards_player_card ON player_support_cards (player_id, card_id);
```

### 3.2 Contracts（C# 9・`Nullable` なし。`namespace Uta.Shared.Contracts.Player`。書き方は `SignUpResponse.cs` と同じ: コンストラクタ + 読み取り専用プロパティ + `[SerializationConstructor]`）

```csharp
[MessagePackObject]
public sealed class OwnedIdolDto
{
    [SerializationConstructor]
    public OwnedIdolDto(int idolId, int training, int awakening, int shards) { /* 代入 */ }
    [Key(0)] public int IdolId { get; }
    [Key(1)] public int Training { get; }
    [Key(2)] public int Awakening { get; }
    [Key(3)] public int Shards { get; }
}

[MessagePackObject]
public sealed class OwnedSupportCardDto
{
    [SerializationConstructor]
    public OwnedSupportCardDto(string supportCardUid, int cardId, int level, int limitBreak) { /* 代入 */ }
    [Key(0)] public string SupportCardUid { get; }   // ULID 26 文字
    [Key(1)] public int CardId { get; }
    [Key(2)] public int Level { get; }
    [Key(3)] public int LimitBreak { get; }
}
```

`SignInResponse` のコンストラクタの引数の**最後**に `OwnedIdolDto[] idols, OwnedSupportCardDto[] supportCards` を足し、`[Key(7)] public OwnedIdolDto[] Idols { get; }`、`[Key(8)] public OwnedSupportCardDto[] SupportCards { get; }` を足す。既存の `[Key(0)]`〜`[Key(6)]` は変えない。

### 3.3 Application

```csharp
// Application/Idol/OwnedIdol.cs   namespace Uta.Server.Application.Idol
public sealed record OwnedIdol(int IdolId, int Training, int Awakening, int Shards);

// Application/Idol/IIdolRepository.cs
public interface IIdolRepository
{
    /// <summary>行が無ければ追加して true。既にあれば false（何もしない）。</summary>
    Task<bool> TryInsertAsync(TxContext tx, Ulid playerId, int idolId);
    /// <summary>idol_id の昇順。</summary>
    Task<IReadOnlyList<OwnedIdol>> ListAsync(TxContext tx, Ulid playerId);
}

// Application/Support/OwnedSupportCard.cs   namespace Uta.Server.Application.Support
public sealed record OwnedSupportCard(Ulid Uid, int CardId, int Level, int LimitBreak);

// Application/Support/ISupportCardRepository.cs
public interface ISupportCardRepository
{
    /// <summary>レベル 1・上限解放 0 の行を追加する。</summary>
    Task InsertAsync(TxContext tx, Ulid playerId, Ulid uid, int cardId);
    /// <summary>support_card_uid の昇順。</summary>
    Task<IReadOnlyList<OwnedSupportCard>> ListAsync(TxContext tx, Ulid playerId);
}

// Application/Player/InitialGrant.cs   namespace Uta.Server.Application.Player
public sealed record InitialGrant(
    int GachaCurrency, int Gold, int SupportPoints,
    IReadOnlyList<int> IdolIds,            // R の育成アイドル。IdolId の昇順
    IReadOnlyList<int> SupportCardIds);    // InitialSupportCard の Order の昇順

public interface IInitialGrantCatalog { InitialGrant Get(); }

// Application/Player/SignInResult.cs
public sealed record SignInResult(PlayerSnapshot Player, IReadOnlyList<OwnedIdol> Idols, IReadOnlyList<OwnedSupportCard> SupportCards);
```

`IPlayerRepository` に足す:

```csharp
/// <summary>通貨を加算する（初期付与）。</summary>
Task GrantCurrenciesAsync(TxContext tx, Ulid playerId, int gachaCurrency, int gold, int supportPoints);
```

```csharp
public sealed class SignUpUseCase(
    IPlayerRepository players, IIdolRepository idols, ISupportCardRepository supportCards, IInitialGrantCatalog catalog)
{
    public async Task<bool> ExecuteAsync(TxContext tx, Ulid playerId)
    {
        if (!await players.InsertIfAbsentAsync(tx, playerId)) return false;
        var grant = catalog.Get();
        await players.GrantCurrenciesAsync(tx, playerId, grant.GachaCurrency, grant.Gold, grant.SupportPoints);
        foreach (var idolId in grant.IdolIds) await idols.TryInsertAsync(tx, playerId, idolId);
        var uids = new List<string>();
        foreach (var cardId in grant.SupportCardIds)
        {
            var uid = Ulid.NewUlid();
            await supportCards.InsertAsync(tx, playerId, uid, cardId);
            uids.Add(uid.ToString());
        }

        // 初期付与のゲームログを 1 件（server-foundation.md「ゲームログ」）。再送・2 回目以降・失敗のときは書かれない
        tx.WriteGameLog(GameLog.Event("player", "sign_up")
            .Target(playerId.ToString())
            .Field("gacha_currency", grant.GachaCurrency)
            .Field("gold", grant.Gold)
            .Field("support_points", grant.SupportPoints)
            .Field("idol_ids", GameLogJson.Ints(grant.IdolIds))
            .Field("support_card_ids", GameLogJson.Ints(grant.SupportCardIds))
            .Field("support_card_uids", GameLogJson.Strings(uids)));
        return true;
    }
}

public sealed class SignInUseCase(IPlayerRepository players, IIdolRepository idols, ISupportCardRepository supportCards)
{
    /// <summary>行が無ければ null。</summary>
    public async Task<SignInResult?> ExecuteAsync(TxContext tx, Ulid playerId)
    {
        var player = await players.FindAsync(tx, playerId);
        if (player is null) return null;
        return new SignInResult(player, await idols.ListAsync(tx, playerId), await supportCards.ListAsync(tx, playerId));
    }
}
```

（`await` は 1 つずつ順に。`TxContext` は並列に使えない。）

`Application/Common/GameLogJson.cs`（`namespace Uta.Server.Application.Common`。`using System.Text.Json.Nodes;`）:

```csharp
public static class GameLogJson
{
    public static JsonArray Ints(IEnumerable<int> values) => new JsonArray(values.Select(x => (JsonNode?)JsonValue.Create(x)).ToArray());
    public static JsonArray Strings(IEnumerable<string> values) => new JsonArray(values.Select(x => (JsonNode?)JsonValue.Create(x)).ToArray());
}
```

`GameLog` は `VolTech.Server.Transaction`（`TxContext` と同じ名前空間）。`Field` の第 2 引数は `JsonNode?`。`int`・`string` はそのまま渡せる（暗黙変換）。

### 3.4 Infrastructure（Dapper。`CommandDefinition` に `tx.Transaction`・`tx.CancellationToken`、接続は `tx.Connection`。`PlayerRepository.cs` と同じ書き方。ULID のパラメーターは `.ToString()`。SELECT は全列に `AS` の別名を付け、private なクラス `Row` に読んで詰め替える）

```sql
-- PlayerRepository.GrantCurrenciesAsync
UPDATE players SET gacha_currency = gacha_currency + @gachaCurrency, gold = gold + @gold, support_points = support_points + @supportPoints
WHERE player_id = @playerId
-- IdolRepository.TryInsertAsync（影響行数が 1 なら true）
INSERT INTO player_idols (player_id, idol_id) VALUES (@playerId, @idolId) ON CONFLICT (player_id, idol_id) DO NOTHING
-- IdolRepository.ListAsync
SELECT idol_id AS IdolId, training AS Training, awakening AS Awakening, shards AS Shards
FROM player_idols WHERE player_id = @playerId ORDER BY idol_id
-- SupportCardRepository.InsertAsync
INSERT INTO player_support_cards (support_card_uid, player_id, card_id) VALUES (@uid, @playerId, @cardId)
-- SupportCardRepository.ListAsync（Uid は string で読み Ulid.Parse する）
SELECT support_card_uid AS Uid, card_id AS CardId, level AS Level, limit_break AS LimitBreak
FROM player_support_cards WHERE player_id = @playerId ORDER BY support_card_uid
```

`Infrastructure/MasterData/MasterInitialGrantCatalog.cs`（`namespace Uta.Server.Infrastructure`、`using Uta.MasterData;`）:

```csharp
public sealed class MasterInitialGrantCatalog : IInitialGrantCatalog
{
    private readonly InitialGrant grant;
    public MasterInitialGrantCatalog(MemoryDatabase db)
    {
        var setting = db.InitialSettingTable.All.Single();                      // Id == 1 の 1 行だけ
        grant = new InitialGrant(
            setting.GachaCurrency, setting.Gold, setting.SupportPoints,
            db.IdolTable.All.Where(x => x.Rarity == Rarity.R).OrderBy(x => x.IdolId).Select(x => x.IdolId).ToList(),
            db.InitialSupportCardTable.All.OrderBy(x => x.Order).Select(x => x.SupportCardId).ToList());
    }
    public InitialGrant Get() => grant;
}
```

`Rarity` は `Uta.MasterData.Rarity`（`Uta.Shared.Domain` の同名の enum を `using` しない）。

### 3.5 Api（`PlayerService.SignInAsync` の変更）

```csharp
var result = await signIn.ExecuteAsync(tx, playerId)
    ?? throw new BusinessException(PlayerErrors.NotCreated, "player not created");
var player = result.Player;
return new SignInResponse(
    player.Level, player.Exp, player.Gold, player.SupportPoints, player.GachaCurrency, false, false,
    result.Idols.Select(x => new OwnedIdolDto(x.IdolId, x.Training, x.Awakening, x.Shards)).ToArray(),
    result.SupportCards.Select(x => new OwnedSupportCardDto(x.Uid.ToString(), x.CardId, x.Level, x.LimitBreak)).ToArray());
```

### 3.6 `Program.cs` の登録

`AddScoped<IPlayerRepository, PlayerRepository>();` の次の行に足す:

```csharp
builder.Services.AddScoped<IIdolRepository, IdolRepository>();
builder.Services.AddScoped<ISupportCardRepository, SupportCardRepository>();
builder.Services.AddSingleton<IInitialGrantCatalog, MasterInitialGrantCatalog>();
```

（`MemoryDatabase` は `AddSingleton(MasterDataLoader.Load())` で登録済み。using を足す。）

## 4. タスク

各タスクの終わりに `dotnet build Uta.slnx`（警告 0・エラー 0）が通る。`.cs` は BOM 付き UTF-8・LF（`server/.editorconfig` のとおり）。Contracts の新規ファイルとフォルダの `.meta` は、5a の計画 4章タスク 1 の手順で作る（`.cs.meta` は `fileFormatVersion: 2` と `guid: <新しい 32 桁の小文字 16 進>` の 2 行。`python -c "import uuid;print(uuid.uuid4().hex)"`）。

1. **マイグレーションと Contracts**: 3.1 の SQL 2 本、3.2 の DTO 2 つ（`OwnedIdolDto`・`OwnedSupportCardDto`。`SignInResponse` はこのタスクでは変えない）。確かめ: `dotnet build Uta.slnx`、`dotnet test --project server/Uta.Server.Tests`（既存の結合テストが通る。`Migration_AddsPlayerColumns` など）。
2. **`SignInResponse` の変更と Application・Infrastructure・Api・登録**: 3.2 の `SignInResponse` の変更（引数の追加と `[Key(7)]`・`[Key(8)]`）と 3.3〜3.6。`SignInResponse` の変更と `PlayerService` の呼び出しの修正は同じタスクで行う（途中でビルドが壊れないように）。確かめ: `dotnet build Uta.slnx`。
3. **テスト**: 5章。確かめ: `dotnet test --project server/Uta.Server.Tests` が全件通る（Docker が要る）。

wiki への反映（実装担当の仕事ではない。3 本の実装後にメインのセッションが行う）: `api-design.md`（`SignInResponse` の `Idols`・`SupportCards`、3章のテーブルの列）、`gacha-support-server.md`、`game-spec.md`。5a で作ったプレイヤーを救わない判断は、DB を作り直す前提として 1章に書いてある。

境界: `SignUpAsync` の 2 回目以降は何も付与しない（`InsertIfAbsentAsync` が false）。付与の途中で失敗したら、トランザクションごとロールバックされ、`players` の行も残らない（再送で作り直せる）。

## 5. テスト

### `MasterInitialGrantCatalogTests`（DB なし。`MasterDataLoader.Load()` で埋め込みのマスタを読む）

| テスト名 | 期待 |
|---|---|
| `Get_ReturnsInitialValues` | `GachaCurrency == 2500`・`Gold == 10000`・`SupportPoints == 10000`、`IdolIds` は `[1, 2]`、`SupportCardIds` は `[1, 2, 3, 4, 5]` |

### `PlayerSignUpSignInTests` の変更・追加（`Postgres` のヘルパーは既存のとおり）

| テスト名 | 手順 | 期待 |
|---|---|---|
| `SignIn_AfterSignUp_ReturnsDefaults` を **`SignIn_AfterSignUp_ReturnsInitialGrant` に改名** | `SignUpAsync` → `SignInAsync` | `Level == 1`・`Exp == 0`・`Gold == 10000`・`SupportPoints == 10000`・`GachaCurrency == 2500`・`HasActiveProduce == false`・`DiscardedProduce == false`。`Idols` は `IdolId` が `[1, 2]` で全員 `Training == 0`・`Awakening == 0`・`Shards == 0`。`SupportCards` は 5 件で `CardId` の集合が `{1,2,3,4,5}`・全件 `Level == 1`・`LimitBreak == 0`・`SupportCardUid` は 26 文字で互いに異なる |
| `SignUp_Twice_DoesNotGrantAgain` | `SignUpAsync`（`req-1`）→ `SignUpAsync`（`req-2`）→ `SignInAsync`（`req-3`） | `Gold == 10000`・`GachaCurrency == 2500`・`Idols` 2 件・`SupportCards` 5 件のまま |
| `SignUp_NewPlayer_CreatesOwnershipRows` | `SignUpAsync` | `SELECT count(*) FROM player_idols WHERE player_id = $1` が 2、`player_support_cards` が 5（どちらも `long`） |
| `SignUp_NewPlayer_WritesOneGameLog` | `SignUpAsync` | `SELECT count(*) FROM game_logs WHERE player_id = $1 AND kind = 'player' AND body->>'reason' = 'sign_up'` が 1（`long`）。2 回目の `SignUpAsync`（別の `x-request-id`）と同じ ID の再送の後も 1 のまま |
| `Migration_CreatesOwnershipTables` | — | `SELECT count(*) FROM information_schema.tables WHERE table_name = ANY(ARRAY['player_idols','player_support_cards'])` が 2 |

既存のテスト（`SignUp_NewPlayer_CreatesRow` など）は、期待値が変わらないものはそのまま通ること。テスト内の DB の検査は、すべて `WHERE player_id = $1` で自分のプレイヤーに絞る（DB は全テストで共有）。`count(*)` は `long`、`int` 列は `int` で読む。

実行: `dotnet build Uta.slnx`・`dotnet test --project server/Uta.Server.Tests`（Docker が要る）。Unity 側のビルド確認はこの計画の範囲外（`.meta` を書くまで）。

## 6. 未決事項

なし。
- `SignInResponse` に所持の一覧を足す形は、[api-design.md](../wiki/architecture/api-design.md) 1章の「所持の育成アイドル・サポートカードは、それぞれのテーブルを作る計画で足す」に従う。
- DTO の一覧は配列（`T[]`）にする。`IReadOnlyList<T>` は IL2CPP（AOT）での MessagePack の扱いが未確認なので避ける（レビュー指摘への対応）。
