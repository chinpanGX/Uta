---
title: 実装計画: masterdata の最初の計画 2/2（プロデュース・育成アイドル・サポートカード・ガチャのマスタ）
status: ready
updated: 2026-11-01
spec: ../wiki/architecture/master/produce-tables.md, ../wiki/architecture/master/idol-support-tables.md, ../wiki/architecture/master/gacha-tables.md, ../wiki/architecture/master/masterdata-pipeline.md, ../wiki/overview/game-spec.md
---

# 実装計画: masterdata の最初の計画 2/2（プロデュース・育成アイドル・サポートカード・ガチャのマスタ）

[1/2](2026-11-01-masterdata-first.md) の続き。**1/2 が `done`（`masterdata/`・`gen.ps1`・サーバーの読み込み・レッスンの 5 テーブルがある）になってから実装する。** 1/2 の「この計画で決めたこと」（ツールの実行、名前空間、テーブル名、`[InputName]`、`bool` の表記、検証の置き場、読み込み）はここでも同じで、書き写さない。

## 1. 目的と前提

[game-spec.md](../wiki/overview/game-spec.md)「実装計画の単位と順」の **2 の後半**。プロデュース（12 テーブル）・育成アイドル／サポートカード（9）・ガチャ（5）の計 26 テーブルの定義と、MVP の CSV（育成アイドル 4 人・サポートカード 10 種・ガチャ 1 本・シナリオ 2 つ）を置き、サーバーのテストで中身を確かめる。

- 仕様の正: テーブルと列は [produce-tables.md](../wiki/architecture/master/produce-tables.md)・[idol-support-tables.md](../wiki/architecture/master/idol-support-tables.md)・[gacha-tables.md](../wiki/architecture/master/gacha-tables.md)、値の出どころは [produce.md](../wiki/systems/produce.md)・[idol-support.md](../wiki/systems/idol-support.md)・[gacha.md](../wiki/systems/gacha.md)
- 関係する ADR: [0007](../wiki/decisions/0007-masterdata-with-addons.md)、[0013](../wiki/decisions/0013-commit-masterdata-binary.md)、[0016](../wiki/decisions/0016-reference-game-values.md)、[0001](../wiki/decisions/0001-ip-policy.md)
- **この計画で決めないこと**: 1/2 の範囲外に加えて、中級・上級、特別レッスン、相談・おでかけ、ドリンク、育成アイテム、信頼度のテーブル（MVP で使わない）

## 2. この計画で決めたこと

| 項目 | 決めたこと | 理由 |
|---|---|---|
| 育成アイドル・サポートカードの値 | 3.5 の表（名前は仮称。ステータス・アビリティは仮の値） | ユーザーの許可（2026-11-01）。参考作品の最大値（SSR: 初期パラメータ+65、ボーナス+8.5%、最大体力+9）に合わせて、レアリティごとに段階を付けた |
| 天井の交換対象 | 育成アイドル SSR（`IdolId` 4）とサポートカード SSR（`SupportCardId` 9）の 2 つ | game-spec.md「SSR の 2 種を候補にする」を、育成アイドルとサポートカードの 1 つずつと読んだ。2 種とも SSR のサポートカードという読みもある（7章） |
| enum の名前 | 名前が仕様に書かれていないものを、3.2 のとおりこの計画で付けた: `Difficulty`・`WeekKind`・`ExamKind`・`ProduceRank`・`RecommendKind`。`ActionKind`・`SupportType`・`SupportAbilityKind`・`ItemKind` は仕様の名前。数値は書かれた順に 0 から振る | produce-tables.md・idol-support-tables.md に enum の中身だけがあるもの |
| `EvaluationRankReward.Rank` | `[Exists]` を付けない | `[ServerOnly]` の表から `[Common]` の表への `[Exists]` は、repository-layout.md 8章で未確認。全 13 ランクがそろうことはテストで確かめる。master/README.md 5章の「逆向きはある」はタスク 4 で直す |
| 検証の置き場 | 1/2 の「検証の置き場」のとおり（行の検査は `IValidatable`、表をまたぐ検査は `MasterDataInvariantTests`） | 同じ不変条件を二重に書かない |
| 式で決まる行 | `playerLevelExp`（99 行）・`supportLevelCost`（147 行）は、PowerShell で作ってよい（スクリプトはコミットしない。書き方は 3.6）。全行の式をテストで確かめる | 手で打つと途中の 1 行が狂いやすい |
| 見出しの表の名前 | gacha-tables.md の `GachaTable`（排出テーブルの見出し）は、定義クラス名を **`GachaDetailTable`** にする（`[MemoryTable("gachaDetailTable")]`、`gachaDetailTable.csv`）。列名（`GachaTableId`・`NormalTableId`・`GuaranteedTableId`）は変えない | **確認**: MasterMemory は表 `Gacha` の型を `GachaTable` と生成する（サンプルの `Item` が `ItemTable` になるのと同じ）ので、同名の定義クラスと衝突する（CS0101）。生成される型は `GachaDetailTableTable`。ユーザーの提案（2026-11-01）。gacha-tables.md・master/README.md の名前はタスク 4 で直す |

## 3. 触るファイル

### 3.1 一覧

