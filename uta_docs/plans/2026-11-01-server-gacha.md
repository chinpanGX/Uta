---
title: 実装計画: ガチャ（サーバー）
status: done
updated: 2026-11-01
spec: ../wiki/systems/gacha.md, ../wiki/architecture/server/gacha-support-server.md, ../wiki/architecture/api-design.md, ../wiki/architecture/master/gacha-tables.md
---

# 実装計画: ガチャ（サーバー）

## 1. 目的と前提

- 何を実装するか: `IGachaService` の `GetGachasAsync`・`DrawAsync`（単発・10 連・確定枠）・`ExchangeAsync`（天井の交換）を、サーバーと `Uta.Shared.Contracts` に作る。重複（育成アイドルのかけら、完凸後のサポート強化pt）、抽選履歴の保存、交換pt の加算も含む。仕様は [gacha.md](../wiki/systems/gacha.md)、処理の手順の正は [gacha-support-server.md](../wiki/architecture/server/gacha-support-server.md) 1・2章、RPC・DTO・エラーは [api-design.md](../wiki/architecture/api-design.md) 1.1・2・3章。
- 位置づけ: [game-spec.md](../wiki/overview/game-spec.md)「実装計画の単位と順」の**順 5 の 2/3**（受け持つ完了条件 2）。
- 前提: [初期付与の計画](2026-11-01-server-initial-grant.md) が済んでいる（`player_idols`・`player_support_cards`・`IIdolRepository`・`ISupportCardRepository`・`IInitialGrantCatalog`・`GameLogJson` がある）。
- 関係する ADR: [0007](../wiki/decisions/0007-masterdata-with-addons.md)、[0008](../wiki/decisions/0008-shared-domain-and-onion.md)、[0010](../wiki/decisions/0010-database.md)、[0012](../wiki/decisions/0012-migration-dbup.md)
- **この計画で決めないこと**: `GetHistoryAsync`（MVP で作らない。履歴の保存だけする）、サポートカードのレベル上げ・上限解放（[次の計画](2026-11-01-server-support-upgrade.md)）、期間・ピックアップ、クライアント側。
- 決めたこと（レビューで確認済みの方針）:
  - **業務エラーの伝え方**（ユーザーの判断。2026-11-01）: Application は `Uta.Shared.Contracts` を参照しない。Application が `UseCaseException`（enum 付き）を投げ、Api の共通フィルター `[UtaService]` が `BusinessException(ErrorCode)` に変換する。例外はサービスの本体から `RunAsync` を抜けて出るので、トランザクションのロールバックは変換の前に済む。
  - **プレイヤー未作成を弾く共通の処理**（[server-foundation.md](../wiki/architecture/server/server-foundation.md) が「3 つ目の Uta の RPC の計画で作る」とした）: 同じ `[UtaService]` フィルターが、`AuthContext.PlayerCreated` が false なら `PLAYER_NOT_CREATED` で弾く。設計書の「全体に掛けて `SignUpAsync` を除く」形ではなく、**`IPlayerService` 以外の Uta のサービスの実装クラスに属性を付ける**形にする（付け忘れは、`ArchitectureTests` の「`ServiceBase<>` の実装は `PlayerService` 以外すべて `[UtaService]` を持つ」で検出する）。
  - **ゲームログ**（[server-foundation.md](../wiki/architecture/server/server-foundation.md)「ゲームログ」）: `DrawAsync`・`ExchangeAsync` はそれぞれ 1 件、`tx.WriteGameLog(GameLog.Event(...))` で書く（中身は 3.4）。
  - **New / Duplicate の判定**は Domain の純粋な関数 `ItemGrantRules.Decide`（server-foundation.md「SQL を書く層」）。DB を読み書きする手順は Application の `ItemGranter`。
  - 抽選の乱数は `IRandomSource`（Application のポート）越しに作る。結合テストが決まった値を返す実装に差し替えて、結果を確かめる。
  - 抽選のテスト（累積の境界）は、`shared/Uta.Shared.Domain.Tests` ではなく `server/Uta.Server.Tests` に置く（`shared/Uta.Shared.Domain*` は順 4 のセッションが触るため）。

## 2. 触るファイル

| 操作 | パス | 中身 |
|---|---|---|
| 作成 | `server/Uta.Server/Migrations/0005_create_gacha_tables.sql` | `gacha_exchange_points`・`gacha_histories` |
| 作成 | `shared/Uta.Shared.Contracts/Gacha/` の下: `IGachaService.cs`・`ItemKind.cs`・`GachaOutcome.cs`・`DrawResultDto.cs`・`GachaStatusDto.cs`・`GetGachasResponse.cs`・`DrawRequest.cs`・`DrawResponse.cs`・`ExchangeRequest.cs`・`ExchangeResponse.cs`・`GachaErrors.cs` と、各 `.cs.meta`、`Gacha.meta`（フォルダ） | サービス IF・DTO・エラー |
| 作成 | `server/Uta.Server/Domain/Gacha/GachaPicker.cs`・`GachaRandom.cs`・`GrantedItemKind.cs`・`GrantOutcome.cs`・`ItemGrantRules.cs` | 抽選・シード・New/Duplicate の判定 |
| 作成 | `server/Uta.Server/Domain/Support/SupportRules.cs` | `MaxLimitBreak = 4` |
| 作成 | `server/Uta.Server/Application/Common/UseCaseError.cs`・`UseCaseException.cs`・`IRandomSource.cs` | 共通 |
| 作成 | `server/Uta.Server/Application/Gacha/` の下: `GachaResult.cs`・`GachaDefinition.cs`（`GachaDefinition`・`GachaPool`・`GachaPoolItem`・`CeilingItem` をまとめて置く）・`GachaStatus.cs`（`GachaStatus`）・`DrawResult.cs`（`DrawResult`）・`ExchangeResult.cs`（`ExchangeResult`）・`IGachaCatalog.cs`・`IDuplicateConversionCatalog.cs`・`IGachaRepository.cs`・`GachaHistoryRow.cs`・`ItemGranter.cs`・`GetGachasUseCase.cs`・`DrawUseCase.cs`・`ExchangeUseCase.cs` | Application |
| 変更 | `server/Uta.Server/Application/Player/IPlayerRepository.cs` | `TrySpendGachaCurrencyAsync`・`AddSupportPointsAsync` |
| 変更 | `server/Uta.Server/Application/Idol/IIdolRepository.cs` | `ExistsAsync`・`AddShardsAsync` |
| 変更 | `server/Uta.Server/Application/Support/ISupportCardRepository.cs` | `HasMaxLimitBreakAsync` |
| 作成 | `server/Uta.Server/Infrastructure/GachaRepository.cs`・`SecureRandomSource.cs` | Dapper 実装・乱数 |
| 作成 | `server/Uta.Server/Infrastructure/MasterData/MasterGachaCatalog.cs`・`MasterDuplicateConversionCatalog.cs` | マスタの読み込み |
| 変更 | `server/Uta.Server/Infrastructure/PlayerRepository.cs`・`IdolRepository.cs`・`SupportCardRepository.cs` | 上のメソッド |
| 作成 | `server/Uta.Server/Api/GachaService.cs`・`UtaServiceAttribute.cs` | Api（共通フィルター） |
| 変更 | `server/Uta.Server/Program.cs` | 登録 |
| 変更 | `server/Uta.Server.Tests/Integration/Support/PlayerClient.cs`・`UtaServerFactory.cs` | 汎用の呼び出し・乱数の差し替え |
| 作成 | `server/Uta.Server.Tests/Integration/Support/ScriptedRandomSource.cs` | 決まった値を返す乱数 |
| 作成 | `server/Uta.Server.Tests/Integration/GachaTests.cs` | 結合テスト |
| 作成 | `server/Uta.Server.Tests/Domain/GachaPickerTests.cs`・`GachaRandomTests.cs`・`ItemGrantRulesTests.cs` | 単体テスト |
| 作成 | `server/Uta.Server.Tests/MasterData/MasterGachaCatalogTests.cs`・`ContractsEnumsMatchTests.cs` | カタログと enum の一致 |
| 変更 | `server/Uta.Server.Tests/Architecture/ArchitectureTests.cs` | `DomainNamespace_HasTypes`・`Services_HaveUtaServiceAttribute` |
| 変更 | `uta_docs/wiki/architecture/server/gacha-support-server.md`・`server-foundation.md`・`api-design.md`・`master/gacha-tables.md`・`overview/game-spec.md` | 完了時にメインのセッションが実装結果を反映（4章の最後。実装担当は触らない） |

