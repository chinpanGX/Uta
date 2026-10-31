---
title: ガチャのマスタ
status: draft
updated: 2026-10-31
---

# ガチャのマスタ

ゲームの仕様は [gacha.md](../../systems/gacha.md)。ここはテーブル・列・enum・区分・検証の正（2026-10-31 に gacha.md 3.1 から移した。中身は変えていない）。全テーブルの索引は [README.md](README.md)。

## 1. テーブルと列 [決定。2026-10-31]

名前は仮。[masterdata-pipeline.md](masterdata-pipeline.md) の規約に従う):

| テーブル | 主キー | 列 |
|---|---|---|
| `Gacha` | `GachaId` | `SingleCost`、`TenCost`、`NormalTableId`、`GuaranteedTableId`、`CeilingPoints`(交換に要る交換pt) |
| `GachaTable` | `GachaTableId` | (見出し) |
| `GachaTableEntry` | `GachaTableId` + `Order` | `ItemKind`(enum。下)、`ItemId`、`Weight`(1 以上) |
| `GachaCeilingItem` | `GachaId` + `Order` | `ItemKind`、`ItemId` |
| `DuplicateConversion` | `ItemKind` + `Rarity` | `Amount`(育成アイドルはかけらの量、サポートカードは完凸後の強化pt の量。[gacha.md](../../systems/gacha.md) 5章・[idol-support.md](../../systems/idol-support.md) 3章の値) |

- **`ItemKind` と `Outcome` の正はここ** [決定。2026-10-31]: `ItemKind` は `Nothing` = 0 / `Idol` = 1 / `SupportCard` = 2、`Outcome` は `Nothing` = 0 / `New` = 1 / `Duplicate` = 2。マスタの enum(`masterdata/Definitions` の写し。[masterdata-pipeline.md](masterdata-pipeline.md) 2章)と DTO の enum(`Uta.Shared`)は同じ名前・同じ数値で別に定義し、DB の `item_kind`・`outcome` には数値(`smallint`)を入れる。`Nothing` はマスタ・DTO・履歴のどこにも入れない(検証で弾く)
- 区分: どれもクライアントで確率を表示するので Common(クライアントにも出す)。`DuplicateConversion` も、結果の画面で量を見せるので Common
- 育成アイドルとサポートカードを 1 つの列 `ItemId` で指す(専用の参照列にしない)のは、DTO と履歴の形(`ItemKind` + `ItemId`)にそろえるため。存在の検証は `[Exists]` ではなく `IValidatable` で、`ItemKind` ごとのマスタを引いて行う
- 検証(`IValidatable`): `ItemKind` が `Nothing` でない、重みが 1 以上、テーブルごとの重みの合計が `int.MaxValue` 以下(`RandomOps.WeightedIndex` の制約)、テーブルが空でない、`ItemId` が存在する、確定枠のテーブルに R がない(`ItemId` のマスタの `Rarity` を見る)

## 未決事項

なし（各システムのページと [masterdata-pipeline.md](masterdata-pipeline.md) の未決事項を見る）

## 実装メモ

(実装後に設計とのズレをここに記録する)