| 操作 | パス | 中身 | タスク |
|---|---|---|---|
| 作成 | `masterdata/Definitions/Produce.cs` | プロデュースの enum 5 つと表 12 | 1 |
| 作成 | `masterdata/Definitions/IdolSupport.cs` | 育成アイドル・サポートカードの enum 3 つと表 9 | 2 |
| 作成 | `masterdata/Definitions/Gacha.cs` | `ItemKind` と表 5 | 3 |
| 作成 | `masterdata/csv/<テーブル名>.csv` | 26 ファイル（3.6 の中身） | 1〜3 |
| 変更（再生成） | `server/Uta.Server/Infrastructure/MasterData/Generated/*.cs`・`masterdata.bytes`・`masterdata.manifest.json` | `gen.ps1` が作り直す（手で直さない） | 1〜3 |
| 変更 | `server/Uta.Server.Tests/MasterData/MasterDataTests.cs` | 件数・値のテストを足す | 1〜3 |
| 変更 | `server/Uta.Server.Tests/MasterData/MasterDataInvariantTests.cs` | 表をまたぐ検証を足す | 1〜3 |
| 変更 | `uta_docs/wiki/` | タスク 4（メインのセッション） | 4 |

### 3.2 enum

`masterdata/Definitions` に置く enum。**数値は 0 を `Nothing` にし、「値」の列の順に振る**。lesson-enums.md・各 `*-tables.md` の名前・数値と**同じ**にする（`InputName` はメンバー名と同じ）。

| 置き場 | enum | メンバー（0 から順。明記のあるものは数値を使う） |
|---|---|---|
| Produce.cs | `Difficulty` | `Nothing` / `Beginner` / `Intermediate` / `Advanced` |
| | `WeekKind` | `Nothing` / `Action` / `Finishing` / `MidExam` / `FinalExam` |
| | `ActionKind` | `Nothing` / `Lesson` / `Class` / `Supply` / `Outing` / `Consult` |
| | `ExamKind` | `Nothing` / `Mid` / `Final` |
| | `ProduceRank` | `Nothing` / `F` / `E` / `D` / `C` / `CPlus` / `B` / `BPlus` / `A` / `APlus` / `S` / `SPlus` / `SS` / `SSPlus`（0〜13。produce.md 6章のランクの順） |
| IdolSupport.cs | `RecommendKind` | `Nothing` / `GoodCondition` / `Focus` / `Impression` / `Motivation` |
| | `SupportType` | `Nothing` / `Singing` / `Rhythm` / `Expression` / `Assist` |
| | `SupportAbilityKind` | `Nothing` / `InitialParam` / `ParamBonus` / `MaxHp` |
| Gacha.cs | `ItemKind` | `Nothing` 0 / `Idol` 1 / `SupportCard` 2（gacha-tables.md が正） |

### 3.3 テーブルと列 — プロデュース（`Produce.cs`）

書式（1/2 の 3.3 と同じ）: `名前 型`（`int` は型を省く）。`PK(n)` = `[PrimaryKey(keyOrder: n)]`（単一キーは `[PrimaryKey]`）。`→ T.Col` = `[Exists(typeof(T), nameof(T.Col))]`、`→? ` = `AllowEmpty = true` 付き。クラスは `public class X : IValidatable<X> { public … { get; init; } }`、文字列は `= ""`。区分は特記がなければ Common（属性なし）。

| クラス（`MemoryTable`） | 列 |
|---|---|
| `Scenario`（`scenario`） | `ScenarioId` PK、`string Name`、`Difficulty Difficulty`、`WeekCount`、`ParamCap`、`RetryCount` |
| `ScenarioWeek`（`scenarioWeek`） | `ScenarioId` PK(0) → Scenario.ScenarioId、`Week` PK(1)、`WeekKind Kind`、`TurnsSense`、`TurnsLogic`、`ClearTarget`、`PerfectTarget`、`ExamEarlyEndScore`、`RivalBaseScore` |
| `WeekActionSlot`（`weekActionSlot`） | `ScenarioId` PK(0) → Scenario.ScenarioId、`Week` PK(1)、`Order` PK(2)、`ActionKind Kind` |
| `ClassOption`（`classOption`） | `ScenarioId` PK(0) → Scenario.ScenarioId、`Week` PK(1)、`Order` PK(2)、`HpCost`、`ParamGain`、`bool GivesCard` |
| `ExamPattern`（`examPattern`） | `ExamPatternId` PK、`FirstPercent`、`SecondPercent`、`ThirdPercent`、`MidFirstTurns`、`MidSecondTurns`、`MidThirdTurns`、`FinalSenseFirstTurns`、`FinalSenseSecondTurns`、`FinalSenseThirdTurns`、`FinalLogicFirstTurns`、`FinalLogicSecondTurns`、`FinalLogicThirdTurns`、`FirstTurnSecondRatePerTenThousand` |
| `ExamRankReward`（`examRankReward`） | `ExamKind ExamKind` PK(0)、`Rank` PK(1)（`int`、1〜3）、`ParamAll` |
| `ScoreBonusSetting`（`scoreBonusSetting`） | `Id` PK、`APerThousand`、`BFirst`、`BSecond`、`BThird` |
| `EvaluationScoreBand`（`evaluationScoreBand`） | `Order` PK、`MinScore`、`ScalePerThousand`、`Fixed` |
| `EvaluationRank`（`evaluationRank`） | `ProduceRank Rank` PK、`MinValue` |
| `EvaluationRankReward`（`evaluationRankReward`）**`[ServerOnly]`**（型全体） | `ProduceRank Rank` PK、`GachaCurrency`、`Gold`、`SupportPoints`、`Exp` |
| `OfferRarityWeight`（`offerRarityWeight`） | `Rarity Rarity` PK、`Weight` |
| `PlayerLevelExp`（`playerLevelExp`） | `Level` PK、`RequiredExp` |

