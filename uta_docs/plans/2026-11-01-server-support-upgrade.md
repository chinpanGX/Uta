---
title: 実装計画: サポートカードのレベル上げ・上限解放（サーバー）
status: done
updated: 2026-11-01
spec: ../wiki/systems/idol-support.md, ../wiki/architecture/server/gacha-support-server.md, ../wiki/architecture/api-design.md
---

# 実装計画: サポートカードのレベル上げ・上限解放（サーバー）

## 1. 目的と前提

- 何を実装するか: `ISupportService` の `LevelUpAsync`（目標のレベルまでをまとめて強化pt・ゴールドで払う）と `LimitBreakAsync`（同じカード ID の別の所持ID を素材にして上限解放）を、サーバーと `Uta.Shared.Contracts` に作る。仕様は [idol-support.md](../wiki/systems/idol-support.md) 2.2・2.2.1、処理の手順の正は [gacha-support-server.md](../wiki/architecture/server/gacha-support-server.md) 3章、RPC・DTO・エラーは [api-design.md](../wiki/architecture/api-design.md) 1.2・2章。
- 位置づけ: [game-spec.md](../wiki/overview/game-spec.md)「実装計画の単位と順」の**順 5 の 3/3**（受け持つ完了条件 3 のうち、レベル上げ・上限解放ができること）。条件 3 の「次のプロデュースの開始時の性能に反映される」は順 6 のプロデュースの開始が読むので、**game-spec.md の順 5 の受け持ちから外し、順 6 に移す**（完了時の反映。4章タスク 4）。
- 前提: [初期付与](2026-11-01-server-initial-grant.md)・[ガチャ](2026-11-01-server-gacha.md) の計画が済んでいる（`UseCaseError`・`UseCaseException`・`[UtaService]`（`UtaServiceAttribute`）・`SupportRules.MaxLimitBreak`・`GameLogJson`・`ISupportCardRepository`・`IPlayerRepository`・テスト支援がある）。
- 関係する ADR: [0008](../wiki/decisions/0008-shared-domain-and-onion.md)、[0010](../wiki/decisions/0010-database.md)
- **この計画で決めないこと**: 性能の計算（アビリティの値。順 6）、編成の検証、クライアント側、素材の確認ダイアログ。必要量の合計（`GetLevelUpCost`）は、いまはサーバーの Infrastructure（マスタの行）で足す。クライアントの予測表示のための `Uta.Shared.Domain` への移し替えは、クライアントの計画で決める（`Uta.Shared.Domain` は順 4 のセッションが触るため、ここでは触らない）。
- 決めたこと: 素材の上限解放回数は問わない（上限解放済みの行も素材にできる。[idol-support.md](../wiki/systems/idol-support.md) 2.2.1 に制限が書かれていない。素材の投資が戻らない旨はクライアントの確認ダイアログの仕事）。ゲームログは `LevelUpAsync`・`LimitBreakAsync` がそれぞれ 1 件（[server-foundation.md](../wiki/architecture/server/server-foundation.md)「ゲームログ」）。

## 2. 触るファイル

| 操作 | パス | 中身 |
|---|---|---|
| 作成 | `shared/Uta.Shared.Contracts/Support/` の下: `ISupportService.cs`・`LevelUpRequest.cs`・`LevelUpResponse.cs`・`LimitBreakRequest.cs`・`LimitBreakResponse.cs`・`SupportErrors.cs` と各 `.cs.meta`、`Support.meta`（フォルダ） | サービス IF・DTO・エラー |
| 変更 | `shared/Uta.Shared.Contracts/Player/PlayerErrors.cs` | `NotEnoughGold` を足す |
| 変更 | `server/Uta.Server/Application/Common/UseCaseError.cs` | 値を足す |
| 作成 | `server/Uta.Server/Application/Support/ISupportCatalog.cs`・`LevelUpUseCase.cs`・`LimitBreakUseCase.cs` | Application |
| 変更 | `server/Uta.Server/Application/Support/ISupportCardRepository.cs`・`Application/Player/IPlayerRepository.cs` | メソッドを足す |
| 変更 | `server/Uta.Server/Infrastructure/SupportCardRepository.cs`・`PlayerRepository.cs` | 実装 |
| 作成 | `server/Uta.Server/Infrastructure/MasterData/MasterSupportCatalog.cs` | マスタの読み込み |
| 作成 | `server/Uta.Server/Api/SupportService.cs` | Api |
| 変更 | `server/Uta.Server/Api/UtaServiceAttribute.cs` | 変換の分岐を足す |
| 変更 | `server/Uta.Server/Program.cs` | 登録 |
| 作成 | `server/Uta.Server.Tests/Integration/SupportTests.cs` | 結合テスト |
| 作成 | `server/Uta.Server.Tests/MasterData/MasterSupportCatalogTests.cs` | 単体テスト |

