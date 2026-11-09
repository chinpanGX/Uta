---
title: 実装計画: client/ の画面 3/6 所持一覧・サポートカードの強化
status: in-progress
updated: 2026-11-09
spec: ../wiki/systems/idol-support.md
---

# 実装計画: client/ の画面 3/6 所持一覧・サポートカードの強化

## ADR 0020〜0022・naming.md への読み替え（2026-11-09。この節が本文・「基盤 11d9b06 への読み替え」の節と食い違ったら、この節が正）

[ADR 0020](../wiki/decisions/0020-masterdata-referenced-outside-shared-domain.md)・[0021](../wiki/decisions/0021-domain-entities-and-repositories.md)・[0022](../wiki/decisions/0022-client-external-concretes-in-infrastructure.md)（3 本とも accepted）と [naming.md](../wiki/architecture/naming.md) に合わせる。**画面の計画に共通の読み替えは [foundation の計画](2026-11-03-client-screens-foundation.md)の R0・8 章・9 章が正**で、この節は owned に固有のことだけを書く。形の手本は [gacha の計画](2026-11-03-client-screens-gacha.md)の G0〜G9 と、その実装（`IGachaUseCase`・`GachaUseCase`・`GachaMapper`・`GachaDtoFactory`・`FakeGachaUseCase`・`FakeGachaService`・`GachaFixtures`）。**この節の O1〜O7 に書いた型・シグネチャ・手順・テストは完成形**で、本文の 2〜5 章のうち、この節に出てくる型・ファイル・テストの記述は読まずに、この節だけで書く。本文のうちこの節が「本文のまま」と書いたもの（O9 に一覧）は本文を読む。前提: gacha（PR #57）の実装が済んでいる。

### O0. 決めた名前（foundation の R0 に書き戻した）

| 本文 | 読み替え | 理由 |
|---|---|---|
| `ISupportPort`・`SupportGateway`・`SupportApi`・`LevelUpSupportUseCase`・`LimitBreakSupportUseCase` | Application の `ISupportUseCase`、Infrastructure の具象 `SupportUseCase`、詰め替えの `SupportMapper`（static） | R0 |
| `ServerReasons` の 6 つ（と `ServerReasonsTests`） | **作らない**（`ServerReasons` は無い）。どの操作も業務エラーを enum に写す | R0（ユーザーの判断 2026-11-08） |
| `SupportStatus`（enum） | `SupportResultKind`（値は本文と同じ 8 つ） | R0 |
| `SupportOutcome`・`SupportLevelUpResult`・`SupportLimitBreakResult` | サーバーの `LevelUpResult(Level, SupportPoints, Gold)`・`LimitBreakResult(LimitBreak, MaxLevel)` と同じ名前・同じメンバーにし、`Kind`（`SupportResultKind`）を足した **`LevelUpResult`**・**`LimitBreakResult`**（gacha の `DrawResult` と同じ形）。`RefreshFailed` は無い（取り直しの失敗は具象が握りつぶす） | naming.md 1章、R0 |
| `SupportSnapshotEdits` | **作らない**。レベル上げ・上限解放の応答の `Common` の player_diff で、基盤がリポジトリ（残高・カードの `Level`／`LimitBreak`・素材の削除）を直す（サーバーの `PlayerDiffCollector` が `TouchBalance`・`TouchSupportCard`・`RemoveSupportCard` を記録する）。具象は自分でリポジトリに書かない。`IPlayerUseCase.RefreshAsync()` は安全網 | R0、gacha G3 |
| `IGameMaster.GetSupportMaxLevel`・`GetSupportMaxLimitBreak`・`GetSupportLevelUpCost`・`SupportLevelUpCost` | Application の **`SupportLevelPlanner`**（`MemoryDatabase` を受けるインスタンス）の `GetMaxLevel`・`GetMaxLimitBreak`・`GetLevelUpCost` と、費用の型 **`LevelUpCost`**（サーバーの `LevelUpCost` と同じ名前）。R0 の `SupportLevelUpCostCalculator`・`SupportLevelUpCost` は作らず、ここにまとめる | R0、naming.md 1章 |
| `LevelUpStep`・`OwnedSortKey`・`OwnedTab`・`OwnedDisabledReason` | `LevelUpStepKind`・`OwnedSortKind`・`OwnedTabKind`・`OwnedDisabledKind` | naming.md 2章 |
| `OwnedSelectionRequest` | **`OwnedSelectionCondition`**（選択モードの条件。`OwnedSelection` は結果の型の名前なので使えない）。`ForSupportCard` は formation R4 の形 `ForSupportCard(int idolId, IReadOnlyCollection<string> excludedUids)`（プロパティは `IdolId`・`ExcludedUids`。`IdolPlan`・`ExcludedSupportCardIds` は無い。プランと除外するカード ID は `OwnedListFactory` がマスタとリポジトリから引く） | naming.md 1章、formation R4 |
| `OwnedRowData` | **`OwnedListItem`**（Application の一覧の 1 件。`Row` はマスタの行の意味なので使わない） | naming.md 4章 |
| `OwnedListBuilder`（static） | **`OwnedListFactory`**（Application。`IReadOnlyIdolRepository`・`IReadOnlySupportCardRepository`・`MemoryDatabase` を受けるインスタンス） | R0 |
| `OwnedRowViewData` | **`OwnedRowViewDto`**（行の View は P1 の `ListRowView`。owned だけが使うので、選んだときの ID も持つ） | R0、naming.md 6章 |
| Presenter が組み立てる文字（`Refresh()` の見出し・ラベル・行の文、詳細の 4 行・アビリティ・ボタンのラベル・確認の文） | **`OwnedDtoFactory`**・**`SupportDetailDtoFactory`**（Presentation。ルートのスコープにシングルトン）と、Dto の **`OwnedViewDto`**・**`SupportDetailViewDto`**・**`SupportLevelStepViewDto`** | R0、ADR 0022 |
| `IGameMaster`・`IdolInfo`・`SupportCardInfo`・`SupportAbilityInfo`、`PlayerCache`・`PlayerSnapshot`・`OwnedIdol`・`OwnedSupportCard`・`RefreshPlayerUseCase` | 作らない。`MemoryDatabase` の `IdolTable`・`SupportCardTable`・`SupportAbilityTable`・`SupportLevelCapTable`・`SupportLevelCostTable`、リポジトリの `PlayerEntity`・`IdolEntity`（`MasterId` = IdolId）・`SupportCardEntity`（`UniqueId` = 所持ID、`MasterId` = SupportCardId）、`IPlayerUseCase.RefreshAsync()` | R0、ADR 0020・0021 |
| `FakeGameMaster`・`StubPlayerPort`・`FakeSupportPort` | `OwnedFixtures`（`TestMasterData` の上の補助）、本物のリポジトリ、`FakePlayerUseCase`、`FakeSupportUseCase`（Presenter のテスト）・`FakeSupportService`（具象のテスト） | R0 |

本文の `OwnedSelection`（`Cancelled`・`ForIdol(int idolId)`・`ForSupportCard(string supportCardUid)`・`IsCancelled`・`IdolId`・`SupportCardUid`）・`OwnedSelectionKind`・`OwnedArgs`・`SupportDetailArgs`（引数名は `supportCardUniqueId`、プロパティは `SupportCardUniqueId`）・`DisplayText`・`FormationValidator.IsPlanCompatible` は名前を変えない（formation がこの名前で使う）。

### O1. 名前空間・`using`・変数名の決まり

- フォルダと名前空間は本文 3 章のとおり（`Application/Support`・`Application/Owned`・`Infrastructure/Support`・`Presentation/Owned`・`Presentation/Common`）
- **enum の型**: 一覧・表示・選択の条件で使う `Rarity`・`Plan`・`ParamKind` は Shared.Domain の `Uta.Shared.Domain.Lesson`、`SupportType`・`SupportAbilityKind` は `Uta.Shared.Domain.Produce`（`DisplayText`・`FormationValidator.IsPlanCompatible` が Shared.Domain の型を受けるため）。マスタの値は `(Uta.Shared.Domain.Lesson.Plan)(int)row.Plan` のように数値を通して変える（値は同じ: R0 の 2 行目）。**マスタと Shared.Domain の enum を両方使うファイル（`OwnedListFactory`・`SupportDetailDtoFactory`・`SupportLevelPlanner`・`OwnedFixtures`・それらのテスト）は `using Uta.MasterData;` を書かず**、マスタの型を `Uta.MasterData.MemoryDatabase`・`Uta.MasterData.Rarity` と名前空間付きで書く（R0）。`SupportLevelPlanner` はマスタの `Rarity` だけを使い Shared.Domain の enum を使わないが、規則を 1 つにするため同じく名前空間付きで書く
- 名前空間 `Uta.Client.<層>.SupportCard`・`…Idol` があるので、マスタの型 `SupportCard`・`Idol` を型名だけで書かない（gacha G1。`var` で受ける）
- **変数名**（foundation 8.5）: フィールド・コンストラクターとメソッドの引数は型名から先頭の `I` と `ReadOnly` を除いて先頭を小文字にしたもの（`ownedView`・`ownedPage`・`ownedArgs`・`ownedDtoFactory`・`ownedListFactory`・`supportDetailView`・`supportDetailPage`・`supportDetailArgs`・`supportDetailDtoFactory`・`supportUseCase`・`supportService`・`playerUseCase`・`dialogOpener`・`screenNavigator`・`memoryDatabase`・`idolRepository`・`supportCardRepository`・`playerRepository`・`supportLevelPlanner`）。この節のシグネチャはこの名前で書いた。コレクション・組み込みの型・ループと短い一時のローカル変数・テストのコードは対象外
- **本文のコード・テストの旧い名前の読み方**（「本文 3.6・3.7・5 章のまま」と書いた所に出る名前は、この表で読む）:

  | 本文の名前 | 読み方 |
  |---|---|
  | `view`（`OwnedPresenter`／`SupportDetailPresenter` の中） | `ownedView`／`supportDetailView` |
  | `args`・`args.SupportCardUid` | `ownedArgs`／`supportDetailArgs`・`supportDetailArgs.SupportCardUniqueId` |
  | `request`（`OwnedPresenter` の状態） | `condition` |
  | `navigator`・`dialogs` | `screenNavigator`・`dialogOpener` |
  | `cache.Current`・`master`・`FindCard()`・`info`・`maxLevel` など（Presenter の中） | Presenter には無い。値は `OwnedDtoFactory`・`SupportDetailDtoFactory` の Dto から読む（O5） |
  | `levelUp.ExecuteAsync(card, target)`・`limitBreak.ExecuteAsync(card, material)`・`outcome.Status` | `supportUseCase.LevelUpAsync(uid, levelStep.TargetLevel)`・`supportUseCase.LimitBreakAsync(uid, materialUid)`・`levelUpResult.Kind`／`limitBreakResult.Kind`（`SupportResultKind`） |
  | テストの `FakeDialogService`・`FakeGameMaster`・`PlayerCache`・`StubPlayerPort`・`FakeSupportPort`（`Calls`） | `FakeDialogOpener`・`OwnedFixtures.CreateMaster()`・本物のリポジトリ（`OwnedFixtures.SetPlayer`／`SetOwned`、差し替えは `Upsert`）・`FakePlayerUseCase`・`FakeSupportUseCase`（`Calls`） |
  | `OwnedSupportCard(uid, cardId, level, limitBreak)` | `new SupportCardEntity(uid, cardId, level, limitBreak)` |
  | `OwnedTab`・`OwnedRowViewData`・`LevelUpStep`・`OwnedSelectionRequest`・`SupportStatus` | O0 の表の名前 |

- **値が変わる static の状態を持たない**（foundation 9 章・CLAUDE.md）。`OwnedSelection.Cancelled` は不変の 1 インスタンスの `static readonly`（get だけのプロパティで出す）。`SupportUseCase` の `handledCodes` は配列のフィールドを持たず、`WithHandledCodes(…)` の引数に直接並べる（O4。gacha の `GachaUseCase` と同じ）
- 書式は C# 9 の範囲・BOM 付き UTF-8・LF。`N0(x)` は `x.ToString("N0", CultureInfo.InvariantCulture)`
- **LINQ は ZLinq**（[ADR 0023](../wiki/decisions/0023-client-linq-with-zlinq.md)・CLAUDE.md）: client/ で LINQ を書くファイル（テストを含む）は `using System.Linq;` と `using ZLinq;` の両方を書く。結果を `IReadOnlyList<T>`・`IEnumerable<T>` として返す・渡す・持つときは `ToArray()`・`ToList()` で確定させる。返り値の型を `IEnumerable<T>` にしない。shared/ は標準の `System.Linq` のまま。この節の `using` の一覧に `System.Linq` があるファイルは `ZLinq` も足して読む

### O2. shared/ と `DisplayText`

- `FormationValidator.IsPlanCompatible` は本文 3.1 のまま（shared/。`Validate` の 2 つ目のループの条件を置き換える）
- `DisplayText` は本文 3.2 のまま（`Presentation/Common/DisplayText.cs`。Shared.Domain の enum を受ける static class。状態を持たない）

### O3. Application

`Application/Support/`（名前空間 `Uta.Client.Application.Support`。1 ファイル 1 型）:

```csharp
public enum SupportResultKind { Succeeded, NotEnoughPoints, NotEnoughGold, InvalidTarget, MaxLimitBreak, InvalidMaterial, NotOwned, Failed }
public enum LevelUpStepKind { Plus1, Plus5, Max }   // SupportDetailViewDto.LevelSteps の添字と同じ順

public sealed class LevelUpResult
{
    public SupportResultKind Kind { get; }
    public int Level { get; }           // Succeeded のとき上げた後のレベル。それ以外は 0
    public int SupportPoints { get; }   // Succeeded のとき残りの強化pt。それ以外は 0
    public int Gold { get; }            // Succeeded のとき残りのゴールド。それ以外は 0
    public static LevelUpResult Success(int level, int supportPoints, int gold);
    public static LevelUpResult Rejected(SupportResultKind kind);   // kind が Succeeded なら ArgumentException
    // コンストラクターは private 1 本
}

public sealed class LimitBreakResult
{
    public SupportResultKind Kind { get; }
    public int LimitBreak { get; }      // Succeeded のとき解放後の回数。それ以外は 0
    public int MaxLevel { get; }        // Succeeded のとき解放後の最大レベル。それ以外は 0
    public static LimitBreakResult Success(int limitBreak, int maxLevel);
    public static LimitBreakResult Rejected(SupportResultKind kind);   // kind が Succeeded なら ArgumentException
}

public interface ISupportUseCase
{
    /// <summary>
    /// supportCardUniqueId が null・空なら ArgumentException（通信しない）。サーバーの業務エラーは
    /// SUPPORT_NOT_ENOUGH_POINTS → NotEnoughPoints、PLAYER_NOT_ENOUGH_GOLD → NotEnoughGold、SUPPORT_INVALID_TARGET_LEVEL → InvalidTarget、SUPPORT_NOT_OWNED → NotOwned。
    /// ほかの失敗は Failed（共通のダイアログは ApiCore が出している）。成功したら所持データを取り直し（失敗しても成功を返す）、Succeeded。
    /// </summary>
    UniTask<LevelUpResult> LevelUpAsync(string supportCardUniqueId, int targetLevel);
    /// <summary>
    /// どちらかの ID が null・空、または 2 つが同じなら ArgumentException（通信しない）。SUPPORT_MAX_LIMIT_BREAK → MaxLimitBreak、SUPPORT_INVALID_MATERIAL → InvalidMaterial、
    /// SUPPORT_NOT_OWNED → NotOwned、ほかは Failed。成功の扱いは LevelUpAsync と同じ。
    /// </summary>
    UniTask<LimitBreakResult> LimitBreakAsync(string supportCardUniqueId, string materialUniqueId);
}

public sealed class LevelUpCost
{
    public LevelUpCost(int supportPoints, int gold);
    public int SupportPoints { get; } public int Gold { get; }
}

/// <summary>サポートカードの最大レベル・上限解放の最大・強化の費用・目標レベル（外部に触れない具象。ルートのスコープにシングルトン）。</summary>
public sealed class SupportLevelPlanner
{
    public SupportLevelPlanner(Uta.MasterData.MemoryDatabase memoryDatabase);
    /// <summary>カードのレア度（SupportCardTable.FindBySupportCardId(supportCardId).Rarity）と limitBreak の SupportLevelCap 行の MaxLevel。行が無ければ InvalidOperationException。</summary>
    public int GetMaxLevel(int supportCardId, int limitBreak);
    /// <summary>カードのレア度の SupportLevelCap 行の LimitBreak の最大。行が無ければ InvalidOperationException。</summary>
    public int GetMaxLimitBreak(int supportCardId);
    /// <summary>レベル fromLevel から toLevel - 1 までの SupportLevelCost 行（カードのレア度、Level = そのレベル）の SupportPoints・Gold の合計（サーバーの MasterSupportCatalog.GetLevelUpCost と同じ）。toLevel <= fromLevel なら 0・0。行が無ければ InvalidOperationException。</summary>
    public LevelUpCost GetLevelUpCost(int supportCardId, int fromLevel, int toLevel);
    /// <summary>
    /// 本文 3.3 の PlanTarget と同じ規則（maxLevel = GetMaxLevel(card.MasterId, card.LimitBreak)）。card.Level >= maxLevel なら card.Level。
    /// Plus1・Plus5 は Math.Min(card.Level + 1 / + 5, maxLevel)（所持が足りなくても返す）。Max は t を maxLevel から card.Level + 1 まで下げ、
    /// GetLevelUpCost(card.MasterId, card.Level, t) の SupportPoints <= supportPoints かつ Gold <= gold になる最初の t。無ければ card.Level。
    /// </summary>
    public int PlanTarget(SupportCardEntity card, int supportPoints, int gold, LevelUpStepKind step);
}
```

- 行は `memoryDatabase.SupportLevelCapTable.All`・`SupportLevelCostTable.All` を `Where` で探す（`FindBy…` の複合キーの形に頼らない）。`SupportCardTable.FindBySupportCardId` が投げる例外は捕まえない
- `using`: `System`・`System.Linq`・`Uta.Client.Domain.SupportCard`（`using Uta.MasterData;` は書かない: O1）

`Application/Owned/`（名前空間 `Uta.Client.Application.Owned`）:

```csharp
public enum OwnedSortKind { Rarity, Level, Acquired }
public enum OwnedDisabledKind { None, AlreadyInFormation, SameCardInFormation, PlanMismatch }
public enum OwnedSelectionKind { Idol, SupportCard, LimitBreakMaterial }

public sealed class OwnedListItem   // 本文 3.4 の OwnedRowData と同じ中身。名前と型だけ変える
{
    public OwnedListItem(bool isIdol, int idolId, string supportCardUid, int cardId, string name, Rarity rarity, Plan plan,
        SupportType supportType, int level, int limitBreak, int shards, bool enabled, OwnedDisabledKind disabledKind);
    // プロパティは本文 3.4 の OwnedRowData と同じ（DisabledReason は DisabledKind）。IdolId は IdolEntity.MasterId、SupportCardUid は SupportCardEntity.UniqueId、CardId は SupportCardEntity.MasterId
}

public sealed class OwnedSelection { ... }   // 本文 3.4 のまま。Cancelled は private static readonly のインスタンスを get だけのプロパティで出す

public sealed class OwnedSelectionCondition   // 値だけ（完了の仕組みは持たない）
{
    /// <summary>育成アイドルを選ばせる。excludedIdolIds は選べない（AlreadyInFormation）。null は空。</summary>
    public static OwnedSelectionCondition ForIdol(IReadOnlyCollection<int> excludedIdolIds = null);
    /// <summary>編成の枠に入れるサポートカードを選ばせる。idolId は育成アイドルの IdolId（0 ならプランで弾かない）。excludedUids はほかの枠の所持ID（null は空）。</summary>
    public static OwnedSelectionCondition ForSupportCard(int idolId, IReadOnlyCollection<string> excludedUids);
    /// <summary>本文 3.4 のまま（targetUid が null・空なら ArgumentException）。</summary>
    public static OwnedSelectionCondition ForLimitBreakMaterial(int supportCardId, string targetUid);

    public OwnedSelectionKind Kind { get; }
    public IReadOnlyCollection<int> ExcludedIdolIds { get; }   // Idol 以外は空
    public int IdolId { get; }                                 // SupportCard 以外は 0
    public IReadOnlyCollection<string> ExcludedUids { get; }   // SupportCard 以外は空
    public int MaterialCardId { get; }                         // LimitBreakMaterial 以外は 0
    public string MaterialTargetUid { get; }                   // LimitBreakMaterial 以外は null
}

/// <summary>所持一覧の並び・絞り込み・選べるか（外部に触れない具象。ルートのスコープにシングルトン）。</summary>
public sealed class OwnedListFactory
{
    public OwnedListFactory(IReadOnlyIdolRepository idolRepository, IReadOnlySupportCardRepository supportCardRepository, Uta.MasterData.MemoryDatabase memoryDatabase);
    /// <summary>本文 3.4 の BuildIdols と同じ規則。所持は idolRepository.FindAll() を MasterId の昇順。condition が null なら全部 Enabled、Kind == Idol なら ExcludedIdolIds の行を AlreadyInFormation、ほかの Kind は ArgumentException。</summary>
    public IReadOnlyList<OwnedListItem> CreateIdols(OwnedSelectionCondition condition);
    /// <summary>本文 3.4 の BuildSupportCards と同じ手順 (1)〜(5) と並べ方（所持は supportCardRepository.FindAll()）。(5) の Kind == SupportCard の判定は次の「選べるかの判定」。</summary>
    public IReadOnlyList<OwnedListItem> CreateSupportCards(OwnedSortKind sort, bool descending, Plan? planFilter, SupportType? supportTypeFilter, OwnedSelectionCondition condition);
}
```