**検証**。メッセージは「テーブル名: 内容」の形にする。produce-tables.md の検証の節のとおり。

*行の検証（`IValidatable<T>`）*

- `Scenario`: `Difficulty` が `Nothing` でない。`WeekCount` ≥ 1、`ParamCap` ≥ 1、`RetryCount` ≥ 0
- `ScenarioWeek`: `Kind` が `Nothing` でない。6 つの数値（`TurnsSense`・`TurnsLogic`・`ClearTarget`・`PerfectTarget`・`ExamEarlyEndScore`・`RivalBaseScore`）が ≥ 0。`PerfectTarget` ≥ `ClearTarget`。`Finishing` は `TurnsSense`・`TurnsLogic`・`ClearTarget`・`PerfectTarget` がすべて ≥ 1。`MidExam`・`FinalExam` は 4 つとも 0 で `RivalBaseScore` ≥ 1。`FinalExam` は `ExamEarlyEndScore` も 0（`MidExam` の 0 は「早期終了なし」）。試験以外は `RivalBaseScore`・`ExamEarlyEndScore` が 0
- `WeekActionSlot`: `Kind` が `Nothing` でない。`Order` ≥ 1
- `ClassOption`: `Order` ≥ 1、`HpCost` ≥ 0、`ParamGain` ≥ 1
- `ExamPattern`: `FirstPercent` + `SecondPercent` + `ThirdPercent` = 100。`MidFirstTurns` + `MidSecondTurns` + `MidThirdTurns` = 9、`FinalSense…` の 3 つの合計 = 12、`FinalLogic…` の合計 = 11（produce.md 5.1。仕様の値を変えたらここも変える）。各ターン数 ≥ 1。`FirstTurnSecondRatePerTenThousand` は 0〜10000
- `ExamRankReward`: `ExamKind` が `Nothing` でない、`Rank` が 1〜3、`ParamAll` ≥ 0
- `ScoreBonusSetting`: `Id` = 1。4 つの係数が ≥ 0
- `EvaluationScoreBand`: `Order` ≥ 1、`MinScore` ≥ 0、`ScalePerThousand` ≥ 0、`Fixed` ≥ 0
- `EvaluationRank`: `Rank` が `Nothing` でない。`MinValue` ≥ 0
- `EvaluationRankReward`: `Rank` が `Nothing` でない。4 つの量が ≥ 0
- `OfferRarityWeight`: `Rarity` が `N`〜`SSR`。`Weight` ≥ 0
- `PlayerLevelExp`: `Level` ≥ 1、`RequiredExp` ≥ 1

*表をまたぐ検証（`MasterDataInvariantTests`）*

- 各シナリオの週番号が 1〜`WeekCount` で抜けも重複もない。最後の週は `FinalExam`。`MidExam`・`FinalExam` の週の前の週は `Finishing`
- `Action` の週は、その週に `Lesson` の `WeekActionSlot` があるとき `TurnsSense`・`TurnsLogic`・`ClearTarget`・`PerfectTarget` がすべて ≥ 1、ないとき 4 つとも 0（`Finishing` の ≥ 1 は行の検証で見る）
- `WeekActionSlot` の週は `Action` か `Finishing`。`Finishing` の週は `Lesson` の 1 行だけ
- `ClassOption` の週には `Class` の `WeekActionSlot` がある。`Class` の枠がある週には `ClassOption` が 1 行以上ある
- `ExamRankReward` は `Mid` と `Final` それぞれに `Rank` 1・2・3 がそろう。`ScoreBonusSetting` は 1 行だけ
- `EvaluationScoreBand` は `Order` が 1 から連続し、先頭の `MinScore` = 0、`MinScore` が `Order` に対して単調増加
- `EvaluationRank` は 13 ランクすべてがそろい、`F` の `MinValue` = 0、`MinValue` がランクの順に単調増加。`EvaluationRankReward` も 13 ランクすべてがそろう
- `OfferRarityWeight` の `Weight` の合計 ≥ 1。`PlayerLevelExp` は `Level` が 1 から連続

### 3.4 テーブルと列 — 育成アイドル・サポートカード（`IdolSupport.cs`）

| クラス（`MemoryTable`） | 列 |
|---|---|
| `Idol`（`idol`） | `IdolId` PK、`string Name`、`Rarity Rarity`、`Plan Plan`、`RecommendKind Recommend`、`MaxHp`、`InitialSinging`、`InitialRhythm`、`InitialExpression`、`BonusSingingPerThousand`、`BonusRhythmPerThousand`、`BonusExpressionPerThousand`、`ExamPatternId` → ExamPattern.ExamPatternId、`ParamKind FirstParam`、`ParamKind SecondParam`、`ParamKind ThirdParam`、`UniqueCardId` → Card.CardId |
| `IdolRecommendDeck`（`idolRecommendDeck`） | `RecommendKind Recommend` PK(0)、`Order` PK(1)、`CardId` → Card.CardId |
| `CommonDeckCard`（`commonDeckCard`） | `Order` PK、`CardId` → Card.CardId |
| `SupportCard`（`supportCard`） | `SupportCardId` PK、`string Name`、`Rarity Rarity`、`SupportType Type`、`Plan Plan` |
| `SupportAbility`（`supportAbility`） | `SupportCardId` PK(0) → SupportCard.SupportCardId、`Order` PK(1)、`SupportAbilityKind Kind`、`ParamKind Param`、`ValueAtLv1`、`ValueAtMax` |
| `SupportLevelCost`（`supportLevelCost`） | `Rarity Rarity` PK(0)、`Level` PK(1)、`SupportPoints`、`Gold` |
| `SupportLevelCap`（`supportLevelCap`） | `Rarity Rarity` PK(0)、`LimitBreak` PK(1)、`MaxLevel` |
| `InitialSupportCard`（`initialSupportCard`） | `Order` PK、`SupportCardId` → SupportCard.SupportCardId |
| `InitialSetting`（`initialSetting`） | `Id` PK、`GachaCurrency`、`Gold`、`SupportPoints` |