## 3. 型とシグネチャ

### 3.1 Contracts（C# 9・`Nullable` なし。DTO の書き方はガチャの計画 3.2 と同じ）

```csharp
// 4 つの DTO。ファイルは 1 クラス 1 ファイル。using MessagePack;  namespace Uta.Shared.Contracts.Support
// 書き方は shared/Uta.Shared.Contracts/Player/SignUpResponse.cs と同じ（コンストラクタ + 読み取り専用プロパティ + [SerializationConstructor]）
[MessagePackObject]
public sealed class LevelUpRequest
{
    [SerializationConstructor]
    public LevelUpRequest(string supportCardUid, int targetLevel) { SupportCardUid = supportCardUid; TargetLevel = targetLevel; }
    [Key(0)] public string SupportCardUid { get; }
    [Key(1)] public int TargetLevel { get; }
}

[MessagePackObject]
public sealed class LevelUpResponse
{
    [SerializationConstructor]
    public LevelUpResponse(int level, int supportPoints, int gold) { Level = level; SupportPoints = supportPoints; Gold = gold; }
    [Key(0)] public int Level { get; }
    [Key(1)] public int SupportPoints { get; }   // 残り
    [Key(2)] public int Gold { get; }            // 残り
}

[MessagePackObject]
public sealed class LimitBreakRequest
{
    [SerializationConstructor]
    public LimitBreakRequest(string supportCardUid, string materialUid) { SupportCardUid = supportCardUid; MaterialUid = materialUid; }
    [Key(0)] public string SupportCardUid { get; }   // 解放する
    [Key(1)] public string MaterialUid { get; }      // 素材
}

[MessagePackObject]
public sealed class LimitBreakResponse
{
    [SerializationConstructor]
    public LimitBreakResponse(int limitBreak, int maxLevel) { LimitBreak = limitBreak; MaxLevel = maxLevel; }
    [Key(0)] public int LimitBreak { get; }   // 解放後の回数
    [Key(1)] public int MaxLevel { get; }     // 解放後の上限レベル
}

// ISupportService.cs   using MagicOnion;
public interface ISupportService : IService<ISupportService>
{
    UnaryResult<LevelUpResponse> LevelUpAsync(LevelUpRequest request);
    UnaryResult<LimitBreakResponse> LimitBreakAsync(LimitBreakRequest request);
}

// SupportErrors.cs   using VolTech.Server.Abstractions;  domain は "uta"
public static class SupportErrors
{
    public static readonly ErrorCode InvalidTargetLevel = new ErrorCode("uta", "SUPPORT_INVALID_TARGET_LEVEL");
    public static readonly ErrorCode NotEnoughPoints = new ErrorCode("uta", "SUPPORT_NOT_ENOUGH_POINTS");
    public static readonly ErrorCode MaxLimitBreak = new ErrorCode("uta", "SUPPORT_MAX_LIMIT_BREAK");
    public static readonly ErrorCode NotOwned = new ErrorCode("uta", "SUPPORT_NOT_OWNED");
    public static readonly ErrorCode InvalidMaterial = new ErrorCode("uta", "SUPPORT_INVALID_MATERIAL");
}
```

