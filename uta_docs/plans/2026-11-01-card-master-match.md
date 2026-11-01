---
title: 実装計画: マスタのカードを CardDefinition に組み立て、テストのカードと一致を確かめる
status: ready
updated: 2026-11-01
spec: ../wiki/architecture/lesson-phases.md, ../wiki/architecture/lesson-domain-types.md, ../wiki/architecture/repository-layout.md, ../wiki/architecture/master/masterdata-pipeline.md
---

# 実装計画: マスタのカードを CardDefinition に組み立て、テストのカードと一致を確かめる

## 1. 目的と前提

[lesson-phases.md](../wiki/architecture/lesson-phases.md) 1章の「マスタができたら、テストのカードとマスタの値が一致することを確かめるテストを 1 本置く」を実現する。マスタの行から `CardDefinition` を作る処理がまだ無いので、先にそれを作る。

**組み立ての置き場は [repository-layout.md](../wiki/architecture/repository-layout.md) 4章の決定（2026-10-31）に従う**: 各側の Infrastructure は、マスタの行の値をそのまま `Uta.Shared.Domain` の**行の型**に詰めて渡すだけにし、グループ ID の解決と入れ子（`EffectDef` の子）の組み立ては **`Uta.Shared.Domain` の関数 1 つ**で行う（クライアントとサーバーで食い違わないため）。この計画では Shared.Domain の関数と、サーバー側の詰め替えを作る。クライアント側の詰め替えは別の計画。

- 関係する ADR: [0007](../wiki/decisions/0007-masterdata-with-addons.md)、[0008](../wiki/decisions/0008-shared-domain-and-onion.md)
- テストのカード ID はマスタの `CardId` に合わせてある（2026-11-01）
- **一致テストの置き場**: `server/Uta.Server.Tests/MasterData/` [ユーザーの判断。2026-11-01]。`Uta.Server.Tests` から `Uta.Shared.Domain.Tests`（Exe の xunit.v3 のテスト）を ProjectReference で参照する。2026-11-01 に試して、`dotnet build server/Uta.Server.Tests` が警告 0・エラー 0、`MasterData` の名前空間のテスト 54 本の成功を確かめた。実装で問題が出たら**止めて報告する**（代わりの置き方を実装担当が選ばない）
- **二重の定義（テストのカード）をなくさない理由**: テストのカードを読める値で持つことで期待値のテスト（`Phase*CardsTests`）が読め、合成カード（ID 901 以上）はマスタにない。マスタから作ると、Shared.Domain.Tests がマスタ（server 側の生成物）に依存するか、MasterMemory を shared に入れる必要がある（ADR 0007・0008 に反する）。一致テストが「ずれ」を見つける役を担う。二重の定義をなくすか（テストのカードをマスタから作る）は、この計画では決めない。lesson-phases.md 1章の「マスタができたら一致テストを 1 本置く」を実現するだけで、暫定の扱いは変えない
- **一致テストが落ちたとき**: テストのカードとマスタ（CSV）のどちらが正しいかは lesson-card.md 14章の表で決まる。実装担当は**直さずに止まり、どの項目がずれたかを報告する**
- **決めないこと**: 二重の定義をなくすこと／クライアント側の詰め替え／サーバーの他の機能（ガチャ・育成）が `ICardCatalog` を使うこと／`UnlockLv`（テストのカードは 1 固定でマスタは 1・5・10。一致テストは比べない）／F08／マスタの `Card.CardId` の採番の規則

## 2. 触るファイル

`D` = `shared/Uta.Shared.Domain/Lesson`、`DT` = `shared/Uta.Shared.Domain.Tests/Lesson`、`S` = `server/Uta.Server/Infrastructure`、`ST` = `server/Uta.Server.Tests/MasterData`。