**検証**。

*行の検証（`IValidatable<T>`）*

- `Idol`: `Rarity` が `R`・`SR`・`SSR`。`Plan` が `Sense` か `Logic`。`Recommend` が `Nothing` でなく、プランと合う（`Sense` は `GoodCondition` か `Focus`、`Logic` は `Impression` か `Motivation`）。`MaxHp` ≥ 1。初期パラメータ・ボーナスが ≥ 0。`FirstParam`・`SecondParam`・`ThirdParam` が `Nothing` でなく、互いに異なる
- `IdolRecommendDeck`: `Recommend` が `Nothing` でない。`Order` ≥ 1
- `CommonDeckCard`: `Order` ≥ 1
- `SupportCard`: `Rarity` が `R`・`SR`・`SSR`。`Type`・`Plan` が `Nothing` でない
- `SupportAbility`: `Kind` が `Nothing` でない。`Kind` が `MaxHp` なら `Param` は `Nothing`、それ以外は `Nothing` でない。`ValueAtLv1` ≥ 0、`ValueAtMax` ≥ `ValueAtLv1`。`Order` ≥ 1
- `SupportLevelCost`: `Rarity` が `R`・`SR`・`SSR`。`Level` ≥ 1。`SupportPoints`・`Gold` ≥ 0
- `SupportLevelCap`: `Rarity` が `R`・`SR`・`SSR`。`LimitBreak` が 0〜4。`MaxLevel` ≥ 1
- `InitialSupportCard`: `Order` ≥ 1
- `InitialSetting`: `Id` = 1。3 つの量が ≥ 0

*表をまたぐ検証（`MasterDataInvariantTests`）*

- 各 `Idol` の `UniqueCardId` のカードの `Plan` が、アイドルの `Plan` と同じか `Free`
- `IdolRecommendDeck` は 4 つのおすすめ効果（`GoodCondition`・`Focus`・`Impression`・`Motivation`）それぞれに `Order` 1〜3 の 3 行がある。`CommonDeckCard` は 5 行
- `SupportLevelCost` は、レアごとに `Level` が 1 から連続し、最後の `Level` + 1 が、同じレアの `SupportLevelCap` の `LimitBreak` = 4 の `MaxLevel` と等しい
- `SupportLevelCap` は、レアごとに `LimitBreak` 0〜4 の 5 行があり、`MaxLevel` が単調増加
- `InitialSupportCard` の `SupportCardId` はすべて異なる（idol-support.md IDL-05）。`InitialSetting` は 1 行だけ

### 3.5 テーブルと列 — ガチャ（`Gacha.cs`）

| クラス（`MemoryTable`） | 列 |
|---|---|
| `Gacha`（`gacha`） | `GachaId` PK、`SingleCost`、`TenCost`、`NormalTableId` → GachaDetailTable.GachaTableId、`GuaranteedTableId` → GachaDetailTable.GachaTableId、`CeilingPoints` |
| `GachaDetailTable`（`gachaDetailTable`） | `GachaTableId` PK |
| `GachaTableEntry`（`gachaTableEntry`） | `GachaTableId` PK(0) → GachaDetailTable.GachaTableId、`Order` PK(1)、`ItemKind ItemKind`、`IdolId` →? Idol.IdolId、`SupportCardId` →? SupportCard.SupportCardId、`Weight` |
| `GachaCeilingItem`（`gachaCeilingItem`） | `GachaId` PK(0) → Gacha.GachaId、`Order` PK(1)、`ItemKind ItemKind`、`IdolId` →? Idol.IdolId、`SupportCardId` →? SupportCard.SupportCardId |
| `DuplicateConversion`（`duplicateConversion`） | `ItemKind ItemKind` PK(0)、`Rarity Rarity` PK(1)、`Amount` |

**検証**（gacha-tables.md の検証の節のとおり）。

*行の検証（`IValidatable<T>`）*

- `Gacha`: `SingleCost`・`TenCost`・`CeilingPoints` ≥ 1。`NormalTableId` ≠ `GuaranteedTableId`
- `GachaTableEntry` / `GachaCeilingItem`: `ItemKind` が `Nothing` でない。`ItemKind` が `Idol` なら `IdolId` だけが埋まり、`SupportCard` なら `SupportCardId` だけが埋まる。`Order` ≥ 1。`GachaTableEntry` は `Weight` ≥ 1
- `DuplicateConversion`: `ItemKind`・`Rarity` が `Nothing` でない。`Amount` ≥ 0

*表をまたぐ検証（`MasterDataInvariantTests`）*