`.cs.meta` と `Support.meta`（フォルダ）は、初期付与の計画のタスク 1 の手順で作る（`.cs.meta` は `fileFormatVersion: 2` と `guid: <新しい 32 桁の小文字 16 進>` の 2 行。フォルダの `.meta` は `shared/Uta.Shared.Contracts/Player.meta` を写して `guid` だけ新しくする）。`ISupportService` に `[SkipAuth]` は付けない。

`PlayerErrors` に `public static readonly ErrorCode NotEnoughGold = new ErrorCode("uta", "PLAYER_NOT_ENOUGH_GOLD");` を足す。

### 3.2 Application

`UseCaseError` に足す: `SupportInvalidTargetLevel, SupportNotEnoughPoints, SupportMaxLimitBreak, SupportNotOwned, SupportInvalidMaterial, PlayerNotEnoughGold`。

```csharp
// namespace Uta.Server.Application.Support
public sealed record LevelUpCost(int SupportPoints, int Gold);

public interface ISupportCatalog
{
    /// <summary>上限解放の回数ごとの上限レベル（SupportLevelCap）。カード ID・回数が無ければ InvalidOperationException。</summary>
    int GetMaxLevel(int cardId, int limitBreak);
    /// <summary>Lv fromLevel → toLevel の必要量の合計（SupportLevelCost の Level が fromLevel 以上 toLevel 未満の行の和）。toLevel > fromLevel。行が足りなければ InvalidOperationException。</summary>
    LevelUpCost GetLevelUpCost(int cardId, int fromLevel, int toLevel);
}

```

`ISupportCardRepository` に足す:

```csharp
/// <summary>自分の所持IDの行（初期付与の計画の OwnedSupportCard）。無い・他人の行なら null。</summary>
Task<OwnedSupportCard?> FindAsync(TxContext tx, Ulid playerId, Ulid uid);
Task UpdateLevelAsync(TxContext tx, Ulid uid, int level);
/// <summary>素材を消す。自分の所持で、カード ID が cardId の行だけ。消せたら true。</summary>
Task<bool> TryDeleteMaterialAsync(TxContext tx, Ulid playerId, Ulid uid, int cardId);
/// <summary>上限解放を 1 増やし、増やした後の回数を返す。既に SupportRules.MaxLimitBreak なら null（更新は 0 行）。</summary>
Task<int?> TryLimitBreakAsync(TxContext tx, Ulid playerId, Ulid uid);
```

`IPlayerRepository` に足す:

```csharp
public sealed record PlayerBalance(int SupportPoints, int Gold);     // IPlayerRepository.cs と同じファイルに置く
/// <summary>強化ptとゴールドを 1 文で払い、残りを返す。どちらかが足りなければ null（更新は 0 行）。</summary>
Task<PlayerBalance?> TrySpendSupportAndGoldAsync(TxContext tx, Ulid playerId, int supportPoints, int gold);
```

```csharp
public sealed record LevelUpResult(int Level, int SupportPoints, int Gold);
public sealed class LevelUpUseCase(IPlayerRepository players, ISupportCardRepository supportCards, ISupportCatalog catalog)
{
    public async Task<LevelUpResult> ExecuteAsync(TxContext tx, Ulid playerId, Ulid uid, int targetLevel);
}

public sealed record LimitBreakResult(int LimitBreak, int MaxLevel);
public sealed class LimitBreakUseCase(ISupportCardRepository supportCards, ISupportCatalog catalog)
{
    public async Task<LimitBreakResult> ExecuteAsync(TxContext tx, Ulid playerId, Ulid uid, Ulid materialUid);
}
```

`LevelUpUseCase.ExecuteAsync`（[gacha-support-server.md](../wiki/architecture/server/gacha-support-server.md) 3章 `LevelUpAsync` のとおり）:

1. `state = await supportCards.FindAsync(tx, playerId, uid)`。null なら `SupportNotOwned`
2. `max = catalog.GetMaxLevel(state.CardId, state.LimitBreak)`。`targetLevel <= state.Level || targetLevel > max` なら `SupportInvalidTargetLevel`
3. `cost = catalog.GetLevelUpCost(state.CardId, state.Level, targetLevel)`
4. `balance = await players.TrySpendSupportAndGoldAsync(tx, playerId, cost.SupportPoints, cost.Gold)`。null なら `current = await players.FindAsync(tx, playerId)`（既存の `PlayerSnapshot`。プレイヤーは `[UtaService]` が存在を保証している）を読み、`current.SupportPoints < cost.SupportPoints` なら `SupportNotEnoughPoints`、そうでなければ `PlayerNotEnoughGold`（強化pt を先に見る）
5. `await supportCards.UpdateLevelAsync(tx, uid, targetLevel)`
6. ゲームログを 1 件書く（`using System.Text.Json.Nodes;`）:
   ```csharp
   tx.WriteGameLog(GameLog.Event("support", "support_level_up")
       .Target(uid.ToString())
       .Field("card_id", state.CardId).Field("from_level", state.Level).Field("to_level", targetLevel)
       .Field("support_points_spent", cost.SupportPoints).Field("gold_spent", cost.Gold)
       .Field("support_points_after", balance.SupportPoints).Field("gold_after", balance.Gold));
   ```
7. `new LevelUpResult(targetLevel, balance.SupportPoints, balance.Gold)`

`LimitBreakUseCase.ExecuteAsync`（同 3章 `LimitBreakAsync` の検証の順）:

1. `target = await supportCards.FindAsync(tx, playerId, uid)`。null なら `SupportNotOwned`
2. `materialUid == uid` なら `SupportInvalidMaterial`
3. `await supportCards.TryDeleteMaterialAsync(tx, playerId, materialUid, target.CardId)` が false なら `SupportInvalidMaterial`
4. `limitBreak = await supportCards.TryLimitBreakAsync(tx, playerId, uid)`。null なら `SupportMaxLimitBreak`（例外でトランザクションごと戻り、3 の削除も戻る）
5. ゲームログを 1 件書く:
   ```csharp
   tx.WriteGameLog(GameLog.Event("support", "support_limit_break")
       .Target(uid.ToString())
       .Field("card_id", target.CardId).Field("material_uid", materialUid.ToString()).Field("limit_break", limitBreak.Value));
   ```
6. `new LimitBreakResult(limitBreak.Value, catalog.GetMaxLevel(target.CardId, limitBreak.Value))`

### 3.3 Infrastructure（SQL。書き方は既存のリポジトリと同じ）

```sql
-- SupportCardRepository.FindAsync（初期付与の ListAsync と同じ形の Row（Uid は string）に読み、Ulid.Parse して OwnedSupportCard に詰める。無ければ null）
SELECT support_card_uid AS Uid, card_id AS CardId, level AS Level, limit_break AS LimitBreak
FROM player_support_cards WHERE support_card_uid = @uid AND player_id = @playerId
-- UpdateLevelAsync（uid は FindAsync で自分の行と確かめてある）
UPDATE player_support_cards SET level = @level WHERE support_card_uid = @uid
-- TryDeleteMaterialAsync（影響行数が 1 なら true）
DELETE FROM player_support_cards WHERE support_card_uid = @uid AND player_id = @playerId AND card_id = @cardId
-- TryLimitBreakAsync（QuerySingleOrDefaultAsync<int?>。@max に SupportRules.MaxLimitBreak）
UPDATE player_support_cards SET limit_break = limit_break + 1
WHERE support_card_uid = @uid AND player_id = @playerId AND limit_break < @max
RETURNING limit_break
-- PlayerRepository.TrySpendSupportAndGoldAsync（private なクラス Row { int SupportPoints; int Gold } に QuerySingleOrDefaultAsync<Row> で読み、null でなければ PlayerBalance に詰める。0 行なら null）
UPDATE players SET support_points = support_points - @supportPoints, gold = gold - @gold
WHERE player_id = @playerId AND support_points >= @supportPoints AND gold >= @gold
RETURNING support_points AS SupportPoints, gold AS Gold
```

