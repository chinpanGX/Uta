---
title: architecture/master（マスタ設計）の目次
status: draft
updated: 2026-11-01
---

# architecture/master（マスタ設計）の目次

マスタデータの実装設計書。定義・生成・配信と列の規約は masterdata-pipeline.md、テーブルと列はシステムごとのページが正（2026-10-31 に systems/ と masterdata-pipeline.md から移した。[ADR 0017](../../decisions/0017-spec-design-plan-stages.md)）。

## 一覧

- [masterdata-pipeline.md](masterdata-pipeline.md) — マスター設計編：MasterMemoryAddons による定義・生成・配信、列の型の制約と表現の規約、出し分けの区分
- [lesson-tables.md](lesson-tables.md) — レッスン・カードのマスタ（5 テーブル）
- [produce-tables.md](produce-tables.md) — プロデュースのマスタ（12 テーブル）
- [idol-support-tables.md](idol-support-tables.md) — 育成アイドル・サポートカードのマスタ（9 テーブル）
- [gacha-tables.md](gacha-tables.md) — ガチャのマスタ（5 テーブル）

## テーブルの索引 [索引。2026-10-31 時点]

MVP で使うマスタのテーブルの一覧。**列と検証の正は「正のページ」**で、ここは名前・主キー・区分だけを引くための索引（列はここに書かない。二重管理しないため）。テーブル・列の名前は、どれも正のページで「名前は仮」。テーブルを足す・消す・名前を変えるときは、正のページと同じ変更でここも直す。

区分の Common は両側に出すもの、ServerOnly はサーバーだけに出すもの（[masterdata-pipeline.md](masterdata-pipeline.md) 4章）。

### 1. レッスン・カード（5 テーブル）

正のページ: [lesson-tables.md](lesson-tables.md)（列）、[lesson-enums.md](../lesson-enums.md)（enum と効果の列の対応）、[lesson-card.md](../../systems/lesson-card.md) 3章（カード定義の考え方）

| テーブル | 主キー | 区分 | 中身 |
|---|---|---|---|
| `Card` | `CardId` | Common | スキルカードの見出し（プラン、種別、レアリティ、解放 Lv など） |
| `CardFace` | `CardId` + `Face` | Common | 通常 / 強化の面。効果・使用条件のグループを指す |
| `CardCost` | `CardId` + `Face` + `Order` | Common | コスト |
| `Effect` | `EffectGroupId` + `Order` | Common | 効果の 1 行（`Op` と引数） |
| `Condition` | `ConditionGroupId` + `Order` | Common | 条件の 1 行（AND） |

- 区分は [lesson-tables.md](lesson-tables.md) で決めている（スキルカードは表示と判定に使うので Common）。考え方は [masterdata-pipeline.md](masterdata-pipeline.md) 4章

### 2. プロデュース（12 テーブル）

正のページ: [produce-tables.md](produce-tables.md)

| テーブル | 主キー | 区分 | 中身 |
|---|---|---|---|
| `Scenario` | `ScenarioId` | Common | シナリオ（難易度、週の数、パラメータ上限、再挑戦の回数など） |
| `ScenarioWeek` | `ScenarioId` + `Week` | Common | 週の種類と、その週のレッスン・試験の設定（ターン数・目標・早期終了スコア・NPC のスコア） |
| `WeekActionSlot` | `ScenarioId` + `Week` + `Order` | Common | 週の行動の枠 |
| `ClassOption` | `ScenarioId` + `Week` + `Order` | Common | 授業の選択肢 |
| `ExamPattern` | `ExamPatternId` | Common | 審査比率と試験のターン配分 |
| `ExamRankReward` | `ExamKind` + `Rank` | Common | 試験の順位ごとのパラメータ上昇 |
| `ScoreBonusSetting` | `Id`（1 行） | Common | スコアボーナスの係数 |
| `EvaluationScoreBand` | `Order` | Common | 最終評価のスコア分の帯 |
| `EvaluationRank` | `Rank` | Common | 評価値ごとのランク |
| `EvaluationRankReward` | `Rank` | **ServerOnly** | ランクごとの報酬 |
| `OfferRarityWeight` | `Rarity` | Common | カードの選択肢のレアリティの重み |
| `PlayerLevelExp` | `Level` | Common | プレイヤー Lv に要る経験値 |

### 3. 育成アイドル・サポートカード（9 テーブル）

正のページ: [idol-support-tables.md](idol-support-tables.md)

| テーブル | 主キー | 区分 | 中身 |
|---|---|---|---|
| `Idol` | `IdolId` | Common | 育成アイドル（レアリティ、プラン、ステータス、ボーナス、固有スキルカードなど） |
| `IdolRecommendDeck` | `Recommend` + `Order` | Common | おすすめ効果ごとの初期デッキのカード |
| `CommonDeckCard` | `Order` | Common | 初期デッキの共通の 5 枚 |
| `SupportCard` | `SupportCardId` | Common | サポートカード（レアリティ、タイプ、プラン） |
| `SupportAbility` | `SupportCardId` + `Order` | Common | サポートカードのアビリティ |
| `SupportLevelCost` | `Rarity` + `Level` | Common | レベル上げに要る強化pt・ゴールド |
| `SupportLevelCap` | `Rarity` + `LimitBreak` | Common | 上限解放の回数ごとの最大レベル |
| `InitialSupportCard` | `Order` | Common | 初期付与のサポートカード |
| `InitialSetting` | `Id`（1 行） | Common | 初期付与の通貨・ゴールド・強化pt |

### 4. ガチャ（5 テーブル）

正のページ: [gacha-tables.md](gacha-tables.md)（`ItemKind` の enum の正もここ）

| テーブル | 主キー | 区分 | 中身 |
|---|---|---|---|
| `Gacha` | `GachaId` | Common | ガチャ（価格、通常枠・確定枠のテーブル、天井の交換pt） |
| `GachaTable` | `GachaTableId` | Common | 排出テーブルの見出し |
| `GachaTableEntry` | `GachaTableId` + `Order` | Common | 排出テーブルの 1 行（`ItemKind` + `IdolId` / `SupportCardId` と重み） |
| `GachaCeilingItem` | `GachaId` + `Order` | Common | 天井で交換できるもの |
| `DuplicateConversion` | `ItemKind` + `Rarity` | Common | 重複したときの変換量 |

### 5. 索引から見えること

- 合計 31 テーブル（2026-11-01 にレッスンの見出し 2 つとプロデュースの `LessonSetting`・`ExamRivalScore` を減らした）。ServerOnly は `EvaluationRankReward` の 1 つ（`InitialSupportCard`・`InitialSetting` は 2026-11-01 に Common にした）。`[ClientOnly]` のテーブル・列はまだ無い（表示名・アイコンは未定義）
- ServerOnly のテーブルを Common のテーブルから `[Exists]` で参照するものは無い。逆向き（`EvaluationRankReward.Rank` → `EvaluationRank`）はあるが、両側に出すテーブルを指すので問題にならない見込み（推測。[repository-layout.md](../repository-layout.md) 8章の `[Exists]` の確認で確かめる）