- 各 `GachaDetailTable` に `GachaTableEntry` が 1 行以上ある。テーブルごとの重みの合計（`long` で足す）が `int.MaxValue` 以下
- `Gacha` の `GuaranteedTableId` のテーブルに、`R` のカード（参照先の `Idol`・`SupportCard` の `Rarity` を読む）がない
- `DuplicateConversion` は `Idol` の `R`・`SR`・`SSR` と `SupportCard` の `R`・`SR`・`SSR` の 6 行がそろう

### 3.6 CSV の中身

書式は 1/2 の 3.4 のとおり。**PowerShell で CSV を作るとき**（`playerLevelExp`・`supportLevelCost` だけ）は、BOM なし・LF で書く（`Out-File`・`Set-Content`・`>` は BOM や改行が変わるので使わない）。例（`playerLevelExp`）:

```powershell
$path = Join-Path (Get-Location) 'masterdata/csv/playerLevelExp.csv'   # リポジトリのルートで実行する。パスは絶対パスにする
$lines = @('PlayerLevelExp,', '出典: uta_docs/wiki/systems/produce.md,', 'int,int', 'Level,RequiredExp')
$lines += (1..99 | ForEach-Object { '{0},{1}' -f $_, (40 * $_) })
[System.IO.File]::WriteAllText($path, (($lines -join "`n") + "`n"), (New-Object System.Text.UTF8Encoding($false)))
```

先頭の 4 行（クラス名・出典・型・ヘッダ）は 1/2 の 3.4 のとおり、**列数ちょうど**にする（`playerLevelExp` は 2 列なので、1・2 行目は 1 つ目のセルだけ書いてカンマを 1 つ付ける。余分な空の列は作らない。アドオンの `samples/csv/dropTable.csv` と同じ形）。`supportLevelCost`（`Rarity`・`Level`・`SupportPoints`・`Gold` の 4 列）も同じ形で、1・2 行目はカンマ 3 つ、3 行目は `enum,int,int,int`、4 行目はヘッダ、データは `R,1,39,39` のように書く（`Rarity` は enum の名前 `R`・`SR`・`SSR`）。末尾は LF 1 つ。書いたあと `gen.ps1` が通れば形式は合っている。2 行目の出典は、プロデュースの 12 ファイルが `出典: uta_docs/wiki/systems/produce.md`、育成アイドル・サポートカードの 9 ファイルが `出典: uta_docs/wiki/systems/idol-support.md`、ガチャの 5 ファイルが `出典: uta_docs/wiki/systems/gacha.md`。

#### 3.6.1 プロデュース

**`scenario`**

| ScenarioId | Name | Difficulty | WeekCount | ParamCap | RetryCount |
|---|---|---|---|---|---|
| 1 | 最短シナリオ（仮） | Beginner | 3 | 1000 | 3 |
| 2 | 初級（仮） | Beginner | 13 | 1000 | 3 |

**`scenarioWeek`**（列: Kind, TurnsSense, TurnsLogic, ClearTarget, PerfectTarget, ExamEarlyEndScore, RivalBaseScore。書いていない列は 0）

| ScenarioId / Week | Kind | 値 |
|---|---|---|
| 1 / 1 | Action | 5, 6, 25, 50 |
| 1 / 2 | Finishing | 9, 9, 75, 300 |
| 1 / 3 | FinalExam | RivalBaseScore 2250 |
| 2 / 1 | Action | 5, 6, 25, 50 |
| 2 / 2 | Action | （レッスンの枠なし。すべて 0） |
| 2 / 3 | Action | 5, 6, 25, 50 |
| 2 / 4 | Action | すべて 0 |
| 2 / 5 | Finishing | 9, 9, 75, 300 |
| 2 / 6 | MidExam | ExamEarlyEndScore 900、RivalBaseScore 900 |
| 2 / 7 | Action | すべて 0 |
| 2 / 8 | Action | すべて 0 |
| 2 / 9 | Action | 6, 7, 40, 80 |
| 2 / 10 | Action | 6, 7, 40, 80 |
| 2 / 11 | Action | すべて 0 |
| 2 / 12 | Finishing | 10, 9, 100, 400 |
| 2 / 13 | FinalExam | RivalBaseScore 2250 |

**`weekActionSlot`**（`Order` は週の中で 1 から。produce.md 3.2・3.1。「※」は `Supply` に固定）

| ScenarioId / Week | 枠（Order 順） |
|---|---|
| 1 / 1 | Lesson |
| 1 / 2 | Lesson |
| 2 / 1 | Lesson |
| 2 / 2 | Class, Supply |
| 2 / 3 | Lesson, Class |
| 2 / 4 | Supply |
| 2 / 5 | Lesson |
| 2 / 7 | Supply |
| 2 / 8 | Class, Supply |
| 2 / 9 | Lesson |
| 2 / 10 | Lesson, Class |
| 2 / 11 | Supply |
| 2 / 12 | Lesson |

（2 / 6 と 2 / 13 は試験の週で枠なし。合計 15 行 + 最短の 2 行 = 17 行）

**`classOption`**（ScenarioId = 2。produce.md 3.4。`GivesCard` はすべて `TRUE`）

| Week | Order 1（HpCost / ParamGain） | Order 2 | Order 3 |
|---|---|---|---|
| 2, 3, 8 | 8 / 40 | 4 / 30 | 0 / 15 |
| 10 | 8 / 80 | 4 / 55 | 0 / 30 |

（合計 12 行）

**`examPattern`**（produce.md 5.1・idol-support.md 1.4。列の順: Percent 1/2/3、Mid 1/2/3、FinalSense 1/2/3、FinalLogic 1/2/3、FirstTurnSecondRatePerTenThousand）