- 名前・レア度・プラン・タイプ・かけらは、育成アイドルは `memoryDatabase.IdolTable.FindByIdolId(e.MasterId)` と `e.Shards`、サポートカードは `memoryDatabase.SupportCardTable.FindBySupportCardId(e.MasterId)`（`Type` が `SupportType`）。マスタの enum は O1 のとおり Shared.Domain の型に変える。育成アイドルの `SupportType` は `SupportType.Nothing`、`Level`・`LimitBreak` は 0
- **選べるかの判定（`Kind == SupportCard`）**: 先に 2 つを作る。`excludedCardIds` = `ExcludedUids` の各 uid を `supportCardRepository.Find(uid)` で引いた所持の `MasterId`（見つからない uid は無視）。`idolPlan` = `IdolId != 0` なら `(Plan)(int)memoryDatabase.IdolTable.FindByIdolId(IdolId).Plan`、`0` なら判定しない。各行を上から順に: `ExcludedUids` に所持ID がある → `AlreadyInFormation`、`excludedCardIds` に `CardId` がある → `SameCardInFormation`、`IdolId != 0` で `!FormationValidator.IsPlanCompatible(idolPlan, 行の Plan)` → `PlanMismatch`。どれにも当たらなければ `Enabled = true`・`None`
- `using`: `System`・`System.Collections.Generic`・`System.Linq`・`Uta.Client.Domain.Idol`・`Uta.Client.Domain.SupportCard`・`Uta.Shared.Domain.Lesson`・`Uta.Shared.Domain.Produce`（`using Uta.MasterData;` は書かない）

### O4. Infrastructure

`Infrastructure/Support/SupportMapper.cs`・`SupportUseCase.cs`（名前空間 `Uta.Client.Infrastructure.Support`。`using`: `System`・`Cysharp.Threading.Tasks`・`Uta.Client.Application.Player`・`Uta.Client.Application.Support`・`Uta.Shared.Contracts.Player`（`PlayerErrors`）・`Uta.Shared.Contracts.Support`・`VolTech.Client`・`VolTech.Server.Abstractions`（`ErrorCode`）。足りなければ `GachaUseCase` の `using` に合わせる）:

```csharp
public static class SupportMapper
{
    public static LevelUpResult ToLevelUpResult(LevelUpResponse response);         // LevelUpResult.Success(response.Level, response.SupportPoints, response.Gold)
    public static LimitBreakResult ToLimitBreakResult(LimitBreakResponse response); // LimitBreakResult.Success(response.LimitBreak, response.MaxLevel)
}

public sealed class SupportUseCase : ISupportUseCase
{
    public SupportUseCase(ISupportService supportService, IPlayerUseCase playerUseCase);
}
```

- `LevelUpAsync`（`async`）: 1. 引数の確かめ（O3 の `<summary>`）→ 2. `var result = await supportService.WithHandledCodes(SupportErrors.InvalidTargetLevel, SupportErrors.NotEnoughPoints, PlayerErrors.NotEnoughGold, SupportErrors.NotOwned).LevelUpAsync(new LevelUpRequest(supportCardUniqueId, targetLevel)).ToResult();` → 3. `result.IsError` なら `result.ErrorCode` を `==` で比べて O3 の対応で `LevelUpResult.Rejected(…)`（どれでもなければ `Failed`）。取り直しはしない → 4. 成功: `var levelUpResult = SupportMapper.ToLevelUpResult(result.Value);` → `try { await playerUseCase.RefreshAsync(); } catch (InvalidOperationException) { }` → `return levelUpResult;`（gacha の `GachaUseCase.DrawAsync` の 5・6 と同じ形）
- `LimitBreakAsync`: 同じ形。`WithHandledCodes(SupportErrors.MaxLimitBreak, SupportErrors.InvalidMaterial, SupportErrors.NotOwned)`、`new LimitBreakRequest(supportCardUniqueId, materialUniqueId)`、`SupportMapper.ToLimitBreakResult`
- どの `ErrorCode` も `ErrorDefinitions` に `Stay` で登録済み（`SupportErrors` 5 つと `PlayerErrors.NotEnoughGold`）。`handledCodes` に入れたものは共通のダイアログを出さずに返る
- player_diff（O0 の `SupportSnapshotEdits` の行）。具象はリポジトリに書かない
- `ApiClient`: `public ISupportService Support { get; }` を `Gacha` の次に足す。private コンストラクターの引数の最後に `ISupportService support` を足して `Support = support;`。`Create` の `var gachaClient = …;` の次に `var supportClient = core.CreateClient<ISupportService>();` を足し、`new ApiClient(..., gachaClient, supportClient)`。`using Uta.Shared.Contracts.Support;` を足す
- `MagicOnionClientInitializer`: `[MagicOnionClientGeneration(typeof(IDeviceService), typeof(IPlayerService), typeof(IGachaService), typeof(ISupportService))]`、`using Uta.Shared.Contracts.Support;`

### O5. Presentation

**所持一覧**（`Presentation/Owned/`）:

```csharp
public enum OwnedTabKind { Idols, SupportCards }

public sealed class OwnedArgs
{
    public OwnedArgs(OwnedSelectionCondition selection);   // null なら閲覧モード
    public OwnedSelectionCondition Selection { get; }
    public static OwnedArgs Browse();                      // new OwnedArgs(null)
}

public sealed class OwnedRowViewDto
{
    public OwnedRowViewDto(int idolId, string supportCardUid, string title, string detail, bool enabled);
    public int IdolId { get; }               // 育成アイドルの行: IdolId。サポートカードの行: 0
    public string SupportCardUid { get; }    // サポートカードの行: 所持ID。育成アイドルの行: null
    public string Title { get; } public string Detail { get; } public bool Enabled { get; }
}

public sealed class OwnedViewDto
{
    public OwnedViewDto(string title, bool tabsVisible, bool controlsVisible, string sortLabel, string orderLabel, string planLabel, string typeLabel,
        bool filtersVisible, IReadOnlyList<OwnedRowViewDto> rows);
    // プロパティは引数と同じ名前（Title・TabsVisible・ControlsVisible・SortLabel・OrderLabel・PlanLabel・TypeLabel・FiltersVisible・Rows）
}

public sealed class OwnedDtoFactory
{
    public OwnedDtoFactory(OwnedListFactory ownedListFactory);
    public OwnedViewDto Create(OwnedSelectionCondition condition, OwnedTabKind tab, OwnedSortKind sort, bool descending, Plan? planFilter, SupportType? typeFilter);
}

public interface IOwnedView   // 本文 3.6 のまま。型だけ OwnedTab → OwnedTabKind、OwnedRowViewData → OwnedRowViewDto
{
    void SetTitle(string title);
    void SetTabs(OwnedTabKind selected, bool visible);
    void SetControls(bool visible, string sortLabel, string orderLabel, string planLabel, string typeLabel, bool filtersVisible);
    void SetRows(IReadOnlyList<OwnedRowViewDto> rows);
    event Action<OwnedTabKind> TabSelected; event Action SortKeyClicked; event Action SortOrderClicked; event Action PlanFilterClicked;
    event Action TypeFilterClicked; event Action<int> RowClicked; event Action BackClicked;
}

[AssetAddress(nameof(OwnedPage))]
public sealed class OwnedPresenter : IScreenWithArgs<OwnedArgs>
{
    [Inject] public OwnedPresenter(OwnedPage ownedPage, OwnedArgs ownedArgs, OwnedDtoFactory ownedDtoFactory, IScreenNavigator screenNavigator)
        : this((IOwnedView)ownedPage, ownedArgs, ownedDtoFactory, screenNavigator) { }
    public OwnedPresenter(IOwnedView ownedView, OwnedArgs ownedArgs, OwnedDtoFactory ownedDtoFactory, IScreenNavigator screenNavigator);   // ownedArgs が null なら ArgumentNullException
    // 公開メソッドは本文 3.6 のまま（InitializeAsync・DidPopEnter・Dispose・CompleteAsync・SelectRowAsync・BackAsync・SelectTab(OwnedTabKind)・CycleSortKey・ToggleSortOrder・CyclePlanFilter・CycleTypeFilter）
}
```

- **`OwnedDtoFactory.Create`**（本文 3.6 の表と `Refresh()` の 2〜4 の規則をここへ移す）:
  - `Title`: `condition` が null → `"所持一覧"`、`Idol` → `"育成アイドルを選ぶ"`、`SupportCard` → `"サポートカードを選ぶ"`、`LimitBreakMaterial` → `"素材にするカードを選ぶ"`
  - `TabsVisible` = `condition == null`。`FiltersVisible` = `condition == null || condition.Kind == OwnedSelectionKind.SupportCard`。`ControlsVisible`: `condition == null` なら `tab == OwnedTabKind.SupportCards`、`Idol` なら `false`、`SupportCard`・`LimitBreakMaterial` なら `true`
  - `Rows`: `tab == Idols` なら `ownedListFactory.CreateIdols(condition)`、そうでなければ `ownedListFactory.CreateSupportCards(sort, descending, FiltersVisible ? planFilter : null, FiltersVisible ? typeFilter : null, condition)` の各 `item` を `new OwnedRowViewDto(item.IdolId, item.SupportCardUid, title, detail, item.Enabled)` に。`title`・`detail`・無効の印は本文 3.6 の `Refresh()` の 3 のまま
  - ラベル（`SortLabel`・`OrderLabel`・`PlanLabel`・`TypeLabel`）は本文 3.6 の `Refresh()` の 4 のまま（行を出さないモードでも作る）
- **`OwnedPresenter`**:
  - 状態: `condition = ownedArgs.Selection`、`filtersVisible`（コンストラクターで `condition == null || condition.Kind == OwnedSelectionKind.SupportCard` に決め、変えない。`OwnedDtoFactory` と同じ式）、`result = OwnedSelection.Cancelled`、`current`（直近の `OwnedViewDto`）、`running`・`closed`・`disposed`（本文 3.6）、`tab`・`sort`・`descending`・`planFilter`（`Plan?`）・`typeFilter`（`SupportType?`）。初期値は本文 3.6 の表の `tab`・初期の並び（`Idol` のモードは `Rarity`・`true`）。`planFilter`・`typeFilter` の初期値は null
  - `Refresh()`: `current = ownedDtoFactory.Create(condition, tab, sort, descending, planFilter, typeFilter);` → `ownedView.SetTitle(current.Title)`、`ownedView.SetTabs(tab, current.TabsVisible)`、`ownedView.SetControls(current.ControlsVisible, current.SortLabel, current.OrderLabel, current.PlanLabel, current.TypeLabel, current.FiltersVisible)`、`ownedView.SetRows(current.Rows)` の順
  - 操作は本文 3.6 の「操作」のまま。読み替え: `request` → `condition`、`rows` → `current.Rows`（`current` は `InitializeAsync` の `Refresh()` で入る。操作はどれも、`current` が null なら何もしない）、`SelectRowAsync` の 3 の `row.IsIdol ? …` → `row.SupportCardUid == null ? OwnedSelection.ForIdol(row.IdolId) : OwnedSelection.ForSupportCard(row.SupportCardUid)`、4 の `new SupportDetailArgs(row.SupportCardUid)`、5 の「閲覧モードで育成アイドル」は `row.SupportCardUid == null`
  - イベントの受け口（名前付きの private メソッド）・`CompleteAsync`・`DidPopEnter`・`Dispose`・`closed` の決まりは本文 3.6 のまま
- **`OwnedPage`**: 本文 3.6 の「`OwnedPage` の実装」のまま（型だけ `OwnedTabKind`・`OwnedRowViewDto`。`SetRows` の `rows[i].Title`・`Detail`・`Enabled`）

**サポートカードの詳細**（`Presentation/Owned/`）:

```csharp
public sealed class SupportDetailArgs { public SupportDetailArgs(string supportCardUniqueId); public string SupportCardUniqueId { get; } }

public sealed class SupportLevelStepViewDto
{
    public SupportLevelStepViewDto(string label, bool enabled, int targetLevel, string confirmText);
    public string Label { get; } public bool Enabled { get; }
    public int TargetLevel { get; }      // Enabled のとき目標のレベル。無効なら今のレベル（カードが無ければ 0）
    public string ConfirmText { get; }   // Enabled のときの確認の文。無効なら null
}

public sealed class SupportDetailViewDto
{
    public SupportDetailViewDto(bool exists, int supportCardId, IReadOnlyList<string> infoLines, IReadOnlyList<string> abilityLines, string resourcesText,
        IReadOnlyList<SupportLevelStepViewDto> levelSteps, string limitBreakLabel, bool limitBreakEnabled, bool hasMaterial);
    // プロパティは引数と同じ名前（Exists・SupportCardId・InfoLines・AbilityLines・ResourcesText・LevelSteps・LimitBreakLabel・LimitBreakEnabled・HasMaterial）
    // LevelSteps は 3 つで、添字は (int)LevelUpStepKind（Plus1・Plus5・Max）
}

public sealed class SupportDetailDtoFactory
{
    public SupportDetailDtoFactory(IReadOnlyPlayerRepository playerRepository, IReadOnlySupportCardRepository supportCardRepository,
        Uta.MasterData.MemoryDatabase memoryDatabase, SupportLevelPlanner supportLevelPlanner);
    public SupportDetailViewDto Create(string supportCardUniqueId);
    /// <summary>本文 3.7 の LimitBreakAsync の 5 の確認の文（素材の Lv・カードの名前・今の解放数）。カードか素材が所持に無ければ null。</summary>
    public string CreateLimitBreakConfirmText(string supportCardUniqueId, string materialUniqueId);
}

public interface ISupportDetailView { ... }   // 本文 3.7 のまま

[AssetAddress(nameof(SupportDetailPage))]
public sealed class SupportDetailPresenter : IScreenWithArgs<SupportDetailArgs>
{
    [Inject] public SupportDetailPresenter(SupportDetailPage supportDetailPage, SupportDetailArgs supportDetailArgs, SupportDetailDtoFactory supportDetailDtoFactory,
        ISupportUseCase supportUseCase, DialogOpener dialogOpener, IScreenNavigator screenNavigator)
        : this((ISupportDetailView)supportDetailPage, supportDetailArgs, supportDetailDtoFactory, supportUseCase, dialogOpener, screenNavigator) { }
    public SupportDetailPresenter(ISupportDetailView supportDetailView, SupportDetailArgs supportDetailArgs, SupportDetailDtoFactory supportDetailDtoFactory,
        ISupportUseCase supportUseCase, DialogOpener dialogOpener, IScreenNavigator screenNavigator);
    // 公開メソッドは本文 3.7 のまま（InitializeAsync・DidPopEnter・Dispose・LevelUpAsync(LevelUpStepKind)・LimitBreakAsync・BackAsync）
}
```

- **`SupportDetailDtoFactory.Create(uid)`**（本文 3.7 の `FindCard()` と `Refresh()` の規則をここへ移す）:
  - `var card = supportCardRepository.Find(uid);`。null なら `new SupportDetailViewDto(false, 0, new[] { "このカードは所持していません。" }, new string[0], "", 3 つの new SupportLevelStepViewDto(名前, false, 0, null), "上限解放", false, false)`（名前は `"+1"`・`"+5"`・`"最大"`）
  - `var player = playerRepository.Find();` の `SupportPoints`・`Gold`（null なら 0）を `sp`・`gold` に。`var info = memoryDatabase.SupportCardTable.FindBySupportCardId(card.MasterId);`（レア度・タイプ・プランは O1 のとおり Shared.Domain の型に変えて `DisplayText` へ）。`maxLevel = supportLevelPlanner.GetMaxLevel(card.MasterId, card.LimitBreak)`、`maxBreak = supportLevelPlanner.GetMaxLimitBreak(card.MasterId)`、`fullMaxLevel = supportLevelPlanner.GetMaxLevel(card.MasterId, maxBreak)`
  - `InfoLines`・`ResourcesText` は本文 3.7 の `SetInfo`・`SetResources` の文のまま
  - `AbilityLines`: `memoryDatabase.SupportAbilityTable.All` のうち `SupportCardId == card.MasterId` を `Order` の昇順に、本文 3.7 の `SetAbilities` の文（`ProduceFormulas.AbilityValue(a.ValueAtLv1, a.ValueAtMax, card.Level, fullMaxLevel)`）。0 件なら `new[] { "なし" }`
  - `LevelSteps`: 3 つの `step` ごとに `target = supportLevelPlanner.PlanTarget(card, sp, gold, step)`。ラベルと有効は本文 3.7 の `SetLevelButtons` のまま（`cost = supportLevelPlanner.GetLevelUpCost(card.MasterId, card.Level, target)`）。有効なら `TargetLevel = target`・`ConfirmText` = 本文 3.7 の `LevelUpAsync` の 3 の文、無効なら `TargetLevel = card.Level`・`ConfirmText = null`
  - `LimitBreakLabel`・`LimitBreakEnabled`: 本文 3.7 の `SetLimitBreak` のまま。`HasMaterial` = `supportCardRepository.FindAll()` に `MasterId == card.MasterId` かつ `UniqueId != card.UniqueId` が 1 つ以上ある
  - `SupportCardId` = `card.MasterId`
- **`SupportDetailPresenter`**（`running`・`closed`・`disposed`・購読・`Dispose`・`BackAsync`・イベントの受け口は本文 3.7 のまま。`await` のあとは `if (disposed) return;`）:
  - `Refresh()`: `var dto = supportDetailDtoFactory.Create(supportDetailArgs.SupportCardUniqueId);` → `SetInfo(dto.InfoLines)`、`SetAbilities(dto.AbilityLines)`、`SetResources(dto.ResourcesText)`、`SetLevelButtons(LevelSteps[0].Label, [1].Label, [2].Label, [0].Enabled, [1].Enabled, [2].Enabled)`、`SetLimitBreak(dto.LimitBreakLabel, dto.LimitBreakEnabled)` の順
  - `LevelUpAsync(step)`: 1. `var dto = …Create(uid);`（押した時点の値）。`!dto.Exists` なら終わる → 2. `var levelStep = dto.LevelSteps[(int)step];` `!levelStep.Enabled` なら終わる → 3. `await dialogOpener.ConfirmAsync(levelStep.ConfirmText)`。`Negative` なら終わる → 4. `var levelUpResult = await supportUseCase.LevelUpAsync(uid, levelStep.TargetLevel);` → 5. 本文 3.7 の 5 の分岐（`Succeeded` → `Refresh()`、4 つの文言の `ShowInfoAsync` のあと `Refresh()`、`Failed` は何もしない）
  - `LimitBreakAsync()`: 1. `dto = Create(uid)`。`!dto.Exists` か `!dto.LimitBreakEnabled` なら終わる → 2. `!dto.HasMaterial` なら `await dialogOpener.ShowInfoAsync("素材にできる同じカードがありません。")` で終わる → 3. `var ownedPresenter = await screenNavigator.PushPageAsync<OwnedPresenter, OwnedArgs>(new OwnedArgs(OwnedSelectionCondition.ForLimitBreakMaterial(dto.SupportCardId, uid)), playAnimation: false); var selection = await screenNavigator.WaitForPopAsync<OwnedSelection>(ownedPresenter);`。`selection == null || selection.IsCancelled` なら終わる → 4. `var confirmText = supportDetailDtoFactory.CreateLimitBreakConfirmText(uid, selection.SupportCardUid);` null なら終わる → 5. `ConfirmAsync(confirmText)`。`Negative` なら終わる → 6. `var limitBreakResult = await supportUseCase.LimitBreakAsync(uid, selection.SupportCardUid);` → 7. 本文 3.7 の 7 の分岐
- **`SupportDetailPage`**: 本文 3.7 の「`SupportDetailPage` の実装」のまま

**ホーム**: `HomePresenter.OpenOwnedAsync()` を `OpenGachaAsync` と同じ形（`running` の `try` の中で `await screenNavigator.PushPageAsync<OwnedPresenter, OwnedArgs>(OwnedArgs.Browse(), playAnimation: false);`）にする。コンストラクターは変えない。`using Uta.Client.Presentation.Owned;` を足す（本文 3.8）。

### O6. Bootstrap

`BootstrapEntry.RegisterUseCases` の `builder.Register<IGachaService>(…)` の次に `builder.Register<ISupportService>(resolver => resolver.Resolve<ApiClient>().Support, Lifetime.Singleton);`、最後（`GachaUseCase` の次）にこの順で 5 行:

```csharp
builder.Register<SupportLevelPlanner>(Lifetime.Singleton);
builder.Register<OwnedListFactory>(Lifetime.Singleton);
builder.Register<OwnedDtoFactory>(Lifetime.Singleton);
builder.Register<SupportDetailDtoFactory>(Lifetime.Singleton);
builder.Register<SupportUseCase>(Lifetime.Singleton).As<ISupportUseCase>();
```

`OwnedDtoFactory`・`SupportDetailDtoFactory` の 2 行はタスク 3a で型ができてから足す。`using Uta.Client.Application.Owned;`・`using Uta.Client.Application.Support;`・`using Uta.Client.Infrastructure.Support;`・`using Uta.Client.Presentation.Owned;`・`using Uta.Shared.Contracts.Support;` を足す。Presenter・ページは登録しない（本文 3.8）。

### O7. テスト（本文 5 章の表を置き換える。置き場 `Tests/EditMode/`、名前空間 `Uta.Client.Tests`）

偽と補助（`Fakes/`・`Support/` は public、1 ファイル 1 型）:

- `Fakes/FakeSupportUseCase.cs`（`ISupportUseCase`）: `FakeSupportUseCase(CallLog log = null)`。`Queue<Func<UniTask<LevelUpResult>>> LevelUpResults`（空なら `InvalidOperationException("FakeSupportUseCase: no level up result")`）、`Queue<Func<UniTask<LimitBreakResult>>> LimitBreakResults`（空なら `…: no limit break result`）、`List<string> Calls`（`"LevelUp:" + uid + "," + targetLevel`／`"LimitBreak:" + uid + "," + materialUid`）。`log` があれば同じ文字列を入口で記録。`Dequeue` した `Func` を呼んでその `UniTask` を返す
- `Fakes/FakeSupportService.cs`（`ISupportService`。`FakeGachaService` と同じ形）: `Queue<Func<UnaryResult<LevelUpResponse>>> LevelUpResults`・`LimitBreakResults`、`List<string> Calls`（`"LevelUp:" + r.SupportCardUniqueId + "," + r.TargetLevel`、`"LimitBreak:" + r.SupportCardUniqueId + "," + r.MaterialUniqueId`）、static の `Ok<T>`・`Fail<T>(ErrorCode)`。`WithHost` は `NotSupportedException`、ほかの `With…` は `this`。キューが空なら `InvalidOperationException("FakeSupportService: no result")`
- `Support/OwnedFixtures.cs`（`public static class`。O1 のとおりマスタの型は `Uta.MasterData.` 付き）:
  ```csharp
  /// <summary>共通の前提のマスタ（本文 5 章の FakeGameMaster の値を MemoryDatabase にしたもの）:
  /// Idol: 1「アイドルA」SSR・Sense、2「アイドルB」R・Logic、3「アイドルC」SR・Sense（OwnedFixtures の中で new Uta.MasterData.Idol { … } を直接書く。列は TestMasterData.Idol と同じで Plan だけ変える。TestMasterData.cs は触らない。3 は所持の並びのテスト用で、SetOwned は持たせない）。
  /// SupportCard: 10「サポートA」SSR・Singing・Free、11「サポートB」SR・Rhythm・Sense、12「サポートC」R・Expression・Logic。
  /// SupportAbility: (10, Order 1, InitialParam, Singing, ValueAtLv1 10, ValueAtMax 50)。
  /// SupportLevelCap: R・SR・SSR それぞれ LimitBreak 0〜4 で MaxLevel = 40 + LimitBreak * 5。
  /// SupportLevelCost: R・SR・SSR それぞれ Level 1〜64 で SupportPoints 100・Gold 50。</summary>
  public static Uta.MasterData.MemoryDatabase CreateMaster();
  /// <summary>playerRepository に new PlayerEntity("P1", 1, 0, gold, supportPoints, 0, false, false) を Upsert。</summary>
  public static void SetPlayer(PlayerRepository playerRepository, int gold = 1000, int supportPoints = 1000);
  /// <summary>idolRepository に IdolEntity("I2", 2, 0, 0, 0)・("I1", 1, 0, 0, 3)、supportCardRepository に ("01A", 10, 3, 0)・("01B", 11, 5, 1)・("01C", 12, 1, 0)・("01D", 10, 1, 0) をこの順に Upsert（Clear してから）。</summary>
  public static void SetOwned(IdolRepository idolRepository, SupportCardRepository supportCardRepository);
  ```
- 偽の View（`FakeOwnedView`・`FakeSupportDetailView`）は本文 5 章の書き方のまま（各テストのファイルの `private sealed class`。記録の書式も本文のまま。`SetRows` は `r.Title + "/" + r.Detail + "/" + r.Enabled`）
- 共通の前提: `OwnedFixtures.CreateMaster()`、本物の `PlayerRepository`・`IdolRepository`・`SupportCardRepository` に `SetPlayer`・`SetOwned`。期待の文字列 `IdolRows`・`SupportRowsRarityDesc`・詳細の既定の表示は本文 5 章の「共通の前提」のまま（値が同じなので同じ文字列になる。`fullMaxLevel` は 60）

| ファイル | テスト | 内容 |
|---|---|---|
| `shared/.../FormationValidatorTests.cs` | `IsPlanCompatible_Cases_ReturnsExpected` | 本文 5 章のまま |
| `DisplayTextTests.cs` | 本文 5 章の 7 本 | 本文のまま |
| `OwnedSelectionConditionTests.cs` | `ForIdol_NullExcluded_IsEmpty`・`ForSupportCard_NullExcluded_IsEmpty`（`IdolId` が引数の値）・`ForLimitBreakMaterial_SetsFields`・`ForLimitBreakMaterial_EmptyTarget_Throws` | |
| `OwnedListFactoryTests.cs`（`OwnedFixtures.CreateMaster()`、本物の `IdolRepository`・`SupportCardRepository`。サポートカードの所持を差し替えるテストは `Clear()` してから、書いた所持だけを `Upsert` する。所持の `Level` は書いていなければ 1、`LimitBreak` は 0。期待は所持ID・IdolId の並びと `Enabled`・`DisabledKind`） | `CreateIdols_OrderedByIdolId` | 所持 `("I3", 3)`・`("I1", 1)`・`("I2", 2)` の順 → IdolId 1・2・3、全部有効 |
| | `CreateIdols_Selection_DisablesExcluded` | `SetOwned` → `ForIdol(new[] { 2 })` → 1 は有効・`None`、2 は無効・`AlreadyInFormation` |
| | `CreateIdols_SupportCondition_Throws` | `ForSupportCard(0, null)` → `ArgumentException` |
| | `CreateSupportCards_SortByRarityDescending` | `SetOwned`・`Rarity`・降順・絞り込みなし・条件なし → 01A・01D・01B・01C |
| | `CreateSupportCards_SortByLevelDescending_TieByUidAscending` | `SetOwned`・`Level`・降順 → 01B・01A・01C・01D |
| | `CreateSupportCards_SortByAcquiredDescending` | `SetOwned`・`Acquired`・降順 → 01D・01C・01B・01A |
| | `CreateSupportCards_PlanFilter_ExactMatch` | `SetOwned`・`Rarity`・降順・`planFilter = Plan.Free` → 01A・01D |
| | `CreateSupportCards_TypeFilter` | `SetOwned`・`supportTypeFilter = SupportType.Rhythm` → 01B |
| | `CreateSupportCards_Selection_DisablesByPriority` | 所持 `"s1"`・`"s2"`・`"s3"`（3 枚とも CardId 12・Plan Logic）・`"s4"`（10・Free）・`"s5"`（11・Sense）、`Acquired`・昇順、`ForSupportCard(1, new[] { "s1" })`（育成アイドル 1 は Sense）→ s1 `AlreadyInFormation`、s2・s3 `SameCardInFormation`（s1 の CardId 12）、s4・s5 有効・`None` |
| | `CreateSupportCards_Selection_PlanMismatch` | 所持 `"s1"`（12・Logic）・`"s4"`（10・Free）、`Acquired`・昇順、`ForSupportCard(1, null)` → s1 `PlanMismatch`、s4 有効 |
| | `CreateSupportCards_Selection_UnknownExcludedUid_Ignored` | `SetOwned`、`ForSupportCard(0, new[] { "zz" })` → 4 枚とも有効（`IdolId == 0` はプランで弾かない） |
| | `CreateSupportCards_Material_ListsSameCardExceptTarget` | 所持 `"m1"`・`"m2"`・`"m3"`（10）・`"x1"`（11）、`Level`・昇順、`ForLimitBreakMaterial(10, "m2")` → m1・m3（全部有効） |
| | `CreateSupportCards_IdolCondition_Throws` | `ForIdol()` → `ArgumentException` |
| | `CreateSupportCards_Empty_ReturnsEmpty` | 所持 0 → 0 件 |
| `SupportLevelPlannerTests.cs` | `GetMaxLevel_FromCap`（10・LB 2 → 50）・`GetMaxLimitBreak_IsFour`・`GetLevelUpCost_SumsRows`（3 → 8 で 500・250）・`GetLevelUpCost_NotAbove_Zero` | |
| | `Plus1_BelowMax_ReturnsLevelPlusOne`（Lv 3 → 4）・`Plus5_NearMax_ClampsToMax`（Lv 38 → 40）・`AtMax_ReturnsCurrentLevel`（Lv 40）・`Plus5_NotAffordable_StillReturnsTarget`（強化pt 0 で 3 → 8）・`Max_AffordableLimited_ReturnsHighestAffordable`（強化pt 250・G 1000 で 3 → 5）・`Max_CannotAffordAny_ReturnsCurrentLevel`（強化pt 50） | カードは `new SupportCardEntity("u", 10, level, 0)` |
| `SupportMapperTests.cs` | `ToLevelUpResult_Maps`・`ToLimitBreakResult_Maps` | `Kind == Succeeded` と 3 つ・2 つの値 |
| `SupportUseCaseTests.cs`（`FakeSupportService`・`FakePlayerUseCase`） | `LevelUp_Success_RefreshesAndReturnsResult` | `Ok(new LevelUpResponse(4, 900, 950))` → `Succeeded`・`Level == 4`、`RefreshCount == 1`、`Calls` が `"LevelUp:01A,4"` |
| | `LevelUp_RefreshFails_StillSucceeds` | `RefreshResults.Enqueue(() => throw new InvalidOperationException("sign-in failed: x"))` → `Succeeded` |
| | `LevelUp_ErrorCodes_MapToKinds` | 4 つの `Fail` を順に → `NotEnoughPoints`・`NotEnoughGold`・`InvalidTarget`・`NotOwned`、`RefreshCount == 0` |
| | `LevelUp_OtherError_ReturnsFailed` | `Fail(SupportErrors.MaxLimitBreak)` → `Failed` |
| | `LevelUp_EmptyUid_Throws` | `try { await …LevelUpAsync("", 4); Assert.Fail(); } catch (ArgumentException) { }`、`Calls` が空 |
| | `LimitBreak_Success_RefreshesAndReturnsResult` | `Ok(new LimitBreakResponse(1, 45))` → `Succeeded`・`LimitBreak == 1`・`MaxLevel == 45`、`Calls` が `"LimitBreak:01A,01D"` |
| | `LimitBreak_ErrorCodes_MapToKinds` | `MaxLimitBreak`・`InvalidMaterial`・`NotOwned`、ほか（`SupportErrors.NotEnoughPoints`）は `Failed` |
| | `LimitBreak_SameUid_Throws` | `ArgumentException`、`Calls` が空 |
| `OwnedDtoFactoryTests.cs` | `Create_Browse_IdolTab`・`Create_IdolSelection_DisabledSuffix`・`Create_Material_HidesFilters` | `OwnedPresenterTests` の同じ 3 本と値が重なるが、両方作る（DtoFactory を単体で確かめるため）。本文 5 章の `OwnedPresenterTests` の `InitializeAsync_Browse_…`・`InitializeAsync_IdolSelection_…`・`InitializeAsync_Material_…` と同じ値を Dto のプロパティで確かめる |
| `OwnedPresenterTests.cs`（本物の `OwnedDtoFactory`・`OwnedListFactory`、`FakeScreenNavigator(log)`） | 本文 5 章の `OwnedPresenterTests` の 20 本 | 本文のまま。読み替え: `OwnedTab` → `OwnedTabKind`、`OwnedSelectionRequest.ForIdol` → `OwnedSelectionCondition.ForIdol`、`ForLimitBreakMaterial(10, "01A")` も同じ。`SelectRowAsync_BrowseSupport_PushesDetail` の `Args[0]` は `SupportCardUniqueId == "01D"` |
| `SupportDetailDtoFactoryTests.cs` | `Create_Default_MatchesBodyStrings` | 本文 5 章の「詳細の既定の表示」の 5 つと同じ文字列（`InfoLines` を `|` でつなぐなど）。`LevelSteps[0].TargetLevel == 4`・`ConfirmText == "Lv 3 → Lv 4 に上げます。\n強化pt 100・ゴールド 50 を使います。"`、`HasMaterial == true`、`SupportCardId == 10` |
| | `Create_CardGone_NotExists` | `"01Z"` → `Exists == false`、本文 5 章の `Refresh_CardGone_DisablesAll` の文字列 |
| | `Create_NoMaterial_HasMaterialFalse` | `"01B"`（CardId 11 は 1 枚）→ `HasMaterial == false` |
| | `CreateLimitBreakConfirmText_Formats`・`CreateLimitBreakConfirmText_MaterialGone_Null` | `("01A", "01D")` → 本文 5 章の `LimitBreakAsync_SelectsMaterialThenConfirms` の確認の文。`("01A", "zz")` → null |
| `SupportDetailPresenterTests.cs`（本物の `SupportDetailDtoFactory`、`FakeSupportUseCase(log)`・`FakeDialogOpener(log)`・`FakeScreenNavigator(log)`） | 本文 5 章の `SupportDetailPresenterTests` の 13 本 | 本文のまま。読み替え: スナップショットの差し替えは `supportCardRepository.Upsert(new SupportCardEntity("01A", 10, 40, 0))`・`OwnedFixtures.SetPlayer(players, supportPoints: 0)` など。`LevelUpResult` に `PortResult…Success(…)` → `supportUseCase.LevelUpResults.Enqueue(() => UniTask.FromResult(LevelUpResult.Success(4, 900, 950)))`（そのとき `supportCardRepository.Upsert(new SupportCardEntity("01A", 10, 4, 0))` してから返すと player_diff の代わりになる）。`Calls` は `FakeSupportUseCase.Calls`。`LevelUpAsync_NotEnoughPoints_ShowsInfo` は `Rejected(NotEnoughPoints)`。**記録の決まり**: `FakeSupportUseCase` は同じ `log` で作るので、`supportUseCase.LevelUpAsync`・`LimitBreakAsync` を呼んだ時点で `"LevelUp:01A,4"`・`"LimitBreak:01A,01D"` が `log` に入る。`InitializeAsync` の表示そのものを見るテスト（`InitializeAsync_*`・`Refresh_CardGone_DisablesAll`）以外は、`InitializeAsync` のあと `log.Entries.Clear()` してから操作する。期待の並びは次のとおり（全体）: `LevelUpAsync_Confirmed_ExecutesAndRefreshes` は `"Confirm:Lv 3 → Lv 4 に上げます。\n強化pt 100・ゴールド 50 を使います。"`, `"LevelUp:01A,4"`, そのあと `SetInfo`・`SetAbilities`・`SetResources`・`SetLevelButtons`・`SetLimitBreak` の 5 つ（値は `Upsert` した Lv 4 のもの。前の 2 つと件数 7 だけ確かめ、5 つは先頭の語 `SetInfo:` などで見る）。`LimitBreakAsync_SelectsMaterialThenConfirms` は `"PushPage:OwnedPresenter"`, `"WaitForPop"`, `"Confirm:…"`（本文の文）, `"LimitBreak:01A,01D"` で始まり、そのあと再表示の 5 つ。`LevelUpAsync_NotEnoughPoints_ShowsInfo` は `"Confirm:…"`, `"LevelUp:01A,4"`, `"Info:強化ptが足りません。"`, 再表示の 5 つ。`LimitBreakAsync_NoMaterial_ShowsInfo` は `SetOwned` のあと `supportCardRepository.Remove("01D")` してから `InitializeAsync`（CardId 10 は 01A だけ）。`LimitBreakAsync_SelectsMaterialThenConfirms` の `Args[0]` は `OwnedArgs` で `Selection.Kind == LimitBreakMaterial`・`MaterialCardId == 10`・`MaterialTargetUid == "01A"`、`LimitBreakResults` に `Success(1, 45)` |
| `HomePresenterTests.cs`（変更） | 組み立て | `CreatePresenter` の `new FakeScreenNavigator(log)` をフィールド `navigator` にする（`Args` を見るため） |
| | `OwnedClicked_ShowsNotReady` → `OwnedClicked_PushesOwnedBrowse` | `RaiseOwnedClicked()` → 初期化の後の記録が `"PushPage:OwnedPresenter"` だけ |
| | `OpenOwnedAsync_PushesOwnedBrowse`（足す） | `"PushPage:OwnedPresenter"`、`navigator.Args[0]` が `OwnedArgs` で `Selection == null` |

- `[UnityTest]` + `UniTask.ToCoroutine` は本文・gacha と同じ。`FormationValidatorTests` は xUnit

### O8. タスクと触るファイル（本文 2 章の表・4 章の「触る」を置き換える）

パスは `client/uta/Assets/` からの相対（`shared/`・`uta_docs/` で始まるものはリポジトリのルートから）。新しい `.cs` には、再コンパイルで生成された `.meta` をコミットする。Prefab・Addressables の設定・`server/` は触らない。

| タスク | 操作 | パス |
|---|---|---|
| 1 | 変更 | `shared/Uta.Shared.Domain/Produce/FormationValidator.cs`、`shared/Uta.Shared.Domain.Tests/Produce/FormationValidatorTests.cs` |
| 1 | 作成 | `Scripts/Presentation/Common/DisplayText.cs`、`Scripts/Application/Owned/OwnedSortKind.cs`・`OwnedDisabledKind.cs`・`OwnedSelectionKind.cs`・`OwnedListItem.cs`・`OwnedSelection.cs`・`OwnedSelectionCondition.cs`・`OwnedListFactory.cs`、`Tests/EditMode/Support/OwnedFixtures.cs`、`Tests/EditMode/DisplayTextTests.cs`・`OwnedSelectionConditionTests.cs`・`OwnedListFactoryTests.cs` |
| 2 | 作成 | `Scripts/Application/Support/SupportResultKind.cs`・`LevelUpStepKind.cs`・`LevelUpResult.cs`・`LimitBreakResult.cs`・`ISupportUseCase.cs`・`LevelUpCost.cs`・`SupportLevelPlanner.cs`、`Scripts/Infrastructure/Support/SupportMapper.cs`・`SupportUseCase.cs`、`Tests/EditMode/Fakes/FakeSupportService.cs`・`FakeSupportUseCase.cs`、`Tests/EditMode/SupportLevelPlannerTests.cs`・`SupportMapperTests.cs`・`SupportUseCaseTests.cs` |
| 2 | 変更 | `Scripts/Infrastructure/Api/ApiClient.cs`・`MagicOnionClientInitializer.cs`、`Scripts/Bootstrap/BootstrapEntry.cs`（`ISupportService` の行と `SupportLevelPlanner`・`OwnedListFactory`・`SupportUseCase` の 3 行） |
| 3a | 作成 | `Scripts/Presentation/Owned/OwnedTabKind.cs`・`OwnedArgs.cs`・`OwnedRowViewDto.cs`・`OwnedViewDto.cs`・`OwnedDtoFactory.cs`・`SupportDetailArgs.cs`・`SupportLevelStepViewDto.cs`・`SupportDetailViewDto.cs`・`SupportDetailDtoFactory.cs`、`Tests/EditMode/OwnedDtoFactoryTests.cs`・`SupportDetailDtoFactoryTests.cs` |
| 3a | 変更 | `Scripts/Bootstrap/BootstrapEntry.cs`（DtoFactory の 2 行） |
| 3b | 作成 | `Scripts/Presentation/Owned/IOwnedView.cs`・`OwnedPresenter.cs`・`ISupportDetailView.cs`・`SupportDetailPresenter.cs`（仮）、`Tests/EditMode/OwnedPresenterTests.cs` |
| 3b | 変更 | `Scripts/Presentation/Owned/OwnedPage.cs`・`SupportDetailPage.cs`、`Scripts/Presentation/Home/HomePresenter.cs`、`Tests/EditMode/HomePresenterTests.cs` |
| 4 | 変更 | `Scripts/Presentation/Owned/SupportDetailPresenter.cs`（仮を置き換える） |
| 4 | 作成 | `Tests/EditMode/SupportDetailPresenterTests.cs` |
| 5 | なし | Play の確認（進行役） |
| 6 | 変更 | wiki（進行役。下の「タスク 6」） |

- **タスク 1**: 本文タスク 1 の手順 1 のまま（shared/ を .NET で確かめる。`dotnet test --project server/Uta.Server.Tests` は Docker が無ければ結合テストを確かめていないと報告）。前提の確認は、gacha の型（`FakeGachaService`・`GachaFixtures`・`TestMasterData`）と、`OwnedPage`・`SupportDetailPage` のフィールドが本文 3.6・3.7 の一覧と同じこと。`DatabaseBuilder.Append` での `SupportCard`・`SupportAbility`・`SupportLevelCap`・`SupportLevelCost` の形はコンパイルで確かめ、違えば止まって報告する
- **タスク 2**: `unity recompile` のあと、`ISupportService` の生成コードでエラーが出ていないこと
- **タスク 3a**（Dto と DtoFactory）: O5 の Dto・Args・`OwnedTabKind`・`OwnedDtoFactory`・`SupportDetailDtoFactory` を完成形で作り、O6 の DtoFactory の 2 行を足す。テストは `OwnedDtoFactoryTests`・`SupportDetailDtoFactoryTests`
- **タスク 3b**（所持一覧の画面とホーム）。本文タスク 3 の手順は読まず、次のとおり行う:
  1. O5 の `IOwnedView`（本文 3.6 の形で型だけ読み替え）と `ISupportDetailView`（本文 3.7 のまま）を作る
  2. `OwnedPage`・`SupportDetailPage` に本文 3.6・3.7 の「…の実装」を足す（型は O5 の名前。`[SerializeField]` のフィールドは足さない・名前を変えない・消さない。足りなければ止まって報告する）
  3. `OwnedPresenter` を O5 のとおり作る。仮の `SupportDetailPresenter` は O5 のシグネチャで、コンストラクターは引数をフィールドに保存するだけ（読まないフィールドの警告が出てもよい。タスク 4 で消える）、`InitializeAsync`・`LevelUpAsync`・`LimitBreakAsync`・`BackAsync` は `UniTask.CompletedTask`、`DidPopEnter`・`Dispose` は空
  4. `HomePresenter.OpenOwnedAsync` を O5 の「ホーム」のとおり直す。`BootstrapEntry` はこのタスクでは変えない（O6 の行はタスク 2・3a で足し済み）
  5. O7 の `OwnedPresenterTests` を作り、`HomePresenterTests` を O7 のとおり直す
  確かめ方: コンパイルエラー 0、`OwnedPresenterTests`・`HomePresenterTests` と既存のテスト（`OwnedPrefabTests`・`PrefabContractTests` を含む）が通る