`MasterSupportCatalog : ISupportCatalog`（`MemoryDatabase db`。コンストラクタで辞書を作る）: `db.SupportCardTable.All` から `cardId → Rarity`、`db.SupportLevelCapTable.All` から `(Rarity, LimitBreak) → MaxLevel`、`db.SupportLevelCostTable.All` から `(Rarity, Level) → (SupportPoints, Gold)`。`GetMaxLevel` はカード ID → レア → 辞書。`GetLevelUpCost` は `Level` が `fromLevel` から `toLevel - 1` までの行を足す（欠けたら `InvalidOperationException`）。

### 3.4 Api

`UtaServiceAttribute.ToErrorCode`（ガチャの計画で作った switch）に足す: `SupportInvalidTargetLevel → SupportErrors.InvalidTargetLevel`、`SupportNotEnoughPoints → SupportErrors.NotEnoughPoints`、`SupportMaxLimitBreak → SupportErrors.MaxLimitBreak`、`SupportNotOwned → SupportErrors.NotOwned`、`SupportInvalidMaterial → SupportErrors.InvalidMaterial`、`PlayerNotEnoughGold → PlayerErrors.NotEnoughGold`。

```csharp
[UtaService]
public sealed class SupportService(ITransactions transactions, LevelUpUseCase levelUp, LimitBreakUseCase limitBreak)
    : ServiceBase<ISupportService>, ISupportService
{
    public async UnaryResult<LevelUpResponse> LevelUpAsync(LevelUpRequest request)
    {
        var playerId = Context.GetAuthContext().PlayerId;
        return await transactions.RunAsync(Context, async tx =>
        {
            var result = await levelUp.ExecuteAsync(tx, playerId, ParseUid(request.SupportCardUid), request.TargetLevel);
            return new LevelUpResponse(result.Level, result.SupportPoints, result.Gold);
        });
    }

    public async UnaryResult<LimitBreakResponse> LimitBreakAsync(LimitBreakRequest request) { /* 同じ形。ParseUid を 2 つ。LimitBreakResponse(result.LimitBreak, result.MaxLevel) */ }

    /// <summary>形が違う文字列は Ulid.Empty（どの所持IDとも一致しない）にして、NOT_OWNED / INVALID_MATERIAL に落とす。</summary>
    private static Ulid ParseUid(string value) => Ulid.TryParse(value, out var uid) ? uid : Ulid.Empty;
}
```

`PlayerService.cs` と同じ先頭の `#pragma warning disable CS8644` とコメントを付ける。`UseCaseException` の変換と未作成の判定は `[UtaService]` が行う。`LimitBreakAsync` は `ParseUid` を 2 回呼んで同じ形で書く。

### 3.5 `Program.cs` の登録

```csharp
builder.Services.AddSingleton<ISupportCatalog, MasterSupportCatalog>();
builder.Services.AddScoped<LevelUpUseCase>();
builder.Services.AddScoped<LimitBreakUseCase>();
```

## 4. タスク

各タスクの終わりに `dotnet build Uta.slnx`（警告 0・エラー 0）が通る。

1. **Contracts**: 3.1（`.meta` を含む）。確かめ: `dotnet build Uta.slnx`。
2. **Application・Infrastructure・Api・登録**: 3.2〜3.5。確かめ: `dotnet build Uta.slnx`、既存のテストが通る。
3. **テスト**: 5章。確かめ: `dotnet test --project server/Uta.Server.Tests` が全件通る（Docker が要る）。

**wiki への反映は実装担当の仕事ではない**。3 本の実装が済んだあと、メインのセッションが、実装設計書（`gacha-support-server.md`・`server-foundation.md`・`api-design.md`・`gacha-tables.md`）を実装結果に合わせて直し、`game-spec.md` の「実装計画の単位と順」で順 5 の受け持ちから条件 3 の「性能への反映」を外して順 6 に移し、順 5 に `done` の印と計画 3 本へのリンクを足す。実装担当はこの章の 1〜3 だけを行う。