| ExamPatternId | 比率 | Mid | FinalSense | FinalLogic | 率 |
|---|---|---|---|---|---|
| 1 | 40 / 33 / 27 | 4 / 3 / 2 | 5 / 4 / 3 | 5 / 3 / 3 | 1500 |
| 2 | 45 / 40 / 15 | 4 / 3 / 2 | 6 / 4 / 2 | 5 / 4 / 2 | 0 |
| 3 | 50 / 30 / 20 | 5 / 2 / 2 | 6 / 3 / 3 | 5 / 3 / 3 | 0 |
| 4 | 60 / 23 / 17 | 5 / 2 / 2 | 7 / 3 / 2 | 6 / 3 / 2 | 0 |

**`examRankReward`**: `Mid` の Rank 1 / 2 / 3 = 20 / 10 / 5、`Final` の Rank 1 / 2 / 3 = 30 / 20 / 10（`ParamAll`。6 行）

**`scoreBonusSetting`**: `Id` 1、`APerThousand` 225、`BFirst` 190、`BSecond` 150、`BThird` 120

**`evaluationScoreBand`**（`Order`: MinScore / ScalePerThousand / Fixed。produce.md 6章）: 1: 0 / 300 / 0、2: 5001 / 150 / 750、3: 10001 / 80 / 1450、4: 20001 / 40 / 2250、5: 30001 / 20 / 2850、6: 40001 / 10 / 3250、7: 200000 / 0 / 5250

**`evaluationRank`** と **`evaluationRankReward`**（produce.md 6・6.1。仮の値）

| Rank | MinValue | GachaCurrency | Gold | SupportPoints | Exp |
|---|---|---|---|---|---|
| F | 0 | 50 | 1000 | 500 | 10 |
| E | 1000 | 50 | 1500 | 800 | 12 |
| D | 2000 | 50 | 2000 | 1000 | 14 |
| C | 3000 | 100 | 3000 | 1500 | 16 |
| CPlus | 4500 | 150 | 4000 | 2000 | 18 |
| B | 6000 | 200 | 5000 | 2500 | 20 |
| BPlus | 8000 | 250 | 6000 | 3000 | 25 |
| A | 10000 | 300 | 8000 | 4000 | 30 |
| APlus | 11500 | 400 | 10000 | 5000 | 35 |
| S | 13000 | 500 | 12000 | 6000 | 40 |
| SPlus | 14500 | 600 | 14000 | 7000 | 45 |
| SS | 16000 | 700 | 17000 | 8500 | 50 |
| SSPlus | 18000 | 800 | 20000 | 10000 | 60 |

（`evaluationRank` は Rank と MinValue、`evaluationRankReward` は Rank と後ろの 4 列）

**`offerRarityWeight`**: N 30、R 30、SR 30、SSR 10

**`playerLevelExp`**: `Level` 1〜99、`RequiredExp` = `40 × Level`（produce.md 7章。99 行）

#### 3.6.2 育成アイドル・サポートカード（名前・ステータスは仮の値）

**`idol`**（ユーザーの許可による仮の値。`ExamPatternId`・`FirstParam`〜は列の組み合わせを散らした）

| IdolId | Name | Rarity | Plan | Recommend | MaxHp | 初期（歌唱/律動/表現） | ボーナス千分率（歌唱/律動/表現） | ExamPatternId | First / Second / Third | UniqueCardId |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | 仮アイドル1（感性R） | R | Sense | GoodCondition | 30 | 30 / 20 / 10 | 50 / 30 / 20 | 1 | Singing / Rhythm / Expression | 107 |
| 2 | 仮アイドル2（理論R） | R | Logic | Impression | 30 | 10 / 30 / 20 | 20 / 50 / 30 | 2 | Rhythm / Expression / Singing | 207 |
| 3 | 仮アイドル3（感性SR） | SR | Sense | Focus | 32 | 40 / 30 / 20 | 70 / 50 / 30 | 3 | Expression / Singing / Rhythm | 107 |
| 4 | 仮アイドル4（理論SSR） | SSR | Logic | Motivation | 35 | 50 / 40 / 30 | 100 / 70 / 50 | 4 | Singing / Expression / Rhythm | 207 |

**`idolRecommendDeck`**（idol-support.md 1.3。Order 1 = アクティブ、2・3 = メンタル。12 行）: `GoodCondition` = 104, 101, 101、`Focus` = 103, 102, 102、`Impression` = 204, 201, 201、`Motivation` = 206, 203, 203（CardId）

**`commonDeckCard`**: Order 1〜5 = CardId 1, 1, 2, 3, 3（F01×2、F02、F03×2）

**`supportCard`**

| SupportCardId | Name | Rarity | Type | Plan |
|---|---|---|---|---|
| 1 | 仮サポート1 | R | Singing | Free |
| 2 | 仮サポート2 | R | Rhythm | Free |
| 3 | 仮サポート3 | R | Expression | Free |
| 4 | 仮サポート4 | R | Assist | Free |
| 5 | 仮サポート5 | R | Singing | Free |
| 6 | 仮サポート6 | SR | Singing | Sense |
| 7 | 仮サポート7 | SR | Rhythm | Logic |
| 8 | 仮サポート8 | SR | Expression | Free |
| 9 | 仮サポート9 | SSR | Singing | Sense |
| 10 | 仮サポート10 | SSR | Expression | Logic |