| 操作 | パス | 中身 | タスク |
|---|---|---|---|
| 変更 | `D/LessonEnums.cs` | `CardFaceKind` を足す | 1 |
| 作成 | `D/CardRows.cs`・`D/CardRows.cs.meta` | 行の型 5 つ | 1 |
| 作成 | `D/CardCatalog.cs`・`D/CardCatalog.cs.meta` | 行から `CardDefinition` を組み立てる `ICardCatalog` | 1 |
| 作成 | `DT/CardCatalogTests.cs` | 組み立てのテスト | 1 |
| 作成 | `S/MasterData/MasterCardCatalog.cs` | `MemoryDatabase` の行を行の型に詰めて `CardCatalog` を作る | 2 |
| 作成 | `ST/MasterCardCatalogTests.cs` | 詰め替えのテスト | 2 |
| 変更 | `ST/MasterDataTests.cs` | `EnumPairs` に `CardFaceKind` を足す | 2 |
| 変更 | `server/Uta.Server.Tests/Uta.Server.Tests.csproj` | `Uta.Shared.Domain.Tests` への ProjectReference | 3 |
| 作成 | `ST/CardDefinitionDescriber.cs`・`ST/TestCardsMatchMasterTests.cs` | 一致テスト | 3 |

`.cs.meta` は既存の `.meta`（例 `D/StatusSet.cs.meta`）と同じ形（`fileFormatVersion: 2` と新しい `guid:`）。

## 3. 型とシグネチャ

### 3.1 Shared.Domain（C# 9、netstandard2.1、`float`/`double` なし、`TreatWarningsAsErrors`）

```csharp
// D/LessonEnums.cs に足す（マスタの CardFaceKind と同じ名前・数値）
public enum CardFaceKind { Nothing = 0, Normal = 1, Enhanced = 2 }

// D/CardRows.cs（namespace Uta.Shared.Domain.Lesson）。マスタの行と同じ列。readonly struct、コンストラクタですべて受け取り、読み取り専用プロパティで返す（プロパティ名は列名の PascalCase）
public readonly struct CardRow
{
    public CardRow(int cardId, Plan plan, CardType type, Rarity rarity, int unlockLv, bool onceInLesson, bool unique, bool startInHand);
}
public readonly struct CardFaceRow
{
    public CardFaceRow(int cardId, CardFaceKind face, int effectGroupId, int useConditionGroupId);   // useConditionGroupId 0 は条件なし
}
public readonly struct CardCostRow
{
    public CardCostRow(int cardId, CardFaceKind face, int order, CostKind kind, int value);
}
public readonly struct EffectRow
{
    public EffectRow(int effectGroupId, int order, EffectOp op, StatusKind status, PersistentTrigger trigger,
        int value, int arg1, int arg2, int arg3, int refConditionGroupId, int refEffectGroupId, int refCardId);   // ref* の 0 は空欄
}
public readonly struct ConditionRow
{
    public ConditionRow(int conditionGroupId, int order, ConditionSubject subject, Comparator comparator, int value);
}

// D/CardCatalog.cs
public sealed class CardCatalog : ICardCatalog
{
    /// <summary>行から全カードを組み立てる。問題があれば InvalidOperationException（メッセージに問題を全部入れる）。</summary>
    public static CardCatalog Create(
        IReadOnlyList<CardRow> cards,
        IReadOnlyList<CardFaceRow> faces,
        IReadOnlyList<CardCostRow> costs,
        IReadOnlyList<EffectRow> effects,
        IReadOnlyList<ConditionRow> conditions);

    public IReadOnlyList<int> CardIds { get; }      // 昇順
    public CardDefinition Get(int cardId);          // 無ければ KeyNotFoundException
}
```

### 3.2 `CardCatalog.Create` の規則

1. 引数のどれかが null なら、ほかの処理の前に即 `ArgumentNullException`（エラーはまとめない）。
2. `cards` の各行から `CardDefinition(cardId, plan, type, rarity, unlockLv, onceInLesson, unique, startInHand, baseFace, enhancedFace)` を作る。`cardId` が重複したら `InvalidOperationException`。
3. 面: `faces` から同じ `CardId` で `Face == Normal` の行が通常版（無ければエラー）、`Face == Enhanced` の行が強化版（無ければ `null`）。同じ `(CardId, Face)` が 2 行あればエラー。
4. `CardFace(costs, useConditions, effects)`:
   - `costs`: `costs` の同じ `CardId` と `Face` の行を `Order` の昇順に `new CostDef(kind, value)`
   - `useConditions`: `UseConditionGroupId == 0` なら空。ほかは条件のグループ
   - `effects`: 効果のグループ `EffectGroupId`