## 3. 型とシグネチャ

### 3.1 DB（`0005_create_gacha_tables.sql`）

```sql
CREATE TABLE gacha_exchange_points (
    player_id char(26) NOT NULL REFERENCES players (player_id),
    gacha_id int NOT NULL,
    points int NOT NULL DEFAULT 0 CHECK (points >= 0),
    PRIMARY KEY (player_id, gacha_id)
);

CREATE TABLE gacha_histories (
    history_id char(26) PRIMARY KEY,
    request_id varchar(64) NOT NULL,
    player_id char(26) NOT NULL REFERENCES players (player_id),
    gacha_id int NOT NULL,
    slot int NOT NULL,
    gacha_table_id int NOT NULL,
    item_kind int NOT NULL,
    item_id int NOT NULL,
    outcome int NOT NULL,
    shards int NOT NULL DEFAULT 0,
    support_points int NOT NULL DEFAULT 0,
    created_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX ix_gacha_histories_player ON gacha_histories (player_id, history_id DESC);
```

### 3.2 Contracts（C# 9・`Nullable` なし・`namespace Uta.Shared.Contracts.Gacha`。一覧は配列 `T[]`（IL2CPP での `IReadOnlyList<T>` が未確認のため）。DTO は `SignUpResponse.cs` と同じ書き方: コンストラクタ + 読み取り専用プロパティ + `[SerializationConstructor]`、`[Key(n)]` は上から 0 始まり）

```csharp
// ItemKind.cs  （値の正は gacha-tables.md。マスタの ItemKind と同じ名前・同じ数値）
public enum ItemKind { Nothing = 0, Idol = 1, SupportCard = 2 }

// GachaOutcome.cs  （api-design.md 1.1 の Outcome。型名は GachaOutcome）
public enum GachaOutcome { Nothing = 0, New = 1, Duplicate = 2 }

[MessagePackObject]
public sealed class DrawResultDto
{
    // 引数名はプロパティ名の camelCase。順は Key の番号
    [SerializationConstructor]
    public DrawResultDto(int slot, ItemKind itemKind, int itemId, GachaOutcome outcome, int shards, int supportPoints, string supportCardUid) { /* 代入 */ }
    [Key(0)] public int Slot { get; }                 // 1〜10。交換は 0
    [Key(1)] public ItemKind ItemKind { get; }
    [Key(2)] public int ItemId { get; }               // 育成アイドルなら IdolId、サポートカードなら SupportCardId
    [Key(3)] public GachaOutcome Outcome { get; }
    [Key(4)] public int Shards { get; }               // 重複した育成アイドルのかけら
    [Key(5)] public int SupportPoints { get; }        // 完凸後の重複の強化pt
    [Key(6)] public string SupportCardUid { get; }    // 増えたサポートカードの所持ID。無ければ空文字（null にしない）
}

[MessagePackObject]
public sealed class GachaStatusDto
{
    [SerializationConstructor]
    public GachaStatusDto(int gachaId, int exchangePoints) { GachaId = gachaId; ExchangePoints = exchangePoints; }
    [Key(0)] public int GachaId { get; }
    [Key(1)] public int ExchangePoints { get; }
}

[MessagePackObject]
public sealed class GetGachasResponse
{
    [SerializationConstructor]
    public GetGachasResponse(GachaStatusDto[] gachas) { Gachas = gachas; }
    [Key(0)] public GachaStatusDto[] Gachas { get; }
}

[MessagePackObject]
public sealed class DrawRequest
{
    [SerializationConstructor]
    public DrawRequest(int gachaId, int count, int expectedCost) { GachaId = gachaId; Count = count; ExpectedCost = expectedCost; }
    [Key(0)] public int GachaId { get; }
    [Key(1)] public int Count { get; }           // 1 か 10
    [Key(2)] public int ExpectedCost { get; }    // 画面に出した価格
}

[MessagePackObject]
public sealed class DrawResponse
{
    [SerializationConstructor]
    public DrawResponse(DrawResultDto[] results, int gachaCurrency, int exchangePoints)
    {
        Results = results; GachaCurrency = gachaCurrency; ExchangePoints = exchangePoints;
    }
    [Key(0)] public DrawResultDto[] Results { get; }
    [Key(1)] public int GachaCurrency { get; }   // 残り
    [Key(2)] public int ExchangePoints { get; }  // このガチャの交換pt
}

[MessagePackObject]
public sealed class ExchangeRequest
{
    [SerializationConstructor]
    public ExchangeRequest(int gachaId, int ceilingItemOrder) { GachaId = gachaId; CeilingItemOrder = ceilingItemOrder; }
    [Key(0)] public int GachaId { get; }
    [Key(1)] public int CeilingItemOrder { get; }   // GachaCeilingItem の Order
}

[MessagePackObject]
public sealed class ExchangeResponse
{
    [SerializationConstructor]
    public ExchangeResponse(DrawResultDto result, int exchangePoints) { Result = result; ExchangePoints = exchangePoints; }
    [Key(0)] public DrawResultDto Result { get; }   // Slot は 0
    [Key(1)] public int ExchangePoints { get; }     // 交換後の残り
}

// IGachaService.cs   using VolTech.Server.Abstractions;  using MagicOnion;
public interface IGachaService : IService<IGachaService>
{
    [NoSideEffects] UnaryResult<GetGachasResponse> GetGachasAsync();
    UnaryResult<DrawResponse> DrawAsync(DrawRequest request);
    UnaryResult<ExchangeResponse> ExchangeAsync(ExchangeRequest request);
}

// GachaErrors.cs   using VolTech.Server.Abstractions;   （PlayerErrors.cs と同じ書き方。domain は "uta"）
public static class GachaErrors
{
    public static readonly ErrorCode NotEnoughCurrency = new ErrorCode("uta", "GACHA_NOT_ENOUGH_CURRENCY");
    public static readonly ErrorCode NotEnoughPoints = new ErrorCode("uta", "GACHA_NOT_ENOUGH_POINTS");
    public static readonly ErrorCode NotFound = new ErrorCode("uta", "GACHA_NOT_FOUND");
    public static readonly ErrorCode InvalidCount = new ErrorCode("uta", "GACHA_INVALID_COUNT");
    public static readonly ErrorCode CostMismatch = new ErrorCode("uta", "GACHA_COST_MISMATCH");
}
```