**`supportAbility`**（Kind / Param / ValueAtLv1 / ValueAtMax。`ParamBonus` は千分率。16 行）

| SupportCardId | Order 1 | Order 2 | Order 3 |
|---|---|---|---|
| 1 | InitialParam / Singing / 3 / 20 | | |
| 2 | InitialParam / Rhythm / 3 / 20 | | |
| 3 | InitialParam / Expression / 3 / 20 | | |
| 4 | MaxHp / Nothing / 1 / 3 | | |
| 5 | ParamBonus / Singing / 10 / 30 | | |
| 6 | InitialParam / Singing / 8 / 40 | ParamBonus / Singing / 20 / 60 | |
| 7 | InitialParam / Rhythm / 8 / 40 | MaxHp / Nothing / 2 / 6 | |
| 8 | ParamBonus / Expression / 20 / 60 | MaxHp / Nothing / 2 / 6 | |
| 9 | InitialParam / Singing / 15 / 65 | ParamBonus / Singing / 30 / 85 | |
| 10 | InitialParam / Expression / 15 / 65 | ParamBonus / Expression / 30 / 85 | MaxHp / Nothing / 3 / 9 |

**`supportLevelCost`**（idol-support.md 2.2）: `Level` L → L+1 の `SupportPoints` = `基準 × L`、`Gold` = `SupportPoints` と同じ。基準: R 39、SR 65、SSR 117。`Level` の範囲: R 1〜39、SR 1〜49、SSR 1〜59（147 行）

**`supportLevelCap`**: R = 20, 25, 30, 35, 40 / SR = 30, 35, 40, 45, 50 / SSR = 40, 45, 50, 55, 60（`LimitBreak` 0〜4 の順。15 行）

**`initialSupportCard`**: Order 1〜5 = SupportCardId 1〜5

**`initialSetting`**: `Id` 1、`GachaCurrency` 2500、`Gold` 10000、`SupportPoints` 10000

#### 3.6.3 ガチャ

**`gacha`**: `GachaId` 1、`SingleCost` 250、`TenCost` 2500、`NormalTableId` 1、`GuaranteedTableId` 2、`CeilingPoints` 200

**`gachaDetailTable`**: `GachaTableId` 1、2

**`gachaTableEntry`**（重みは排出率の 0.01% 単位。gacha.md 3章の率: 育成アイドル SSR 2.0%・SR 6.8%・R 31.2%、サポートカード SSR 3.0%・SR 10.2%・R 46.8% を、同じレアの中で等分した）

| GachaTableId | 行（Order 順。ItemKind / 相手の ID / Weight） |
|---|---|
| 1（通常枠。14 行） | Idol 1 / 1560、Idol 2 / 1560、Idol 3 / 680、Idol 4 / 200、SupportCard 1〜5 / 各 936、SupportCard 6〜8 / 各 340、SupportCard 9〜10 / 各 150 |
| 2（確定枠。7 行） | Idol 3 / 680、Idol 4 / 200、SupportCard 6〜8 / 各 340、SupportCard 9〜10 / 各 150 |

（`Order` は各テーブルの中で 1 から、表の行の順に連番。`Idol` の行は `IdolId` に、`SupportCard` の行は `SupportCardId` に ID を書き、もう一方は空欄。通常枠の合計は 10000、確定枠は 2200 で SSR の割合は約 22.7%）

**`gachaCeilingItem`**: GachaId 1 の Order 1 = Idol 4、Order 2 = SupportCard 9

**`duplicateConversion`**（gacha.md 5章・idol-support.md 3章）: Idol の SSR 50 / SR 25 / R 5、SupportCard の SSR 3000 / SR 1000 / R 300（6 行）

## 4. タスク

1/2 の「タスク」の冒頭と同じ（各タスクの終わりにビルド・テストが通る。生成物を手で直さない。設計判断に出会ったら止まって報告する）。テストは、1/2 の `MasterDataTests.cs`・`MasterDataInvariantTests.cs` に足す。テーブルは `MemoryDatabase` の `<クラス名>Table`（`All`・`Count`）で引く。

### タスク 1: プロデュースのマスタ

1. `Definitions/Produce.cs` を 3.2・3.3 のとおり書く（`IValidatable<T>` は 1/2 の 3.3 の書き方）
2. CSV 12 を 3.6.1 のとおり作る（`playerLevelExp` は PowerShell の 1 行などで作ってよい）
3. `powershell -NoProfile -File masterdata/gen.ps1 master-gen` → `check` → ビルド・テスト
4. `MasterDataTests` に足す: `Produce_HaveExpectedCounts`（`scenario` 2、`scenarioWeek` 16、`weekActionSlot` 17、`classOption` 12、`examPattern` 4、`examRankReward` 6、`scoreBonusSetting` 1、`evaluationScoreBand` 7、`evaluationRank` 13、`evaluationRankReward` 13、`offerRarityWeight` 4、`playerLevelExp` 99）。`Scenario2_Week6_IsMidExamWithRival900`（`ExamEarlyEndScore` 900、`RivalBaseScore` 900）。`PlayerLevelExp_EveryRowIs40TimesLevel`（99 行すべて）。`MasterDataInvariantTests` に 3.3 の「表をまたぐ検証」を足す。`Difficulty`・`WeekKind`・`ActionKind`・`ExamKind`・`ProduceRank` は元の enum がまだないので、写しの比較テストは足さない（元の enum を `Uta.Shared.Domain` に作る計画が、比較テストを足す）