5. 条件のグループ: `conditions` の同じ `ConditionGroupId` の行を `Order` の昇順に `new ConditionDef(subject, comparator, value)`。0 件ならエラー。
6. 効果のグループ: `effects` の同じ `EffectGroupId` の行を `Order` の昇順に `new EffectDef(op, status, trigger, value, arg1, arg2, arg3, refCardId, conditions, children)`。`conditions` は `RefConditionGroupId == 0` なら `null`、ほかは条件のグループ。`children` は `RefEffectGroupId == 0` なら `null`、ほかは効果のグループ（再帰。深さ 1 はマスタの検証が保証するが、循環で無限再帰しないよう、効果のグループをたどる深さを、面から呼ぶ最初のグループを 1 と数えて、5 に入ろうとしたらエラー）。0 件ならエラー。
7. エラーのあったカード（通常版の面がない、グループが 0 件、など）は作らず、作れたカードだけを `CardDefinitionValidator.Validate` にかけ、エラーがあれば集める（[lesson-phases.md](../wiki/architecture/lesson-phases.md) 1章「未実装のプリミティブはマスタ読み込み時にエラー」）。1〜7 のエラーは最後にまとめ、1 件でもあれば `InvalidOperationException`（メッセージは 1 件ずつ改行で連結。文言の例: `カード <id>: 通常版の面がない`、`効果グループ <id> がない`、`条件グループ <id> がない`、`カード <id> が重複`。検証のエラーは `CardDefinitionValidator` の文言のまま）。`Face` が `Nothing` の面、存在しない `CardId` を指す面・コストの行は無視する。`CostDef` のコンストラクタが `Value < 0` で投げる例外は、マスタが `Value >= 0` を検証済みなのでそのまま通してよい。
8. `CardIds` は昇順の配列（`IReadOnlyList<int>`）。

### 3.3 サーバーの詰め替え（C# 14 可。ファイルスコープ namespace）

```csharp
// S/MasterData/MasterCardCatalog.cs   namespace Uta.Server.Infrastructure;
public static class MasterCardCatalog
{
    public static CardCatalog Create(MemoryDatabase db);   // MasterMemory.Addons の MemoryDatabase（MasterDataLoader.Load() の戻り値）
}
```

`Uta.MasterData` と `Uta.Shared.Domain.Lesson` は同名の型（`Plan`・`CardType`・`Rarity`・`CostKind`・`ConditionSubject`・`Comparator`・`EffectOp`・`StatusKind`・`PersistentTrigger`・`CardFaceKind`・`CardFace`）を持つので、**`using Uta.MasterData;` は書かず**、マスタの型は `Uta.MasterData.Xxx` と完全修飾する（`MemoryDatabase` は `using MasterMemory.Addons;`。`MasterDataLoader` と同じ）。Domain の型は `using Uta.Shared.Domain.Lesson;`。**これは `ST/` のテスト 3 ファイルも同じ**（既存の `MasterDataTests` は `using Uta.MasterData;` を書いているが、新しいファイルでは書かない）。

`db.CardTable.All`・`db.CardFaceTable.All`・`db.CardCostTable.All`・`db.EffectTable.All`・`db.ConditionTable.All` の各行を、列をそのまま（enum は `(Domain の型)(int)マスタの値`）行の型に詰めて、`CardCatalog.Create` に渡す。複合キーの検索メソッドは使わない（生成コードに依存せず、Shared.Domain の関数に任せるため）。列は [masterdata/Definitions/Lesson.cs](../../masterdata/Definitions/Lesson.cs) の同名のプロパティ（`Card`: `CardId, Plan, Type, Rarity, UnlockLv, OnceInLesson, Unique, StartInHand`／`CardFace`: `CardId, Face, EffectGroupId, UseConditionGroupId`／`CardCost`: `CardId, Face, Order, Kind, Value`／`Effect`: `EffectGroupId, Order, Op, Status, Trigger, Value, Arg1, Arg2, Arg3, RefConditionGroupId, RefEffectGroupId, RefCardId`／`Condition`: `ConditionGroupId, Order, Subject, Comparator, Value`）。