`IGachaService` に `[SkipAuth]` は付けない。`GetGachasAsync` だけ `[NoSideEffects]`（読み取り専用のトランザクション。冪等性の対象にならない）。

### 3.3 Domain（`namespace Uta.Server.Domain.Gacha`／`Uta.Server.Domain.Support`。`Uta.Shared.Domain` だけを参照する）

```csharp
// GachaPicker.cs
public static class GachaPicker
{
    /// <summary>重みの並び（Order 昇順）から 1 つ選んだ添字を返す。RandomOps.WeightedIndex を呼ぶだけ。</summary>
    public static int Pick(IRandom random, IReadOnlyList<int> weights) => RandomOps.WeightedIndex(random, weights);
}

// GachaRandom.cs
public static class GachaRandom
{
    /// <summary>テストと SecureRandomSource 用。SplitMix64 で 4 回引いて状態にする（Xoshiro256StarStar.FromSeed）。</summary>
    public static IRandom FromSeed(ulong seed) => Xoshiro256StarStar.FromSeed(seed);

    /// <summary>暗号用乱数（RandomNumberGenerator）で 64 ビットのシードを作る。保存しない。</summary>
    public static IRandom CreateSecure()
    {
        Span<byte> buffer = stackalloc byte[8];
        RandomNumberGenerator.Fill(buffer);
        return FromSeed(BinaryPrimitives.ReadUInt64LittleEndian(buffer));
    }
}

// GrantedItemKind.cs / GrantOutcome.cs  （ItemKind・GachaOutcome と同じ数値。Application はこの型を使い、Api が Contracts の型へ (int) で読み替える）
public enum GrantedItemKind { Idol = 1, SupportCard = 2 }
public enum GrantOutcome { New = 1, Duplicate = 2 }

// ItemGrantRules.cs  （gacha.md 5章。DB を読まない純粋な関数）
public static class ItemGrantRules
{
    /// <summary>育成アイドルは所持済みなら Duplicate（かけらに変換）。サポートカードは同じカードの完凸を持っていれば Duplicate（強化ptに変換）、そうでなければ New。</summary>
    public static GrantOutcome Decide(GrantedItemKind kind, bool idolOwned, bool supportCardMaxed) =>
        kind == GrantedItemKind.Idol
            ? (idolOwned ? GrantOutcome.Duplicate : GrantOutcome.New)
            : (supportCardMaxed ? GrantOutcome.Duplicate : GrantOutcome.New);
}

// Support/SupportRules.cs
public static class SupportRules { public const int MaxLimitBreak = 4; }
```

### 3.4 Application

```csharp
// Common/UseCaseError.cs  namespace Uta.Server.Application.Common
public enum UseCaseError
{
    GachaInvalidCount, GachaNotFound, GachaCostMismatch, GachaNotEnoughCurrency, GachaNotEnoughPoints,
}

// Common/UseCaseException.cs
public sealed class UseCaseException(UseCaseError error, string message) : Exception(message)
{
    public UseCaseError Error { get; } = error;
}

// Common/IRandomSource.cs
public interface IRandomSource { IRandom Create(); }     // Create は要求ごとに 1 回呼ぶ（シードは保存しない）
```

`namespace Uta.Server.Application.Gacha`:

```csharp
// GrantedItemKind・GrantOutcome は Uta.Server.Domain.Gacha（using する）

public sealed record GachaResult(
    int Slot, GrantedItemKind Kind, int ItemId, GrantOutcome Outcome, int Shards, int SupportPoints, Ulid? SupportCardUid)
{
    /// <summary>ゲームログ用。キーは slot・item_kind・item_id・outcome・shards・support_points・support_card_uid（無ければ null）。</summary>
    public JsonObject ToLogJson() => new JsonObject
    {
        ["slot"] = Slot,
        ["item_kind"] = (int)Kind,
        ["item_id"] = ItemId,
        ["outcome"] = (int)Outcome,
        ["shards"] = Shards,
        ["support_points"] = SupportPoints,
        ["support_card_uid"] = SupportCardUid?.ToString(),
    };
}   // using System.Text.Json.Nodes;

public sealed record GachaPoolItem(GrantedItemKind Kind, int ItemId, int Weight);

/// <summary>排出テーブル 1 つ（マスタの GachaTableEntry の集まり）。MasterMemory が生成する GachaTable と名前が重ならないよう Pool と呼ぶ。</summary>
public sealed class GachaPool
{
    public GachaPool(int gachaTableId, IReadOnlyList<GachaPoolItem> items)
    {
        GachaTableId = gachaTableId;
        Items = items;
        Weights = items.Select(x => x.Weight).ToList();
    }
    public int GachaTableId { get; }
    public IReadOnlyList<GachaPoolItem> Items { get; }     // Order 昇順
    public IReadOnlyList<int> Weights { get; }
}

public sealed record CeilingItem(int Order, GrantedItemKind Kind, int ItemId);

public sealed record GachaDefinition(
    int GachaId, int SingleCost, int TenCost, int CeilingPoints,
    GachaPool Normal, GachaPool Guaranteed, IReadOnlyList<CeilingItem> CeilingItems);

public interface IGachaCatalog
{
    IReadOnlyList<GachaDefinition> All { get; }     // GachaId 昇順
    GachaDefinition? Find(int gachaId);
}

public interface IDuplicateConversionCatalog
{
    /// <summary>重複したときの変換量。育成アイドルはかけら、サポートカードは完凸後の強化pt。無ければ InvalidOperationException。</summary>
    int GetAmount(GrantedItemKind kind, int itemId);
}

public sealed record GachaHistoryRow(
    Ulid HistoryId, string RequestId, Ulid PlayerId, int GachaId, int Slot, int GachaTableId,
    int ItemKind, int ItemId, int Outcome, int Shards, int SupportPoints);

public interface IGachaRepository
{
    /// <summary>交換pt を加算し、加算後の値を返す（行が無ければ作る）。</summary>
    Task<int> AddExchangePointsAsync(TxContext tx, Ulid playerId, int gachaId, int points);
    /// <summary>交換pt を減らし、残りを返す。足りない（行が無い場合を含む）なら null。</summary>
    Task<int?> TrySpendExchangePointsAsync(TxContext tx, Ulid playerId, int gachaId, int points);
    /// <summary>プレイヤーの交換pt。行があるガチャだけ（キーは GachaId）。</summary>
    Task<IReadOnlyDictionary<int, int>> GetExchangePointsAsync(TxContext tx, Ulid playerId);
    Task AddHistoryAsync(TxContext tx, GachaHistoryRow row);
}

/// <summary>所持品への書き込み（gacha.md 5章）。Draw と Exchange が使う。Slot は 0 で返す（呼び出し側が with で入れる）。</summary>
public sealed class ItemGranter(
    IIdolRepository idols, ISupportCardRepository supportCards, IPlayerRepository players, IDuplicateConversionCatalog conversions)
{
    public async Task<GachaResult> GrantAsync(TxContext tx, Ulid playerId, GrantedItemKind kind, int itemId)
    {
        if (kind == GrantedItemKind.Idol)
        {
            var owned = await idols.ExistsAsync(tx, playerId, itemId);            // 枠ごとに読む（同じ 10 連で同じアイドルが 2 回出ても正しい）
            if (ItemGrantRules.Decide(kind, owned, false) == GrantOutcome.New)
            {
                await idols.TryInsertAsync(tx, playerId, itemId);
                return new GachaResult(0, kind, itemId, GrantOutcome.New, 0, 0, null);
            }

            var shards = conversions.GetAmount(kind, itemId);
            await idols.AddShardsAsync(tx, playerId, itemId, shards);
            return new GachaResult(0, kind, itemId, GrantOutcome.Duplicate, shards, 0, null);
        }

        var maxed = await supportCards.HasMaxLimitBreakAsync(tx, playerId, itemId);
        if (ItemGrantRules.Decide(kind, false, maxed) == GrantOutcome.Duplicate)
        {
            var points = conversions.GetAmount(kind, itemId);
            await players.AddSupportPointsAsync(tx, playerId, points);
            return new GachaResult(0, kind, itemId, GrantOutcome.Duplicate, 0, points, null);
        }

        var uid = Ulid.NewUlid();
        await supportCards.InsertAsync(tx, playerId, uid, itemId);
        return new GachaResult(0, kind, itemId, GrantOutcome.New, 0, 0, uid);
    }
}
```

