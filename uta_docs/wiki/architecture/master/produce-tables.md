---
title: プロデュースのマスタ
status: draft
updated: 2026-11-01
---

# プロデュースのマスタ

ゲームの仕様は [produce.md](../../systems/produce.md)。ここはテーブル・列・区分・検証の正（2026-10-31 に produce.md 3.1 から移した。中身は変えていない）。全テーブルの索引は [README.md](README.md)。

## 1. テーブルと列 [決定。2026-10-31。名前は仮]

列の型の規約は [masterdata-pipeline.md](masterdata-pipeline.md) 3章:

| テーブル | 主キー | 列 |
|---|---|---|
| `Scenario` | `ScenarioId` | `Name`、`Difficulty`(enum: `Nothing` / `Beginner` / `Intermediate` / `Advanced`)、`WeekCount`、`ParamCap`(パラメータ上限)、`RetryCount`(再挑戦の回数。3) |
| `ScenarioWeek` | `ScenarioId` + `Week` | `Kind`(enum: `Nothing` / `Action` / `Finishing` / `MidExam` / `FinalExam`)。その週のレッスン・試験の設定: `TurnsSense`、`TurnsLogic`(育成アイドルのプランで使い分ける。共通プランのアイドルはいない)、`ClearTarget`、`PerfectTarget`(レッスンの枠がない週と試験の週は、この 4 つが 0)、`ExamEarlyEndScore`(中間試験の早期終了。0 ＝なし)、`RivalBaseScore`([produce.md](../../systems/produce.md) 5.3 の R。試験の週だけ。中間の R は早期終了スコアと同じ値を入れるが、[produce.md](../../systems/produce.md) 8章で別々に調整できるよう列を分ける) |
| `WeekActionSlot` | `ScenarioId` + `Week` + `Order` | `Kind`(enum `ActionKind`: `Nothing` / `Lesson` / `Class` / `Supply` / `Outing` / `Consult`)。週の行動の枠(画面の並び)。休むは枠に書かない(下) |
| `ClassOption` | `ScenarioId` + `Week` + `Order` | `HpCost`(0＝コストなし)、`ParamGain`、`GivesCard`(`bool`) |
| `ExamPattern` | `ExamPatternId` | `FirstPercent` / `SecondPercent` / `ThirdPercent`(審査比率。表示だけに使い、計算では読まない)、`MidFirstTurns` / `MidSecondTurns` / `MidThirdTurns`、`FinalSenseFirstTurns`…(最終・感性の 3 つ)、`FinalLogicFirstTurns`…(最終・理論の 3 つ)、`FirstTurnSecondRatePerTenThousand`(先頭のターンを 2 番目にする確率。[produce.md](../../systems/produce.md) 5.1。1 番目の比率が 45% 以上の型は 0) |
| `ExamRankReward` | `ExamKind`(enum: `Nothing` / `Mid` / `Final`) + `Rank`(1〜3) | `ParamAll`(全パラメータに足す量) |
| `ScoreBonusSetting` | `Id`(1 行だけ) | `APerThousand`([produce.md](../../systems/produce.md) 5.2 の a)、`BFirst` / `BSecond` / `BThird`([produce.md](../../systems/produce.md) 5.2 の b) |
| `EvaluationScoreBand` | `Order` | `MinScore`(この値以上)、`ScalePerThousand`、`Fixed`([produce.md](../../systems/produce.md) 6章のスコア分の表) |
| `EvaluationRank` | `Rank`(enum: `Nothing` / `F` … `SSPlus`) | `MinValue`(この評価値以上) |
| `EvaluationRankReward` | `Rank` | `GachaCurrency`、`Gold`、`SupportPoints`、`Exp`([produce.md](../../systems/produce.md) 6.1 の報酬) |
| `OfferRarityWeight` | `Rarity` | `Weight`([produce.md](../../systems/produce.md) 8章) |
| `PlayerLevelExp` | `Level` | `RequiredExp`(Level → Level+1 に要る経験値。[produce.md](../../systems/produce.md) 7章) |

- 区分 [決定。2026-10-31]: `EvaluationRankReward` だけ ServerOnly(報酬の付与はサーバー専用。[api-design.md](../api-design.md) 4章)。ほかは Common。`ScenarioWeek`(NPC のスコア `RivalBaseScore` を含む)を Common にするのは、試験中の順位の表示と順位の判定を `Uta.Shared.Domain` で行うため(NPC のスコアはクライアントにも見える)
- `LessonSetting`・`ExamRivalScore` は別のテーブルにせず `ScenarioWeek` の列にした [決定。2026-11-01。ユーザーの判断]。どちらも週と 1 対 1 で、別にすると参照の検証と「全列 0 の行」が増えるため
- 行動の効果は、行動の種類ごとの専用の表(`ClassOption`)で持つ。支給は MVP では選択肢を引くだけなので表を持たない(育成pt を扱うときに `SupplySetting` を足す。[produce.md](../../systems/produce.md) 3.3)。カードの効果グループ([lesson-enums.md](../lesson-enums.md))は使わない。MVP の行動は 3 種類だけで、専用の列のほうが読みやすく検証もしやすいため
- **マスタにする値と定数にする値の基準** [決定。2026-11-01。ユーザーの判断]: 遊びの調整で変える値(ターン数・目標・NPC のスコア・報酬・確率・重み・必要経験値)はマスタにする。式の形を決める係数・割合(下の 2 つ)は `Uta.Shared.Domain` の定数にする。迷ったら、[produce.md](../../systems/produce.md) 8章で調整する値かどうかで決める
- 休むの回復量(最大体力の 70%)はルールの値として `Uta.Shared.Domain` の定数に置く(行動ごとの差がない)
- [produce.md](../../systems/produce.md) 5.3 の NPC の割合(80 / 65 / 50 / 35 / 20%)、休む・仕上げの回復率(70%)、パーフェクトの回復(残りターン × 2)、[produce.md](../../systems/produce.md) 6章のパラメータ分の倍率(2300 / 1000)と順位分(1700 / 900 / 500)、[lesson-card.md](../../systems/lesson-card.md) 4.1 のレッスンルールの値は `Uta.Shared.Domain` の定数に置く
- 検証(`IValidatable`): 週番号が 1〜`WeekCount` で抜けがない、最後の週が `FinalExam`、試験の週の前の週が `Finishing`、`WeekActionSlot` は `Action` の週と `Finishing` の週(`Lesson` 1 行)にだけある、`Action` の週でレッスンの枠があるとき・仕上げの週はターン数と目標が 1 以上、試験の週はターン数・目標が 0 で `RivalBaseScore` が 1 以上、試験以外の週は `RivalBaseScore`・`ExamEarlyEndScore` が 0。**試験のターン数の正は `ExamPattern` の割り振りの合計**(`Uta.Shared.Domain` にターン数の定数は置かない)。合計が [produce.md](../../systems/produce.md) 5.1 の値(中間 9、最終・感性 12、最終・理論 11)と同じことを検証する(仕様とマスタのずれを見つけるため。仕様の値を変えたら、この検証も変える) [2026-11-01]

## 未決事項

なし（各システムのページと [masterdata-pipeline.md](masterdata-pipeline.md) の未決事項を見る）

## 実装メモ

(実装後に設計とのズレをここに記録する)
