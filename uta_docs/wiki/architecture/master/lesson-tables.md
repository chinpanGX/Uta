---
title: レッスン・カードのマスタ
status: draft
updated: 2026-10-31
---

# レッスン・カードのマスタ

ゲームの仕様は [lesson-card.md](../../systems/lesson-card.md) 3章（カード定義の考え方）。ここはテーブル・列の正（2026-10-31 に [masterdata-pipeline.md](masterdata-pipeline.md) 3章「難所: カード定義の入れ子」から移した。中身は変えていない）。この形を選んだ理由と、効果の列（`Value` / `Arg`）の規約は masterdata-pipeline.md 3章、`Op` ごとの列の対応と enum の名前・数値は [lesson-enums.md](../lesson-enums.md)。全テーブルの索引は [README.md](README.md)。

## 1. テーブルと列 [決定。2026-10-28。名前は仮]

| テーブル | 主キー | 主な列 |
|---|---|---|
| `Card` | `CardId` | プラン、種別、レアリティ、`UnlockLv`、`OnceInLesson`、`Unique`、`StartInHand` |
| `CardFace` | `CardId` + `Face`（通常 / 強化） | `EffectGroupId`、`UseConditionGroupId`（空欄＝条件なし） |
| `CardCost` | `CardId` + `Face` + `Order` | `Kind`（enum `CostKind`）、`Value` |
| `EffectGroup` | `EffectGroupId` | （見出し。`Memo` など） |
| `Effect` | `EffectGroupId` + `Order` | `Op`（enum）、`Status`（enum）、`Trigger`（enum）、`Value`、`Arg1`〜`Arg3`（`int`）、`RefConditionGroupId`、`RefEffectGroupId`、`RefCardId` |
| `ConditionGroup` | `ConditionGroupId` | （見出し） |
| `Condition` | `ConditionGroupId` + `Order` | `Subject`（enum）、`Comparator`（enum）、`Value`。グループ内はすべて満たすと真（AND） |

- 区分はすべて Common（スキルカードは表示と判定に使う。[masterdata-pipeline.md](masterdata-pipeline.md) 4章）

## 未決事項

なし（各システムのページと [masterdata-pipeline.md](masterdata-pipeline.md) の未決事項を見る）

## 実装メモ

(実装後に設計とのズレをここに記録する)
