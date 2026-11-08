---
title: 実装計画: サーバーの Catalog をやめ、マスタを UseCase から直接読む（ADR 0020 のサーバー側）
status: in-progress
updated: 2026-11-08
spec: ../wiki/decisions/0020-masterdata-referenced-outside-shared-domain.md, ../wiki/architecture/repository-layout.md, ../wiki/architecture/naming.md
---

# 実装計画: サーバーの Catalog をやめ、マスタを UseCase から直接読む

## この計画で決めたこと

- **範囲は [ADR 0020](../wiki/decisions/0020-masterdata-referenced-outside-shared-domain.md) のサーバー側だけ**。マスタの詰め替えの IF 6 つ（`IGachaCatalog`・`IDuplicateConversionCatalog`・`IInitialGrantCatalog`・`ISupportCatalog`・`IProduceRewardCatalog`・`IPlayerLevelCatalog`）、その実装（`Master<名詞>Catalog` 6 つ）、詰め替え先の型（`GachaDefinition`・`GachaPool`・`GachaPoolItem`・`CeilingItem`・`InitialGrant`・`LevelUpCost`）と、その詰め替えのテスト 4 ファイルを消す。使う側（UseCase・`ItemGranter`・`ProduceCompletion`）が `MemoryDatabase` を DI で受けて、マスタの表を直接引く
- **詰め替えと導出の分け方**（ADR 0020「影響・リスク」）: 表の行を引くだけのものは使う場所に書く。行から計算する値（ガチャの重みの列、強化の必要量の合計、重複の変換量）も、使う UseCase の中で作る。2 つの UseCase（`DrawUseCase`・`ExchangeUseCase`）が同じ写しを使う「マスタの品目（`ItemKind`・`IdolId`・`SupportCardId`）→ 付与する品目（`GrantedItemKind`・ID）」だけは、static の `GachaItemMapper`（Application。[naming.md](../wiki/architecture/naming.md) 7章の Mapper）に置く
- **表引きのキーは今の主キーで足りる**。`[SecondaryKey]` を足さない（マスタの定義と生成物は変えない）。複合主キーの表は `FindBy<A>And<B>((a, b))`、先頭のキーで絞る範囲は `FindRangeBy<A>And<B>((a, 最小), (a, 最大))` で引く。どちらも MasterMemory 3.0.4 の生成物にある（2026-11-08 に `dotnet build server/Uta.Server -p:EmitCompilerGeneratedFiles=true` で、`GachaTableEntryTable.FindRangeByGachaTableIdAndOrder`・`SupportLevelCostTable.FindRangeByRarityAndLevel` があることを確かめた）。`All` は主キーの昇順（MasterMemory は表を作るときに主キーで並べ替えて持つ。並びに頼る `ProduceCompletion` の必要経験値の列は 3.5 のテストで固定する）
- **マスタに行が無いときの例外は、MasterMemory の `FindBy…` が投げるもの（`KeyNotFoundException`）に任せる**。今の Catalog は `InvalidOperationException` に包んでいたが、どちらもマスタの不備で、要求は内部エラーで失敗する点は同じ。マスタの不備は `MasterDataInvariantTests` が止める。例外: 強化の必要量は範囲で引くので、行が足りないと黙って少なく数えてしまう。件数を確かめて `InvalidOperationException` を投げる（今と同じ）
- **`MasterCardCatalog`・`MasterProduceCatalog` を `CardCatalogFactory`・`ProduceCatalogFactory` に改名する**（naming.md「今のコードとの差」の行。ADR 0020「Shared.Domain の型を組み立てる…は残す（名前は…変える）」）。中身は変えない。置き場は `Infrastructure/MasterData/` のまま
- **アーキテクチャテスト**: `MasterData_IsReferencedOnlyByInfrastructure` を消し、`SharedDomain_DoesNotDependOnMasterData`（Shared.Domain のアセンブリが `Uta.MasterData` に依存しない）を足す（ADR 0020「accepted にしたら直すもの」）。Shared.Domain は MasterMemory を参照していないので、今は構造上いつも通る。Shared.Domain の参照が増えたときの見張りとして置く
- 消すテストのうち、マスタの値を固定していたもの（重複の変換量・強化の上限 Lv・評価ランクの報酬・プレイヤー Lv の行数）は、`MasterDataTests` に表を直接引くテストとして移す（3.5）。結合テストは Docker が要るので、Docker なしで回る単体テストに値の固定を残すため。そのほかの中身は、`MasterDataTests`（`InitialSetting_Is2500_10000_10000`・`GachaTables_WeightSums`・`Gacha_CeilingItems` など）・`MasterDataInvariantTests` と結合テスト（`GachaTests`・`SupportTests`・`PlayerSignUpSignInTests`・`ProducePlayTests`）で確かめられている
- **積み PR**: [ADR 0021 のサーバー側の PR](https://github.com/chinpanGX/Uta/pull/48)（`claude/server-domain-entities`）が main に入る前に計画を切ったので、計画のブランチ `claude/plan-masterdata-without-catalog` はそこから切った。実装は、#48 が main に入ってから main で始める（入る前に始めるなら、#48 のブランチから積む）。この計画は #48 の後のコード（`Uta.Server.Domain.*` のリポジトリの IF、`ProduceReward` が `Domain/Produce/`）を前提に書いた

## ユーザーの確認が要ること

なし（範囲・名前・置き場は ADR 0020 と naming.md で決まっている。上の [決定] は、ADR の範囲の中で答えが 1 つに決まるもの）。

## 1. 目的と前提

ADR 0020 は「生成したマスタは Shared.Domain 以外のどの層から参照してもよい」と決め、サーバーの Catalog をやめるのはサーバー側の実装計画に回した（[ADR 0022](../wiki/decisions/0022-client-external-concretes-in-infrastructure.md)「accepted にする時点と、進めてよいこと」）。この計画がその実装計画。クライアント側は [基盤移行の計画](2026-11-08-client-foundation-migration.md) で済んでいる。

- 関係する ADR: [0020](../wiki/decisions/0020-masterdata-referenced-outside-shared-domain.md)（本体）、[0007](../wiki/decisions/0007-masterdata-with-addons.md)（マスタの生成）、[0008](../wiki/decisions/0008-shared-domain-and-onion.md)（Shared.Domain は生成物に依存しない）、[0021](../wiki/decisions/0021-domain-entities-and-repositories.md)（前提の PR #48）
- 前の計画: [サーバーのエンティティの計画](2026-11-08-server-domain-entities.md)（「決めないこと」に、この計画の範囲を書いた）

### 決めないこと（範囲外）

- naming.md「今のコードとの差」のうち、Catalog と `Master<名詞>Catalog` の 2 行のほか（`GachaStatus`・`ProduceView`・`ProduceEndSummary`・`GrantOutcome`・`UtaServiceAttribute` など）
- サーバーの Domain がマスタを使う形に書き直すこと（`PlayerLevelRules.AddExp`・`GachaPicker.Pick` は今どおりプリミティブを受ける）。ADR 0020 は Domain からの参照を許すが、この計画で Domain の型は変えない
- マスタの定義・CSV・生成物（`masterdata/`、`Infrastructure/MasterData/Generated/`・`masterdata.bytes`）。変えない
- クライアント（ADR 0020 のクライアント側は済んでいる）

## 2. 触るファイル

パスは `server/Uta.Server/` からの相対。テストは `server/Uta.Server.Tests/` からの相対。改名は `git mv`（履歴を残す）。

| 操作 | パス | 中身 | タスク |
|---|---|---|---|
| 削除 | `Application/Gacha/GachaDefinition.cs` | `GachaPoolItem`・`GachaPool`・`CeilingItem`・`GachaDefinition` | 1 |
| 削除 | `Application/Gacha/IGachaCatalog.cs` | | 1 |
| 削除 | `Application/Gacha/IDuplicateConversionCatalog.cs` | | 1 |
| 削除 | `Infrastructure/MasterData/MasterGachaCatalog.cs` | | 1 |
| 削除 | `Infrastructure/MasterData/MasterDuplicateConversionCatalog.cs` | | 1 |
| 作成 | `Application/Gacha/GachaItemMapper.cs` | マスタの品目 → 付与する品目（3章） | 1 |
| 変更 | `Application/Gacha/DrawUseCase.cs` | `IGachaCatalog` → `MemoryDatabase`（3章） | 1 |
| 変更 | `Application/Gacha/ExchangeUseCase.cs` | 同上 | 1 |
| 変更 | `Application/Gacha/GetGachasUseCase.cs` | 同上 | 1 |
| 変更 | `Application/Gacha/ItemGranter.cs` | `IDuplicateConversionCatalog` → `MemoryDatabase` | 1 |
| 削除 | テスト `MasterData/MasterGachaCatalogTests.cs` | | 1 |
| 変更 | テスト `Architecture/ArchitectureTests.cs` | `MasterData_IsReferencedOnlyByInfrastructure` → `SharedDomain_DoesNotDependOnMasterData` | 1 |
| 削除 | `Application/Player/IInitialGrantCatalog.cs`・`Application/Player/InitialGrant.cs` | | 2 |
| 削除 | `Application/Support/ISupportCatalog.cs` | `ISupportCatalog`・`LevelUpCost` | 2 |
| 削除 | `Application/Produce/IProduceRewardCatalog.cs`・`Application/Produce/IPlayerLevelCatalog.cs` | | 2 |
| 削除 | `Infrastructure/MasterData/MasterInitialGrantCatalog.cs`・`MasterSupportCatalog.cs`・`MasterProduceRewardCatalog.cs`・`MasterPlayerLevelCatalog.cs` | | 2 |
| 変更 | `Application/Player/SignUpUseCase.cs` | `IInitialGrantCatalog` → `MemoryDatabase` | 2 |
| 変更 | `Application/Support/LevelUpUseCase.cs`・`LimitBreakUseCase.cs` | `ISupportCatalog` → `MemoryDatabase` | 2 |
| 変更 | `Application/Produce/ProduceCompletion.cs` | `IProduceRewardCatalog`・`IPlayerLevelCatalog` → `MemoryDatabase` | 2 |
| 削除 | テスト `MasterData/MasterInitialGrantCatalogTests.cs`・`MasterSupportCatalogTests.cs`・`MasterProduceRewardCatalogTests.cs` | | 2 |
| 改名 | `Infrastructure/MasterData/MasterCardCatalog.cs` → `CardCatalogFactory.cs` | クラス名 `MasterCardCatalog` → `CardCatalogFactory` | 3 |
| 改名 | `Infrastructure/MasterData/MasterProduceCatalog.cs` → `ProduceCatalogFactory.cs` | クラス名 `MasterProduceCatalog` → `ProduceCatalogFactory` | 3 |
| 改名 | テスト `MasterData/MasterCardCatalogTests.cs` → `CardCatalogFactoryTests.cs`、`MasterProduceCatalogTests.cs` → `ProduceCatalogFactoryTests.cs` | テストのクラス名と呼び出し | 3 |
| 変更 | テスト `Integration/ProducePlayTests.cs`（309 行目）、`MasterData/TestCardsMatchMasterTests.cs`（15 行目） | 呼び出しの改名（フィールド名 `MasterCatalog` は変えない） | 3 |
| 変更 | テスト `MasterData/MasterDataTests.cs` | 末尾に、消すテストから値の固定を移す（3.5） | 1・2 |
| 変更 | `Program.cs` | DI の登録（タスク 1: `IGachaCatalog`・`IDuplicateConversionCatalog` の 2 行を消す。タスク 2: 4 行を消す。タスク 3: Factory の呼び出しの改名） | 1・2・3 |
| 変更 | wiki（タスク 4） | メインのセッションが直す | 4 |

新しく作る `GachaItemMapper.cs` は、ほかの `.cs` と同じく BOM 付き UTF-8・改行 LF にする。

`Application/` の UseCase が `using Uta.MasterData;` を足す。**同じ名前の型・名前空間に注意する**:

- `Uta.MasterData` にはクラス `Gacha`・`Idol`・`SupportCard` などがあり、`Uta.Server.Application.Gacha`・`Uta.Server.Domain.Idol` などの名前空間と同じ名前になる。マスタの行は `var` で受け、型名を書かない。書く必要があれば `Uta.MasterData.Gacha` と名前空間付きで書く
- enum の `ProduceRank`・`Rarity` などは Shared.Domain にも同じ名前がある（ADR 0020「enum が 2 系統ある」）。Shared.Domain の名前空間を `using` しているファイルでは、マスタの enum を `Uta.MasterData.ProduceRank` と名前空間付きで書く

## 3. 型とシグネチャ

### 3.1 `Application/Gacha/GachaItemMapper.cs`（作成）

```csharp
using Uta.MasterData;
using Uta.Server.Domain.Gacha;

namespace Uta.Server.Application.Gacha;

/// <summary>マスタの排出・天井の行の品目（ItemKind と IdolId・SupportCardId）を、付与する品目の種類と ID に写す。</summary>
public static class GachaItemMapper
{
    public static (GrantedItemKind Kind, int ItemId) ToGrantedItem(GachaTableEntry entry) =>
        ToGrantedItem(entry.ItemKind, entry.IdolId, entry.SupportCardId);

    public static (GrantedItemKind Kind, int ItemId) ToGrantedItem(GachaCeilingItem item) =>
        ToGrantedItem(item.ItemKind, item.IdolId, item.SupportCardId);

    private static (GrantedItemKind Kind, int ItemId) ToGrantedItem(ItemKind kind, int idolId, int supportCardId) =>
        kind switch
        {
            ItemKind.Idol => (GrantedItemKind.Idol, idolId),
            ItemKind.SupportCard => (GrantedItemKind.SupportCard, supportCardId),
            _ => throw new InvalidOperationException($"unexpected item kind: {kind}"),
        };
}
```

### 3.2 コンストラクターの変更

| 型 | 今 | 変更後 |
|---|---|---|
| `DrawUseCase` | `(IGachaCatalog catalog, IPlayerRepository players, IGachaRepository gachas, ItemGranter granter, IRandomSource randomSource)` | `(MemoryDatabase master, IPlayerRepository players, IGachaRepository gachas, ItemGranter granter, IRandomSource randomSource)` |
| `ExchangeUseCase` | `(IGachaCatalog catalog, IGachaRepository gachas, ItemGranter granter)` | `(MemoryDatabase master, IGachaRepository gachas, ItemGranter granter)` |
| `GetGachasUseCase` | `(IGachaCatalog catalog, IGachaRepository gachas)` | `(MemoryDatabase master, IGachaRepository gachas)` |
| `ItemGranter` | `(IIdolRepository idols, ISupportCardRepository supportCards, IPlayerRepository players, IDuplicateConversionCatalog conversions)` | `(IIdolRepository idols, ISupportCardRepository supportCards, IPlayerRepository players, MemoryDatabase master)` |
| `SignUpUseCase` | `(IPlayerRepository players, IIdolRepository idols, ISupportCardRepository supportCards, IInitialGrantCatalog catalog)` | `(IPlayerRepository players, IIdolRepository idols, ISupportCardRepository supportCards, MemoryDatabase master)` |
| `LevelUpUseCase` | `(IPlayerRepository players, ISupportCardRepository supportCards, ISupportCatalog catalog)` | `(IPlayerRepository players, ISupportCardRepository supportCards, MemoryDatabase master)` |
| `LimitBreakUseCase` | `(ISupportCardRepository supportCards, ISupportCatalog catalog)` | `(ISupportCardRepository supportCards, MemoryDatabase master)` |
| `ProduceCompletion` | `(IPlayerRepository players, IProduceSessionRepository sessions, IProduceRewardCatalog rewards, IPlayerLevelCatalog levels)` | `(IPlayerRepository players, IProduceSessionRepository sessions, MemoryDatabase master)` |

`MemoryDatabase` は今の `Program.cs` がシングルトンで登録している（`builder.Services.AddSingleton(MasterDataLoader.Load())`）。登録は変えない。UseCase は Scoped のまま。

戻り値の型（`DrawResult`・`ExchangeResult`・`GachaStatus`・`GachaResult`・`LevelUpResult`・`LimitBreakResult`・`ProduceEndSummary`）とメソッドのシグネチャは変えない。

### 3.3 各メソッドの中身（変更後）

マスタの引き方を、今の Catalog の実装と同じ結果になるように書く。

**`DrawUseCase.ExecuteAsync`**（count の検査・支払い・ゲームログ・戻り値は今のまま）:

```csharp
if (!master.GachaTable.TryFindByGachaId(gachaId, out var gacha))
{
    throw new UseCaseException(UseCaseError.GachaNotFound, "gacha not found");
}

var cost = count == 1 ? gacha.SingleCost : gacha.TenCost;
// （cost の検査と TrySpendGachaCurrencyAsync は今のまま）

var normal = master.GachaTableEntryTable
    .FindRangeByGachaTableIdAndOrder((gacha.NormalTableId, int.MinValue), (gacha.NormalTableId, int.MaxValue)).ToList();
var guaranteed = master.GachaTableEntryTable
    .FindRangeByGachaTableIdAndOrder((gacha.GuaranteedTableId, int.MinValue), (gacha.GuaranteedTableId, int.MaxValue)).ToList();
var normalWeights = normal.Select(x => x.Weight).ToList();
var guaranteedWeights = guaranteed.Select(x => x.Weight).ToList();

var random = randomSource.Create();
var results = new List<GachaResult>();
for (var slot = 1; slot <= count; slot++)
{
    var isGuaranteed = count == 10 && slot == 10;
    var entries = isGuaranteed ? guaranteed : normal;
    var weights = isGuaranteed ? guaranteedWeights : normalWeights;
    var tableId = isGuaranteed ? gacha.GuaranteedTableId : gacha.NormalTableId;
    var (kind, itemId) = GachaItemMapper.ToGrantedItem(entries[GachaPicker.Pick(random, weights)]);
    var result = (await granter.GrantAsync(tx, playerId, kind, itemId)) with { Slot = slot };
    await gachas.AddHistoryAsync(tx, new GachaHistoryRecord(
        Ulid.NewUlid(), requestId, playerId, gachaId, slot, tableId,
        (int)result.Kind, result.ItemId, (int)result.Outcome, result.Shards, result.SupportPoints));
    results.Add(result);
}
```

- 排出の行は `(GachaTableId, Order)` の昇順で返るので、重みの並びは今の `GachaPool.Weights`（Order の昇順）と同じ。結合テスト `Draw_Ten_UsesGuaranteedTableForTenthSlot` などの決まった乱数の結果は変わらない
- 排出の行が 0 件のテーブルは、master-gen の検証（「テーブルが空でない」。[gacha-tables.md](../wiki/architecture/master/gacha-tables.md)）と `MasterDataInvariantTests.GachaDetailTables_HaveEntriesAndWeightSumFitsInt` が止めるので、空の場合の分岐は書かない
- 排出の行の読み出しは、支払い（`TrySpendGachaCurrencyAsync`）の後に置く（今の Catalog は起動後に作り置きしていたので、どこで読んでも同じ。支払いが失敗したときに読まずに済む）

**`ExchangeUseCase.ExecuteAsync`**:

```csharp
if (!master.GachaTable.TryFindByGachaId(gachaId, out var gacha)
    || !master.GachaCeilingItemTable.TryFindByGachaIdAndOrder((gachaId, ceilingItemOrder), out var ceilingItem))
{
    throw new UseCaseException(UseCaseError.GachaNotFound, "gacha or ceiling item not found");
}

var points = await gachas.TrySpendExchangePointsAsync(tx, playerId, gachaId, gacha.CeilingPoints)
    ?? throw new UseCaseException(UseCaseError.GachaNotEnoughPoints, "not enough exchange points");

var (kind, itemId) = GachaItemMapper.ToGrantedItem(ceilingItem);
var result = await granter.GrantAsync(tx, playerId, kind, itemId);
// （履歴・ゲームログ・戻り値は今のまま。ゲームログの cost_points は gacha.CeilingPoints）
```

**`GetGachasUseCase.ExecuteAsync`**:

```csharp
/// <summary>マスタの GachaTable の順（GachaId 昇順）。交換pt の行が無いガチャは 0。</summary>
public async Task<IReadOnlyList<GachaStatus>> ExecuteAsync(TxContext tx, Ulid playerId)
{
    var points = await gachas.GetExchangePointsAsync(tx, playerId);
    return master.GachaTable.All
        .Select(x => new GachaStatus(x.GachaId, points.TryGetValue(x.GachaId, out var value) ? value : 0))
        .ToList();
}
```

**`ItemGranter`**: `conversions.GetAmount(kind, itemId)` の 2 か所を、private のメソッド `GetConversionAmount(kind, itemId)` の呼び出しにする。

```csharp
/// <summary>重複したときの変換量。育成アイドルはかけら、サポートカードは完凸後の強化pt。</summary>
private int GetConversionAmount(GrantedItemKind kind, int itemId)
{
    var (itemKind, rarity) = kind == GrantedItemKind.Idol
        ? (ItemKind.Idol, master.IdolTable.FindByIdolId(itemId).Rarity)
        : (ItemKind.SupportCard, master.SupportCardTable.FindBySupportCardId(itemId).Rarity);
    return master.DuplicateConversionTable.FindByItemKindAndRarity((itemKind, rarity)).Amount;
}
```

**`SignUpUseCase.ExecuteAsync`**: `var grant = catalog.Get();` を次にし、以降の `grant.GachaCurrency`・`grant.Gold`・`grant.SupportPoints` を `setting.…` に、`grant.IdolIds` を `idolIds` に、`grant.SupportCardIds` を `supportCardIds` に置き換える（ゲームログの項目も同じ置き換え）。

```csharp
var setting = master.InitialSettingTable.FindById(1);                                                  // 1 行だけ（MasterDataInvariantTests）
var idolIds = master.IdolTable.All.Where(x => x.Rarity == Rarity.R).Select(x => x.IdolId).ToList();   // R の育成アイドル。IdolId の昇順
var supportCardIds = master.InitialSupportCardTable.All.Select(x => x.SupportCardId).ToList();        // Order の昇順
```

`GameLogJson.Ints` は `IEnumerable<int>` を受けるので、`List<int>` をそのまま渡す。

**`LevelUpUseCase.ExecuteAsync`**: `catalog.GetMaxLevel(...)` と `catalog.GetLevelUpCost(...)` を次にし、以降の `cost.SupportPoints`・`cost.Gold` を `supportPoints`・`gold` に置き換える（ゲームログの項目も同じ）。

```csharp
var rarity = master.SupportCardTable.FindBySupportCardId(state.MasterId).Rarity;
var max = master.SupportLevelCapTable.FindByRarityAndLimitBreak((rarity, state.LimitBreak)).MaxLevel;
if (targetLevel <= state.Level || targetLevel > max)
{
    throw new UseCaseException(UseCaseError.SupportInvalidTargetLevel, "invalid target level");
}

// Lv state.Level → targetLevel の必要量は、SupportLevelCost の Level が state.Level 以上 targetLevel 未満の行の和
var costs = master.SupportLevelCostTable
    .FindRangeByRarityAndLevel((rarity, state.Level), (rarity, targetLevel - 1)).ToList();
if (costs.Count != targetLevel - state.Level)
{
    throw new InvalidOperationException($"support level cost not found: {rarity} level {state.Level}..{targetLevel - 1}");
}

var supportPoints = costs.Sum(x => x.SupportPoints);
var gold = costs.Sum(x => x.Gold);
```

**`LimitBreakUseCase.ExecuteAsync`**: 最後の `catalog.GetMaxLevel(target.MasterId, limitBreak)` を次にする。

```csharp
var rarity = master.SupportCardTable.FindBySupportCardId(target.MasterId).Rarity;
return new LimitBreakResult(limitBreak, master.SupportLevelCapTable.FindByRarityAndLimitBreak((rarity, limitBreak)).MaxLevel);
```

**`ProduceCompletion.CompleteAsync`**: `rewards.Get(result.Rank)` と `levels.RequiredExps` を次にする（ファイルは `using Uta.Shared.Domain.Produce;` を持つので、マスタの `ProduceRank` は名前空間付きで書く）。

```csharp
var row = master.EvaluationRankRewardTable.FindByRank((Uta.MasterData.ProduceRank)(int)result.Rank);
var reward = new ProduceReward(row.GachaCurrency, row.Gold, row.SupportPoints, row.Exp);
var player = await players.FindAsync(tx, playerId) ?? throw new InvalidOperationException("player not found");
var requiredExps = master.PlayerLevelExpTable.All.Select(x => x.RequiredExp).ToList();   // Level の昇順（Lv 1 → 2 が先頭）
var (level, exp) = PlayerLevelRules.AddExp(player.Level, player.Exp, reward.Exp, requiredExps);
```

`ProduceReward` は `Uta.Server.Domain.Produce.ProduceReward`（#48 で Domain に移した。`IPlayerRepository.GrantProduceRewardAsync` が受ける）。残す。

### 3.4 アーキテクチャテスト

`MasterData_IsReferencedOnlyByInfrastructure` を消し、同じ場所に次を足す。

```csharp
[Fact]
public void SharedDomain_DoesNotDependOnMasterData()
{
    var result = Types.InAssembly(typeof(Uta.Shared.Domain.Lesson.CardCatalog).Assembly)
        .ShouldNot().HaveDependencyOnAny("Uta.MasterData").GetResult();
    Assert.True(result.IsSuccessful);
}
```

### 3.5 `MasterDataTests` に移すテスト

`server/Uta.Server.Tests/MasterData/MasterDataTests.cs` の末尾に足す（このファイルは `using Uta.MasterData;` と `Db` を持つ。値は消すテストと同じ）。

```csharp
[Theory]
[InlineData(ItemKind.Idol, Rarity.R, 5)]
[InlineData(ItemKind.Idol, Rarity.SR, 25)]
[InlineData(ItemKind.Idol, Rarity.SSR, 50)]
[InlineData(ItemKind.SupportCard, Rarity.R, 300)]
[InlineData(ItemKind.SupportCard, Rarity.SR, 1000)]
[InlineData(ItemKind.SupportCard, Rarity.SSR, 3000)]
public void DuplicateConversion_AmountByKindAndRarity(ItemKind kind, Rarity rarity, int amount)
{
    Assert.Equal(amount, Db.DuplicateConversionTable.FindByItemKindAndRarity((kind, rarity)).Amount);
}

[Theory]
[InlineData(Rarity.R, 0, 20)]
[InlineData(Rarity.R, 4, 40)]
[InlineData(Rarity.SR, 0, 30)]
[InlineData(Rarity.SSR, 4, 60)]
public void SupportLevelCap_MaxLevelByRarityAndLimitBreak(Rarity rarity, int limitBreak, int maxLevel)
{
    Assert.Equal(maxLevel, Db.SupportLevelCapTable.FindByRarityAndLimitBreak((rarity, limitBreak)).MaxLevel);
}

[Fact]
public void EvaluationRankReward_FAndSSPlus()
{
    var f = Db.EvaluationRankRewardTable.FindByRank(ProduceRank.F);
    Assert.Equal((50, 1000, 500, 10), (f.GachaCurrency, f.Gold, f.SupportPoints, f.Exp));
    var ssPlus = Db.EvaluationRankRewardTable.FindByRank(ProduceRank.SSPlus);
    Assert.Equal((800, 20000, 10000, 60), (ssPlus.GachaCurrency, ssPlus.Gold, ssPlus.SupportPoints, ssPlus.Exp));
}

// ProduceCompletion は All をそのまま必要経験値の列にする。PlayerLevelRules.AddExp は requiredExps[level - 1] で引くので、All が Level の昇順であることを固定する
[Fact]
public void PlayerLevelExp_AllIsInLevelOrder()
{
    var all = Db.PlayerLevelExpTable.All;
    Assert.Equal(Enumerable.Range(1, 99), all.Select(x => x.Level));
    Assert.Equal(40, all.First().RequiredExp);
    Assert.Equal(3960, all.Last().RequiredExp);
}
```

表の行数（評価ランクの報酬 13 行・プレイヤー Lv 99 行）は、今の `MasterDataTests.Produce_HaveExpectedCounts` が固定しているので足さない。

## 4. タスク

各タスクの終わりに、`dotnet build Uta.slnx`（警告 0・エラー 0）と `dotnet test --project server/Uta.Server.Tests` を回す。結合テストは Docker が要る（Testcontainers）。Docker につながらないときは、結合テスト以外が通ることを確かめ、結合テストを確かめられなかったと報告する。

### タスク 1: ガチャの Catalog をやめ、アーキテクチャテストを変える

触るファイル: 2章の表のタスク 1 の行と `Program.cs`。

1. `ArchitectureTests.cs` の `MasterData_IsReferencedOnlyByInfrastructure` を、3.4 の `SharedDomain_DoesNotDependOnMasterData` に置き換える（先に置き換えないと、Application が `Uta.MasterData` を参照した時点で落ちる）
2. `Application/Gacha/GachaItemMapper.cs` を 3.1 のとおりに作る
3. `DrawUseCase`・`ExchangeUseCase`・`GetGachasUseCase`・`ItemGranter` を 3.2・3.3 のとおりに変える。各ファイルに `using Uta.MasterData;` を足す
4. 3.5 の `DuplicateConversion_AmountByKindAndRarity` を `MasterDataTests.cs` に足す
5. `GachaDefinition.cs`・`IGachaCatalog.cs`・`IDuplicateConversionCatalog.cs`・`MasterGachaCatalog.cs`・`MasterDuplicateConversionCatalog.cs`・テストの `MasterGachaCatalogTests.cs` を消す
6. `Program.cs` から `builder.Services.AddSingleton<IGachaCatalog, MasterGachaCatalog>();` と `builder.Services.AddSingleton<IDuplicateConversionCatalog, MasterDuplicateConversionCatalog>();` を消す

境界: ガチャ ID が無い → `GachaNotFound`（今と同じ）。天井の Order が無い → `GachaNotFound`（今と同じ）。マスタの `ItemKind` が `Nothing` → `InvalidOperationException`（今は 0 の `GrantedItemKind` を作っていた。master-gen の検証（生成物の `IValidatable`。[gacha-tables.md](../wiki/architecture/master/gacha-tables.md)）が `Nothing` を止めるので起きない）。

確かめ方: `GachaTests` が全件通る（決まった乱数の結果・重複の変換量 5 と 300・天井の交換が今と同じ）。`ArchitectureTests` が全件通る。`grep -rnE 'IGachaCatalog|IDuplicateConversionCatalog|GachaDefinition|GachaPool|\bCeilingItem\b' server --include=*.cs --exclude-dir=bin --exclude-dir=obj` が 0 件（`GachaItemMapper` の `GachaCeilingItem` は先頭の `\b` で外れる）。

### タスク 2: 初期付与・サポート・育成の報酬の Catalog をやめる

触るファイル: 2章の表のタスク 2 の行と `Program.cs`。

1. `SignUpUseCase`・`LevelUpUseCase`・`LimitBreakUseCase`・`ProduceCompletion` を 3.2・3.3 のとおりに変える。`ProduceCompletion` 以外は `using Uta.MasterData;` を足す。`ProduceCompletion` も `using Uta.MasterData;` を足し、マスタの `ProduceRank` だけ名前空間付きで書く
2. 3.5 の `SupportLevelCap_MaxLevelByRarityAndLimitBreak`・`EvaluationRankReward_FAndSSPlus`・`PlayerLevelExp_AllIsInLevelOrder` を `MasterDataTests.cs` に足す
3. `IInitialGrantCatalog.cs`・`InitialGrant.cs`・`ISupportCatalog.cs`・`IProduceRewardCatalog.cs`・`IPlayerLevelCatalog.cs`・`MasterInitialGrantCatalog.cs`・`MasterSupportCatalog.cs`・`MasterProduceRewardCatalog.cs`・`MasterPlayerLevelCatalog.cs` と、テストの `MasterInitialGrantCatalogTests.cs`・`MasterSupportCatalogTests.cs`・`MasterProduceRewardCatalogTests.cs` を消す
4. `Program.cs` から `IInitialGrantCatalog`・`IProduceRewardCatalog`・`IPlayerLevelCatalog`・`ISupportCatalog` の 4 行の登録を消す。使わなくなった `using` があれば消す（ビルドの警告で見る）

境界: 強化の目標 Lv が今の Lv 以下か上限を超える → `SupportInvalidTargetLevel`（今と同じ。上限を引いてから判定する）。必要量の行が足りない → `InvalidOperationException`（今と同じ）。育成のランクが `Nothing` → マスタに行が無く `KeyNotFoundException`（今は `InvalidOperationException`。完了した育成のランクは F 以上なので起きない）。

確かめ方: `SupportTests`（`LevelUp_ToTarget_PaysAndUpdates` の 117 は Lv 1→3 の和）・`PlayerSignUpSignInTests`・`ProducePlayTests`（報酬と Lv）・`ProduceOperationTests` が全件通る。`grep -rnE 'IInitialGrantCatalog|\bInitialGrant\b|ISupportCatalog|LevelUpCost|IProduceRewardCatalog|IPlayerLevelCatalog' server --include=*.cs --exclude-dir=bin --exclude-dir=obj` が 0 件（テスト名 `SignIn_AfterSignUp_ReturnsInitialGrant` は、`_` の後で先頭の `\b` に当たらないので外れる）。

### タスク 3: `MasterCardCatalog`・`MasterProduceCatalog` を Factory に改名する

触るファイル: 2章の表のタスク 3 の行と `Program.cs`。

1. `git mv` で `Infrastructure/MasterData/MasterCardCatalog.cs` → `CardCatalogFactory.cs`、`MasterProduceCatalog.cs` → `ProduceCatalogFactory.cs`。クラス名を `CardCatalogFactory`・`ProduceCatalogFactory` にする。中身と XML ドキュメントは変えない
2. `git mv` でテストの `MasterData/MasterCardCatalogTests.cs` → `CardCatalogFactoryTests.cs`、`MasterProduceCatalogTests.cs` → `ProduceCatalogFactoryTests.cs`。テストのクラス名を `CardCatalogFactoryTests`・`ProduceCatalogFactoryTests` にし、中の呼び出しを新しい名前にする
3. `Program.cs` の `MasterCardCatalog.Create`・`MasterProduceCatalog.Create`、`Integration/ProducePlayTests.cs`・`MasterData/TestCardsMatchMasterTests.cs` の呼び出しを新しい名前にする

確かめ方: ビルドとテストが通る。`grep -rnE 'Master[A-Za-z]*Catalog\b' server --include=*.cs --exclude-dir=bin --exclude-dir=obj` の結果が、`TestCardsMatchMasterTests.cs` のフィールド名 `MasterCatalog`（15・40・49 行目の 3 件。変えない）だけ。

### タスク 4: wiki を直す（メインのセッション）

| ページ | 直すところ |
|---|---|
| [repository-layout.md](../wiki/architecture/repository-layout.md) 3章 | サーバーの層の表の「参照してよいもの」の Api・Application・Domain の列に「生成したマスタ（`Uta.MasterData`）」を足す。表の下の「`Uta.MasterData` を参照してよいのは Infrastructure だけ…」の段落を「`Uta.MasterData` は Shared.Domain 以外のどの層から参照してもよい（[ADR 0020](../wiki/decisions/0020-masterdata-referenced-outside-shared-domain.md)。2026-11-08 までは Infrastructure だけだった）。`Uta.Shared.Domain` は参照しない（`ArchitectureTests.SharedDomain_DoesNotDependOnMasterData`）。使う側は `MemoryDatabase`（シングルトン）を DI で受ける」にする。「生成したマスタの置き場」の「サーバーでは生成物を参照してよいのは Infrastructure だけ」を消し、置き場（`Infrastructure/MasterData/`）は変えないことを書く |
| [naming.md](../wiki/architecture/naming.md)「今のコードとの差」 | `MasterCardCatalog`・`MasterProduceCatalog` の行と、`I<名詞>Catalog`・`Master<名詞>Catalog` の行を消す（済んだ行は消すのがこの表の運用） |
| [produce-server.md](../wiki/architecture/server/produce-server.md) | 1章の層の説明の「`Application`(ユースケース・Catalog の IF)」「`Infrastructure`(…マスタの詰め替え)」、4章の報酬の「`IProduceRewardCatalog`」、ファイルの表の `IProduceRewardCatalog`・`IPlayerLevelCatalog`・`MasterData/MasterProduceCatalog.cs`・`MasterProduceRewardCatalog.cs`・`MasterPlayerLevelCatalog.cs` を、この計画の形（`ProduceCompletion` が `MemoryDatabase` から引く、`ProduceCatalogFactory.cs`）にする |
| [gacha-support-server.md](../wiki/architecture/server/gacha-support-server.md) 1 章 | 「サーバーはマスタを Infrastructure の `MemoryDatabase` から読む」を「UseCase が DI で受けた `MemoryDatabase` から読む（ADR 0020）。表は今の主キーで引く（`FindBy…`・先頭のキーで絞る `FindRangeBy…`。`[SecondaryKey]` は足していない。2026-11-08）。マスタの品目（`ItemKind`・`IdolId`・`SupportCardId`）→ 付与する品目（`GrantedItemKind`・ID）の読み替えは Application の `GachaItemMapper`」にする。同じ段落の「`server/` と `masterdata/` はまだない」は古いので消す |
| [gacha-tables.md](../wiki/architecture/master/gacha-tables.md) | 「サーバーの Infrastructure で `ItemKind` を見て読み替える」を「サーバーの Application（`GachaItemMapper`）で…」にする |
| [masterdata-pipeline.md](../wiki/architecture/master/masterdata-pipeline.md) 2章の enum の写し | 「各側の Infrastructure が数値のまま変換する」を「マスタを読む側（サーバーは UseCase と `Infrastructure/MasterData/` の Factory、クライアントは ADR 0020 の各層）が数値のまま変換する」にする |
| [naming.md](../wiki/architecture/naming.md) 冒頭の「印」の段落 | 「ADR 0020 の分（Catalog をやめる）は、その実装計画までサーバーのコードは今のまま」を「ADR 0020 の分はサーバー側の実装計画（この計画へのリンク）で済んだ」にする |
| [glossary.md](../wiki/glossary.md)「ポート」 | サーバーの例から `I<名詞>Catalog` を消す（`IRandomSource` だけにする） |
| [lesson-domain-types.md](../wiki/architecture/lesson-domain-types.md) | 「サーバーの詰め替えは `…/MasterCardCatalog.cs`」を `CardCatalogFactory.cs` にし、改名の日付を添える |
| [ADR 0020](../wiki/decisions/0020-masterdata-referenced-outside-shared-domain.md) | 先頭の注記に「サーバー側はこの計画（`../../plans/2026-11-08-server-masterdata-without-catalog.md` へのリンク）で実装した」を足す（決定の本文は変えない） |

終わりに `grep -rnE 'I(Gacha|Support|InitialGrant|DuplicateConversion|ProduceReward|PlayerLevel)Catalog|Master(Card|Produce|Gacha|Support|InitialGrant|DuplicateConversion|ProduceReward|PlayerLevel)Catalog|GachaDefinition|LevelUpCost|MasterData_IsReferencedOnlyByInfrastructure' uta_docs/wiki` で、ADR（`decisions/`）と、改名・変更の経緯として旧い名前を書いた行（[masterdata-pipeline.md](../wiki/architecture/master/masterdata-pipeline.md) の 2026-11-01 の履歴の行と、クライアントの「`MasterCardCatalog` から改名し」の行、[lesson-domain-types.md](../wiki/architecture/lesson-domain-types.md) の改名の経緯）のほかに残っていないことを確かめる。`python -I .claude/skills/linting-wiki/lint_wiki.py` がエラー 0。

## 5. テスト

足すテスト: `ArchitectureTests.SharedDomain_DoesNotDependOnMasterData`（3.4）と、`MasterDataTests` に移す 4 つ（3.5）。

消すテスト（中身はほかで確かめられている。「この計画で決めたこと」）:

| 消すテスト | 代わりに見るもの |
|---|---|
| `MasterGachaCatalogTests.Catalog_MatchesMasterCsv` | `MasterDataTests.Gacha_HaveExpectedCounts`・`GachaTables_WeightSums`・`Gacha_CeilingItems`、`MasterDataInvariantTests.Gacha_GuaranteedTableHasNoRarityR`、`GachaTests` |
| `MasterGachaCatalogTests.DuplicateConversion_ReturnsAmountByRarity` | `MasterDataTests.DuplicateConversion_AmountByKindAndRarity`（3.5）、`GachaTests.Draw_DuplicateIdol_ConvertsToShards`（5）・`Draw_MaxedSupportCard_ConvertsToSupportPoints`（300）、`MasterDataInvariantTests.DuplicateConversion_HasSixRows` |
| `MasterInitialGrantCatalogTests.Get_ReturnsInitialValues` | `PlayerSignUpSignInTests.SignIn_AfterSignUp_ReturnsInitialGrant` |
| `MasterSupportCatalogTests` の 2 件 | `MasterDataTests.SupportLevelCap_MaxLevelByRarityAndLimitBreak`（3.5）、`SupportTests.LevelUp_ToTarget_PaysAndUpdates`（Lv 1→3 で 117）・`LevelUp_ToCap_Succeeds`・`LevelUp_AboveCap_Rejected`・`LimitBreak_FullFlow_ReachesMaxLevel`、`MasterDataTests.SupportLevelCost_EveryRowIsBaseTimesLevel` |
| `MasterProduceRewardCatalogTests` の 5 件 | `MasterDataTests.EvaluationRankReward_FAndSSPlus`・`PlayerLevelExp_AllIsInLevelOrder`（3.5）・`Produce_HaveExpectedCounts`（行数）、`MasterDataInvariantTests.EvaluationRanks_HaveAllRanksAndIncreasingMinValue`、`ProducePlayTests`（報酬・Lv・`produce_results` の反映をマスタの値と比べる）、`MasterDataTests.PlayerLevelExp_EveryRowIs40TimesLevel` |

実行するコマンド: `dotnet build Uta.slnx`、`dotnet test --project server/Uta.Server.Tests`（結合テストは Docker が要る）。

## 6. 未決事項

なし。

## 7. 実装メモ

| タスク | コミット | ズレ |
|---|---|---|