追加するポートのメソッド:

```csharp
// IPlayerRepository
/// <summary>ガチャ通貨を減らし、残りを返す。足りなければ null（更新は 0 行）。</summary>
Task<int?> TrySpendGachaCurrencyAsync(TxContext tx, Ulid playerId, int cost);
Task AddSupportPointsAsync(TxContext tx, Ulid playerId, int points);
// IIdolRepository
Task<bool> ExistsAsync(TxContext tx, Ulid playerId, int idolId);
Task AddShardsAsync(TxContext tx, Ulid playerId, int idolId, int shards);
// ISupportCardRepository
/// <summary>同じカード ID で上限解放が SupportRules.MaxLimitBreak の行を 1 つでも持っているか。</summary>
Task<bool> HasMaxLimitBreakAsync(TxContext tx, Ulid playerId, int cardId);
```

ユースケース:

```csharp
public sealed record GachaStatus(int GachaId, int ExchangePoints);
public sealed class GetGachasUseCase(IGachaCatalog catalog, IGachaRepository gachas)
{
    /// <summary>catalog.All の順（GachaId 昇順）。交換pt の行が無いガチャは 0。gachas.GetExchangePointsAsync を 1 回呼んで引く。</summary>
    public async Task<IReadOnlyList<GachaStatus>> ExecuteAsync(TxContext tx, Ulid playerId);
}

public sealed record DrawResult(IReadOnlyList<GachaResult> Results, int GachaCurrency, int ExchangePoints);
public sealed class DrawUseCase(
    IGachaCatalog catalog, IPlayerRepository players, IGachaRepository gachas, ItemGranter granter, IRandomSource randomSource)
{
    public async Task<DrawResult> ExecuteAsync(TxContext tx, Ulid playerId, string requestId, int gachaId, int count, int expectedCost);
}

public sealed record ExchangeResult(GachaResult Result, int ExchangePoints);
public sealed class ExchangeUseCase(IGachaCatalog catalog, IGachaRepository gachas, ItemGranter granter)
{
    public async Task<ExchangeResult> ExecuteAsync(TxContext tx, Ulid playerId, string requestId, int gachaId, int ceilingItemOrder);
}
```

`DrawUseCase.ExecuteAsync` の手順（[gacha-support-server.md](../wiki/architecture/server/gacha-support-server.md) 2章の `DrawAsync` のとおり。例外は `new UseCaseException(UseCaseError.X, "…")`）:

1. `count` が 1 でも 10 でもなければ `GachaInvalidCount`
2. `catalog.Find(gachaId)` が null なら `GachaNotFound`
3. `cost = count == 1 ? SingleCost : TenCost`。`expectedCost != cost` なら `GachaCostMismatch`
4. `remaining = await players.TrySpendGachaCurrencyAsync(tx, playerId, cost)`。null なら `GachaNotEnoughCurrency`
5. `random = randomSource.Create()`
6. `slot` を 1 から `count` まで（順に）: `table = (count == 10 && slot == 10) ? gacha.Guaranteed : gacha.Normal`。`item = table.Items[GachaPicker.Pick(random, table.Weights)]`。`result = (await granter.GrantAsync(tx, playerId, item.Kind, item.ItemId)) with { Slot = slot }`。`gachas.AddHistoryAsync(...)`（`HistoryId = Ulid.NewUlid()`、`RequestId = requestId`、`GachaTableId = table.GachaTableId`、`ItemKind = (int)result.Kind`、`Outcome = (int)result.Outcome`、そのほかは `result` の値。`SupportCardUid` は履歴に持たない）。`results` に足す
7. `points = await gachas.AddExchangePointsAsync(tx, playerId, gachaId, count)`
8. ゲームログを 1 件書く（`using System.Text.Json.Nodes;`）:
   ```csharp
   tx.WriteGameLog(GameLog.Event("gacha", "gacha_draw")
       .Target(gachaId.ToString())
       .Field("count", count).Field("cost", cost)
       .Field("gacha_currency_after", remaining.Value).Field("exchange_points_after", points)
       .Field("results", new JsonArray(results.Select(x => (JsonNode?)x.ToLogJson()).ToArray())));
   ```
9. `new DrawResult(results, remaining.Value, points)`

`ExchangeUseCase.ExecuteAsync` の手順:

1. `gacha = catalog.Find(gachaId)` が null、または `gacha.CeilingItems` に `Order == ceilingItemOrder` が無ければ `GachaNotFound`
2. `points = await gachas.TrySpendExchangePointsAsync(tx, playerId, gachaId, gacha.CeilingPoints)`。null なら `GachaNotEnoughPoints`
3. `item = gacha.CeilingItems.First(x => x.Order == ceilingItemOrder)`（1 で存在を確かめてある）。`result = await granter.GrantAsync(tx, playerId, item.Kind, item.ItemId)`（`Slot` は 0 のまま）。`gachas.AddHistoryAsync` に 1 行（`HistoryId = Ulid.NewUlid()`、`RequestId = requestId`、`PlayerId = playerId`、`GachaId = gachaId`、`Slot = 0`、`GachaTableId = 0`、`ItemKind = (int)result.Kind`、`ItemId = result.ItemId`、`Outcome = (int)result.Outcome`、`Shards`・`SupportPoints` は `result` の値）
4. ゲームログを 1 件書く:
   ```csharp
   tx.WriteGameLog(GameLog.Event("gacha", "gacha_exchange")
       .Target(gachaId.ToString())
       .Field("ceiling_item_order", ceilingItemOrder).Field("cost_points", gacha.CeilingPoints)
       .Field("exchange_points_after", points.Value).Field("result", result.ToLogJson()));
   ```
5. `new ExchangeResult(result, points.Value)`

### 3.5 Infrastructure

SQL（Dapper。`PlayerRepository.cs` と同じ書き方。ULID は `.ToString()`）:

```sql
-- PlayerRepository.TrySpendGachaCurrencyAsync（QuerySingleOrDefaultAsync<int?>。0 行なら null）
UPDATE players SET gacha_currency = gacha_currency - @cost
WHERE player_id = @playerId AND gacha_currency >= @cost
RETURNING gacha_currency
-- PlayerRepository.AddSupportPointsAsync
UPDATE players SET support_points = support_points + @points WHERE player_id = @playerId
-- IdolRepository.ExistsAsync（ExecuteScalarAsync<bool>）
SELECT EXISTS (SELECT 1 FROM player_idols WHERE player_id = @playerId AND idol_id = @idolId)
-- IdolRepository.AddShardsAsync
UPDATE player_idols SET shards = shards + @shards WHERE player_id = @playerId AND idol_id = @idolId
-- SupportCardRepository.HasMaxLimitBreakAsync（ExecuteScalarAsync<bool>。@max に SupportRules.MaxLimitBreak）
SELECT EXISTS (SELECT 1 FROM player_support_cards WHERE player_id = @playerId AND card_id = @cardId AND limit_break >= @max)
-- GachaRepository.AddExchangePointsAsync（ExecuteScalarAsync<int>）
INSERT INTO gacha_exchange_points (player_id, gacha_id, points) VALUES (@playerId, @gachaId, @points)
ON CONFLICT (player_id, gacha_id) DO UPDATE SET points = gacha_exchange_points.points + @points
RETURNING points
-- GachaRepository.TrySpendExchangePointsAsync（QuerySingleOrDefaultAsync<int?>）
UPDATE gacha_exchange_points SET points = points - @cost
WHERE player_id = @playerId AND gacha_id = @gachaId AND points >= @cost
RETURNING points
-- GachaRepository.GetExchangePointsAsync（private なクラス Row { int GachaId; int Points } に QueryAsync<Row> で読み、ToDictionary(x => x.GachaId, x => x.Points)）
SELECT gacha_id AS GachaId, points AS Points FROM gacha_exchange_points WHERE player_id = @playerId
-- GachaRepository.AddHistoryAsync
INSERT INTO gacha_histories
    (history_id, request_id, player_id, gacha_id, slot, gacha_table_id, item_kind, item_id, outcome, shards, support_points)
VALUES (@historyId, @requestId, @playerId, @gachaId, @slot, @gachaTableId, @itemKind, @itemId, @outcome, @shards, @supportPoints)
```

`SecureRandomSource : IRandomSource` は `Create() => GachaRandom.CreateSecure()`。

`MasterGachaCatalog : IGachaCatalog`（`using Uta.MasterData;`。コンストラクタ `MasterGachaCatalog(MemoryDatabase db)` で全部を組み立てて保持する）:

- 表の取り出し: `db.GachaTable.All`（生成された表の型。Application の `GachaPool` とは別の名前）・`db.GachaTableEntryTable.All`・`db.GachaCeilingItemTable.All`。`GachaDetailTable` の生成名は `GachaDetailTableTable`（使わない）
- `GachaPool`（Application の型）は `GachaTableEntry` を `GachaTableId` で絞って `Order` 昇順に並べ、`GachaPoolItem(Kind, ItemId, Weight)` にする。ガチャごとに、`Normal` は `Gacha.NormalTableId`、`Guaranteed` は `Gacha.GuaranteedTableId` のテーブルから作る。`ItemId` は `ItemKind.Idol` なら `IdolId`、`ItemKind.SupportCard` なら `SupportCardId`。`Kind` は `(GrantedItemKind)(int)entry.ItemKind`（`Nothing` は検証で弾かれている）
- `GachaCeilingItem` も同じ読み替えで `GachaId` ごとに `CeilingItem(Order, Kind, ItemId)` を `Order` 昇順で作る
- `All` は `GachaId` 昇順

`MasterDuplicateConversionCatalog : IDuplicateConversionCatalog`（`MemoryDatabase db`）: コンストラクタで `Dictionary<(GrantedItemKind, int), int>` を作る。育成アイドル: `db.IdolTable.All` の各行の `Rarity` で `db.DuplicateConversionTable.All`（`ItemKind == Idol && Rarity == 行のレア`）の `Amount` を引く。サポートカード: `db.SupportCardTable.All` の `Rarity` で `ItemKind == SupportCard` の行を引く。行が無ければ `InvalidOperationException`。`GetAmount` はそのキーが無ければ `InvalidOperationException`。

### 3.6 Api（`namespace Uta.Server.Api`）

```csharp
// UtaServiceAttribute.cs   （基盤の例 external/VolTech-MagicOnion/tests/VolTech.Server.Tests/Support/TestServices.cs の ThrowBusinessFilter と同じく MagicOnionFilterAttribute を継承する）
/// <summary>IPlayerService 以外の Uta のサービスの実装クラスに付ける。プレイヤー未作成を PLAYER_NOT_CREATED で弾き、UseCaseException を BusinessException に変換する。</summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class UtaServiceAttribute : MagicOnionFilterAttribute
{
    public override async ValueTask Invoke(ServiceContext context, Func<ServiceContext, ValueTask> next)
    {
        if (context.FindAuthContext() is { PlayerCreated: false })
        {
            throw new BusinessException(PlayerErrors.NotCreated, "player not created");
        }

        try
        {
            await next(context);
        }
        catch (UseCaseException exception)
        {
            throw new BusinessException(ToErrorCode(exception.Error), exception.Message);
        }
    }

    private static ErrorCode ToErrorCode(UseCaseError error) => error switch
    {
        UseCaseError.GachaInvalidCount => GachaErrors.InvalidCount,
        UseCaseError.GachaNotFound => GachaErrors.NotFound,
        UseCaseError.GachaCostMismatch => GachaErrors.CostMismatch,
        UseCaseError.GachaNotEnoughCurrency => GachaErrors.NotEnoughCurrency,
        UseCaseError.GachaNotEnoughPoints => GachaErrors.NotEnoughPoints,
        _ => throw new ArgumentOutOfRangeException(nameof(error), error, null),
    };
}
```