- **タスク 4**（詳細の Presenter）: 仮の `SupportDetailPresenter` を O5 の完成形に置き換え（`Refresh()`・`LevelUpAsync`・`LimitBreakAsync` は O5、`BackAsync`・購読・`Dispose`・イベントの受け口は本文 3.7）、O7 の `SupportDetailPresenterTests` を作る。確かめ方: `SupportDetailPresenterTests` と既存のテストが通る
- **Presentation の `using`**（O5 の型）: Presenter は `System`・`Cysharp.Threading.Tasks`・`UnityScreenNavigator`・`VContainer`（`GachaPresenter` と同じ）と `Uta.Client.Application.Owned`（`OwnedPresenter`・`SupportDetailPresenter`）・`Uta.Client.Application.Support`・`Uta.Client.Presentation.Common`（`DialogOpener`・`DialogResult`。`SupportDetailPresenter`）。`OwnedDtoFactory` は `System.Collections.Generic`・`Uta.Client.Application.Owned`・`Uta.Client.Presentation.Common`（`DisplayText`）・`Uta.Shared.Domain.Lesson`・`Uta.Shared.Domain.Produce`。`SupportDetailDtoFactory` はそれに `System.Globalization`・`System.Linq`・`ZLinq`・`Uta.Client.Application.Support`・`Uta.Client.Domain.Player`・`Uta.Client.Domain.SupportCard`（`ProduceFormulas` は `Uta.Shared.Domain.Produce`。`using Uta.MasterData;` は書かない: O1）。足りなければ足してよい（`using` の過不足は設計判断ではない）
- **タスク 5**（Play）: **実装担当に渡さず、進行役が行う**。本文タスク 5 の手順と止まる条件のまま。Play の前に `unity command eval 'UnityEditor.EditorUtility.RequestScriptReload(); return "ok";'`（foundation 9 章。ドメインのリロードを切っているため）。フレームを進めるときは Editor を前面にする（`EditorApplication.Step()` を連打しない）。強化の前後で詳細の「強化pt・ゴールド」の文字が変わること（player_diff の反映）を見る
- **タスク 6**（記録）: **進行役が `uta-implement` の 6 と一緒に行う**。本文タスク 6 の 1〜7 に次を足す: `naming.md` の「今のコードとの差」から owned の例（`SupportStatus`・`OwnedListBuilder`・`OwnedSelectionRequest`・`OwnedRowViewData` など）を消す。`api-design.md` 1.1 の `LevelUpAsync`・`LimitBreakAsync` の行に「クライアントは Infrastructure の `SupportUseCase`（`ISupportUseCase`）が呼ぶ」を足す。索引の 1 章の順 6 の行を `done` にし、「何を作るか」を `ISupportUseCase`・`SupportUseCase`・DtoFactory の形に直す。本文タスク 6 の 1 の `OwnedSelectionRequest` は `OwnedSelectionCondition`

### O9. 本文のうち有効なもの（ここに無い本文の記述は無効。O0〜O8 が正）

- 「この計画で決めたこと」1〜3・6〜11・13〜16（型名は O0 で読む）。12 の「マスタから読む」は `SupportLevelPlanner.GetMaxLimitBreak`
- 1 章の前提 2 と「決めないこと」
- 3.1（`IsPlanCompatible`）・3.2（`DisplayText`）
- 3.4 の `OwnedSelection` と、`BuildIdols`・`BuildSupportCards` の手順・並べ方の規則（`OwnedListFactory` が使う。選べるかの判定は O3）
- 3.6 の表（初期の `tab`・並び）・`Refresh()` の 3・4 の文（`OwnedDtoFactory` が使う）・操作・イベントの受け口・選択モードの呼び方・`OwnedPage` の実装
- 3.7 の `ISupportDetailView`・`Refresh()` の文・`LevelUpAsync`／`LimitBreakAsync` の確認の文と結果の文言・`BackAsync`・イベントの受け口・`SupportDetailPage` の実装
- 4 章のタスク 5 の止まる条件、各タスクの終わりの確かめ方（コンパイル 0・EditMode 全件・フォントの動的データを空に戻す・Prefab の差分を戻す）
- 5 章の冒頭（偽の View の書き方）・「共通の前提」の期待の文字列・`OwnedPresenterTests`・`SupportDetailPresenterTests` の表（O7 の読み替えのうえで）・Play・手動の確認の項目
- 無効: 2 章の表、3.3・3.5・3.8 の `BootstrapEntry` の 3 行、3.4 の `OwnedRowData`・`OwnedSelectionRequest`・`OwnedListBuilder` のシグネチャ、5 章の `ServerReasonsTests`・`SupportSnapshotEditsTests`・`LevelUpSupportUseCaseTests`・`LimitBreakSupportUseCaseTests` の行、「基盤 11d9b06 への読み替え」の `SupportGateway` に関わる行

## 基盤 11d9b06 への読み替え（2026-11-08。[クライアントの移行計画](2026-11-08-client-foundation-migration.md)。この節が本文と食い違ったら、この節が正）

- `XxxApi`（`ApiServiceBase<…>` のラッパー。`GachaApi`・`SupportApi`・`ProduceApi`）は**作らない**。`UtaApiClient` が `public IGachaService Gacha { get; }`（`ISupportService Support`・`IProduceService Produce`）を持ち、`Create` の中で `core.CreateClient<I…Service>()` で作る。本文の「`ServiceClientFactory.Create<I…Service>(callInvoker)` で作って `new XxxApi(core, client)`」は「`core.CreateClient<I…Service>()`」に読み替える。private コンストラクターの引数の型は `XxxApi` ではなく `I…Service`
- ゲートウェイのコンストラクターは `I…Service` を受ける（`UtaApiClient` を受けない）。`BootstrapEntry` に `builder.Register<I…Service>(resolver => resolver.Resolve<UtaApiClient>().Xxx, Lifetime.Singleton);` を足す（`PlayerGateway` と同じ）
- 呼び方: `xxx.XxxAsync(req, handledCodes)` → `await xxx.WithHandledCodes(codes).XxxAsync(req).ToResult()`（`handledCodes` の無い RPC は `WithHandledCodes` を付けない）。`result.IsSuccess` → `!result.IsError`。`result.Error.Code.Equals(X)` → `result.ErrorCode == X`。`result.Error.ToString()`・`result.Error.Code.Reason` は今のまま使える
- 本文のファイル表の `Api/XxxApi.cs`（作成）の行は無効（作らない）。テストの偽物は、`PlayerGatewayTests` と同じく偽の `I…Service`（`IService<T>` の 5 メンバーを実装し、`WithHeaders` は `this` を返す）
- `[MagicOnionClientGeneration]` の型の並びに `typeof(I…Service)` を足す指示は今のまま有効（`Uta.Shared.Contracts` の同じアセンブリなので、足し忘れても `IPlayerService` で足りるが、本文どおり足す）
- DTO の ID の名前（`OwnedIdolDto.IdolId` など）は、[移行計画](2026-11-08-client-foundation-migration.md)の R9 の表に読み替える（2026-11-08）
- クライアントのエンティティ・リポジトリの形（`IdolEntity.IdolId` → `MasterId`、`SupportCardEntity.SupportCardUid`・`CardId` → `UniqueId`・`MasterId`、`PlayerEntity` の `UniqueId`、コンストラクターの先頭の `uniqueId`、`Find(string uniqueId)`、名前空間 `Uta.Client.Domain.Idol`・`…SupportCard`・`Uta.Client.Infrastructure.Idol`・`…SupportCard`）は、[移行計画](2026-11-08-client-foundation-migration.md)の R8 に読み替える（2026-11-08）

## この計画で決めたこと

ユーザーは、この節と次の「ユーザーの確認が要ること」だけを読めば判断できるように書く。

1. **画面は P2 の Prefab と View の骨格の上に作る**（2026-11-07 の Prefab 化）。`OwnedPage`・`SupportDetailPage` の Prefab・子の並び・`[SerializeField]` のフィールドは P2（[2026-11-07-client-prefabs-screens-a.md](2026-11-07-client-prefabs-screens-a.md) 3.2・3.3）が作る。この計画は、その骨格に `IOwnedView`・`ISupportDetailView` の実装（フィールドを使う表示と、`Awake` でのボタンの購読）を足すだけで、C# で UI を組まない（旧版の `BuildUi`・`UiFactory`・`EnsureBuilt` と「画面のレイアウト」の節はやめた）
2. **P2 の構造に合わせた表示の決め方**: 0 件の「ありません」は一覧の外の `emptyText` を出し隠しする（一覧の中に置かない）。タブの選択は `TabBarView.SetSelected`（下線 `underline` の表示）で表す（旧: ボタンを押せなくする）。プラン・タイプのボタンは `filterRow`（下段）ごと出し隠しする。所持一覧の行は P1 の `ListRow`（`TitleText`・`SubText`・`RightText`。`RightText` は空）。レベルのボタン（+1／+5／最大）は Prefab に固定で 3 つあり、ラベルと押せるかだけを変える
3. **所持一覧・詳細は `Home` シーンの中のページ**（screen-design 8章、foundation 3.3.1）。ホームのナビの「アイドル」ボタン（P2 の `HomePage` の `Nav/NavOwnedButton`）→ `HomePresenter.OpenOwnedAsync` → `OwnedPresenter` を積む → 行のタップで `SupportDetailPresenter` を積む → 上限解放の素材は `OwnedPresenter`（選択モード）をもう 1 枚積む。どれも `Home` シーンの `IScreenNavigator` で積み、「＜」で `PopPageAsync(this)`。シーンはまたがない
4. **ユースケース・ゲートウェイの登録は `BootstrapEntry.RegisterUseCases`（ルートのスコープ）に 3 行足すだけ**。画面の登録（旧 `UiRoot.RegisterPage`）は無い（Prefab は P2 が Addressables の `Screens` に登録済み。Presenter は基盤が Push のたびに作る）
5. `ServerReasons`（2 本目）に、この計画が使う reason 6 つ（`SUPPORT_*` 5 つと `PLAYER_NOT_ENOUGH_GOLD`）を足す（2 本目の「ほかの reason は、それを使う計画が足す」のとおり）
6. **[Uta案] 育成アイドルの一覧は `IdolId` の昇順に固定**し、並び替え・絞り込みを持たない（MVP の育成アイドルは 4 人で、並べ替える意味が小さいため）。叩き台 [screen-wireframes.md](../wiki/overview/screen-wireframes.md) 10 の「一覧の並び: レア度・Lv・入手順。絞り込み: プラン」は、MVP では**サポートカードだけ**に持たせる（叩き台との差。タスク 6 で screen-wireframes.md 10 に注記する）（決めたこと 14）
7. サポートカードの「入手順」は所持ID（ULID）の昇順（ULID は発行時刻の順。`string.CompareOrdinal` で比べる。サーバーの返す並びに頼らない）
8. 上限解放の素材は、仕様（idol-support.md 2.2.1）どおり**プレイヤーが選ぶ**。選び方は所持一覧の選択モード（素材の選択）を使う
9. レベル上げ（+1／+5／最大）と上限解放は、**確認ダイアログを出してから** RPC を呼ぶ（叩き台 10「強化・上限解放…は、確認ダイアログ → サーバーで確定」）。上限解放の確認には、素材の Lv にかかわらず常に「素材は無くなります。強化に使った強化pt・ゴールドは戻りません。」を出す（決めたこと 16）
10. サポートカードの詳細と強化（レベル上げ・上限解放）は **1 ページ**（`SupportDetailPage`）にする（screen-design.md 2・3章の「詳細 ─▶ 強化」を 1 枚にまとめる。タスク 6 で 2・3章を直す）（決めたこと 15）
11. 詳細のアビリティは枠 1（`SupportAbility` の行）を、今のレベルの値で出す（`ProduceFormulas.AbilityValue`。サーバーのプロデュース開始と同じ計算）
12. 上限解放の最大回数はマスタ（`IGameMaster.GetSupportMaxLimitBreak`）から読む。サーバーは定数 `SupportRules.MaxLimitBreak = 4` を使うので、マスタの `SupportLevelCap` に各レアの `LimitBreak = 4` の行があることが前提（今のマスタはそうなっている。食い違っても、超えた分はサーバーが `SUPPORT_MAX_LIMIT_BREAK` で拒否する）
13. プランの適合の規則は Shared.Domain の `FormationValidator.IsPlanCompatible` に 1 つ（shared/ の変更。サーバーの `Validate` も使う）
14. **育成アイドルの一覧を `IdolId` の順に固定し、並び・絞り込みをサポートカードだけにしてよいか**（決めたこと 6。叩き台 10 からの縮小）。代わりの案は、育成アイドルにもプランの絞り込みを付ける（分岐が増える）（ユーザーが推奨どおり了承した。2026-11-07）
15. **サポートカードの詳細と強化を 1 ページにしてよいか**（決めたこと 10。screen-design 2・3章の「詳細 ─▶ 強化」の 2 枚からの変更）（ユーザーが推奨どおり了承した。2026-11-07）
16. **上限解放の確認文「強化に使った強化pt・ゴールドは戻りません」を、素材の Lv にかかわらず常に出してよいか**（決めたこと 9。仕様 2.2.1 は「素材のレベルが 1 より大きいとき」）（ユーザーが推奨どおり了承した。2026-11-07）

## ユーザーの確認が要ること

なし（全件、2026-11-07 にユーザーが推奨どおり了承した。「この計画で決めたこと」の末尾に移した）。

## 1. 目的と前提

[game-spec.md](../wiki/overview/game-spec.md) の順 7 の C# の 3 本目。所持一覧（育成アイドル・サポートカード）と、サポートカードの詳細（アビリティの表示・レベル上げ・上限解放）の画面、`ISupportService` を呼ぶクライアントの層を作る。所持一覧には、**編成（4 本目）と上限解放の素材が使う「選択モード」**を持たせる。あわせて、プランの適合の規則を Shared.Domain の `FormationValidator.IsPlanCompatible` に 1 つにまとめる（shared/ の変更）。画面の叩き台は [screen-wireframes.md](../wiki/overview/screen-wireframes.md) 10、仕様は [idol-support.md](../wiki/systems/idol-support.md)（IDL-01・SUP-01・SUP-02。2.1 編成のルール、2.2.1 上限解放の操作、2.3 アビリティ）、RPC は `shared/Uta.Shared.Contracts/Support/`・[gacha-support-server.md](../wiki/architecture/server/gacha-support-server.md)。

- 方針の正は [screen-design.md](../wiki/architecture/screen-design.md) 7章（Prefab・Addressables・TextMeshPro）と 8章（シーン構成）。画面の作り方（Presenter・View・Args・遷移・実行中フラグ・破棄の印・テストの書き方）は **[foundation](2026-11-03-client-screens-foundation.md) 3.3 の共通ルールに従う**（この計画では繰り返さない）。C# の公開 API の正は foundation の 3章（一覧は 3.0）。基盤の `IScreenNavigator`（`external/UnityScreenNavigator`）をそのまま使う
- **PR #28 の読み替え**: PR #28（未マージ。PR #28 が足す ADR）は `external/` のサブモジュールを全部消す（UnityScreenNavigator・Supplement・VolTech-MagicOnion・MasterMemoryAddons）。サーバー・shared は `packages/` の `.nupkg`、Unity は `manifest.json` の git URL で参照し、`Uta.slnx` から共通基盤が外れる。**PR #28 のあとは、共通基盤のソース（USN の `Runtime/ChinpanGX`・README・`AddressableAssetLoader`、VolTech-MagicOnion のコードなど）は `client/uta/Library/PackageCache/<パッケージ名>@<ハッシュ>/`（USN は `com.harumak.unityscreennavigator`。Unity でパッケージを解決した環境にだけある）か GitHub のリポジトリを読む。マージ前は `external/<名前>`**。この計画に書いた `external/` のパスは、そう読み替える（`external/` は直さない、の決まりは変わらない）
- **Prefab と View のフィールドの正は P2**（[2026-11-07-client-prefabs-screens-a.md](2026-11-07-client-prefabs-screens-a.md) 3.1 の表・3.2 の `OwnedPage`／`SupportDetailPage`・3.3 の対応表）。この計画はフィールドを足さない・名前を変えない。足りなければ止まって報告する（P2 を直す）
- 関係する ADR: [0008](../wiki/decisions/0008-shared-domain-and-onion.md)（層。判定は Shared.Domain の関数を両側で使う）、[0009](../wiki/decisions/0009-client-stack.md)（uGUI + UnityScreenNavigator・VContainer・UniTask）
- 計画の並び（[索引](2026-11-07-client-screens-index.md)）: 計画 0 → P1（[prefabs-common](2026-11-07-client-prefabs-common.md)）→ P2 → foundation → gacha → **この計画** → P3 → formation・week・lesson
- 使う前の計画の型:
  - P1: `PageBase`、部品の View（`ButtonView`・`HeaderView`・`TabBarView`・`TabView`・`ListRowBase`・`ListRowView`）、部品の Prefab `ListRow`
  - P2: `OwnedPage`・`SupportDetailPage` の骨格（フィールドだけ）と Prefab（Addressables のアドレス `OwnedPage`・`SupportDetailPage`）
  - foundation: 部品の View に足したメソッド（3.2。`ButtonView.Button`・`SetLabel`・`SetInteractable`、`HeaderView.BackButton`・`SetTitle`、`TabBarView.TabClicked`・`SetSelected`、`ListRowBase.Button`・`SetInteractable`、`ListRowView.SetTexts`）、`UiObjects.ClearChildren`・`AddRow<T>`、`DialogService`・`DialogResult`、`IGameMaster`（`GetSupportAbilities` を含む）、`RefreshPlayerUseCase`、`PlayerCache`、`HomePresenter`、`BootstrapEntry.RegisterUseCases`、`UtaErrors`（`SupportErrors` 5 つと `PlayerErrors.NotEnoughGold` が `Stay` で登録済み）、テスト用の `CallLog`・`FakeScreenNavigator`・`FakeDialogService`・`StubPlayerPort`
  - gacha（2 本目）: `PortResult<T>`・`ServerReasons`・テスト用の `FakeGameMaster`（`Tests/EditMode/Fakes/`。`Abilities` の口を持つ）、`HomePresenter` に足された `IScreenNavigator`
- **前提 1**: P1・P2・foundation・gacha が done。タスク 1 の前に、`Presentation/Owned/OwnedPage.cs`・`SupportDetailPage.cs` のフィールドが P2 の 3.1 の表と同じこと、`HomePresenter` のコンストラクターに `IScreenNavigator` があることを確かめ、違えば止まって報告する
- **前提 2**: Unity 7（`7000.0.0a12`）の Editor が、この worktree の `client/uta` を開いていること（`unity status --until-ready`）。**Editor を開けない環境では、コンパイル・EditMode のテスト・Play のどれも確かめられない**。そのときは「確認できていない」と書いてユーザーに実行を頼む（推測で「通った」と書かない）。shared/ の変更（タスク 1 の 1）は .NET SDK で確かめられる
- **決めないこと / 作らないもの**: 育成アイドルの詳細・強化（IDL-02。MVP の画面に入れない）、アビリティの枠 2〜6（MVP は枠 1 だけ。[idol-support.md](../wiki/systems/idol-support.md) 2.3）、スキルカードサポート・イベント（MVP 外）。編成の画面での開始時の補正の表示は 4 本目が決める。Prefab の階層・見た目の値（P2）

## 2. 触るファイル

パスは、`client/uta/Assets/` で始まらないものはリポジトリのルートからの相対、それ以外は `client/uta/Assets/` からの相対。新しい `.cs` の `.meta` は Unity が作ったものをコミットする。Prefab（`.prefab`）と Addressables の設定は触らない（P2 の生成物）。

| 操作 | パス | 中身 |
|---|---|---|
| 変更 | `shared/Uta.Shared.Domain/Produce/FormationValidator.cs` | `IsPlanCompatible` を足し、`Validate` がそれを呼ぶ（3.1） |
| 変更 | `shared/Uta.Shared.Domain.Tests/Produce/FormationValidatorTests.cs` | `IsPlanCompatible` のテストを足す（5章） |
| 作成 | `Scripts/Presentation/Common/DisplayText.cs` | レア度・プラン・タイプ・パラメータ・アビリティの表示名（3.2） |
| 変更 | `Scripts/Application/Common/ServerReasons.cs` | reason を 6 つ足す（3.3） |
| 作成 | `Scripts/Application/Support/ISupportPort.cs`・`SupportLevelUpResult.cs`・`SupportLimitBreakResult.cs` | 通信ポートと、ポートが返す型（3.3） |
| 作成 | `Scripts/Application/Support/SupportStatus.cs`・`SupportOutcome.cs`・`LevelUpStep.cs` | ユースケースの結果の型（3.3） |
| 作成 | `Scripts/Application/Support/SupportLevelPlanner.cs` | 目標レベルの決め方（3.3） |
| 作成 | `Scripts/Application/Support/SupportSnapshotEdits.cs` | 取り直しに失敗したときに応答でキャッシュを直す（3.3） |
| 作成 | `Scripts/Application/Support/LevelUpSupportUseCase.cs`・`LimitBreakSupportUseCase.cs` | ユースケース（3.3） |
| 作成 | `Scripts/Application/Owned/OwnedSortKey.cs`・`OwnedDisabledReason.cs`・`OwnedSelectionKind.cs`・`OwnedRowData.cs`・`OwnedSelection.cs`・`OwnedSelectionRequest.cs`・`OwnedListBuilder.cs` | 一覧の組み立て・選択モード（3.4。1 ファイル 1 型） |
| 作成 | `Scripts/Infrastructure/Api/SupportApi.cs` | `ApiServiceBase<ISupportService>`（3.5） |
| 変更 | `Scripts/Infrastructure/Api/UtaApiClient.cs` | `Support`（`SupportApi`）を足す（3.5） |
| 変更 | `Scripts/Infrastructure/Api/UtaMagicOnionClientInitializer.cs` | `typeof(ISupportService)` を足す（3.5） |
| 作成 | `Scripts/Infrastructure/Support/SupportGateway.cs` | `ISupportPort` の実装（3.5） |
| 作成 | `Scripts/Presentation/Owned/OwnedTab.cs`・`OwnedArgs.cs`・`OwnedRowViewData.cs`・`IOwnedView.cs`・`OwnedPresenter.cs` | 所持一覧（3.6） |
| 変更 | `Scripts/Presentation/Owned/OwnedPage.cs` | P2 の骨格に `IOwnedView` の実装を足す（3.6。フィールドは変えない） |
| 作成 | `Scripts/Presentation/Owned/SupportDetailArgs.cs`・`ISupportDetailView.cs`・`SupportDetailPresenter.cs` | サポートカードの詳細（3.7） |
| 変更 | `Scripts/Presentation/Owned/SupportDetailPage.cs` | P2 の骨格に `ISupportDetailView` の実装を足す（3.7。フィールドは変えない） |
| 変更 | `Scripts/Presentation/Home/HomePresenter.cs` | `OpenOwnedAsync` の中身を、所持一覧を積む処理に置き換える（3.8） |
| 変更 | `Scripts/Bootstrap/BootstrapEntry.cs` | `RegisterUseCases` に 3 行足す（3.8） |
| 変更 | `Tests/EditMode/ServerReasonsTests.cs` | 足した reason の行を足す（5章） |
| 変更 | `Tests/EditMode/HomePresenterTests.cs` | 「アイドル」のボタンのテストを差し替え、1 本足す（5章） |
| 作成 | `Tests/EditMode/*.cs`（5章） | テスト |
| 変更 | `uta_docs/wiki/architecture/screen-design.md`・`architecture/produce-domain.md`・`overview/screen-wireframes.md`・`systems/idol-support.md`・`architecture/unity7-evaluation.md` | 結果の記録（タスク 6） |