### タスク 2: 育成アイドル・サポートカードのマスタ

1. `Definitions/IdolSupport.cs` を 3.2・3.4 のとおり書く
2. CSV 9 を 3.6.2 のとおり作る（`supportLevelCost` は PowerShell の 1 行などで作ってよい）
3. `gen.ps1 master-gen` → `check` → ビルド・テスト
4. `MasterDataTests` に足す: `IdolSupport_HaveExpectedCounts`（`idol` 4、`idolRecommendDeck` 12、`commonDeckCard` 5、`supportCard` 10、`supportAbility` 16、`supportLevelCost` 147、`supportLevelCap` 15、`initialSupportCard` 5、`initialSetting` 1）。`Idols_UniqueCardsAre107Or207`。`SupportLevelCost_EveryRowIsBaseTimesLevel`（R 39・SR 65・SSR 117 に `Level` をかけた値で、`Gold` は `SupportPoints` と同じ。147 行すべて）。`InitialSetting_Is2500_10000_10000`。`MasterDataInvariantTests` に 3.4 の「表をまたぐ検証」を足す。`RecommendKind`・`SupportType`・`SupportAbilityKind` は元の enum がまだないので、写しの比較テストは足さない（元の enum を作る計画が足す）

### タスク 3: ガチャのマスタ

1. `Definitions/Gacha.cs` を 3.2・3.5 のとおり書く
2. CSV 5 を 3.6.3 のとおり作る
3. `gen.ps1 master-gen` → `check` → ビルド・テスト
4. `MasterDataTests` に足す: `Gacha_HaveExpectedCounts`（`gacha` 1、`gachaDetailTable` 2、`gachaTableEntry` 21、`gachaCeilingItem` 2、`duplicateConversion` 6）。`GachaTables_WeightSums`（テーブル 1 の合計 10000、テーブル 2 の合計 2200）。`GachaTables_RarityShares`（テーブル 1 の育成アイドル SSR = 200、サポートカード SSR = 300、SR の合計 = 1700、R の合計 = 7800。`Idol`・`SupportCard` の `Rarity` を引いて集計する）。`Gacha_CeilingItems`（Idol 4 と SupportCard 9）。`ItemKind` も元の enum（`Uta.Shared.Contracts`）がまだないので、写しの比較テストは足さない（作る計画が足す）。`MasterDataInvariantTests` に 3.5 の「表をまたぐ検証」を足す

### タスク 4: wiki の反映（メインのセッション。確認のあと）

- [produce-tables.md](../wiki/architecture/master/produce-tables.md)・[idol-support-tables.md](../wiki/architecture/master/idol-support-tables.md)・[gacha-tables.md](../wiki/architecture/master/gacha-tables.md): `GachaTable`（排出テーブルの見出し）を `GachaDetailTable` に直す（gacha-tables.md・master/README.md の表・gacha.md の該当箇所。列名 `GachaTableId` などは変えない）。この計画で付けた enum の名前とメンバー（`Difficulty`・`WeekKind`・`ExamKind`・`ProduceRank`・`RecommendKind`）を表の列の説明に書き、検証の節を「行の検査は `IValidatable`、表をまたぐ検査はサーバーのテスト」に直し（3.3〜3.5 の分け方を写す。追加した検証も含む）、実装メモを書く。master/README.md 5章の「逆向き（`EvaluationRankReward.Rank` → `EvaluationRank`）はある」を直す
- [idol-support.md](../wiki/systems/idol-support.md): 未決事項のうち「具体的な数値」だけを決めたこと（仮の値）に直し（オリジナル名称は game-spec.md の未決事項のまま。名前は仮称）、1.2 に「MVP は固有スキルカードに S07・L07 を流用する（game-spec.md）」と注記する
- [game-spec.md](../wiki/overview/game-spec.md): 未決事項の「Uta のキャラクターのステータス・サポートカードの値」「ガチャ 1 本の中身」を決めたこと（仮の値）に直し、実装計画の単位と順の 2b を `done` の扱いにする
- `python -I .claude/skills/linting-wiki/lint_wiki.py` で点検。この計画を `status: done` にする

## 5. テスト

4章の各タスクのテストを足す。コマンドは 1/2 の 5章と同じ。Unity・実機でしか確かめられないものはない。

## 6. 未決事項

なし（7章の確認は 2026-11-01 に済んだ）。

## 7. 確認してほしいこと（2026-11-01 にユーザーが確認済み）

- **仮の値を置いた箇所**（ユーザーの許可あり。2026-11-01）: 育成アイドル 4 人・サポートカード 10 種のステータスとアビリティ、名前（「仮アイドル1」など）、最短シナリオの値
- **天井の交換対象**: 育成アイドル SSR とサポートカード SSR の 1 つずつにした。「SSR の 2 種」が SSR のサポートカード 2 種を指すなら `gachaCeilingItem` を直す
- **enum の名前**: `Difficulty`・`WeekKind`・`ExamKind`・`ProduceRank`・`RecommendKind`。`Uta.Shared.Domain` にまだない enum は、作るときにこの計画の名前・数値に合わせる。`ItemKind` の DTO 側（`Uta.Shared.Contracts`）も同じ
- **検証の追加**: tables ページにない検証を足した（`Idol` の `Recommend` とプランの対応、`FinalExam` の `ExamEarlyEndScore` = 0 など）。タスク 4 で tables ページに書き写す