（`using MagicOnion.Server; using VolTech.Server; using VolTech.Server.Abstractions; using Uta.Shared.Contracts.Gacha; using Uta.Shared.Contracts.Player; using Uta.Server.Application.Common;`。`MagicOnionFilterAttribute` の名前空間は基盤の `TestServices.cs` の `using` を見て合わせる。アプリが付ける属性のフィルターは基盤のフィルター（Auth の Order は `int.MinValue + 2`）より内側（Order 既定 `int.MaxValue`）なので、`FindAuthContext()` が使える。`[SkipAuth]` の RPC では null なので素通りする。）

```csharp
// GachaService.cs   （PlayerService.cs と同じ先頭の #pragma warning disable CS8644 とコメント）
[UtaService]
public sealed class GachaService(ITransactions transactions, GetGachasUseCase getGachas, DrawUseCase draw, ExchangeUseCase exchange)
    : ServiceBase<IGachaService>, IGachaService
{
    public async UnaryResult<GetGachasResponse> GetGachasAsync()
    {
        var playerId = Context.GetAuthContext().PlayerId;
        return await transactions.RunAsync(Context, async tx =>
        {
            var gachas = await getGachas.ExecuteAsync(tx, playerId);
            return new GetGachasResponse(gachas.Select(x => new GachaStatusDto(x.GachaId, x.ExchangePoints)).ToArray());
        });
    }

    public async UnaryResult<DrawResponse> DrawAsync(DrawRequest request)
    {
        var playerId = Context.GetAuthContext().PlayerId;
        var requestId = Context.CallContext.RequestHeaders.GetValue(ApiHeaders.RequestId)!;   // 無い・形が違うときは Transactions が本体の前に INVALID_ARGUMENT を返す
        return await transactions.RunAsync(Context, async tx =>
        {
            var result = await draw.ExecuteAsync(tx, playerId, requestId, request.GachaId, request.Count, request.ExpectedCost);
            return new DrawResponse(result.Results.Select(ToDto).ToArray(), result.GachaCurrency, result.ExchangePoints);
        });
    }

    public async UnaryResult<ExchangeResponse> ExchangeAsync(ExchangeRequest request)
    {
        var playerId = Context.GetAuthContext().PlayerId;
        var requestId = Context.CallContext.RequestHeaders.GetValue(ApiHeaders.RequestId)!;
        return await transactions.RunAsync(Context, async tx =>
        {
            var result = await exchange.ExecuteAsync(tx, playerId, requestId, request.GachaId, request.CeilingItemOrder);
            return new ExchangeResponse(ToDto(result.Result), result.ExchangePoints);
        });
    }

    private static DrawResultDto ToDto(GachaResult x) => new DrawResultDto(
        x.Slot, (ItemKind)(int)x.Kind, x.ItemId, (GachaOutcome)(int)x.Outcome, x.Shards, x.SupportPoints, x.SupportCardUid?.ToString() ?? "");
}
```

`ItemKind`・`GachaOutcome` は Contracts の型（`using Uta.Shared.Contracts.Gacha;`）。`Uta.MasterData` は参照しない。`PlayerService` には `[UtaService]` を付けない（`SignInAsync` が自分で `PLAYER_NOT_CREATED` を投げる）。

### 3.7 `Program.cs` の登録（`AddSingleton<IInitialGrantCatalog, …>` の次に足す）

```csharp
builder.Services.AddSingleton<IGachaCatalog, MasterGachaCatalog>();
builder.Services.AddSingleton<IDuplicateConversionCatalog, MasterDuplicateConversionCatalog>();
builder.Services.AddSingleton<IRandomSource, SecureRandomSource>();
builder.Services.AddScoped<IGachaRepository, GachaRepository>();
builder.Services.AddScoped<ItemGranter>();
builder.Services.AddScoped<GetGachasUseCase>();
builder.Services.AddScoped<DrawUseCase>();
builder.Services.AddScoped<ExchangeUseCase>();
```

`GachaService` は登録しない（`MapMagicOnionService()` が拾う）。

## 4. タスク

各タスクの終わりに `dotnet build Uta.slnx`（警告 0・エラー 0）が通る。Contracts の `.meta` は初期付与の計画のタスク 1 と同じ手順。

1. **Contracts と DB**: 3.1・3.2。確かめ: `dotnet build Uta.slnx`、既存の `dotnet test --project server/Uta.Server.Tests`。
2. **Domain・Application の型とポート・Infrastructure の SQL**: 3.3 と、3.4 のうちユースケース以外（`UseCaseError`・`UseCaseException`・`IRandomSource`・`GachaResult` ほかの型・`IGachaCatalog`・`IDuplicateConversionCatalog`・`IGachaRepository`・`GachaHistoryRow`・`ItemGranter`・追加するポートのメソッド）と、3.5 の SQL を実装するリポジトリのメソッド・`SecureRandomSource`。ポートを足したら実装も同じタスクで作る（ビルドを通すため）。確かめ: `dotnet build Uta.slnx`。
3. **ユースケース**: 3.4 の `GetGachasUseCase`・`DrawUseCase`・`ExchangeUseCase`（`GachaStatus`・`DrawResult`・`ExchangeResult` を含む）。確かめ: `dotnet build Uta.slnx`。
4. **カタログと Api と登録**: 3.5 の `MasterGachaCatalog`・`MasterDuplicateConversionCatalog`、3.6、3.7。確かめ: `dotnet build Uta.slnx`。
5. **テスト支援と Architecture テスト**: 5章の「支援」と `DomainNamespace_HasTypes`・`Services_HaveUtaServiceAttribute`。確かめ: `dotnet test --project server/Uta.Server.Tests`（既存の結合テストを含めて通る）。
6. **単体テスト**: 5章の「単体テスト」の残り（`GachaPickerTests`・`GachaRandomTests`・`ItemGrantRulesTests`・`MasterGachaCatalogTests`・`ContractsEnumsMatchTests`）。確かめ: `dotnet test --project server/Uta.Server.Tests`（DB が要らないもの。全件通ること）。
7. **結合テスト**: 5章の「結合テスト」。確かめ: `dotnet test --project server/Uta.Server.Tests` が全件通る（Docker が要る）。

**wiki への反映は実装担当の仕事ではない**。3 本の実装が済んだあと、メインのセッションが、2章の最後の行のページ（`gacha-support-server.md`・`server-foundation.md`・`api-design.md`・`gacha-tables.md`・`game-spec.md`）を実装結果に合わせて直す（抽選のテストの置き場、ゲームログの中身、`UseCaseException` と `[UtaService]`、`IRandomSource`、`GachaOutcome` と DTO が配列であること、`ItemKind` の一致テスト）。実装担当はこの章の 1〜7 だけを行う。

境界: `count` が 0・2・11・負 → `GACHA_INVALID_COUNT`。通貨ちょうどで 10 連 → 残り 0。抽選・所持品・履歴・交換pt・冪等性の保存済みレスポンスは同じトランザクション（例外で全部戻る）。同じプレイヤーの同時の要求は VolTech-MagicOnion が直列にする。

## 5. テスト

### 支援