`Tests/EditMode/Fakes/FakeGameMaster.cs`（2 本目）は変えない（アビリティは 2 本目の `Abilities` の口に入れる）。asmdef は変えない（foundation 3.1 と P1 で足りている）。

## 3. 型とシグネチャ

C# は、`shared/`・`client/` とも C# 9 の範囲（1 本目 3章の冒頭のとおり。コレクション式 `[...]` は使わず `new[] { ... }`）。名前空間はフォルダに合わせる: `Scripts/Application/Support` → `Uta.Client.Application.Support`、`Application/Owned` → `Uta.Client.Application.Owned`、`Infrastructure/Api` → `Uta.Client.Infrastructure.Api`、`Infrastructure/Support` → `Uta.Client.Infrastructure.Support`、`Presentation/Owned` → `Uta.Client.Presentation.Owned`、`Presentation/Common` → `Uta.Client.Presentation.Common`。数値の 3 桁区切りは 1 本目 3.3.6 のとおり `ToString("N0", CultureInfo.InvariantCulture)`（以下 `N0(x)` と書く）。

### 3.0 後続の計画が使う公開 API（一覧）

| 型・メンバー | 使う計画 | 節 |
|---|---|---|
| `FormationValidator.IsPlanCompatible(Plan idolPlan, Plan supportPlan)`（shared/） | formation（編成の表示）、サーバー | 3.1 |
| `DisplayText.RarityName`・`PlanName`・`SupportTypeName`・`ParamName`・`Ability` | formation・week・lesson | 3.2 |
| `ServerReasons` の `Support*`・`PlayerNotEnoughGold` | — | 3.3 |
| `OwnedArgs`・`OwnedSelectionRequest.ForIdol`／`ForSupportCard`／`ForLimitBreakMaterial`・`OwnedSelection`・`OwnedPresenter`（選択モード。`PushPageAsync<OwnedPresenter, OwnedArgs>` → `WaitForPopAsync<OwnedSelection>`） | formation | 3.4・3.6 |

### 3.1 `Uta.Shared.Domain.Produce.FormationValidator`（shared/）

```csharp
public static class FormationValidator
{
    /// <summary>
    /// サポートカードのプランが育成アイドルのプランに合うか（idol-support.md 2.1 の 6）。
    /// supportPlan が Plan.Free か、idolPlan と同じなら true。それ以外（Plan.Nothing を含む）は false。
    /// クライアントの所持一覧・編成の表示と、Validate の両方がこれを使う（規則を 1 か所に置く）。
    /// </summary>
    public static bool IsPlanCompatible(Plan idolPlan, Plan supportPlan)
    {
        return supportPlan == Plan.Free || supportPlan == idolPlan;
    }

    // Validate: 2 つ目のループの条件 `plan != Plan.Free && plan != idol.Plan` を `!IsPlanCompatible(idol.Plan, plan)` に置き換える。ほかは変えない
    public static FormationCheck Validate(ProduceCatalog catalog, int idolId, IReadOnlyList<int> supportCardIds);
}
```

### 3.2 `Uta.Client.Presentation.Common.DisplayText`

```csharp
using Uta.Shared.Domain.Lesson;    // Rarity・Plan・ParamKind（マスタの生成の名前空間は using しない: 同名の enum があるため）
using Uta.Shared.Domain.Produce;   // SupportType・SupportAbilityKind

public static class DisplayText
{
    public static string RarityName(Rarity rarity);            // N → "N"、R → "R"、SR → "SR"、SSR → "SSR"、Nothing → "-"
    public static string PlanName(Plan plan);                  // Free → "共通"、Sense → "感性"、Logic → "理論"、Nothing → "-"
    public static string SupportTypeName(SupportType type);    // Singing → "歌唱"、Rhythm → "律動"、Expression → "表現"、Assist → "アシスト"、Nothing → "-"
    public static string ParamName(ParamKind param);           // Singing → "歌唱"、Rhythm → "律動"、Expression → "表現"、Nothing → "-"
    /// <summary>
    /// アビリティ（枠 1）の 1 行の文字。value は今のレベルの値。
    /// InitialParam → "初期" + ParamName(param) + " +" + value（例 "初期歌唱 +49"）。
    /// ParamBonus（千分率）→ ParamName(param) + "ボーナス +" + (value / 10) + "." + (value % 10) + "%"（例 85 → "歌唱ボーナス +8.5%"。float を使わない）。
    /// MaxHp → "最大体力 +" + value。Nothing → "-"。
    /// </summary>
    public static string Ability(SupportAbilityKind kind, ParamKind param, int value);
}
```

- **メソッド名を型と同じにしない**（`RarityName`・`PlanName`・`SupportTypeName`・`ParamName`。week の `CardTypeName`、lesson の `LessonKindName` と同じ規則）。型と同じ名前のメソッドにすると、クラスの中の単純名 `Plan` がメソッドに束縛され、`Plan.Free` がコンパイルエラーになるため。呼び出し側は `DisplayText.PlanName(x)` と書く（4〜6 本目の計画もこの名前で使う）
- `Plan` の `Free` を「共通」にするのは用語集（glossary.md の「プラン」）・idol-support.md 2.1 に合わせるため
- 範囲外の値（enum に無い数値）は `"-"`

### 3.3 `Uta.Client.Application.Support`

まず 2 本目の `Uta.Client.Application.Common.ServerReasons` に次の 6 つを足す（値は `Uta.Shared.Contracts` の `ErrorCode.Reason` と同じ文字列。食い違いは `ServerReasonsTests` で確かめる）:

```csharp
public const string SupportInvalidTargetLevel = "SUPPORT_INVALID_TARGET_LEVEL";   // SupportErrors.InvalidTargetLevel
public const string SupportNotEnoughPoints = "SUPPORT_NOT_ENOUGH_POINTS";         // SupportErrors.NotEnoughPoints
public const string SupportMaxLimitBreak = "SUPPORT_MAX_LIMIT_BREAK";             // SupportErrors.MaxLimitBreak
public const string SupportNotOwned = "SUPPORT_NOT_OWNED";                        // SupportErrors.NotOwned
public const string SupportInvalidMaterial = "SUPPORT_INVALID_MATERIAL";          // SupportErrors.InvalidMaterial
public const string PlayerNotEnoughGold = "PLAYER_NOT_ENOUGH_GOLD";               // PlayerErrors.NotEnoughGold
```

```csharp
public sealed class SupportLevelUpResult
{
    public SupportLevelUpResult(int level, int supportPoints, int gold);
    public int Level { get; } public int SupportPoints { get; } public int Gold { get; }   // LevelUpResponse の写し（上げた後のレベル・残りの強化pt・残りのゴールド）
}
public sealed class SupportLimitBreakResult
{
    public SupportLimitBreakResult(int limitBreak, int maxLevel);
    public int LimitBreak { get; } public int MaxLevel { get; }   // LimitBreakResponse の写し
}

public interface ISupportPort
{
    UniTask<PortResult<SupportLevelUpResult>> LevelUpAsync(string supportCardUid, int targetLevel);
    UniTask<PortResult<SupportLimitBreakResult>> LimitBreakAsync(string supportCardUid, string materialUid);
}

public enum SupportStatus
{
    Succeeded,
    NotEnoughPoints,    // ServerReasons.SupportNotEnoughPoints
    NotEnoughGold,      // ServerReasons.PlayerNotEnoughGold
    InvalidTarget,      // ServerReasons.SupportInvalidTargetLevel
    MaxLimitBreak,      // ServerReasons.SupportMaxLimitBreak
    InvalidMaterial,    // ServerReasons.SupportInvalidMaterial
    NotOwned,           // ServerReasons.SupportNotOwned
    Failed              // それ以外の業務エラーと、業務エラーでない失敗（ApiCore が共通のダイアログを出し終えている）
}

public sealed class SupportOutcome
{
    public SupportStatus Status { get; }
    /// <summary>RPC は成功したが、そのあとの取り直し（RefreshPlayerUseCase）が失敗した。キャッシュは応答の値で直してある（SupportSnapshotEdits）。Succeeded 以外では false。</summary>
    public bool RefreshFailed { get; }
    public static SupportOutcome Success(bool refreshFailed);
    public static SupportOutcome Rejected(SupportStatus status);   // status が Succeeded なら ArgumentException
}

public enum LevelUpStep { Plus1, Plus5, Max }

public static class SupportLevelPlanner
{
    /// <summary>
    /// 押した時点の目標レベル。maxLevel = master.GetSupportMaxLevel(rarity, card.LimitBreak)。
    /// card.Level >= maxLevel なら card.Level（変化なし）。
    /// Plus1 は Math.Min(card.Level + 1, maxLevel)、Plus5 は Math.Min(card.Level + 5, maxLevel)（所持が足りなくても返す。足りなければサーバーが NotEnoughPoints／NotEnoughGold）。
    /// Max は t を maxLevel から card.Level + 1 まで 1 ずつ下げ、cost = master.GetSupportLevelUpCost(rarity, card.Level, t) が
    /// cost.SupportPoints <= supportPoints かつ cost.Gold <= gold になる最初の t。無ければ card.Level。
    /// </summary>
    public static int PlanTarget(OwnedSupportCard card, Rarity rarity, int supportPoints, int gold, LevelUpStep step, IGameMaster master);
}

public static class SupportSnapshotEdits
{
    /// <summary>
    /// snapshot の写しを返す。Gold = result.Gold、SupportPoints = result.SupportPoints、SupportCards のうち SupportCardUid == supportCardUid の 1 枚の Level = result.Level（並びは変えない）。
    /// ほかの値はそのまま。該当の所持が無ければ、カードの列はそのまま（Gold・SupportPoints だけ変える）。
    /// </summary>
    public static PlayerSnapshot WithLevelUp(PlayerSnapshot snapshot, string supportCardUid, SupportLevelUpResult result);
    /// <summary>
    /// snapshot の写しを返す。SupportCardUid == supportCardUid の 1 枚の LimitBreak = result.LimitBreak、SupportCardUid == materialUid の 1 枚を取り除く（ほかの並びは変えない）。ほかの値はそのまま。
    /// </summary>
    public static PlayerSnapshot WithLimitBreak(PlayerSnapshot snapshot, string supportCardUid, string materialUid, SupportLimitBreakResult result);
}

public sealed class LevelUpSupportUseCase
{
    public LevelUpSupportUseCase(ISupportPort port, RefreshPlayerUseCase refresh, PlayerCache cache);
    public UniTask<SupportOutcome> ExecuteAsync(OwnedSupportCard card, int targetLevel);
}

public sealed class LimitBreakSupportUseCase
{
    public LimitBreakSupportUseCase(ISupportPort port, RefreshPlayerUseCase refresh, PlayerCache cache);
    public UniTask<SupportOutcome> ExecuteAsync(OwnedSupportCard card, OwnedSupportCard material);
}
```

`LevelUpSupportUseCase.ExecuteAsync` の手順:
1. `targetLevel <= card.Level` なら `ArgumentOutOfRangeException(nameof(targetLevel))`（通信しない）
2. `var r = await port.LevelUpAsync(card.SupportCardUid, targetLevel);`
3. 失敗: `r.IsBusinessError` で `r.Reason` が `ServerReasons.SupportNotEnoughPoints` → `Rejected(NotEnoughPoints)`、`PlayerNotEnoughGold` → `NotEnoughGold`、`SupportInvalidTargetLevel` → `InvalidTarget`、`SupportNotOwned` → `NotOwned`。それ以外の reason と、業務エラーでない失敗は `Rejected(Failed)`
4. 成功: `try { await refresh.ExecuteAsync(); return SupportOutcome.Success(false); } catch (Exception) { cache.Set(SupportSnapshotEdits.WithLevelUp(cache.Current, card.SupportCardUid, r.Value)); return SupportOutcome.Success(true); }`（1 本目 3.3.5「RPC 成功後の取り直しの失敗は成功として扱う」。追加のダイアログは出さない。取り直しの通信の失敗は ApiCore が出し終えている）

`LimitBreakSupportUseCase.ExecuteAsync` の手順:
1. `material.SupportCardUid == card.SupportCardUid` なら `ArgumentException(nameof(material))`（通信しない）。**`CardId` が違うかはここで確かめない**（選択モードが同じ `CardId` だけを出す。最後はサーバーが `SUPPORT_INVALID_MATERIAL` で拒否する）
2. `var r = await port.LimitBreakAsync(card.SupportCardUid, material.SupportCardUid);`
3. 失敗: `SupportMaxLimitBreak` → `MaxLimitBreak`、`SupportInvalidMaterial` → `InvalidMaterial`、`SupportNotOwned` → `NotOwned`。それ以外は `Failed`
4. 成功: レベル上げと同じ。取り直しが失敗したら `cache.Set(SupportSnapshotEdits.WithLimitBreak(cache.Current, card.SupportCardUid, material.SupportCardUid, r.Value))` して `Success(true)`

### 3.4 `Uta.Client.Application.Owned`

```csharp
public enum OwnedSortKey { Rarity, Level, Acquired }     // サポートカードの並び。育成アイドルは使わない
public enum OwnedDisabledReason { None, AlreadyInFormation, SameCardInFormation, PlanMismatch }
public enum OwnedSelectionKind { Idol, SupportCard, LimitBreakMaterial }

public sealed class OwnedRowData   // 表示の文字列は持たない（Application は Presentation の DisplayText を参照できない。文字列は OwnedPresenter が作る）
{
    public OwnedRowData(bool isIdol, int idolId, string supportCardUid, int cardId, string name, Rarity rarity, Plan plan,
        SupportType supportType, int level, int limitBreak, int shards, bool enabled, OwnedDisabledReason disabledReason);
    public bool IsIdol { get; }
    public int IdolId { get; }                 // 育成アイドル: IdolId。サポートカード: 0
    public string SupportCardUid { get; }      // サポートカード: 所持ID。育成アイドル: null
    public int CardId { get; }                 // サポートカード: OwnedSupportCard.CardId（= SupportCardInfo.SupportCardId）。育成アイドル: 0
    public string Name { get; } public Rarity Rarity { get; } public Plan Plan { get; }
    public SupportType SupportType { get; }    // 育成アイドルは SupportType.Nothing
    public int Level { get; } public int LimitBreak { get; }   // 育成アイドルは 0
    public int Shards { get; }                 // 育成アイドルの OwnedIdol.Shards。サポートカードは 0
    public bool Enabled { get; }               // 選択モードで選べるか（閲覧モードは常に true）
    public OwnedDisabledReason DisabledReason { get; }   // Enabled なら None
}

public sealed class OwnedSelection
{
    public static OwnedSelection Cancelled { get; }                       // 1 つのインスタンスを使い回す
    public static OwnedSelection ForIdol(int idolId);
    public static OwnedSelection ForSupportCard(string supportCardUid);   // 素材の選択もこれ
    public bool IsCancelled { get; }
    public int IdolId { get; }              // ForIdol 以外は 0
    public string SupportCardUid { get; }   // ForSupportCard 以外は null
}

public sealed class OwnedSelectionRequest   // 選択モードの要求（値だけ。完了の仕組みは持たない。結果は基盤の WaitForPopAsync で返す: 3.6）
{
    /// <summary>育成アイドルを選ばせる。excludedIdolIds は選べない（AlreadyInFormation）。null は空。</summary>
    public static OwnedSelectionRequest ForIdol(IReadOnlyCollection<int> excludedIdolIds = null);
    /// <summary>
    /// 編成の枠に入れるサポートカードを選ばせる。excludedUids（ほかの枠の所持ID）、excludedSupportCardIds（ほかの枠のカード ID）、
    /// idolPlan に合わないカード（!FormationValidator.IsPlanCompatible(idolPlan, カードの Plan)）は選べない。null は空。
    /// </summary>
    public static OwnedSelectionRequest ForSupportCard(Plan idolPlan, IReadOnlyCollection<string> excludedUids, IReadOnlyCollection<int> excludedSupportCardIds);
    /// <summary>上限解放の素材を選ばせる。CardId == supportCardId で SupportCardUid != targetUid の所持だけを並べる（全部選べる）。targetUid が null・空なら ArgumentException。</summary>
    public static OwnedSelectionRequest ForLimitBreakMaterial(int supportCardId, string targetUid);

    public OwnedSelectionKind Kind { get; }
    public IReadOnlyCollection<int> ExcludedIdolIds { get; }          // Idol 以外は空
    public Plan IdolPlan { get; }                                     // SupportCard 以外は Plan.Nothing
    public IReadOnlyCollection<string> ExcludedUids { get; }          // SupportCard 以外は空
    public IReadOnlyCollection<int> ExcludedSupportCardIds { get; }   // SupportCard 以外は空
    public int MaterialCardId { get; }                                // LimitBreakMaterial 以外は 0
    public string MaterialTargetUid { get; }                          // LimitBreakMaterial 以外は null
}

public static class OwnedListBuilder
{
    /// <summary>
    /// player.Idols を IdolId の昇順に並べた行（並び替え・絞り込みは無い）。
    /// request が null なら全部 Enabled。Kind == Idol なら ExcludedIdolIds に入る行を Enabled = false・AlreadyInFormation。
    /// Kind がそれ以外なら ArgumentException。
    /// </summary>
    public static IReadOnlyList<OwnedRowData> BuildIdols(PlayerSnapshot player, IGameMaster master, OwnedSelectionRequest request);

    /// <summary>
    /// player.SupportCards の行。手順: (1) Kind == LimitBreakMaterial なら CardId == MaterialCardId かつ SupportCardUid != MaterialTargetUid の所持だけ残す。
    /// (2) planFilter が null でなければ SupportCardInfo.Plan == planFilter のものだけ（完全一致。Free を選んだら Free のカードだけ）。
    /// (3) supportTypeFilter が null でなければ SupportCardInfo.Type == supportTypeFilter のものだけ。(4) 並べる。(5) 選べるかを決める。
    /// request が null・LimitBreakMaterial なら全部 Enabled。Kind == Idol なら ArgumentException。
    /// </summary>
    public static IReadOnlyList<OwnedRowData> BuildSupportCards(PlayerSnapshot player, IGameMaster master, OwnedSortKey sort, bool descending,
        Plan? planFilter, SupportType? supportTypeFilter, OwnedSelectionRequest request);
}
```

`OwnedListBuilder` の規則:
- 名前・レア度・プラン・タイプは `master.GetIdol(idolId)`・`master.GetSupportCard(card.CardId)` から取る（無ければ `KeyNotFoundException` がそのまま出る）
- サポートカードの並べ方: 第 1 の基準で比べ、同じなら `SupportCardUid` の昇順（`string.CompareOrdinal`）で決める（所持ID は重ならないので順は 1 つに決まる）。第 1 の基準: `Rarity` は `(int)Rarity`、`Level` は `Level`、`Acquired` は `SupportCardUid`（`string.CompareOrdinal`）。`descending == true` なら第 1 の基準だけ逆にする（`Rarity` は SSR が先、`Level` は大きいほうが先、`Acquired` は新しいほうが先）。同じ値どうしの第 2 の基準（所持ID の昇順）は向きを変えない
- 選べるかの判定（`Kind == SupportCard`）は、上から順に最初に当たったものを理由にする: `ExcludedUids` に所持ID がある → `AlreadyInFormation`、`ExcludedSupportCardIds` に `CardId` がある → `SameCardInFormation`、`!FormationValidator.IsPlanCompatible(IdolPlan, info.Plan)` → `PlanMismatch`。どれにも当たらなければ `Enabled = true`・`None`
- 0 件なら空のリストを返す

### 3.5 `Uta.Client.Infrastructure`