## 4. タスク

**順番に実行する。** 終わるたびに `dotnet build Uta.slnx`（警告 0・エラー 0）、`dotnet test --project shared/Uta.Shared.Domain.Tests`、`dotnet test --project server/Uta.Server.Tests` が通ること。サーバーのテストの結合テストは Docker が要る（Testcontainers）。Docker が無くて結合テストだけが失敗するなら、`dotnet test --project server/Uta.Server.Tests --filter-namespace "Uta.Server.Tests.MasterData"` が通ることを確かめて、そのことを報告する（結合テスト以外の失敗は直す）。

### タスク 1: Shared.Domain の組み立て

触る: `D/LessonEnums.cs`・`D/CardRows.cs`・`D/CardCatalog.cs`（と `.meta`）・`DT/CardCatalogTests.cs`。3.1・3.2 のとおり作る。テストは 5 章。`CardFaceKind` の追加は末尾に足すだけで、既存の enum は変えない。

### タスク 2: サーバーの詰め替え

触る: `S/MasterData/MasterCardCatalog.cs`・`ST/MasterCardCatalogTests.cs`・`ST/MasterDataTests.cs`。3.3 のとおり作る。`MasterDataTests.EnumPairs` に `{ typeof(Uta.MasterData.CardFaceKind), typeof(Uta.Shared.Domain.Lesson.CardFaceKind) }` を足す。テストは 5 章。

### タスク 3: 一致テスト

触る: `Uta.Server.Tests.csproj`（`<ProjectReference Include="..\..\shared\Uta.Shared.Domain.Tests\Uta.Shared.Domain.Tests.csproj" />` を足す）、`ST/CardDefinitionDescriber.cs`・`ST/TestCardsMatchMasterTests.cs`（作成）。

- `CardDefinitionDescriber`（`internal static class`、namespace `Uta.Server.Tests.MasterData`）: `public static string Describe(CardDefinition card)`。形: `card=<Id>|<Plan>|<Type>|<Rarity>|<OnceInLesson>|<Unique>|<StartInHand>|base:<面>|enh:<面 または none>`。enum は `(int)`、真偽は `1`/`0`、整数は `ToString(CultureInfo.InvariantCulture)`。**`UnlockLv` は含めない**。面: `costs[<kind>:<value>;…] conds[<subject>:<comparator>:<value>;…] effects[<効果>;…]`。効果: `(<op>,<status>,<trigger>,<value>,<arg1>,<arg2>,<arg3>,<refCardId>,c[<条件>;…],e[<子の効果>;…])`（子は同じ形で再帰）。
- 一致テストが落ちたら、直さずに止まって報告する（1章）。

## 5. テスト

`CardCatalogTests`（タスク 1。`DT`。手で作った少数の行で組み立てる）:

- `Create_OneCardWithBothFaces_BuildsDefinition`: 1 枚（`Plan.Free`・`CardType.Active`・`Rarity.N`・once なし）、通常版 `Consume` 4・`GainParam` 9、強化版 `Consume` 3・`GainParam` 14 → `Get` の `Base`・`Enhanced` のコスト・効果が一致
- `Create_NoEnhancedFace_EnhancedIsNull`: 強化版の行なし → `Enhanced` が `null`
- `Create_CostsAndEffects_OrderedByOrder`: `Order` を逆順に渡す → `Order` の昇順になる
- `Create_UseConditionGroup_BuildsConditions`: `UseConditionGroupId` 付きの面 → `UseConditions` が条件の行どおり
- `Create_EffectWithCondition_BuildsEffectConditions`: `RefConditionGroupId` 付きの効果 → `Conditions` が 1 件
- `Create_PersistentWithChildGroup_BuildsChildren`: `AddPersistent` の `RefEffectGroupId` → `Children` が子の効果の行どおり
- `Create_MissingNormalFace_Throws`・`Create_DuplicateCardId_Throws`・`Create_MissingEffectGroup_Throws`・`Create_MissingConditionGroup_Throws`・`Create_CyclicEffectGroup_Throws`（グループ A の効果が `RefEffectGroupId` で A 自身を指す）: `InvalidOperationException`
- `Create_UnsupportedOp_ThrowsWithValidatorMessage`: `EffectOp.AddParamBonus` の効果 → 例外のメッセージが `未対応の効果` を含む
- `Create_NullArgument_Throws`: 5 つの引数それぞれを null にすると `ArgumentNullException`
- `Get_Unknown_ThrowsKeyNotFound`・`CardIds_Ascending`

`MasterCardCatalogTests`（タスク 2。`ST`。`MasterDataLoader.Load()` の `MemoryDatabase` で `MasterCardCatalog.Create(db)`）:

- `Create_Mvp_Has36Cards`: `CardIds` が 36 件・昇順・先頭 1・末尾 215
- `Get_F01_HasNormalAndEnhancedFaces`: `Get(1)` の `Plan` が `Free`、`Type` が `Active`、`Rarity` が `N`、通常版のコスト 1 件（`Consume` 4）、効果 1 件（`GainParam` の `Value` 9）、強化版のコスト `Consume` 3・効果 `GainParam` 14
- `Get_S14_HasConditionedEffect`: `Get(114)` の通常版の効果 2 件、2 件目の `Conditions` が 1 件（`Focus` `GreaterOrEqual` 3）
- `Get_S13_HasPersistentWithChild`: `Get(113)` の通常版の効果に `AddPersistent`（`Trigger` が `OnActiveCardUse`）があり、`Children` が 1 件（`AddStatus` の `Focus`、`Value` 1）
- `Get_S04_HasUseCondition`: `Get(104)` の通常版の `UseConditions` が 1 件（`GoodConditionTurns` `GreaterOrEqual` 2）

`TestCardsMatchMasterTests`（タスク 3）:

- `CardIds_MatchBetweenTestCardsAndMaster`: `Phase1Cards.AllIds` + `Phase2Cards.AllIds` + `Phase3Cards.AllIds` + `Phase4Cards.AllIds`（`Uta.Shared.Domain.Tests.Lesson`）を昇順に並べた列が、`MasterCardCatalog.Create(...).CardIds` と等しい（どちらも 36 件）
- `Card_TestCardMatchesMaster`（`Theory`。データは `public static TheoryData<int>` を `[MemberData]` で渡す。中身は 4 つの `AllIds` を連結して昇順にした 36 件。マスタ側の `CardCatalog` は `private static readonly` で 1 回だけ作る。`MasterDataTests` と同じ形）: テストのカード（`Phase1Cards.All()`〜`Phase4Cards.All()` を連結して `Id` で引く）の `CardDefinitionDescriber.Describe` が、マスタのカードの `Describe` と等しい

## 6. 未決事項

なし。

## 7. 実装後の wiki の更新（メインのセッションが行う）

- [lesson-domain-types.md](../wiki/architecture/lesson-domain-types.md): 未決事項の「マスタの行を受け取る型と…組み立てる関数」を実装済みにし、2章に行の型と `CardCatalog` を足す
- [repository-layout.md](../wiki/architecture/repository-layout.md) 10.4: `Uta.Server.Tests` が `Uta.Shared.Domain.Tests` を参照することを書く
- [lesson-phases.md](../wiki/architecture/lesson-phases.md) 1章: 一致テストを置いたこと、暫定が終わる条件
- [lesson-enums.md](../wiki/architecture/lesson-enums.md): `CardFaceKind` を足す
- [masterdata-pipeline.md](../wiki/architecture/master/masterdata-pipeline.md): 実装メモ