境界: `targetLevel` が今のレベル以下・上限超過・負・0 → `SUPPORT_INVALID_TARGET_LEVEL`。ちょうど上限 → 成功。形が違う所持ID → `NOT_OWNED` / `INVALID_MATERIAL`。素材と対象が同じ ID → `INVALID_MATERIAL`。上限解放 4 回の対象 → `SUPPORT_MAX_LIMIT_BREAK`（素材は消えない）。

## 5. テスト

### 単体テスト（`MasterSupportCatalogTests`。`MasterDataLoader.Load()`）

| テスト名 | 期待 |
|---|---|
| `GetMaxLevel_ReturnsCapByRarityAndLimitBreak` | カード 1（R）: 回数 0 → 20、4 → 40。カード 6（SR）: 0 → 30。カード 9（SSR）: 4 → 60 |
| `GetLevelUpCost_SumsRows` | カード 1（R、基準 39）: Lv 1 → 2 は `(39, 39)`、Lv 1 → 3 は `(39 + 78, 39 + 78)` = `(117, 117)`。カード 9（SSR、基準 117）: Lv 1 → 2 は `(117, 117)` |

### 結合テスト（`SupportTests`。`[Collection(PostgresCollection.Name)]`。各テストは `UtaServerFactory(postgres)` と `PlayerClient`、`SignUpAsync`（`req-1`）、`SignInAsync`（`req-2`）で始め、`SignInResponse.SupportCards` から所持IDを得る（カード 1 の行を `CardId == 1` で選ぶ）。以降の `x-request-id` は `req-3` から順に変える。`RpcException` の検査はガチャの計画と同じ。初期の強化pt・ゴールドは 10000。DB の検査は `WHERE player_id = $1`（または `support_card_uid = $1`）で絞り、`count(*)` は `long`、`int` 列は `int` で読む。呼び出しは `client.CallAsync<ISupportService, LevelUpResponse>(x => x.LevelUpAsync(new LevelUpRequest(uid, 3)), "req-3")` の形。素材の追加は `postgres.ExecuteAsync("INSERT INTO player_support_cards (support_card_uid, player_id, card_id) VALUES ($1, $2, $3)", uid, client.PlayerId, cardId)`、レベルの変更は `UPDATE player_support_cards SET level = 5 WHERE support_card_uid = $1`）

