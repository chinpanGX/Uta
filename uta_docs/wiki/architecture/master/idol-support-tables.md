---
title: 育成アイドル・サポートカードのマスタ
status: draft
updated: 2026-11-01
---

# 育成アイドル・サポートカードのマスタ

ゲームの仕様は [idol-support.md](../../systems/idol-support.md)。ここはテーブル・列・区分の正（2026-10-31 に idol-support.md 6章から移した。中身は変えていない）。全テーブルの索引は [README.md](README.md)。

## 1. テーブルと列 [決定。2026-10-31。名前は仮]

列の型は [masterdata-pipeline.md](masterdata-pipeline.md) 3章の規約(`int` / `long` / `string` / `bool` / enum)。enum はすべて 0 を `Nothing` にする([lesson-enums.md](../lesson-enums.md)「決まっていること」)。`Rarity`・`Plan`・`ParamKind` は `Uta.Shared.Domain` の enum の写しを `masterdata/Definitions` に置く([masterdata-pipeline.md](masterdata-pipeline.md) 2章。2026-10-31 に「`Uta.Shared.Domain` の enum をそのまま使う」から直した)。空欄は `Nothing` になるので、必須の列は `Nothing` でないことを検証する。そのうえで `Idol.Plan` は `Free` 以外、`Idol.Rarity`・`SupportCard.Rarity` は R 以上。`SupportAbility` の `MaxHp` の行の `Param` は `Nothing` にする(ほかの `Kind` では `Nothing` 以外)。MVP で使うものだけを書く。具体の行は masterdata の最初の実装計画で作る([game-spec.md](../../overview/game-spec.md) MVP の最小のコンテンツ)。

| テーブル | 主キー | 列 | 区分 |
|---|---|---|---|
| `Idol` | `IdolId` | `Name`、`Rarity`(enum)、`Plan`(enum)、`Recommend`(enum: `Nothing` / `GoodCondition` / `Focus` / `Impression` / `Motivation`)、`MaxHp`、`InitialSinging` / `InitialRhythm` / `InitialExpression`、`BonusSingingPerThousand` / `BonusRhythmPerThousand` / `BonusExpressionPerThousand`、`ExamPatternId`、`FirstParam` / `SecondParam` / `ThirdParam`(`ParamKind`。審査比率の 1〜3 番)、`UniqueCardId` | Common |
| `IdolRecommendDeck` | `Recommend` + `Order` | `CardId`([idol-support.md](../../systems/idol-support.md) 1.3 の表。1 つのおすすめ効果に 3 行。同じカードは 2 行) | Common |
| `SupportCard` | `SupportCardId` | `Name`、`Rarity`、`Type`(enum `SupportType`: `Nothing` / `Singing` / `Rhythm` / `Expression` / `Assist`)、`Plan` | Common |
| `SupportAbility` | `SupportCardId` + `Order` | `Kind`(enum `SupportAbilityKind`: `Nothing` / `InitialParam` / `ParamBonus` / `MaxHp`)、`Param`(`ParamKind`。`MaxHp` の行は `Nothing`)、`ValueAtLv1`、`ValueAtMax`(`ParamBonus` は千分率) | Common |
| `SupportLevelCost` | `Rarity` + `Level` | `SupportPoints`、`Gold`(Level → Level+1 に要る量) | Common |
| `SupportLevelCap` | `Rarity` + `LimitBreak` | `MaxLevel` | Common |
| `InitialSupportCard` | `Order` | `SupportCardId` | Common |
| `InitialSetting` | `Id`(1 行だけ) | `GachaCurrency`(初期付与。2,500)、`Gold`(10,000 [仮])、`SupportPoints`(10,000 [仮]) | Common |
| `CommonDeckCard` | `Order` | `CardId`(初期デッキの共通の 5 枚。[idol-support.md](../../systems/idol-support.md) 1.3 の F01×2・F02×1・F03×2 の 5 行) | Common |

- 完凸後の重複の変換量は、ガチャの `DuplicateConversion`([gacha-tables.md](gacha-tables.md))に置く
- [決定。2026-11-01。ユーザーの判断]
  - enum の名前は列の名前と分ける(`SupportType`・`SupportAbilityKind`)。`Type` は `System.Type` とぶつかり、`Kind` はほかのテーブルの列の enum と同じ名前空間でぶつかるため
  - `SupportAbility` は縦持ち(アビリティ 1 つで 1 行)のままにする。`MaxHp` の行の `Param` は `Nothing` で、ほかの `Kind` では `Nothing` 以外(`IValidatable`)
  - `InitialSupportCard`・`InitialSetting` は MVP では Common にする(それまでは ServerOnly)。クライアントに見えても困る値ではなく、ServerOnly の検証がアドオンでまだ確かめられていないため
- 初期デッキのカード ID は、[lesson-card.md](../../systems/lesson-card.md) 14章の記号に対応するマスタのカード ID を `CommonDeckCard`・`IdolRecommendDeck`・`UniqueCardId` に書く

## 未決事項

なし（各システムのページと [masterdata-pipeline.md](masterdata-pipeline.md) の未決事項を見る）

## 実装メモ

- 実装 [2026-11-01。計画 `plans/2026-11-01-masterdata-rest.md`]: `masterdata/Definitions/IdolSupport.cs`、CSV 9 ファイル。育成アイドル 4 人・サポートカード 10 種。**名前とステータス・アビリティは仮の値**（ユーザーの許可。レアリティごとに段階を付け、SSR は参考作品の最大値に合わせた）
- この計画で名前を付けた enum: `RecommendKind`（`GoodCondition` / `Focus` / `Impression` / `Motivation`）、`SupportType`（`Singing` / `Rhythm` / `Expression` / `Assist`）、`SupportAbilityKind`（`InitialParam` / `ParamBonus` / `MaxHp`）。0 は `Nothing`、あとは書かれた順に 1 から
- 検証の置き場: 行の検査は `IValidatable`、表をまたぐ検査はサーバーのテスト（`MasterDataInvariantTests`）。上に加えて次を検査する: `Idol.Recommend` はプランと合う（`Sense` は `GoodCondition` / `Focus`、`Logic` は `Impression` / `Motivation`）、`UniqueCardId` のカードの `Plan` はアイドルと同じか `Free`、`IdolRecommendDeck` は 4 つのおすすめ効果それぞれに `Order` 1〜3、`CommonDeckCard` は 5 行、`SupportLevelCost` の最後の `Level` + 1 が `SupportLevelCap` の `LimitBreak` 4 の `MaxLevel` と等しい、`SupportLevelCap` はレアごとに 5 行で `MaxLevel` が単調増加、`InitialSupportCard` は重複なし
- `supportLevelCost`（147 行）は `基準 × Level`（R 39・SR 65・SSR 117）で、`Gold` は `SupportPoints` と同じ。テストで全行の式を確かめる
