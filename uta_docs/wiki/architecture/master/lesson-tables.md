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

## 2. 検証 [決定。2026-11-01]

- 自分の行の列だけで決まる検査は、行のクラスの `IValidatable<T>`（`Card`・`CardFace`・`CardCost`・`Condition`・`Effect`）。`Effect` の `Op` ごとの列の規則は [lesson-enums.md](../lesson-enums.md)「マスタの検証の規則」
- `Effect` の入れ子の深さ（1）だけは、`IValidatable` の `CallOnce()` で `Effect` の全行を読んで検査する
- 表をまたぐ検査はサーバーのテスト `MasterDataInvariantTests`: すべてのカードに `Normal` と `Enhanced` の `CardFace` が 1 行ずつある。すべての `CardFace` に同じ `(CardId, Face)` の `CardCost` が 1 行以上あり、すべての `CardCost` に同じ `(CardId, Face)` の `CardFace` がある
- 参照は属性（`[Exists]`）

## 未決事項

なし（各システムのページと [masterdata-pipeline.md](masterdata-pipeline.md) の未決事項を見る）

## 実装メモ

2026-11-01（[計画](../../../plans/2026-11-01-masterdata-first.md)）:

- 5 テーブルの定義は `masterdata/Definitions/Lesson.cs`、CSV は `masterdata/csv/`（`card` 36 行、`cardFace` 72、`cardCost` 72、`effect` 129、`condition` 14）。`CardFace.Face` の enum は `CardFaceKind`（`Nothing`・`Normal`・`Enhanced`）。元の enum が `Uta.Shared.Domain` にまだないので、写しとの比較テストはない
- `unlockLv` は**仮の値**（N・R = 1、SR = 5、SSR = 10）
- 参照の列（`UseConditionGroupId`・`RefConditionGroupId`・`RefEffectGroupId`・`RefCardId`）は、使わないとき CSV のセルを空にする（`0` は「ID 0 を探す」ことになって失敗する）
- 効果内条件は、直前の 1 効果の行にだけ付ける。持続効果の子のグループは `EffectGroupId = 親グループ + 1`（面ごとの `… × 100 + 面 × 10 + 2`）