```csharp
// Api/SupportApi.cs
public sealed class SupportApi : ApiServiceBase<ISupportService>
{
    public SupportApi(ApiCore core, ISupportService client) : base(core, client) { }
    public UniTask<ApiResult<LevelUpResponse>> LevelUpAsync(LevelUpRequest request, IEnumerable<ErrorCode> handledCodes = null)
        => InvokeAsync(nameof(ISupportService.LevelUpAsync), request, static (c, r) => c.LevelUpAsync(r), handledCodes);
    public UniTask<ApiResult<LimitBreakResponse>> LimitBreakAsync(LimitBreakRequest request, IEnumerable<ErrorCode> handledCodes = null)
        => InvokeAsync(nameof(ISupportService.LimitBreakAsync), request, static (c, r) => c.LimitBreakAsync(r), handledCodes);
}

// Support/SupportGateway.cs
public sealed class SupportGateway : ISupportPort
{
    public SupportGateway(UtaApiClient api);
    public async UniTask<PortResult<SupportLevelUpResult>> LevelUpAsync(string supportCardUid, int targetLevel);
    public async UniTask<PortResult<SupportLimitBreakResult>> LimitBreakAsync(string supportCardUid, string materialUid);
}
```

- `SupportGateway` のフィールド（2 本目の `GachaGateway` の `drawHandled` と同じ書き方）:
  ```csharp
  private readonly SupportApi support;   // コンストラクターで api.Support
  private readonly ErrorCode[] levelUpHandled = { SupportErrors.InvalidTargetLevel, SupportErrors.NotEnoughPoints, PlayerErrors.NotEnoughGold, SupportErrors.NotOwned };
  private readonly ErrorCode[] limitBreakHandled = { SupportErrors.MaxLimitBreak, SupportErrors.InvalidMaterial, SupportErrors.NotOwned };
  ```
  どのコードも 1 本目で `UtaErrors` に `Stay` で登録済み。`handledCodes` に入れたものは共通ダイアログを出さずに返り、入れなかったもの（`Stay` のほかのコード、通信の失敗）は ApiCore が共通ダイアログを出してから返る
- `LevelUpAsync`: `var result = await support.LevelUpAsync(new LevelUpRequest(supportCardUid, targetLevel), levelUpHandled);`
  - `result.IsSuccess` → `PortResult<SupportLevelUpResult>.Success(new SupportLevelUpResult(result.Value.Level, result.Value.SupportPoints, result.Value.Gold))`
  - 失敗で `IsHandled(levelUpHandled, result.Error.Code)` → `PortResult<SupportLevelUpResult>.BusinessError(result.Error.Code.Reason, result.Error.ToString())`
  - それ以外 → `PortResult<SupportLevelUpResult>.Failure(result.Error.ToString())`
- `LimitBreakAsync`: `var result = await support.LimitBreakAsync(new LimitBreakRequest(supportCardUid, materialUid), limitBreakHandled);`。分岐は `LevelUpAsync` と同じで、型は `PortResult<SupportLimitBreakResult>`、成功の値は `new SupportLimitBreakResult(result.Value.LimitBreak, result.Value.MaxLevel)`、判定の配列は `limitBreakHandled`
- 判定は **2 本目の `GachaGateway.DrawAsync` と同じく `ErrorCode` の `Equals`**（`Domain`・`Reason` の文字列を自分で比べない）: `private static bool IsHandled(ErrorCode[] handled, ErrorCode code)` は `foreach (var c in handled) { if (c.Equals(code)) { return true; } } return false;`（LINQ を使わない）
- `using`: `Cysharp.Threading.Tasks`、`Uta.Client.Application.Common`、`Uta.Client.Application.Support`、`Uta.Client.Infrastructure.Api`、`Uta.Shared.Contracts.Player`（`PlayerErrors`）、`Uta.Shared.Contracts.Support`、`VolTech.Server.Abstractions`（`ErrorCode`。`PlayerGateway` と同じ）
- `UtaApiClient`（2 本目で `Gacha` を足した版に、同じ形で足す）:
  - プロパティ `public SupportApi Support { get; }` を `Gacha` の次に足す
  - private のコンストラクターの最後（`GachaApi gacha` の次）に `SupportApi support` を足し、本体の `Gacha = gacha;` の次に `Support = support;`
  - `Create` で、`gachaClient` の行の次に `var supportClient = ServiceClientFactory.Create<ISupportService>(callInvoker);` を足し、最後の `new UtaApiClient(..., new PlayerApi(core, playerClient), new GachaApi(core, gachaClient))` を `new UtaApiClient(..., new PlayerApi(core, playerClient), new GachaApi(core, gachaClient), new SupportApi(core, supportClient))` にする（`...` の部分は変えない）
  - `using Uta.Shared.Contracts.Support;` を足す
- `UtaMagicOnionClientInitializer`: `[MagicOnionClientGeneration(typeof(IDeviceService), typeof(IPlayerService), typeof(IGachaService), typeof(ISupportService))]`（`using Uta.Shared.Contracts.Support;`）。生成は Unity の再コンパイルで行われる
- `ApiResult` の成功・失敗はテストから作れない（`internal`。2 本目 3.3 と同じ）ので、`SupportGateway` の分岐は EditMode でテストしない（Play で見る）

### 3.6 所持一覧（`Uta.Client.Presentation.Owned`）

```csharp
public enum OwnedTab { Idols, SupportCards }

public sealed class OwnedArgs   // 1 本目 3.3.4 の Args
{
    public OwnedArgs(OwnedSelectionRequest selection);   // null なら閲覧モード
    public OwnedSelectionRequest Selection { get; }
    public static OwnedArgs Browse();                    // new OwnedArgs(null)
}

public sealed class OwnedRowViewData
{
    public OwnedRowViewData(string title, string detail, bool enabled);
    public string Title { get; } public string Detail { get; } public bool Enabled { get; }
}

public interface IOwnedView
{
    void SetTitle(string title);
    void SetTabs(OwnedTab selected, bool visible);
    void SetControls(bool visible, string sortLabel, string orderLabel, string planLabel, string typeLabel, bool filtersVisible);
    void SetRows(IReadOnlyList<OwnedRowViewData> rows);
    event Action<OwnedTab> TabSelected;
    event Action SortKeyClicked;
    event Action SortOrderClicked;
    event Action PlanFilterClicked;
    event Action TypeFilterClicked;
    event Action<int> RowClicked;     // 引数は SetRows に渡した行の添字
    event Action BackClicked;
}

public sealed class OwnedPage : PageBase, IOwnedView { ... }   // フィールドは P2 の骨格。実装は下の「OwnedPage の実装」

[AssetAddress(nameof(OwnedPage))]
public sealed class OwnedPresenter : IScreenWithArgs<OwnedArgs>
{
    [Inject] public OwnedPresenter(OwnedPage view, OwnedArgs args, PlayerCache cache, IGameMaster master, IScreenNavigator navigator);
        // : this((IOwnedView)view, args, cache, master, navigator)
    public OwnedPresenter(IOwnedView view, OwnedArgs args, PlayerCache cache, IGameMaster master, IScreenNavigator navigator);
        // args が null なら ArgumentNullException
    public UniTask InitializeAsync();          // 7 つのイベントを購読し、Refresh()。UniTask.CompletedTask
    public void DidPopEnter();                 // Refresh()（詳細から戻ったとき）
    public void Dispose();                     // disposed = true、7 つの購読を解く
    public UniTask<object> CompleteAsync();    // UniTask.FromResult<object>(result)。基盤が Pop の完了時に呼び、WaitForPopAsync の結果になる
    public UniTask SelectRowAsync(int index);  // RowClicked の受け口が Forget で呼ぶ
    public UniTask BackAsync();                // BackClicked の受け口が Forget で呼ぶ
    public void SelectTab(OwnedTab tab);       // 以下 4 つは同期（通信も遷移もない）。イベントの受け口から直接呼ぶ
    public void CycleSortKey();
    public void ToggleSortOrder();
    public void CyclePlanFilter();
    public void CycleTypeFilter();
}
```

**選択モードの呼び方**（4 本目の編成と、3.7 の上限解放の素材が使う）:

```csharp
var presenter = await navigator.PushPageAsync<OwnedPresenter, OwnedArgs>(new OwnedArgs(request), playAnimation: false);
var selection = await navigator.WaitForPopAsync<OwnedSelection>(presenter);
if (selection == null || selection.IsCancelled) { /* 何もしない */ }
```

（結果は `OwnedPresenter.CompleteAsync` が返す。画面が戻る以外の理由で閉じても、基盤が `CompleteAsync` を呼ぶので `await` は止まらない。テストの `FakeScreenNavigator` は `PopResults` が空なら `null` を返すので、`null` はキャンセルと同じに扱う。）

**`OwnedPresenter` の状態**
- `request = args.Selection`、`result = OwnedSelection.Cancelled`、`rows`（直近に組んだ `IReadOnlyList<OwnedRowData>`）、`running`（実行中フラグ。1 本目 3.3.2 の 5）、`closed`（`PopPageAsync(this)` を呼んだら true にし、戻さない）、`disposed`（破棄の印。1 本目 3.3.2 の 6）
- モードごとの初期値:

| モード（`request`） | 見出し（`SetTitle`） | `tab` | タブの表示（`SetTabs` の `visible`） | 並び・絞り込みの行（`SetControls` の `visible`） | プラン・タイプのボタン（`filtersVisible`） | 初期の並び（`sortKey`・`descending`） |
|---|---|---|---|---|---|---|
| 閲覧（null） | 所持一覧 | `Idols` | `true` | `tab == SupportCards` | `true` | `Rarity`・`true`（降順） |
| `Idol` | 育成アイドルを選ぶ | `Idols` | `false` | `false` | `false` | `Rarity`・`true`（並べ替えには使わない。ラベルを作るためだけに持つ） |
| `SupportCard` | サポートカードを選ぶ | `SupportCards` | `false` | `true` | `true` | `Rarity`・`true`（降順） |
| `LimitBreakMaterial` | 素材にするカードを選ぶ | `SupportCards` | `false` | `true` | `false` | `Level`・`false`（昇順） |

- `filtersVisible` はモードで決まる定数（コンストラクターで決め、変えない）。`Refresh()` はこの値を絞り込みの有無と `SetControls` の最後の引数の両方に使う

- サポートカードの状態: `sortKey`・`descending`（上の表）、`planFilter`（null = 全て）、`typeFilter`（null = 全て）。タブを替えても残す（サポートに戻ったとき、ボタンのラベルに今の状態が出るので見える）

**`Refresh()`**
1. `var player = cache.Current;`
2. `tab == Idols` なら `rows = OwnedListBuilder.BuildIdols(player, master, request)`、そうでなければ `rows = OwnedListBuilder.BuildSupportCards(player, master, sortKey, descending, filtersVisible ? planFilter : null, filtersVisible ? typeFilter : null, request)`（`filtersVisible` は上の表の「プラン・タイプのボタン」）
3. 行の文字（`OwnedRowViewData`）:
   - `title` = `Name + " [" + DisplayText.RarityName(Rarity) + "]"`
   - `detail`: 育成アイドルは `DisplayText.PlanName(Plan) + "　かけら " + Shards`、サポートカードは `DisplayText.SupportTypeName(SupportType) + "　" + DisplayText.PlanName(Plan) + "　Lv " + Level + "　解放 " + LimitBreak`（区切りは全角の空白 1 つ）
   - 無効の行は `detail` の末尾に `"　（編成済み）"`（`AlreadyInFormation`）／`"　（同じカードは編成できない）"`（`SameCardInFormation`）／`"　（プランが合わない）"`（`PlanMismatch`）を足す
4. `view.SetTitle(見出し)`、`view.SetTabs(tab, タブの表示)`、`view.SetControls(並び・絞り込みの行の表示, sortLabel, orderLabel, planLabel, typeLabel, filtersVisible)`、`view.SetRows(行の文字)` の順に呼ぶ
   - `sortLabel`: `Rarity` → `"並び: レア度"`、`Level` → `"並び: Lv"`、`Acquired` → `"並び: 入手順"`
   - `orderLabel`: `descending` なら `"降順"`、そうでなければ `"昇順"`
   - `planLabel`: `planFilter` が null なら `"プラン: 全て"`、そうでなければ `"プラン: " + DisplayText.PlanName(planFilter.Value)`
   - `typeLabel`: `typeFilter` が null なら `"タイプ: 全て"`、そうでなければ `"タイプ: " + DisplayText.SupportTypeName(typeFilter.Value)`
   - 行を出さないモード（`Idol`）でもラベルは上の規則で作って渡す（View が隠す）

**操作**
- `SelectTab(tab)`: `request != null` か `tab` が今と同じなら何もしない。そうでなければ `tab` を替えて `Refresh()`
- `CycleSortKey()`: `tab != SupportCards` なら何もしない。`Rarity` → `Level` → `Acquired` → `Rarity`。`Refresh()`
- `ToggleSortOrder()`: `tab != SupportCards` なら何もしない。`descending` を反転。`Refresh()`
- `CyclePlanFilter()`: `tab != SupportCards` か `filtersVisible` が false なら何もしない。null → `Free` → `Sense` → `Logic` → null。`Refresh()`
- `CycleTypeFilter()`: 同じ条件。null → `Singing` → `Rhythm` → `Expression` → `Assist` → null。`Refresh()`
- `SelectRowAsync(index)`: `if (running || closed || disposed) return; running = true; try { ... } finally { running = false; }`。中身:
  1. `index` が `rows` の範囲外なら何もしない
  2. `var row = rows[index];`
  3. 選択モード（`request != null`）: `!row.Enabled` なら何もしない。そうでなければ `result = row.IsIdol ? OwnedSelection.ForIdol(row.IdolId) : OwnedSelection.ForSupportCard(row.SupportCardUid);`、`closed = true;`、`await navigator.PopPageAsync(this, playAnimation: false);`
  4. 閲覧モードでサポートカード: `await navigator.PushPageAsync<SupportDetailPresenter, SupportDetailArgs>(new SupportDetailArgs(row.SupportCardUid), playAnimation: false);`
  5. 閲覧モードで育成アイドル: 何もしない（育成アイドルの詳細は MVP で作らない）
- `BackAsync()`: `if (running || closed || disposed) return; running = true; try { closed = true; await navigator.PopPageAsync(this, playAnimation: false); } finally { running = false; }`（`result` は `Cancelled` のまま）
- `closed` で、行のタップと「戻る」の連打・組み合わせで `PopPageAsync(this)` が 2 回呼ばれるのを防ぐ（1 本目 3.3.2 の 9。閉じた後の Presenter を渡すと基盤が `InvalidOperationException` を投げる）
- 同期の 5 つ（`SelectTab` など）は、`disposed` なら何もしない

イベントの受け口（2 本目の `GachaPresenter.InitializeAsync` の 1 と同じ書き方）: **受け口は名前付きの private の同期メソッドにし、ラムダで購読しない**。`InitializeAsync` で `view.Xxx += OnXxx;`、`Dispose` で同じメソッドを `view.Xxx -= OnXxx;` する（ラムダだと `-=` で外せないため）。
- `private void OnTabSelected(OwnedTab tab) { SelectTab(tab); }`（`view.TabSelected`）
- `private void OnSortKeyClicked() { CycleSortKey(); }`（`view.SortKeyClicked`）
- `private void OnSortOrderClicked() { ToggleSortOrder(); }`（`view.SortOrderClicked`）
- `private void OnPlanFilterClicked() { CyclePlanFilter(); }`（`view.PlanFilterClicked`）
- `private void OnTypeFilterClicked() { CycleTypeFilter(); }`（`view.TypeFilterClicked`）
- `private void OnRowClicked(int index) { SelectRowAsync(index).Forget(); }`（`view.RowClicked`）
- `private void OnBackClicked() { BackAsync().Forget(); }`（`view.BackClicked`）

**`OwnedPage` の実装**（P2 の骨格に足す。foundation 3.3.3 の形。フィールドと Prefab の子の対応は P2 の 3.3 の表）

P2 の骨格のフィールド（変えない）: `HeaderView header`、`TabBarView tabBar`、`RectTransform controls`、`RectTransform filterRow`、`ButtonView sortButton`・`orderButton`・`planButton`・`typeButton`、`TMP_Text emptyText`、`RectTransform listContent`、`ListRowView rowPrefab`。足すもの:

```csharp
public sealed class OwnedPage : PageBase, IOwnedView
{
    // [SerializeField] のフィールドは P2 のまま

    public event Action<OwnedTab> TabSelected;
    public event Action SortKeyClicked;
    public event Action SortOrderClicked;
    public event Action PlanFilterClicked;
    public event Action TypeFilterClicked;
    public event Action<int> RowClicked;
    public event Action BackClicked;

    private void Awake();
    public void SetTitle(string title);
    public void SetTabs(OwnedTab selected, bool visible);
    public void SetControls(bool visible, string sortLabel, string orderLabel, string planLabel, string typeLabel, bool filtersVisible);
    public void SetRows(IReadOnlyList<OwnedRowViewData> rows);
}
```

- タブの添字: `0` = `OwnedTab.Idols`（`TabIdols`）、`1` = `OwnedTab.SupportCards`（`TabSupport`）（P2 の `tabs` の並び）
- `Awake()`:
  - `header.BackButton.Button.onClick.AddListener(() => BackClicked?.Invoke());`
  - `tabBar.TabClicked += index => TabSelected?.Invoke(index == 0 ? OwnedTab.Idols : OwnedTab.SupportCards);`（`TabBarView` が自分の `Awake` でタブのボタンを購読し、添字で知らせる。どちらの `Awake` が先でも C# の `event` の購読なので動く）
  - `sortButton.Button.onClick` → `SortKeyClicked`、`orderButton` → `SortOrderClicked`、`planButton` → `PlanFilterClicked`、`typeButton` → `TypeFilterClicked`（どれも `AddListener(() => Xxx?.Invoke())`）
- `SetTitle(title)`: `header.SetTitle(title)`
- `SetTabs(selected, visible)`: `tabBar.gameObject.SetActive(visible);` → `tabBar.SetSelected(selected == OwnedTab.Idols ? 0 : 1);`（選んだタブの下線 `underline` を出し、もう一方を消す。ボタンはどちらも押せるまま。今のタブを押したときは Presenter が何もしない）
- `SetControls(visible, sortLabel, orderLabel, planLabel, typeLabel, filtersVisible)`: `controls.gameObject.SetActive(visible);` → `filterRow.gameObject.SetActive(filtersVisible);`（プラン・タイプの段ごと）→ `sortButton.SetLabel(sortLabel)`・`orderButton.SetLabel(orderLabel)`・`planButton.SetLabel(planLabel)`・`typeButton.SetLabel(typeLabel)`（隠しているボタンのラベルも替える）
- `SetRows(rows)`:
  1. `UiObjects.ClearChildren(listContent);`
  2. `emptyText.gameObject.SetActive(rows.Count == 0);`（文字は Prefab の「ありません」のまま。C# は替えない）
  3. 添字 `i` ごとに `var row = UiObjects.AddRow(rowPrefab, listContent, "Row" + i);`、`row.SetTexts(rows[i].Title, rows[i].Detail, "");`（右の文字は使わない）、`row.SetInteractable(rows[i].Enabled);`（押せない行は Button の ColorTint の disabled で暗くなる）、`var index = i; row.Button.onClick.AddListener(() => RowClicked?.Invoke(index));`（行は次の `ClearChildren` で消えるので、解除は書かない）
- 文字の大きさ・色・行の高さは Prefab（P1 の `ListRow` は高さ 160、`TitleText`・`SubText` の 2 段）が決める。C# は文字と表示・押せるかだけを変える（foundation 3.3.3）

### 3.7 サポートカードの詳細（`Uta.Client.Presentation.Owned`）

```csharp
public sealed class SupportDetailArgs
{
    public SupportDetailArgs(string supportCardUid);
    public string SupportCardUid { get; }
}

public interface ISupportDetailView
{
    void SetInfo(IReadOnlyList<string> lines);
    void SetAbilities(IReadOnlyList<string> lines);
    void SetResources(string text);
    void SetLevelButtons(string plus1, string plus5, string max, bool plus1Enabled, bool plus5Enabled, bool maxEnabled);
    void SetLimitBreak(string label, bool enabled);
    event Action Plus1Clicked;
    event Action Plus5Clicked;
    event Action MaxClicked;
    event Action LimitBreakClicked;
    event Action BackClicked;
}

public sealed class SupportDetailPage : PageBase, ISupportDetailView { ... }   // フィールドは P2 の骨格。実装は下の「SupportDetailPage の実装」

[AssetAddress(nameof(SupportDetailPage))]
public sealed class SupportDetailPresenter : IScreenWithArgs<SupportDetailArgs>
{
    [Inject] public SupportDetailPresenter(SupportDetailPage view, SupportDetailArgs args, PlayerCache cache, IGameMaster master,
        LevelUpSupportUseCase levelUp, LimitBreakSupportUseCase limitBreak, DialogService dialogs, IScreenNavigator navigator);
        // : this((ISupportDetailView)view, ...)
    public SupportDetailPresenter(ISupportDetailView view, SupportDetailArgs args, PlayerCache cache, IGameMaster master,
        LevelUpSupportUseCase levelUp, LimitBreakSupportUseCase limitBreak, DialogService dialogs, IScreenNavigator navigator);
    public UniTask InitializeAsync();             // 5 つのイベントを購読し、Refresh()
    public void DidPopEnter();                    // Refresh()（素材の選択から戻ったとき）
    public void Dispose();                        // disposed = true、5 つの購読を解く
    public UniTask LevelUpAsync(LevelUpStep step);
    public UniTask LimitBreakAsync();
    public UniTask BackAsync();
}
```

`FindCard()` = `cache.Current.SupportCards` のうち `SupportCardUid == args.SupportCardUid` の 1 枚（無ければ null）。以下 `info = master.GetSupportCard(card.CardId)`、`maxLevel = master.GetSupportMaxLevel(info.Rarity, card.LimitBreak)`、`maxBreak = master.GetSupportMaxLimitBreak(info.Rarity)`、`fullMaxLevel = master.GetSupportMaxLevel(info.Rarity, maxBreak)`、`sp = cache.Current.SupportPoints`、`gold = cache.Current.Gold`。

