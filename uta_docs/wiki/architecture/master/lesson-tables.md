---
title: レッスン・カードのマスタ
status: draft
updated: 2026-11-01
---

# レッスン・カードのマスタ

ゲームの仕様は [lesson-card.md](../../systems/lesson-card.md) 3章（カード定義の考え方）。ここはテーブル・列の正（2026-10-31 に [masterdata-pipeline.md](masterdata-pipeline.md) 3章「難所: カード定義の入れ子」から移した。中身は変えていない）。この形を選んだ理由と、効果の列（`Value` / `Arg`）の規約は masterdata-pipeline.md 3章、`Op` ごとの列の対応と enum の名前・数値は [lesson-enums.md](../lesson-enums.md)。全テーブルの索引は [README.md](README.md)。

## 1. テーブルと列 [決定。2026-10-28。名前は仮]

| テーブル | 主キー | 主な列 |
|---|---|---|
| `Card` | `CardId` | プラン、種別、レアリティ、`UnlockLv`、`OnceInLesson`、`Unique`、`StartInHand` |
| `CardFace` | `CardId` + `Face`（通常 / 強化） | `EffectGroupId`、`UseConditionGroupId`（空欄＝条件なし） |
| `CardCost` | `CardId` + `Face` + `Order` | `Kind`（enum `CostKind`）、`Value` |
| `Effect` | `EffectGroupId` + `Order` | `Op`（enum）、`Status`（enum）、`Trigger`（enum）、`Value`、`Arg1`〜`Arg3`（`int`）、`RefConditionGroupId`、`RefEffectGroupId`、`RefCardId` |
| `Condition` | `ConditionGroupId` + `Order` | `Subject`（enum）、`Comparator`（enum）、`Value`。グループ内はすべて満たすと真（AND） |

- 区分はすべて Common（スキルカードは表示と判定に使う。[masterdata-pipeline.md](masterdata-pipeline.md) 4章）
- グループの参照は、`Effect`・`Condition` の列を `[Exists]` で指す（`CardFace.EffectGroupId` と `Effect.RefEffectGroupId` は `[Exists(typeof(Effect), nameof(Effect.EffectGroupId))]`、`UseConditionGroupId`・`RefConditionGroupId` は `Condition` の `ConditionGroupId` を同じ形で。空欄を許す列は `AllowEmpty = true`）。グループの見出しテーブル（`EffectGroup`・`ConditionGroup`）は置かない [決定。2026-11-01。ユーザーの判断]。アドオンの `[Exists]` は主キー以外の列も参照できる（[defining-master-data.md](../../../../external/MasterMemoryAddons/docs/guide/defining-master-data.md)）ので、見出しが無くても属性で検証でき、テーブルが 2 つ減る
- グループで持つ形（効果・条件を 1 つの ID でまとめる）はそのまま。MVP の 36 枚でグループ ID を手で振る手間（約 90）は受け入れる [決定。2026-11-01]

## 未決事項

なし（各システムのページと [masterdata-pipeline.md](masterdata-pipeline.md) の未決事項を見る）

## 実装メモ

(実装後に設計とのズレをここに記録する)
