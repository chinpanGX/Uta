---
title: ガチャのマスタ
status: draft
updated: 2026-11-01
---

# ガチャのマスタ

ゲームの仕様は [gacha.md](../../systems/gacha.md)。ここはテーブル・列・enum・区分・検証の正（2026-10-31 に gacha.md 3.1 から移した。中身は変えていない）。全テーブルの索引は [README.md](README.md)。

## 1. テーブルと列 [決定。2026-10-31]

名前は仮。[masterdata-pipeline.md](masterdata-pipeline.md) の規約に従う。

| テーブル | 主キー | 列 |
|---|---|---|
| `Gacha` | `GachaId` | `SingleCost`、`TenCost`、`NormalTableId`、`GuaranteedTableId`、`CeilingPoints`(交換に要る交換pt) |
| `GachaDetailTable` | `GachaTableId` | (排出テーブルの見出し。定義クラス名を `GachaTable` にしないのは、MasterMemory が表 `Gacha` の型を `GachaTable` と生成して衝突する（CS0101）ため。生成される型は `GachaDetailTableTable` [決定。2026-11-01。ユーザーの提案]) |
| `GachaTableEntry` | `GachaTableId` + `Order` | `ItemKind`(enum。下)、`IdolId`(`[Exists(typeof(Idol), AllowEmpty = true)]`)、`SupportCardId`(`[Exists(typeof(SupportCard), AllowEmpty = true)]`)、`Weight`(1 以上) |
| `GachaCeilingItem` | `GachaId` + `Order` | `ItemKind`、`IdolId`、`SupportCardId`(`GachaTableEntry` と同じ属性) |
| `DuplicateConversion` | `ItemKind` + `Rarity` | `Amount`(育成アイドルはかけらの量、サポートカードは完凸後の強化pt の量。[gacha.md](../../systems/gacha.md) 5章・[idol-support.md](../../systems/idol-support.md) 3章の値) |

- **`ItemKind` の正はここ** [決定。2026-10-31]: `Nothing` = 0 / `Idol` = 1 / `SupportCard` = 2。マスタの enum(`masterdata/Definitions` の写し。[masterdata-pipeline.md](masterdata-pipeline.md) 2章)と DTO の enum(`Uta.Shared.Contracts`)は同じ名前・同じ数値で別に定義する。`Nothing` はマスタ・DTO・履歴のどこにも入れない(検証で弾く)
- `Outcome`(New / Duplicate)はマスタで使わないので、正は [api-design.md](../api-design.md) 1.1(2026-11-01 にここから移した)
- 区分: どれもクライアントで確率を表示するので Common(クライアントにも出す)。`DuplicateConversion` も、結果の画面で量を見せるので Common
- 育成アイドルとサポートカードは**専用の参照列 2 つ**(`IdolId`・`SupportCardId`)で指し、存在は `[Exists]` で検証する [決定。2026-11-01。ユーザーの判断。それまでは 1 つの列 `ItemId` だった]。[masterdata-pipeline.md](masterdata-pipeline.md) 3章の「参照は専用の列＋`[Exists]`」の規約どおり。`ItemKind` に合う方だけを書き、もう一方は空欄。DTO と履歴の形(`ItemKind` + `ItemId`)へは、サーバーの Infrastructure で `ItemKind` を見て読み替える
- 検証 [2026-11-01 に実装。行の検査は `IValidatable`、表をまたぐ検査はサーバーのテスト（`MasterDataInvariantTests`）]。`IValidatable`: `ItemKind` が `Nothing` でない、`ItemKind` に合う参照列だけが埋まっている(`Idol` なら `IdolId` だけ、`SupportCard` なら `SupportCardId` だけ)、重みが 1 以上、テーブルごとの重みの合計が `int.MaxValue` 以下(`RandomOps.WeightedIndex` の制約。`long` で足して比べる)、テーブルが空でない、確定枠のテーブルに R がない(参照先のマスタの `Rarity` を見る)。`GachaCeilingItem` にも同じ参照列の検証を掛ける

## 未決事項

なし（各システムのページと [masterdata-pipeline.md](masterdata-pipeline.md) の未決事項を見る）

## 実装メモ

- 実装 [2026-11-01。計画 `plans/2026-11-01-masterdata-rest.md`]: `masterdata/Definitions/Gacha.cs`。MVP の中身は `masterdata/csv/` の 5 ファイル（ガチャ 1 本。通常枠 14 行・確定枠 7 行、重みは 0.01% 単位で通常枠の合計 10000・確定枠 2200）
- 見出しの表は `GachaDetailTable`（`gachaDetailTable`）。列名（`GachaTableId`・`NormalTableId`・`GuaranteedTableId`）は変えていない
- 天井の交換対象は育成アイドル SSR（`IdolId` 4）とサポートカード SSR（`SupportCardId` 9）の 1 つずつ。game-spec.md の「SSR の 2 種」をこう読んだ。2 種ともサポートカードなら `gachaCeilingItem.csv` を直す
- `ItemKind` の元の enum は `Uta.Shared.Contracts.Gacha.ItemKind`（2026-11-01 に実装）。マスタの写し・サーバーの `GrantedItemKind` との名前と数値の一致は `server/Uta.Server.Tests/MasterData/ContractsEnumsMatchTests.cs` が確かめる