**`Refresh()`**
- `card` が null（素材にした・キャッシュから消えた）: `SetInfo(new[] { "このカードは所持していません。" })`、`SetAbilities(new string[0])`、`SetResources("")`、`SetLevelButtons("+1", "+5", "最大", false, false, false)`、`SetLimitBreak("上限解放", false)` で終わる
- `SetInfo` の 4 行: `info.Name`、`DisplayText.RarityName(info.Rarity) + "　" + DisplayText.SupportTypeName(info.Type) + "　" + DisplayText.PlanName(info.Plan)`、`"Lv " + card.Level + " / " + maxLevel`、`"上限解放 " + card.LimitBreak + " / " + maxBreak`
- `SetAbilities`: `master.GetSupportAbilities(info.SupportCardId)` の各行 `a`（`Order` の昇順）を `a.Order + " " + DisplayText.Ability(a.Kind, a.Param, ProduceFormulas.AbilityValue(a.ValueAtLv1, a.ValueAtMax, card.Level, fullMaxLevel))`（例 `"1 初期歌唱 +49"`）。0 件なら `new[] { "なし" }`
- `SetResources("強化pt " + N0(sp) + "　ゴールド " + N0(gold))`
- `SetLevelButtons`: 3 つの `step`（名前は `+1`・`+5`・`最大`）ごとに `target = SupportLevelPlanner.PlanTarget(card, info.Rarity, sp, gold, step, master)`
  - `target > card.Level`: 有効。ラベル `名前 + " → Lv " + target + "（強化pt " + N0(cost.SupportPoints) + " / G " + N0(cost.Gold) + "）"`（`cost = master.GetSupportLevelUpCost(info.Rarity, card.Level, target)`）
  - `card.Level >= maxLevel`: 無効。ラベル `名前 + "（上限）"`
  - それ以外（`最大` で、1 レベルも払えない）: 無効。ラベル `"最大（足りません）"`
- `SetLimitBreak`: `card.LimitBreak >= maxBreak` なら `("上限解放（最大）", false)`、それ以外は `("上限解放（素材を選ぶ）", true)`

**`LevelUpAsync(step)`**（`running`・`closed`・`disposed` は 3.6 と同じ。`if (running || closed || disposed) return; running = true; try { ... } finally { running = false; }`。下の「`await` のあと」は、どれも `if (disposed) return;` を先に置く: 1 本目 3.3.2 の 6）
1. `card = FindCard()`。null なら終わる
2. **押した時点の値で**計算し直す: `target = SupportLevelPlanner.PlanTarget(card, info.Rarity, sp, gold, step, master)`。`target <= card.Level` なら何もしない（無効のボタンのイベントが来ても安全）
3. `cost = master.GetSupportLevelUpCost(info.Rarity, card.Level, target)`。`await dialogs.ConfirmAsync("Lv " + card.Level + " → Lv " + target + " に上げます。\n強化pt " + N0(cost.SupportPoints) + "・ゴールド " + N0(cost.Gold) + " を使います。")`。`await` のあと。`Negative` なら終わる
4. `var outcome = await levelUp.ExecuteAsync(card, target);` `await` のあと
5. `Succeeded` → `Refresh()`（`RefreshFailed` でもキャッシュは直してあるので同じ。追加のダイアログは出さない）。`NotEnoughPoints` → `dialogs.ShowInfoAsync("強化ptが足りません。")`、`NotEnoughGold` → `"ゴールドが足りません。"`、`InvalidTarget` → `"レベルを上げられません。"`、`NotOwned` → `"このカードは所持していません。"`（どれも `await` し、`await` のあと `Refresh()`）。`Failed` → 何も出さない（ApiCore が出し終えている）

**`LimitBreakAsync()`**（実行中フラグ・破棄の印は同じ）
1. `card = FindCard()`。null なら終わる。`card.LimitBreak >= maxBreak` なら何もしない
2. 素材の候補 = `cache.Current.SupportCards` のうち `CardId == card.CardId` かつ `SupportCardUid != card.SupportCardUid`。0 件なら `await dialogs.ShowInfoAsync("素材にできる同じカードがありません。")` で終わる
3. 素材を選ばせる（同じ `Home` シーンの中で、所持一覧の選択モードをもう 1 枚積む）: `var presenter = await navigator.PushPageAsync<OwnedPresenter, OwnedArgs>(new OwnedArgs(OwnedSelectionRequest.ForLimitBreakMaterial(card.CardId, card.SupportCardUid)), playAnimation: false); var selection = await navigator.WaitForPopAsync<OwnedSelection>(presenter);` `await` のあと。`selection == null || selection.IsCancelled` なら終わる（素材の一覧が閉じると、このページの `DidPopEnter` が先に `Refresh()` する）
4. `material = cache.Current.SupportCards` から `selection.SupportCardUid` で探す。null なら終わる
5. `await dialogs.ConfirmAsync("Lv " + material.Level + " の「" + info.Name + "」を素材にして上限解放します（" + card.LimitBreak + " → " + (card.LimitBreak + 1) + "）。\n素材は無くなります。強化に使った強化pt・ゴールドは戻りません。")`。`await` のあと。`Negative` なら終わる
6. `var outcome = await limitBreak.ExecuteAsync(card, material);` `await` のあと
7. `Succeeded` → `Refresh()`。`MaxLimitBreak` → `"これ以上は解放できません。"`、`InvalidMaterial` → `"素材にできません。"`、`NotOwned` → `"このカードは所持していません。"`（`ShowInfoAsync` を `await` し、`await` のあと `Refresh()`）。`Failed` → 何も出さない

**`BackAsync()`**: 3.6 の `BackAsync` と同じ（`closed = true` のあと `PopPageAsync(this, playAnimation: false)`）。

イベントの受け口（3.6 と同じ。名前付きの private の同期メソッドにし、`InitializeAsync` で `+=`、`Dispose` で同じメソッドを `-=` する。ラムダで購読しない）:
- `private void OnPlus1Clicked() { LevelUpAsync(LevelUpStep.Plus1).Forget(); }`（`view.Plus1Clicked`）
- `private void OnPlus5Clicked() { LevelUpAsync(LevelUpStep.Plus5).Forget(); }`（`view.Plus5Clicked`）
- `private void OnMaxClicked() { LevelUpAsync(LevelUpStep.Max).Forget(); }`（`view.MaxClicked`）
- `private void OnLimitBreakClicked() { LimitBreakAsync().Forget(); }`（`view.LimitBreakClicked`）
- `private void OnBackClicked() { BackAsync().Forget(); }`（`view.BackClicked`）

**`SupportDetailPage` の実装**（P2 の骨格に足す。foundation 3.3.3 の形。フィールドと Prefab の子の対応は P2 の 3.3 の表）

P2 の骨格のフィールド（変えない）: `HeaderView header`、`TMP_Text infoText`、`TMP_Text resourcesText`、`ButtonView plus1Button`・`plus5Button`・`maxButton`・`limitBreakButton`、`TMP_Text abilitiesText`。見出し「サポートカード」と「アビリティ」は Prefab の固定の文字（C# は替えない）。足すもの:

```csharp
public sealed class SupportDetailPage : PageBase, ISupportDetailView
{
    // [SerializeField] のフィールドは P2 のまま

    public event Action Plus1Clicked;
    public event Action Plus5Clicked;
    public event Action MaxClicked;
    public event Action LimitBreakClicked;
    public event Action BackClicked;

    private void Awake();
    public void SetInfo(IReadOnlyList<string> lines);
    public void SetAbilities(IReadOnlyList<string> lines);
    public void SetResources(string text);
    public void SetLevelButtons(string plus1, string plus5, string max, bool plus1Enabled, bool plus5Enabled, bool maxEnabled);
    public void SetLimitBreak(string label, bool enabled);
}
```

- `Awake()`: `header.BackButton.Button.onClick` → `BackClicked`、`plus1Button.Button.onClick` → `Plus1Clicked`、`plus5Button` → `Plus5Clicked`、`maxButton` → `MaxClicked`、`limitBreakButton` → `LimitBreakClicked`（どれも `AddListener(() => Xxx?.Invoke())`）
- `SetInfo(lines)`: `infoText.text = string.Join("\n", lines);`
- `SetAbilities(lines)`: `abilitiesText.text = string.Join("\n", lines);`（0 行なら空文字）
- `SetResources(text)`: `resourcesText.text = text;`
- `SetLevelButtons(...)`: `plus1Button.SetLabel(plus1); plus1Button.SetInteractable(plus1Enabled);`、`plus5Button`・`maxButton` も同じ。ボタンは Prefab に固定で 3 つ（P2 の決めたこと 4。ラベルは 32pt で「+1 → Lv 43（強化pt 5,140 / G 5,140）」が 1 行に入る）
- `SetLimitBreak(label, enabled)`: `limitBreakButton.SetLabel(label); limitBreakButton.SetInteractable(enabled);`

### 3.8 ホームと Bootstrap

- `HomePresenter.OpenOwnedAsync()`（foundation 3.7。gacha で `IScreenNavigator navigator` がコンストラクターに足されている）: 実行中フラグ（foundation 3.3.2 の 5）の `try` の中身を `await navigator.PushPageAsync<OwnedPresenter, OwnedArgs>(OwnedArgs.Browse(), playAnimation: false);` に置き換える（「準備中です。」を出さなくなる。戻り値は使わない）。コンストラクターの依存は足さない。`using Uta.Client.Presentation.Owned;` を足す。押すボタンは P2 の `HomePage` の `Body/Main/Nav/NavOwnedButton`（ラベル「アイドル」。foundation の `IHomeView.OwnedClicked`）で、`HomePage` は変えない
- 積む先は `Home` シーンのスコープの `IScreenNavigator`（`HomeLifetimeScope` の `SceneUiInstaller.Install`。foundation 3.2）。所持一覧・詳細・素材の一覧はどれも `Home` シーンのページで、`ISceneNavigator` は使わない。一覧からホームへは「＜」で 1 枚ずつ戻る（ホームの `DidPopEnter` が表示をやり直す）
- `BootstrapEntry.RegisterUseCases`（foundation 3.8）の最後に 3 行足す: `builder.Register<SupportGateway>(Lifetime.Singleton).As<ISupportPort>();`、`builder.Register<LevelUpSupportUseCase>(Lifetime.Singleton);`、`builder.Register<LimitBreakSupportUseCase>(Lifetime.Singleton);`。`using Uta.Client.Application.Support;`・`using Uta.Client.Infrastructure.Support;` を足す
- **Presenter・ページは登録しない**（foundation 3.3.2・3.3.9。基盤が Push のたびに子スコープで作り、Prefab は Addressables のアドレス `OwnedPage`・`SupportDetailPage` から読む。アドレスの登録は P2 が済ませている）

## 4. タスク

各タスクの終わりに、`client/uta` で `unity recompile --format json` がコンパイルエラー 0、EditMode のテストで既存とそのタスクまでのテストが全部通る状態にする（foundation 4章の冒頭のとおり。回し方は P1 の決めたこと 12: Editor を開いたまま `TestRunnerApi` を eval で回す。コマンドは CLAUDE.md「ビルド・テスト・実行」・unity7-evaluation 4章）。**eval で回せなければ、Editor の Test Runner（EditMode）を手動で回してもらい、確認できていないと報告する。止まらない**（P1 の決めたこと 12）。**Editor を開けない環境では、通ったとは書かず、確かめていないと報告してユーザーに実行を頼む**。shared/ のテストは `dotnet test --project shared/Uta.Shared.Domain.Tests`。コミットの前に `unity command eval 'Uta.Client.Editor.Prefabs.FontAssets.ClearDynamicData(); return "ok";'`（P1 の `FontAssets`）を実行する（Play で日本語を出すとフォントのアセットに字が足されるため）。Prefab（`.prefab`）と `AddressableAssetsData/` に差分が出ていたら、この計画の変更ではないので戻し、報告に書く。

### タスク 1: Shared.Domain のプランの適合と、表示名・一覧の組み立て

前提の確認: 1章の前提 1（P2 の `OwnedPage`・`SupportDetailPage` のフィールド、`HomePresenter` の `IScreenNavigator`、2 本目の `FakeGameMaster` の `Abilities`）。違えば止まって報告する。

触る: `shared/Uta.Shared.Domain/Produce/FormationValidator.cs`、`shared/Uta.Shared.Domain.Tests/Produce/FormationValidatorTests.cs`、`Presentation/Common/DisplayText.cs`、`Application/Owned/*`、`Tests/EditMode/DisplayTextTests.cs`・`OwnedListBuilderTests.cs`・`OwnedSelectionRequestTests.cs`。
手順:
1. 3.1 のとおり `IsPlanCompatible` を足し、`Validate` の条件を置き換える。5章の `FormationValidatorTests` を足す。`dotnet build Uta.slnx`（警告 0・エラー 0）と `dotnet test --project shared/Uta.Shared.Domain.Tests` を通す（既存の `Validate_Cases_ReturnsExpected` が変わらず通ること）。サーバーも `FormationValidator.Validate` を使うので `dotnet test --project server/Uta.Server.Tests` も回す（Docker が無ければ結合テストは確かめていないと報告する）
2. 3.2・3.4 のとおり作る
3. テストのアビリティは 2 本目の `FakeGameMaster.Abilities`（`Dictionary<int, List<SupportAbilityInfo>>`）に入れる（`FakeGameMaster.cs` は変えない）
確かめ方: `DisplayTextTests`・`OwnedListBuilderTests`・`OwnedSelectionRequestTests` が通る。

### タスク 2: ユースケース・API・ゲートウェイ

触る: `Application/Common/ServerReasons.cs`、`Application/Support/*`、`Infrastructure/Api/SupportApi.cs`・`UtaApiClient.cs`・`UtaMagicOnionClientInitializer.cs`、`Infrastructure/Support/SupportGateway.cs`、`Tests/EditMode/ServerReasonsTests.cs`・`SupportLevelPlannerTests.cs`・`SupportSnapshotEditsTests.cs`・`LevelUpSupportUseCaseTests.cs`・`LimitBreakSupportUseCaseTests.cs`。
手順: 3.3・3.5 のとおり作る（`ServerReasons` に 6 つ足す）。
確かめ方: `ServerReasonsTests`・`SupportLevelPlannerTests`・`SupportSnapshotEditsTests`・`LevelUpSupportUseCaseTests`・`LimitBreakSupportUseCaseTests` が通る。`unity recompile` のあと、`ISupportService` のクライアントの生成コード（`UtaMagicOnionClientInitializer` の `partial`）でエラーが出ていないこと（コンソールの読み出しは [unity7-evaluation.md](../wiki/architecture/unity7-evaluation.md) 4章のコマンド）。

### タスク 3: View の実装と所持一覧の画面

触る: `Presentation/Owned/OwnedTab.cs`・`OwnedArgs.cs`・`OwnedRowViewData.cs`・`IOwnedView.cs`・`OwnedPresenter.cs`（作成）、`OwnedPage.cs`（P2 の骨格に足す）、`SupportDetailArgs.cs`・`ISupportDetailView.cs`・`SupportDetailPresenter.cs`（作成。Presenter は仮）、`SupportDetailPage.cs`（P2 の骨格に足す）、`Presentation/Home/HomePresenter.cs`、`Bootstrap/BootstrapEntry.cs`、`Tests/EditMode/OwnedPresenterTests.cs`・`HomePresenterTests.cs`。
手順:
1. 3.6 の `OwnedTab`・`OwnedArgs`・`OwnedRowViewData`・`IOwnedView` と、3.7 の `SupportDetailArgs`・`ISupportDetailView` を作る
2. `OwnedPage`・`SupportDetailPage` に、3.6・3.7 の「…の実装」を足す。**`[SerializeField]` のフィールドは足さない・名前を変えない・消さない**（Prefab の参照が外れるため）。実装に要るフィールドが骨格に無ければ止まって報告する
3. `OwnedPresenter` を 3.6 のとおり作る。閲覧モードの行のタップが `SupportDetailPresenter` を使うので、**3.7 のシグネチャだけを持つ仮の `SupportDetailPresenter`** をこのタスクで作る（`[AssetAddress(nameof(SupportDetailPage))]`、`IScreenWithArgs<SupportDetailArgs>`、コンストラクター 2 本は引数を保存するだけ、`InitializeAsync` は `UniTask.CompletedTask`、`DidPopEnter`・`Dispose` は空、`LevelUpAsync`・`LimitBreakAsync`・`BackAsync` は `UniTask.CompletedTask`）。中身はタスク 4
4. 3.8 のとおり `HomePresenter`・`BootstrapEntry` を直す
5. 5章の `OwnedPresenterTests` を作り、`HomePresenterTests` を直す
確かめ方: コンパイルエラー 0。`OwnedPresenterTests`・`HomePresenterTests` と既存のテスト（P1 の `PrefabContractTests`、P2 の `OwnedPrefabTests` を含む。フィールドを変えていないことの確かめ）が通る。

### タスク 4: サポートカードの詳細の Presenter

触る: `Presentation/Owned/SupportDetailPresenter.cs`（仮を置き換える）、`Tests/EditMode/SupportDetailPresenterTests.cs`。
手順: 3.7 のとおり書く。5章の `SupportDetailPresenterTests` を作る。
確かめ方: `SupportDetailPresenterTests` と既存のテストが通る。

### タスク 5: Play モードの確認（止まる条件あり）

触る: なし（確かめだけ）。
手順: Editor で Play（`Bootstrap` から始まる。サーバーは CLAUDE.md の「ローカルのサーバー」の手順で起動済み）。5章の「Play モード・手動」をユーザーに見てもらう（Play の開始と停止、コンソールの読み出しは [unity7-evaluation.md](../wiki/architecture/unity7-evaluation.md) 4章のコマンド）。コンソールに例外・エラーが無いこと。
止まる条件（当たったら止まって報告する。直し方を自分で決めない）:
- `OwnedPage`・`SupportDetailPage` が Addressables から読めない（Push で例外。P2 の登録か基盤の読み込みの問題）
- 素材の一覧（2 枚目の `OwnedPresenter`）から戻ったときに、`WaitForPopAsync` が返らない
終わったことの確かめ方: 5章の Play の項目を全部見た。Editor が開けないときは「確認できていない」と書いてユーザーに頼む。

### タスク 6: 結果の記録

1. [screen-design.md](../wiki/architecture/screen-design.md)（frontmatter の `updated` を実施日に）: 2章の表の「所持一覧・詳細」と「サポートカードの強化」を、「詳細と強化（レベル上げ・上限解放）は 1 ページ（`SupportDetailPage`）」に直す。3章の図の「所持一覧 ─▶ 詳細 ─▶ 強化」を「所持一覧 ─▶ サポートカードの詳細（強化を含む）」に直し、「上限解放の素材は所持一覧の選択モード（`OwnedArgs`・`OwnedSelectionRequest`・`WaitForPopAsync<OwnedSelection>`。`Home` シーンの中のページ）で選ぶ」を足す。実装メモに、計画とのズレ（なければ「なし」）を書く
2. [produce-domain.md](../wiki/architecture/produce-domain.md) の Shared.Domain の型の一覧（`FormationValidator(編成の検証 5・6)` の行）に `IsPlanCompatible`（プランの適合。クライアントの表示も使う）を足す
3. [idol-support.md](../wiki/systems/idol-support.md): 2.1 の「検証の置き場」の「同じ関数」に `FormationValidator.IsPlanCompatible`（6）と書き添える。「実装メモ」に「上限解放の確認ダイアログは、素材の Lv にかかわらず常に『強化に使った強化pt・ゴールドは戻らない』を出す（2.2.1 の『素材のレベルが 1 より大きいとき』より広い）」と書く
4. screen-wireframes.md 10 の「一覧の並び」の行の後ろに「[Uta案] MVP では育成アイドルは 4 人なので、`IdolId` の昇順に固定し、並び・絞り込みはサポートカードだけに出す（計画）」を足す
5. [unity7-evaluation.md](../wiki/architecture/unity7-evaluation.md): 2章の表・3.3 に、所持一覧（Addressables からのページの読み込み・スクロール・行の Prefab の Instantiate）と詳細の画面で確かめたこと・確かめていないことを足す
6. `python -I .claude/skills/linting-wiki/lint_wiki.py` がエラー 0
7. この計画の frontmatter を `status: done`、`updated` をその日にする

## 5. テスト

置き場・名前空間・書き方は foundation 3.3.7・3.3.8（client/ の EditMode。名前空間 `Uta.Client.Infrastructure.Tests`。偽の View は各テストのファイルの中の `private sealed class`。`FakeScreenNavigator`・`FakeDialogService`・偽の View は同じ `CallLog` を使う）。shared/ は xUnit（既存の `FormationValidatorTests` に足す）。**`OwnedPage`・`SupportDetailPage`（Prefab の View）の EditMode のテストは書かない**（foundation 3.3.7。フィールドと子の対応は P2 の `OwnedPrefabTests`、参照の欠けは P1 の `PrefabContractTests` が見る。表示は Play で見る）。

偽の View の記録の書式（3.3.7 の `"<メソッド名>:<引数を , でつないだもの>"` に従い、リストは `|` でつなぐ）:
- `FakeOwnedView`: `"SetTitle:" + title`、`"SetTabs:" + selected + "," + visible`、`"SetControls:" + visible + "," + sort + "," + order + "," + plan + "," + type + "," + filtersVisible`、`"SetRows:" + string.Join("|", rows の各 r の r.Title + "/" + r.Detail + "/" + r.Enabled)`。`RaiseRowClicked(int)` などでイベントを起こす
- `FakeSupportDetailView`: `"SetInfo:" + string.Join("|", lines)`、`"SetAbilities:" + string.Join("|", lines)`、`"SetResources:" + text`、`"SetLevelButtons:" + plus1 + "," + plus5 + "," + max + "," + 3 つの bool`、`"SetLimitBreak:" + label + "," + enabled`
- 偽のポート `FakeSupportPort : ISupportPort`（返す `PortResult` を public フィールド `LevelUpResult`・`LimitBreakResult` で差し込み、呼ばれた引数を `List<string> Calls` に `"LevelUp:" + uid + "," + targetLevel`／`"LimitBreak:" + uid + "," + materialUid` で記録）は、使うテストのファイルの中に `private sealed class` で書く
- 取り直し（`RefreshPlayerUseCase`）は foundation の `StubPlayerPort` で作る。取り直しの失敗は `SignInResults.Enqueue(PlayerSignInResult.Failure("e"))`（`RefreshPlayerUseCase` が例外を投げる）、成功は `Snapshot` に入れた値