| テスト名 | 手順 | 期待 |
|---|---|---|
| `LevelUp_ToTarget_PaysAndUpdates` | カード 1（R）を `TargetLevel = 3` | `Level == 3`・`SupportPoints == 10000 - 117`・`Gold == 10000 - 117`。DB: `player_support_cards.level == 3`、`players.support_points == 9883` |
| `LevelUp_ToCap_Succeeds` | `UPDATE players SET support_points = 100000, gold = 100000 WHERE player_id = $1` の後、カード 1 を `TargetLevel = 20`（上限解放 0 の上限） | 成功し `Level == 20` |
| `LevelUp_AboveCap_Rejected` | `TargetLevel = 21` | `SUPPORT_INVALID_TARGET_LEVEL`。支払いなし |
| `LevelUp_NotAboveCurrent_Rejected` | `TargetLevel = 1`（今は 1） | `SUPPORT_INVALID_TARGET_LEVEL` |
| `LevelUp_NotOwned_Rejected` | 他のプレイヤーの所持ID（別の `PlayerClient.CreateAsync(channel, "fedcba9876543210-secret2")`＝2 人目の端末で `SignUpAsync`・`SignInAsync` して得る）、存在しない ULID（`Ulid.NewUlid().ToString()`）、`"not-a-ulid"` の 3 通り | すべて `SUPPORT_NOT_OWNED` |
| `LevelUp_NotEnoughPoints_Rejected` | `UPDATE players SET support_points = 0` の後に `TargetLevel = 2` | `SUPPORT_NOT_ENOUGH_POINTS`。DB の `gold` は 10000 のまま |
| `LevelUp_NotEnoughGold_Rejected` | `UPDATE players SET gold = 0` の後に `TargetLevel = 2` | `PLAYER_NOT_ENOUGH_GOLD`。`support_points` は 10000 のまま |
| `LevelUp_BothShort_ReportsPointsFirst` | 強化pt も ゴールドも 0 | `SUPPORT_NOT_ENOUGH_POINTS` |
| `LevelUp_SameRequestId_ReturnsStoredResponse` | カード 1 を `TargetLevel = 3` で、同じ `x-request-id` で 2 回 | 2 回目も同じ応答（`Level == 3`・`SupportPoints == 9883`）。`players.support_points == 9883`（支払いは 1 回分） |
| `LevelUp_WritesOneGameLog` | `LevelUp_ToTarget_PaysAndUpdates` と同じ操作 | `game_logs` に `kind = 'support' AND body->>'reason' = 'support_level_up'` が 1 件。失敗した要求（`TargetLevel = 21`）の後も 1 のまま |
| `LevelUp_BeforeSignUp_ReturnsPlayerNotCreated` | 登録だけして `LevelUpAsync` | `PLAYER_NOT_CREATED` |
| `LimitBreak_WithMaterial_Unlocks` | 前提: `INSERT INTO player_support_cards (support_card_uid, player_id, card_id) VALUES ($1, $2, 1)`（素材。`Ulid.NewUlid().ToString()`）。解放するカードはカード 1 の初期の行、素材は前提で追加した行 | `LimitBreak == 1`・`MaxLevel == 25`。DB: 素材の行は消え、`limit_break == 1` |
| `LimitBreak_ConsumesLevelledMaterialWithoutRefund` | 素材を追加して `UPDATE … SET level = 5` の後に使う | 成功。`players.support_points`・`gold` は変わらない |
| `LimitBreak_WritesOneGameLog` | `LimitBreak_WithMaterial_Unlocks` と同じ操作 | `game_logs` に `kind = 'support' AND body->>'reason' = 'support_limit_break'` が 1 件 |
| `LimitBreak_SameUid_RejectedAsMaterial` | 素材 = 解放するカードと同じ ID | `SUPPORT_INVALID_MATERIAL` |
| `LimitBreak_DifferentCardMaterial_Rejected` | 素材にカード 2 の所持ID | `SUPPORT_INVALID_MATERIAL`。素材の行は残る |
| `LimitBreak_NotOwnedMaterial_Rejected` | 素材 = 他人の所持ID／存在しない ULID | `SUPPORT_INVALID_MATERIAL` |
| `LimitBreak_NotOwnedTarget_Rejected` | 解放するカード = 存在しない ULID | `SUPPORT_NOT_OWNED` |
| `LimitBreak_AtMax_RejectedAndMaterialKept` | 前提: `UPDATE player_support_cards SET limit_break = 4 WHERE support_card_uid = $1`、素材を 1 枚追加 | `SUPPORT_MAX_LIMIT_BREAK`。素材の行は残る（ロールバック） |
| `LimitBreak_FullFlow_ReachesMaxLevel` | 前提: `UPDATE players SET support_points = 100000, gold = 100000`。素材を 4 枚追加して 4 回解放 → `LevelUp` で `TargetLevel = 40` | 4 回目の `LimitBreak == 4`・`MaxLevel == 40`、レベル上げが成功 |
| `LimitBreak_ConcurrentSameMaterial_OneSucceeds` | 素材 1 枚を追加。解放するカード = 初期のカード 1 の行。`Task.WhenAll` で同じ素材の `LimitBreakAsync` を `req-a`・`req-b` の 2 つ並列に呼ぶ（それぞれ `try/catch` で `RpcException` を受けて結果を集める） | 成功 1・失敗 1（`SUPPORT_INVALID_MATERIAL`）。`limit_break == 1` |

実行: `dotnet test --project server/Uta.Server.Tests`（Docker が要る）。

## 6. 未決事項

なし。
- 素材・解放の対象の所持ID の形が不正な文字列は `Ulid.Empty` にして、所持なしと同じエラーにする（`ParseUid`）。
- ゲームログの中身と素材の扱いは 1章の「決めたこと」。
- `LevelUp` のゴールド不足は共通の `PLAYER_NOT_ENOUGH_GOLD`（[api-design.md](../wiki/architecture/api-design.md) 2章）。