```csharp
// PlayerClient: 既存の CallAsync<T>（IPlayerService 用）は変えない。汎用版を足す。呼ぶときは型引数を省略できないので、
//   client.CallAsync<IGachaService, DrawResponse>(x => x.DrawAsync(new DrawRequest(1, 1, 250)), "req-2") の形で書く
public Task<T> CallAsync<TService, T>(Func<TService, UnaryResult<T>> call, string? requestId) where TService : IService<TService>;
// 中身は既存と同じ（authorization と、requestId が null でなければ x-request-id を付ける）。MagicOnionClient.Create<TService>(channel).WithHeaders(headers)

// UtaServerFactory: コンストラクタに IRandomSource? randomSource = null を足す（最後の引数）。null でなければ
//   ConfigureTestServices で services.RemoveAll<IRandomSource>(); services.AddSingleton(randomSource);

// ScriptedRandomSource : IRandomSource   （Integration/Support/）
//   コンストラクタ ScriptedRandomSource(params ulong[] values)。Create() は、同じ values を先頭から順に返す IRandom を返す
//   （値は Queue を共有する。複数回 Create されても同じ列を続けて使う）。空になって NextUInt64 が呼ばれたら InvalidOperationException
```

選ばせたい値 `r` に対しては `r + 表の重みの合計` を渡す（[gacha-support-server.md](../wiki/architecture/server/gacha-support-server.md) 1章。`RandomOps.NextInt` の棄却法を避けるため）。テストのヘルパー: `Normal(int r) => (ulong)(r + 10000)`、`Guaranteed(int r) => (ulong)(r + 2200)`。

マスタ（`masterdata/csv`）から計算した当たりの範囲（`r` の区間）:

| 通常枠（合計 10000） | r |
|---|---|
| 育成アイドル 1（R）/ 2（R）/ 3（SR）/ 4（SSR） | 0〜1559 / 1560〜3119 / 3120〜3799 / 3800〜3999 |
| サポートカード 1〜5（R。各 936） | 4000 から 936 ずつ（1: 4000〜4935、2: 4936〜、3: 5872〜、4: 6808〜、5: 7744〜8679） |
| サポートカード 6〜8（SR。各 340） / 9・10（SSR。各 150） | 8680〜9019、9020〜9359、9360〜9699 / 9700〜9849、9850〜9999 |

| 確定枠（合計 2200） | r |
|---|---|
| 育成アイドル 3 / 4 | 0〜679 / 680〜879 |
| サポートカード 6 / 7 / 8 / 9 / 10 | 880〜1219 / 1220〜1559 / 1560〜1899 / 1900〜2049 / 2050〜2199 |

レア: 育成アイドル 1・2 = R、3 = SR、4 = SSR。サポートカード 1〜5 = R、6〜8 = SR、9・10 = SSR。重複の量: 育成アイドル R 5 / SR 25 / SSR 50、サポートカード（完凸後）R 300 / SR 1000 / SSR 3000。ガチャは `GachaId` 1 の 1 本（単発 250・10 連 2500・天井 200 pt・交換対象は `Order` 1 = 育成アイドル 4、`Order` 2 = サポートカード 9）。初期付与はガチャ通貨 2500・育成アイドル 1・2・サポートカード 1〜5。

### 単体テスト

| テスト名 | 期待 |
|---|---|
| `GachaPickerTests.Pick_BoundaryValues_SelectExpectedRows` | 重み `[1560, 1560, 680, 200]`（合計 4000）で、`r` = 0・1559 → 0、1560・3119 → 1、3120・3799 → 2、3800・3999 → 3（`r + 4000` を返す `IRandom` を使う） |
| `ItemGrantRulesTests.Decide_ByKindAndState` | `(Idol, owned: false)` → New、`(Idol, owned: true)` → Duplicate、`(SupportCard, maxed: false)` → New、`(SupportCard, maxed: true)` → Duplicate |
| `GachaPickerTests.Pick_SingleRow_AlwaysZero` | 重み `[5]` で `r = 0` → 0 |
| `GachaRandomTests.FromSeed_SameSeed_SameSequence` | 同じシードで 3 回引いた列が一致し、`Xoshiro256StarStar.FromSeed` と一致する |
| `GachaRandomTests.CreateSecure_TwoInstances_Differ` | 2 つの 最初の `NextUInt64` が異なる |
| `MasterGachaCatalogTests.Catalog_MatchesMasterCsv` | `MasterDataLoader.Load()` から作る。`All` は 1 件、`SingleCost == 250`・`TenCost == 2500`・`CeilingPoints == 200`。`Normal.Weights` の合計が 10000・件数 14、`Guaranteed.Weights` の合計が 2200・件数 7、`Guaranteed` に育成アイドル 1・2 とサポートカード 1〜5 を含まない。`CeilingItems` は `(1, Idol, 4)`・`(2, SupportCard, 9)` |
| `MasterGachaCatalogTests.DuplicateConversion_ReturnsAmountByRarity` | `MasterDuplicateConversionCatalog`: `(Idol, 1)` → 5、`(Idol, 3)` → 25、`(Idol, 4)` → 50、`(SupportCard, 1)` → 300、`(SupportCard, 6)` → 1000、`(SupportCard, 9)` → 3000 |
| `ContractsEnumsMatchTests.ItemKind_MatchesMasterAndApplication` | `Uta.Shared.Contracts.Gacha.ItemKind` と `Uta.MasterData.ItemKind` の名前と数値が一致。`GrantedItemKind` の `Idol`・`SupportCard` の数値もそれと一致。`GachaOutcome.New`・`Duplicate` が `GrantOutcome` と同じ数値 |
| `ArchitectureTests.DomainNamespace_HasTypes` | `Assert.NotEmpty(LayerTypes("Domain").GetTypes())` |
| `ArchitectureTests.Services_HaveUtaServiceAttribute` | NetArchTest ではなくリフレクションで書く: `var services = typeof(PlayerResolver).Assembly.GetTypes().Where(t => !t.IsAbstract && IsServiceBase(t) && t != typeof(PlayerService)).ToList();`（`IsServiceBase` は `for (var b = t.BaseType; b is not null; b = b.BaseType) if (b.IsGenericType && b.GetGenericTypeDefinition() == typeof(ServiceBase<>)) return true; return false;`）。`Assert.NotEmpty(services)` と `Assert.All(services, t => Assert.NotNull(t.GetCustomAttribute<UtaServiceAttribute>()))` |

### 結合テスト（`GachaTests`。`[Collection(PostgresCollection.Name)]`。各テストは `new UtaServerFactory(postgres, randomSource: …)` と `PlayerClient.CreateAsync` で始め、`SignUpAsync` を `req-1` で呼ぶ。以降の `x-request-id` は `req-2` からテストごとに順に変える。`DrawAsync` の `ExpectedCost` は単発 250・10 連 2500。DB の検査（`postgres.QueryScalarAsync<T>`）は全テストで DB を共有するので、必ず `WHERE player_id = $1`（`PlayerClient.PlayerId`）で絞る。`count(*)` は `long`、`int` 列（`gacha_currency` など）は `int` で読む。`RpcException` の検査は 5a の `SignIn_BeforeSignUp_ReturnsPlayerNotCreated` と同じ（`StatusCode == FailedPrecondition` と `grpc-status-details-bin` に reason を含む））