**画面のテスト（`OwnedPresenterTests`・`SupportDetailPresenterTests`）の共通の前提**（各テストの `SetUp` で作る。期待の文字列はこの値から決まる）:
- `FakeGameMaster`（2 本目）:
  - `IdolList`: `new IdolInfo(1, "アイドルA", Rarity.SSR, Plan.Sense, 30, 50, 50, 50, ParamKind.Singing, ParamKind.Rhythm, ParamKind.Expression)`、`new IdolInfo(2, "アイドルB", Rarity.R, Plan.Logic, 30, 50, 50, 50, ParamKind.Singing, ParamKind.Rhythm, ParamKind.Expression)`
  - `SupportCardList`: `new SupportCardInfo(10, "サポートA", Rarity.SSR, SupportType.Singing, Plan.Free)`、`new SupportCardInfo(11, "サポートB", Rarity.SR, SupportType.Rhythm, Plan.Sense)`、`new SupportCardInfo(12, "サポートC", Rarity.R, SupportType.Expression, Plan.Logic)`
  - `Abilities[10] = new List<SupportAbilityInfo> { new SupportAbilityInfo(1, SupportAbilityKind.InitialParam, ParamKind.Singing, 10, 50) }`
  - `MaxLevel = (_, lb) => 40 + lb * 5`、`MaxLimitBreak = _ => 4`、`LevelUpCost = (_, from, to) => new SupportLevelUpCost((to - from) * 100, (to - from) * 50)`
- `PlayerCache` に `Set` する `PlayerSnapshot`: `new PlayerSnapshot(1, 0, 1000, 1000, 0, false, false, idols, cards)`（Gold 1000・SupportPoints 1000）
  - `idols`: `new OwnedIdol(2, 0, 0, 0)`、`new OwnedIdol(1, 0, 0, 3)`（この順。かけらは 1 が 3、2 が 0）
  - `cards`: `new OwnedSupportCard("01A", 10, 3, 0)`、`new OwnedSupportCard("01B", 11, 5, 1)`、`new OwnedSupportCard("01C", 12, 1, 0)`、`new OwnedSupportCard("01D", 10, 1, 0)`（この順）
- `StubPlayerPort.Snapshot` は上と同じスナップショット（取り直しの成功で同じ値が入る。値を変えたいテストは差し替える）
- 期待の行の文字列（3.6 の `Refresh()` の規則で作ったもの。以下この名前で書く）:
  - `IdolRows` = `"アイドルA [SSR]/感性　かけら 3/True|アイドルB [R]/理論　かけら 0/True"`
  - `SupportRowsRarityDesc` = `"サポートA [SSR]/歌唱　共通　Lv 3　解放 0/True|サポートA [SSR]/歌唱　共通　Lv 1　解放 0/True|サポートB [SR]/律動　感性　Lv 5　解放 1/True|サポートC [R]/表現　理論　Lv 1　解放 0/True"`（レア度の降順。同じ SSR の 01A・01D は所持ID の昇順）
- 詳細（`SupportDetailArgs("01A")`）の既定の表示（この順）: `"SetInfo:サポートA|SSR　歌唱　共通|Lv 3 / 40|上限解放 0 / 4"`、`"SetAbilities:1 初期歌唱 +" + ProduceFormulas.AbilityValue(10, 50, 3, 60)`（`fullMaxLevel` = 40 + 4 × 5 = 60）、`"SetResources:強化pt 1,000　ゴールド 1,000"`、`"SetLevelButtons:+1 → Lv 4（強化pt 100 / G 50）,+5 → Lv 8（強化pt 500 / G 250）,最大 → Lv 13（強化pt 1,000 / G 500）,True,True,True"`（最大: 強化pt 1,000 で 10 レベル分）、`"SetLimitBreak:上限解放（素材を選ぶ）,True"`

| ファイル | テスト | 内容 |
|---|---|---|
| `shared/.../FormationValidatorTests.cs` | `IsPlanCompatible_Cases_ReturnsExpected`（`[Theory]`） | `(Sense, Free)` true、`(Sense, Sense)` true、`(Sense, Logic)` false、`(Logic, Sense)` false、`(Logic, Free)` true、`(Sense, Nothing)` false |
| `ServerReasonsTests.cs`（変更） | `Reasons_MatchContractsErrorCodes` に 6 行足す | `ServerReasons.SupportInvalidTargetLevel == SupportErrors.InvalidTargetLevel.Reason`、`SupportNotEnoughPoints`・`SupportMaxLimitBreak`・`SupportNotOwned`・`SupportInvalidMaterial` も同じ、`PlayerNotEnoughGold == PlayerErrors.NotEnoughGold.Reason` |
| `DisplayTextTests.cs` | `RarityName_AllValues` / `PlanName_AllValues` / `SupportTypeName_AllValues` / `ParamName_AllValues` | 各 enum の全部の値（`Nothing` を含む）と、範囲外の値（`(Plan)99` など）が `"-"` |
| | `Ability_InitialParam` / `Ability_ParamBonus_PerThousand` / `Ability_MaxHp` | `"初期歌唱 +49"`、85 → `"歌唱ボーナス +8.5%"`・100 → `"歌唱ボーナス +10.0%"`、`"最大体力 +9"` |
| `SupportLevelPlannerTests.cs` | `Plus1_BelowMax_ReturnsLevelPlusOne` / `Plus5_NearMax_ClampsToMax` / `AtMax_ReturnsCurrentLevel` | `FakeGameMaster.MaxLevel = (_, lb) => 20 + lb * 5` |
| | `Plus5_NotAffordable_StillReturnsTarget` | 強化pt 0 でも Lv 3 → 8 |
| | `Max_AffordableLimited_ReturnsHighestAffordable` | `LevelUpCost = (_, from, to) => new SupportLevelUpCost((to - from) * 100, (to - from) * 50)`、強化pt 250・G 1000 なら Lv + 2 |
| | `Max_CannotAffordAny_ReturnsCurrentLevel` | |
| `SupportSnapshotEditsTests.cs` | `WithLevelUp_ReplacesLevelAndResources` | 該当の 1 枚の Level と Gold・SupportPoints だけ変わり、カードの並び・ほかの値は同じ |
| | `WithLimitBreak_RaisesLimitBreakAndRemovesMaterial` | 該当の LimitBreak が応答の値、素材が消え、ほかの並びは同じ |
| `OwnedSelectionRequestTests.cs` | `ForIdol_NullExcluded_IsEmpty` / `ForSupportCard_NullCollections_AreEmpty` | |
| | `ForLimitBreakMaterial_SetsFields` / `ForLimitBreakMaterial_EmptyTarget_Throws` | |
| `OwnedListBuilderTests.cs` | `BuildIdols_OrderedByIdolId` | `player.Idols` が 3, 1, 2 の順でも 1, 2, 3 |
| | `BuildIdols_Selection_DisablesExcluded` / `BuildIdols_SupportRequest_Throws` | |
| | `BuildSupportCards_SortByLevelDescending_TieByUidAscending` | Lv が同じ 2 枚は所持ID の昇順 |
| | `BuildSupportCards_SortByAcquiredDescending` | 所持ID の降順 |
| | `BuildSupportCards_PlanFilter_ExactMatch` / `BuildSupportCards_TypeFilter` | `Free` で絞ると `Free` のカードだけ |
| | `BuildSupportCards_Selection_DisablesByPriority` | `idolPlan = Sense`。`excludedUids` の 1 枚（プランも合わない）→ `AlreadyInFormation`、`excludedSupportCardIds` の CardId の別の所持（プランも合わない）→ `SameCardInFormation`、`Logic` → `PlanMismatch`、`Free` → 有効 |
| | `BuildSupportCards_Material_ListsSameCardExceptTarget` | 同じ CardId の 3 枚（target を含む）と別の CardId 1 枚 → target 以外の 2 枚だけ、全部有効 |
| | `BuildSupportCards_IdolRequest_Throws` / `BuildSupportCards_Empty_ReturnsEmpty` | |
| `LevelUpSupportUseCaseTests.cs` | `Execute_Success_Refreshes` | `RefreshFailed == false`、キャッシュはサインインの値 |
| | `Execute_SuccessButRefreshFails_PatchesCache` | `StubPlayerPort.SignInResults` に `Failure` → `Succeeded`・`RefreshFailed == true`、キャッシュの該当カードの Level と Gold・SupportPoints が応答の値 |
| | `Execute_NotEnoughPoints_ReturnsStatus` / `Execute_NotEnoughGold_ReturnsStatus` / `Execute_InvalidTarget_ReturnsStatus` / `Execute_NotOwned_ReturnsStatus` | |
| | `Execute_OtherFailure_ReturnsFailed` / `Execute_TargetNotAboveLevel_Throws` | 後者はポートが呼ばれない |
| `LimitBreakSupportUseCaseTests.cs` | `Execute_Success_Refreshes` / `Execute_SuccessButRefreshFails_PatchesCache` | 後者は素材がキャッシュから消え、該当の LimitBreak が応答の値 |
| | `Execute_SameUid_Throws` / `Execute_InvalidMaterial_ReturnsStatus` / `Execute_MaxLimitBreak_ReturnsStatus` / `Execute_NotOwned_ReturnsStatus` | |
| `OwnedPresenterTests.cs` | `InitializeAsync_Browse_ShowsIdolsWithoutControls` | `log` が `"SetTitle:所持一覧"`, `"SetTabs:Idols,True"`, `"SetControls:False,並び: レア度,降順,プラン: 全て,タイプ: 全て,True"`, `"SetRows:" + IdolRows`（IdolId の昇順）の順 |
| | `SelectTab_Support_ShowsControlsAndSortedRows` | `SelectTab(SupportCards)` → 続く記録が `"SetTitle:所持一覧"`, `"SetTabs:SupportCards,True"`, `"SetControls:True,並び: レア度,降順,プラン: 全て,タイプ: 全て,True"`, `"SetRows:" + SupportRowsRarityDesc` |
| | `CycleSortKey_CyclesThreeKeys` | サポートのタブで `CycleSortKey()` を 3 回 → `SetControls` の 2 つ目の値が順に `並び: Lv`・`並び: 入手順`・`並び: レア度`。`並び: Lv`（降順）の行は `"サポートB [SR]/律動　感性　Lv 5　解放 1/True|サポートA [SSR]/歌唱　共通　Lv 3　解放 0/True|サポートC [R]/表現　理論　Lv 1　解放 0/True|サポートA [SSR]/歌唱　共通　Lv 1　解放 0/True"`（Lv 1 の 01C・01D は所持ID の昇順）。`並び: 入手順`（降順）の行は 01D・01C・01B・01A の順 |
| | `ToggleSortOrder_Toggles` | サポートのタブで `ToggleSortOrder()` → `"SetControls:True,並び: レア度,昇順,プラン: 全て,タイプ: 全て,True"`、行は 01C・01B・01A・01D の順 |
| | `CyclePlanFilter_Cycles` | サポートのタブで `CyclePlanFilter()` を 4 回 → `SetControls` の 4 つ目の値が順に `プラン: 共通`・`プラン: 感性`・`プラン: 理論`・`プラン: 全て`。`プラン: 共通` の行は `"SetRows:サポートA [SSR]/歌唱　共通　Lv 3　解放 0/True|サポートA [SSR]/歌唱　共通　Lv 1　解放 0/True"` |
| | `CycleTypeFilter_Cycles` | サポートのタブで `CycleTypeFilter()` を 5 回 → `SetControls` の 5 つ目の値が順に `タイプ: 歌唱`・`タイプ: 律動`・`タイプ: 表現`・`タイプ: アシスト`・`タイプ: 全て`。`タイプ: アシスト` の行は `"SetRows:"`（0 件） |
| | `SelectTab_KeepsSupportFilters` | サポートで `CycleTypeFilter()`（`タイプ: 歌唱`）→ `SelectTab(Idols)` → `SelectTab(SupportCards)` → 最後の `SetControls` が `"SetControls:True,並び: レア度,降順,プラン: 全て,タイプ: 歌唱,True"` |
| | `SelectTab_InSelectionMode_Ignored` | `ForIdol()` の選択モードで `InitializeAsync` のあと `SelectTab(SupportCards)` → 記録が増えない |
| | `SelectRowAsync_BrowseSupport_PushesDetail` | サポートのタブで `SelectRowAsync(1)` → `"PushPage:SupportDetailPresenter"`、`Args[0]` が `SupportDetailArgs` で `SupportCardUid == "01D"`（`SupportRowsRarityDesc` の 2 行目） |
| | `SelectRowAsync_BrowseIdol_DoesNothing` | `log` に `"PushPage:"`・`"PopPage:"` が無い |
| | `SelectRowAsync_Selection_SetsResultAndPops` | `ForIdol()` の選択モードで `SelectRowAsync(1)` → `"PopPage:OwnedPresenter"`、`await CompleteAsync()` の結果が `OwnedSelection` で `IsCancelled == false`・`IdolId == 2` |
| | `SelectRowAsync_Twice_PopsOnce` | 2 回呼ぶ（2 回目は 1 回目の後）→ `"PopPage:"` が 1 回 |
| | `SelectRowThenBack_PopsOnce` | `SelectRowAsync` の後の `BackAsync` で `"PopPage:"` が増えない。結果は選んだもの |
| | `SelectRowAsync_DisabledRow_DoesNothing` / `SelectRowAsync_OutOfRange_DoesNothing` | |
| | `BackAsync_Selection_CompletesCancelled` | `CompleteAsync()` の結果の `IsCancelled == true` |
| | `InitializeAsync_IdolSelection_HidesTabsAndControls` | `ForIdol(new[] { 2 })` → `log` が `"SetTitle:育成アイドルを選ぶ"`, `"SetTabs:Idols,False"`, `"SetControls:False,並び: レア度,降順,プラン: 全て,タイプ: 全て,False"`, `"SetRows:アイドルA [SSR]/感性　かけら 3/True|アイドルB [R]/理論　かけら 0　（編成済み）/False"` の順 |
| | `InitializeAsync_Material_HidesFiltersAndSortsByLevelAscending` | `ForLimitBreakMaterial(10, "01A")` → `log` が `"SetTitle:素材にするカードを選ぶ"`, `"SetTabs:SupportCards,False"`, `"SetControls:True,並び: Lv,昇順,プラン: 全て,タイプ: 全て,False"`, `"SetRows:サポートA [SSR]/歌唱　共通　Lv 1　解放 0/True"` の順（01A 以外の CardId 10 は 01D だけ） |
| | `Dispose_UnsubscribesEvents` | `Dispose()` のあと `RaiseBackClicked()` しても `"PopPage:"` が無い |
| | `SelectRowAsync_AfterDispose_DoesNothing` | `Dispose()` のあと `SelectRowAsync(0)` → `"PushPage:"`・`"PopPage:"` が無い |
| `SupportDetailPresenterTests.cs` | `InitializeAsync_ShowsInfoAbilitiesAndButtons` | `log` が共通の前提の「詳細の既定の表示」の 5 つ（`SetInfo`・`SetAbilities`・`SetResources`・`SetLevelButtons`・`SetLimitBreak`）の順 |
| | `InitializeAsync_AtMaxLevel_ButtonsShowCap` | 01A を `new OwnedSupportCard("01A", 10, 40, 0)` にしたスナップショット → `"SetLevelButtons:+1（上限）,+5（上限）,最大（上限）,False,False,False"` |
| | `InitializeAsync_MaxNotAffordable_ShowsShortage` | SupportPoints 0 のスナップショット → `"SetLevelButtons:+1 → Lv 4（強化pt 100 / G 50）,+5 → Lv 8（強化pt 500 / G 250）,最大（足りません）,True,True,False"`（+1・+5 は所持が足りなくても押せる。3.3 の `PlanTarget`） |
| | `LevelUpAsync_Confirmed_ExecutesAndRefreshes` | `LevelUpResult` に `PortResult<SupportLevelUpResult>.Success(new SupportLevelUpResult(4, 900, 950))` → `InitializeAsync` の後の記録が `"Confirm:Lv 3 → Lv 4 に上げます。\n強化pt 100・ゴールド 50 を使います。"` で始まり、そのあと `SetInfo` から `SetLimitBreak` までの 5 つ（再表示）。`Calls` が `"LevelUp:01A,4"` だけ |
| | `LevelUpAsync_Declined_DoesNotExecute` | `ConfirmResults` に `Negative` → ポートが呼ばれない |
| | `LevelUpAsync_UsesCacheAtClickTime` | 表示の後に 01A を `new OwnedSupportCard("01A", 10, 5, 0)` にしたスナップショットを `cache.Set` して `LevelUpAsync(Plus1)` → 確認が `"Confirm:Lv 5 → Lv 6 に上げます。\n強化pt 100・ゴールド 50 を使います。"`、`Calls` が `"LevelUp:01A,6"` |
| | `LevelUpAsync_NotEnoughPoints_ShowsInfo` | `"Info:強化ptが足りません。"` |
| | `LimitBreakAsync_NoMaterial_ShowsInfo` | `"Info:素材にできる同じカードがありません。"`、`"PushPage:"` が無い |
| | `LimitBreakAsync_SelectsMaterialThenConfirms` | `PopResults` に `OwnedSelection.ForSupportCard("01D")`。`log` が `"PushPage:OwnedPresenter"`, `"WaitForPop"`, `"Confirm:Lv 1 の「サポートA」を素材にして上限解放します（0 → 1）。\n素材は無くなります。強化に使った強化pt・ゴールドは戻りません。"` の順、`Calls` が `"LimitBreak:01A,01D"`、`Args[0]` が `OwnedArgs` で `Selection.Kind == LimitBreakMaterial`・`MaterialCardId == 10`・`MaterialTargetUid == "01A"` |
| | `LimitBreakAsync_SelectionCancelled_DoesNotExecute` | `PopResults` が空（null）→ `"Confirm:"` が無く、ポートが呼ばれない |
| | `LimitBreakAsync_AtMax_DoesNothing` | 01A を `new OwnedSupportCard("01A", 10, 3, 4)` にしたスナップショット → `"SetLimitBreak:上限解放（最大）,False"`。`LimitBreakAsync()` のあと `"PushPage:"` も `"Info:"` も無い |
| | `Refresh_CardGone_DisablesAll` | `SupportDetailArgs("01Z")`（所持に無い）→ `log` が `"SetInfo:このカードは所持していません。"`, `"SetAbilities:"`, `"SetResources:"`, `"SetLevelButtons:+1,+5,最大,False,False,False"`, `"SetLimitBreak:上限解放,False"` の順 |
| | `LevelUpAsync_AfterDispose_DoesNothing` | `Dispose()` のあと `LevelUpAsync(Plus1)` → `"Confirm:"` が無く、ポートが呼ばれない |
| `HomePresenterTests.cs`（変更） | `OwnedClicked_ShowsNotReady` を `OwnedClicked_PushesOwnedBrowse` に差し替える | `InitializeAsync` のあと `RaiseOwnedClicked()` → `InitializeAsync` の後の記録が `"PushPage:OwnedPresenter"` だけ。ほかのボタンのテストは変えない（foundation の決めたこと 16） |
| | `OpenOwnedAsync_PushesOwnedBrowse`（追加） | `"PushPage:OwnedPresenter"`、`Args[0]` が `OwnedArgs` で `Selection == null` |

Play モード・手動（タスク 5。Editor、サーバー起動済み。初期付与済みのプレイヤーに、ガチャで同じサポートカードを 2 枚以上引かせておく）:
- タイトル → ホームの「アイドル」で所持一覧（`Home` シーンのまま。ヘッダーに「＜ 所持一覧」、タブ 2 つ）。育成アイドルのタブは IdolId の順で、並び・絞り込みの 2 段が出ない。選んでいるタブに下線が出る
- 「サポート」のタブで、並び・昇順/降順・プラン・タイプの 2 段が出て、押すとラベルと並びが変わる。絞り込みで 0 件になると一覧の下に「ありません」が出て、戻すと消える
- サポートカードの行をタップ → 詳細。上から名前・レア度などの 4 行、強化pt・ゴールド、+1・+5・最大（ラベルが 1 行に入る）、上限解放、「アビリティ」と「1 初期歌唱 +N」の形
- 「+1」→ 確認 → Lv が上がり、強化pt・ゴールドが減る。「最大」で足りる範囲の最大まで上がる
- 強化ptが足りないときの「+5」→ 確認 →「強化ptが足りません。」
- 同じカードを 2 枚持った状態で「上限解放（素材を選ぶ）」→ 素材の一覧（見出し「素材にするカードを選ぶ」、タブなし、プラン・タイプの段なし、同じカードだけ）→ 選ぶ → 確認 → 解放数が増え、「＜」で所持一覧に戻ると素材が消えている
- 素材の一覧で「＜」→ 詳細に戻り、何も起きない
- 所持一覧で「＜」→ ホームに戻り、ホームの表示（プレイヤーLv・ガチャ通貨）が今の値
- 選択モード（育成アイドル・サポートカード）は 4 本目の編成で確かめる

## 6. 未決事項

いまは ready。ユーザーの確認は全件了承済み（2026-11-07）。

なし。（`HomePage.resumePanel` の型は P2 で `GameObject` に統一済み。）

## 7. 実装メモ

| タスク | コミット | ズレ |
|---|---|---|
| 1 | 02926fc | なし（OwnedFixtures の Idol・SupportCard の行は private の補助で作った） |
| 2 | 4266039 | なし |