| テスト名 | 手順 | 期待 |
|---|---|---|
| `Draw_Single_PaysAndGrantsNewCard` | 乱数 `[Normal(4000)]`（サポートカード 1）で単発 | `Results` 1 件: `Slot == 1`・`ItemKind == SupportCard`・`ItemId == 1`・`Outcome == New`・`SupportCardUid` が 26 文字。`GachaCurrency == 2250`・`ExchangePoints == 1`。DB: `player_support_cards` の `card_id = 1` の行が 2、`gacha_histories` が 1 行（`slot = 1`・`gacha_table_id = 1`・`item_kind = 2`・`item_id = 1`・`outcome = 1`・`request_id` が送ったもの）、`gacha_exchange_points` の `points = 1` |
| `Draw_Single_WritesOneGameLog` | 乱数 `[Normal(4000)]` の単発。同じ `x-request-id` で再送。`Count = 2` の失敗 | `SELECT count(*) FROM game_logs WHERE player_id = $1 AND kind = 'gacha' AND body->>'reason' = 'gacha_draw'` が 1。再送・失敗の後も 1 のまま |
| `Draw_Ten_UsesGuaranteedTableForTenthSlot` | 乱数 = 通常枠 9 個 `[4000, 4936, 5872, 6808, 7744, 3120, 3120, 4000, 4936]`（それぞれ `Normal(r)`）+ 確定枠 `[Guaranteed(680)]`、10 連 | `Results` が 10 件で `Slot` は 1〜10。1〜5 枠目 `Outcome == New`（サポートカード 1〜5）、6 枠目 = 育成アイドル 3 `New`、7 枠目 = 育成アイドル 3 `Duplicate`・`Shards == 25`、8・9 枠目 = サポートカード 1・2 `New`（`SupportCardUid` あり）、10 枠目 = 育成アイドル 4 `New`。`GachaCurrency == 0`・`ExchangePoints == 10`。DB: `gacha_histories` が 10 行、`player_idols` の `idol_id = 3` の `shards = 25`、`player_support_cards` の `card_id = 1` が 3 行 |
| `Draw_DuplicateIdol_ConvertsToShards` | 乱数 `[Normal(0)]`（育成アイドル 1。初期付与で所持）で単発 | `Outcome == Duplicate`・`Shards == 5`・`SupportCardUid` は空文字。DB: `player_idols` の `idol_id = 1` の `shards = 5` |
| `Draw_MaxedSupportCard_ConvertsToSupportPoints` | `UPDATE player_support_cards SET limit_break = 4 WHERE player_id = $1 AND card_id = 1` で前提を作り、乱数 `[Normal(4000)]` で単発 | `Outcome == Duplicate`・`SupportPoints == 300`。DB: `player_support_cards` の `card_id = 1` は 1 行のまま、`players.support_points == 10300` |
| `Draw_InvalidCount_Rejected` | `Count = 2` | `GACHA_INVALID_COUNT`。DB: `gacha_currency == 2500` |
| `Draw_UnknownGacha_Rejected` | `GachaId = 99` | `GACHA_NOT_FOUND` |
| `Draw_CostMismatch_Rejected` | `ExpectedCost = 100` | `GACHA_COST_MISMATCH`。通貨は変わらない |
| `Draw_NotEnoughCurrency_Rejected` | `UPDATE players SET gacha_currency = 249` の後に単発 | `GACHA_NOT_ENOUGH_CURRENCY`。通貨 249 のまま、`gacha_histories` は 0 行 |
| `Draw_SameRequestId_ReturnsStoredResponse` | 乱数 `[Normal(4000)]` で同じ `x-request-id` を 2 回 | 2 回目も同じ `SupportCardUid`。通貨は 2250、`gacha_histories` は 1 行（乱数も 1 つしか使わない） |
| `Draw_ConcurrentRequests_NeverOverspend` | 本物の乱数（`randomSource` なし）で、別々の `x-request-id` の単発を 12 並列（`Task.WhenAll`） | 成功がちょうど 10 件、失敗 2 件は `GACHA_NOT_ENOUGH_CURRENCY`。`gacha_currency == 0`、`gacha_exchange_points` の `points == 10` |
| `Draw_BeforeSignUp_ReturnsPlayerNotCreated` | 登録だけして（`SignUpAsync` なし）`DrawAsync` | `PLAYER_NOT_CREATED`（`FailedPrecondition`） |
| `Draw_WithoutRequestId_InvalidArgument` | `requestId: null` | `InvalidArgument` |
| `Exchange_EnoughPoints_GrantsAndSpends` | `INSERT INTO gacha_exchange_points (player_id, gacha_id, points) VALUES ($1, 1, 250)` の後 `Order = 1`（育成アイドル 4） | `Result.Slot == 0`・`ItemKind == Idol`・`ItemId == 4`・`Outcome == New`。`ExchangePoints == 50`。DB: `player_idols` に `idol_id = 4`、`gacha_histories` が 1 行（`slot = 0`・`gacha_table_id = 0`）。通貨は 2500 のまま |
| `Exchange_WritesOneGameLog` | `Exchange_EnoughPoints_GrantsAndSpends` と同じ前提で交換 | `game_logs` に `kind = 'gacha' AND body->>'reason' = 'gacha_exchange'` が 1 件 |
| `Exchange_SupportCardItem_AddsRow` | 同じ前提で `Order = 2`（サポートカード 9） | `Outcome == New`・`SupportCardUid` あり。`ExchangePoints == 50` |
| `Exchange_NotEnoughPoints_Rejected` | 前提の行なし（交換pt の行がない）で `Order = 1` | `GACHA_NOT_ENOUGH_POINTS` |
| `Exchange_UnknownItemOrGacha_Rejected` | `Order = 3` と `GachaId = 99` | どちらも `GACHA_NOT_FOUND` |
| `GetGachas_ReturnsExchangePoints` | `GetGachasAsync`（`requestId: null`。`[NoSideEffects]`）→ 乱数 `[Normal(0)]` で単発 → 再び `GetGachasAsync` | 最初は `Gachas` が 1 件で `GachaId == 1`・`ExchangePoints == 0`、後は 1 |
| `Migration_CreatesGachaTables` | — | `information_schema.tables` に `gacha_exchange_points`・`gacha_histories` の 2 つ |

`PLAYER_NOT_CREATED` の判定は `AuthContext.PlayerCreated`（`PlayerResolver` の結果）。実行: `dotnet test --project server/Uta.Server.Tests`（Docker が要る）。

## 6. 未決事項

なし。
- `GetHistoryAsync` は作らない（[api-design.md](../wiki/architecture/api-design.md) 1章）。
- 履歴の `outcome`・`item_kind` は int（enum の数値）で持つ。
- 交換の履歴の `gacha_table_id` は 0（[gacha.md](../wiki/systems/gacha.md) 4章）。
