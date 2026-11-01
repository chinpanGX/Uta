---
title: 実装計画: Uta.Shared.Domain の週の進行
status: done
updated: 2026-11-01
spec: ../wiki/systems/produce.md, ../wiki/architecture/produce-domain.md, ../wiki/systems/idol-support.md, ../wiki/architecture/lesson-phases.md
---

# 実装計画: Uta.Shared.Domain の週の進行

## 1. 目的と前提

[produce.md](../wiki/systems/produce.md) 10章の局面の遷移（開始 → 週の選択 → レッスン → カードの選択肢 → 週の終わり → 試験 → 試験の結果 → 終了）を、`shared/Uta.Shared.Domain` に実装する。[game-spec.md](../wiki/overview/game-spec.md) の「実装計画の単位と順」の順 4。受け持つ完了条件は 5 の「状態の再生」の部分（同じシードと操作列で、再生した状態が項目ごとに一致する。[produce-domain.md](../wiki/architecture/produce-domain.md) 7章）。

前提（実装済み）: レッスンのエンジン（`LessonEngine`・`LessonState`・`ExamSetup`・`LessonResult`。[lesson-domain-types.md](../wiki/architecture/lesson-domain-types.md)）、乱数（`RandomStreams`・`RandomOps`）、マスタ 31 テーブルと CSV（`masterdata/csv/`）。

関係する ADR: [0008](../wiki/decisions/0008-shared-domain-and-onion.md)（Shared.Domain はシードを持たず、サーバー権威）、[0016](../wiki/decisions/0016-reference-game-values.md)（値）。

**この計画で決めたこと**（ユーザーの判断。2026-11-01）:

- マスタは「行の型＋`ProduceCatalog.Create`」で受け取る（`CardRows`／`CardCatalog.Create` と同じ形）。サーバー・クライアントが masterdata の表を行の型に詰め替える
- `ProduceSnapshot`（状態の保存・復元用の平らな型）は作らない。順 6 の計画で、持続効果の識別（由来のカード ID・面・効果の位置）を含めて設計する

**この計画で決めたこと [Uta案。設計書の食い違い・不足を埋めたもの。実装後に [produce-domain.md](../wiki/architecture/produce-domain.md) に反映する]**:

- 操作の表し方: `ProduceCommand`（種類 `OperationKind` と引数 3 つ）が操作の 1 件。記録 `OperationRecord` は、コマンドに操作番号・週・挑戦回数・ターンを付けたもの。記録だけの種類の enum は作らない
- 操作番号の食い違い（`PRODUCE_STALE_OPERATION`）の判定はサーバーの `Api` が行う。`ProduceEngine.Apply` は受け付けたとき `NextOperationNumber` を 1 増やして記録を足すだけで、要求の番号は見ない
- `ProduceStartInput` は育成アイドルの ID だけを持つ（トレーニング・覚醒は MVP で作らない。作るときに足す）
- 乱数は `IRandomStreamSource`（`Uta.Shared.Domain.Rng`）から受け取る。契約: 同じ `ProduceState` に対する一連の呼び出し（要求をまたぐ場合を含む）で、同じキーには**前の続きから引けるストリーム**を返す。サーバーは保存済みのストリームの状態から復元して返し、要求の終わりに状態を保存する（produce-domain.md 6章）。1 要求の間は同じオブジェクトを返してよい
- `ICardCatalog` に `CardIds`（昇順）を足す（スキルカードの選択肢の候補を数えるため）

**この計画で決めないこと（範囲外）**:

- `ProduceSnapshot`、`ProduceState` の保存の形（順 6）
- `server/`・`client/`・`masterdata/` の変更。サーバー・クライアントの行の型への詰め替え（順 6・7）
- 特別レッスン、おでかけ、相談、ドリンク、育成アイテム、信頼度、サポートイベント、スキルカードサポート、レンタル枠、トレーニング・覚醒、再抽選・除去（残り回数 0 なので常に拒否）、中級・上級の週構成、プレイヤーLv の更新と報酬の付与（サーバー）
- クライアントが `ProduceState` を持たずに「選べるか」を判定する入口（`GetWeekChoices` などは `ProduceState` を受ける。クライアントが DTO から判定するときの入口は順 7 で決める）
- IL2CPP の実機での再生の確認（`client/uta/Assets/Tests/SharedDomainReplay/`）
- 自動プレイによるバランス調整（produce.md 8章）

## 2. 触るファイル

`D` = `shared/Uta.Shared.Domain/Produce`、`T` = `shared/Uta.Shared.Domain.Tests/Produce`。名前空間はそれぞれ `Uta.Shared.Domain.Produce`、`Uta.Shared.Domain.Tests.Produce`。

| 操作 | パス | 中身 | タスク |
|---|---|---|---|
| 変更 | `shared/Uta.Shared.Domain/Lesson/CardDefinition.cs` | `ICardCatalog` に `CardIds` | 1 |
| 作成 | `shared/Uta.Shared.Domain/Rng/IRandomStreamSource.cs` | 乱数ストリームを渡すポート | 1 |
| 作成 | `D/ProduceEnums.cs` | enum 一式 | 1 |
| 作成 | `D/ProduceRows.cs` | マスタの行の型 | 1 |
| 作成 | `D/ProduceRules.cs` | ルールの定数 | 1 |
| 変更 | `shared/Uta.Shared.Domain.Tests/Lesson/TestCardCatalog.cs` | `CardIds` | 1 |
| 作成 | `T/TestRandomStreamSource.cs`・`T/TestRandomStreamSourceTests.cs` | テスト用のストリームの供給元 | 1 |
| 作成 | `D/ProduceMasterRows.cs`・`D/ProduceDefinitions.cs`・`D/ProduceCatalog.cs` | 行から定義を組み立てる | 2 |
| 作成 | `T/TestProduceMaster.cs`・`T/ProduceCatalogTests.cs` | CSV を読むテスト用の読み込みと検査のテスト | 2 |
| 作成 | `D/ProduceFormulas.cs`・`T/ProduceFormulasTests.cs` | 計算式 | 3 |
| 作成 | `D/ProduceCommand.cs`・`D/ProduceInputs.cs`・`D/FormationValidator.cs` | コマンド・記録・開始時の入力・編成の検証 | 4 |
| 作成 | `T/ProduceCommandTests.cs`・`T/FormationValidatorTests.cs` | 同上のテスト | 4 |
| 作成 | `D/OfferPool.cs`・`D/ExamTurnBuilder.cs` | カードの選択肢の抽選、審査ターンの列 | 5 |
| 作成 | `T/ScriptedRandom.cs`・`T/OfferPoolTests.cs`・`T/ExamTurnBuilderTests.cs` | テスト用の乱数と、同上のテスト | 5 |
| 作成 | `D/ProduceState.cs`・`D/ProduceStateComparer.cs`・`D/ProduceEngine.cs` | 状態・比較・エンジン（試験の手前まで） | 6 |
| 作成 | `T/ProduceBot.cs`・`T/ProduceEngineStartTests.cs`・`T/ProduceEngineWeekTests.cs`・`T/ProduceEngineLessonTests.cs`・`T/ProduceStateComparerTests.cs` | エンジンのテスト | 6 |
| 変更 | `D/ProduceEngine.cs` | 試験・再挑戦・終了 | 7 |
| 作成 | `T/ProduceEngineExamTests.cs` | 試験のテスト | 7 |
| 作成 | `T/ProduceReplayTests.cs` | 再生のテスト（完了条件 5 の再生の部分） | 8 |

## 3. 型とシグネチャ

C# は `shared/` の `.editorconfig` に従う（C# 9。ブロック形式の namespace、Allman、`var`、`record` なし、プライマリコンストラクタなし）。`netstandard2.1`、`Nullable` は無効、`TreatWarningsAsErrors`。`float`・`double`・`System.Random`・リフレクション（`Enum.IsDefined`・`Enum.Parse` など）を `shared/Uta.Shared.Domain` で使わない。`.cs` は BOM 付き UTF-8、改行は LF。公開する型には、既存の `Lesson/` のコードと同じ密度で `<summary>` を日本語で付ける。

`using` は使う名前空間だけを書く。`Plan`・`Rarity`・`ParamKind`・`CardType`・`CardInstance`・`LessonState`・`LessonEngine` などは `Uta.Shared.Domain.Lesson`。

### 3.1 enum（`D/ProduceEnums.cs`）

値を明示する。マスタの enum（`masterdata/Definitions/Produce.cs`・`IdolSupport.cs`）と名前・数値を同じにする。

```csharp
public enum Difficulty { Nothing = 0, Beginner = 1, Intermediate = 2, Advanced = 3 }
public enum WeekKind { Nothing = 0, Action = 1, Finishing = 2, MidExam = 3, FinalExam = 4 }
public enum ActionKind { Nothing = 0, Lesson = 1, Class = 2, Supply = 3, Outing = 4, Consult = 5 }
public enum ExamKind { Nothing = 0, Mid = 1, Final = 2 }
public enum ProduceRank { Nothing = 0, F = 1, E = 2, D = 3, C = 4, CPlus = 5, B = 6, BPlus = 7, A = 8, APlus = 9, S = 10, SPlus = 11, SS = 12, SSPlus = 13 }
public enum RecommendKind { Nothing = 0, GoodCondition = 1, Focus = 2, Impression = 3, Motivation = 4 }
public enum SupportType { Nothing = 0, Singing = 1, Rhythm = 2, Expression = 3, Assist = 4 }
public enum SupportAbilityKind { Nothing = 0, InitialParam = 1, ParamBonus = 2, MaxHp = 3 }
```

この計画で足す enum（Domain だけの型）:

```csharp
public enum ProducePhase { Nothing = 0, WeekSelect = 1, CardOffer = 2, Lesson = 3, Exam = 4, ExamResult = 5, Finished = 6 }
public enum OperationKind   // produce-domain.md 5章の表のとおり（値を保存に使う）
{ Nothing = 0, ChooseWeekAction = 1, ConsultExchange = 2, ConsultEnhance = 3, ConsultRemove = 4, ConsultFinish = 5,
  OfferPick = 6, OfferReroll = 7, OfferRemove = 8, OfferDecline = 9, DiscardDrink = 10, ExamRetry = 11, ExamNoRetry = 12,
  UseCard = 13, UseDrink = 14, Skip = 15 }
public enum ProduceApplyResult { Accepted = 0, UnexpectedPhase = 1, InvalidArgument = 2 }
public enum ExamOutcome { Nothing = 0, MidFailed = 1, FinalFailed = 2, FinalPassed = 3 }   // produce_results の「試験の結果」
public enum FormationCheck { Ok = 0, DuplicateCardId = 1, PlanMismatch = 2 }
public enum WeekChoiceKind { Nothing = 0, Lesson = 1, Class = 2, Supply = 3, Rest = 4 }
```

### 3.2 マスタの行の型（`D/ProduceRows.cs`）

すべて `public readonly struct`。**コンストラクタの引数は、下の表の列の順（= `masterdata/csv/<表>.csv` の列の順。引数名は列名の camelCase）**。同じ名前の get-only プロパティ（PascalCase）を持つ。コンストラクタでは検査しない（検査は `ProduceCatalog.Create`）。

| 型 | 引数（型 名前）。対応する CSV |
|---|---|
| `ScenarioRow` | `int ScenarioId`、`string Name`、`Difficulty Difficulty`、`int WeekCount`、`int ParamCap`、`int RetryCount`（scenario.csv） |
| `ScenarioWeekRow` | `int ScenarioId`、`int Week`、`WeekKind Kind`、`int TurnsSense`、`int TurnsLogic`、`int ClearTarget`、`int PerfectTarget`、`int ExamEarlyEndScore`、`int RivalBaseScore`（scenarioWeek.csv） |
| `WeekActionSlotRow` | `int ScenarioId`、`int Week`、`int Order`、`ActionKind Kind`（weekActionSlot.csv） |
| `ClassOptionRow` | `int ScenarioId`、`int Week`、`int Order`、`int HpCost`、`int ParamGain`、`bool GivesCard`（classOption.csv） |
| `ExamPatternRow` | `int ExamPatternId`、`int FirstPercent`、`int SecondPercent`、`int ThirdPercent`、`int MidFirstTurns`、`int MidSecondTurns`、`int MidThirdTurns`、`int FinalSenseFirstTurns`、`int FinalSenseSecondTurns`、`int FinalSenseThirdTurns`、`int FinalLogicFirstTurns`、`int FinalLogicSecondTurns`、`int FinalLogicThirdTurns`、`int FirstTurnSecondRatePerTenThousand`（examPattern.csv） |
| `ExamRankRewardRow` | `ExamKind ExamKind`、`int Rank`、`int ParamAll`（examRankReward.csv） |
| `ScoreBonusSettingRow` | `int Id`、`int APerThousand`、`int BFirst`、`int BSecond`、`int BThird`（scoreBonusSetting.csv） |
| `EvaluationScoreBandRow` | `int Order`、`int MinScore`、`int ScalePerThousand`、`int Fixed`（evaluationScoreBand.csv） |
| `EvaluationRankRow` | `ProduceRank Rank`、`int MinValue`（evaluationRank.csv） |
| `OfferRarityWeightRow` | `Rarity Rarity`、`int Weight`（offerRarityWeight.csv） |
| `IdolRow` | `int IdolId`、`string Name`、`Rarity Rarity`、`Plan Plan`、`RecommendKind Recommend`、`int MaxHp`、`int InitialSinging`、`int InitialRhythm`、`int InitialExpression`、`int BonusSingingPerThousand`、`int BonusRhythmPerThousand`、`int BonusExpressionPerThousand`、`int ExamPatternId`、`ParamKind FirstParam`、`ParamKind SecondParam`、`ParamKind ThirdParam`、`int UniqueCardId`（idol.csv） |
| `IdolRecommendDeckRow` | `RecommendKind Recommend`、`int Order`、`int CardId`（idolRecommendDeck.csv） |
| `SupportCardRow` | `int SupportCardId`、`string Name`、`Rarity Rarity`、`SupportType Type`、`Plan Plan`（supportCard.csv） |
| `SupportAbilityRow` | `int SupportCardId`、`int Order`、`SupportAbilityKind Kind`、`ParamKind Param`、`int ValueAtLv1`、`int ValueAtMax`（supportAbility.csv） |
| `SupportLevelCapRow` | `Rarity Rarity`、`int LimitBreak`、`int MaxLevel`（supportLevelCap.csv） |
| `CommonDeckCardRow` | `int Order`、`int CardId`（commonDeckCard.csv） |

`ExamKind ExamKind` と `int Rank` のような「型と同じ名前のプロパティ」は C# で許される（引数名は `examKind`）。

### 3.3 ルールの定数（`D/ProduceRules.cs`）

```csharp
public static class ProduceRules
{
    public const int ParamCount = 3;                       // 歌唱・律動・表現
    public const int OfferSlotCount = 3;                   // スキルカードの選択肢の枠数（produce.md 3.5）
    public const int RestRecoverPercent = 70;              // 休む（produce.md 3.3）
    public const int FinishingRecoverPercent = 70;         // 仕上げレッスンの後（produce.md 4.2）
    public const int PerfectRecoverPerRemainingTurn = 2;   // パーフェクトで終わったとき（produce.md 4.3）
    public const int PassRank = 3;                         // 3 位以内で合格（produce.md 5.1）
    public const int NpcCount = 5;
    public const int MaxSupportCards = 5;                  // idol-support.md 2.1
    public const int MaxLimitBreak = 4;
    public const int ParamEvaluationPerThousand = 2300;    // produce.md 6章のパラメータ分の倍率（2.3）
    public static int NpcPercent(int index);               // 0..4 → 80, 65, 50, 35, 20。範囲外は ArgumentOutOfRangeException
    public static int FinalRankScore(int rank);            // 1 → 1700、2 → 900、3 → 500、それ以外（0 を含む）→ 0
}
```

`ProduceParams`（同じファイル。`public static class`）: パラメータの添字。`int ToIndex(ParamKind kind)` は `Singing` → 0、`Rhythm` → 1、`Expression` → 2、それ以外は `ArgumentOutOfRangeException`。`ParamKind FromIndex(int index)` は逆（範囲外は同じ例外）。`bool IsValid(ParamKind kind)` は 1〜3 のとき真。

### 3.4 乱数の供給元（`Rng/IRandomStreamSource.cs`）

```csharp
namespace Uta.Shared.Domain.Rng
{
    /// <summary>キーごとの乱数ストリームを渡すポート。同じキーには、続きから引ける同じストリームを返す（produce-domain.md 6章）。</summary>
    public interface IRandomStreamSource
    {
        IRandom Get(RandomStreamKey key);
    }
}
```

`ICardCatalog`（`Lesson/CardDefinition.cs`）に `IReadOnlyList<int> CardIds { get; }`（昇順）を足す。`CardCatalog` はすでに同名のプロパティを持つ（変更しない）。

### 3.5 定義の型とカタログ（タスク 2）

`D/ProduceMasterRows.cs`: `public sealed class ProduceMasterRows`。次のプロパティを持つ。型は `IReadOnlyList<行の型>`、get/set、初期値は空の配列（`new XRow[0]`）。`Scenarios`（`ScenarioRow`）、`ScenarioWeeks`、`WeekActionSlots`、`ClassOptions`、`ExamPatterns`、`ExamRankRewards`、`ScoreBonusSettings`、`EvaluationScoreBands`、`EvaluationRanks`、`OfferRarityWeights`、`Idols`、`IdolRecommendDecks`、`SupportCards`、`SupportAbilities`、`SupportLevelCaps`、`CommonDeckCards`（行の型は 3.2 の表の名前に対応）。

`D/ProduceDefinitions.cs`（すべて `public sealed class`。コンストラクタは `internal`、プロパティは get-only）:

| 型 | プロパティ |
|---|---|
| `ClassOptionDefinition` | `int HpCost`、`int ParamGain`、`bool GivesCard` |
| `WeekDefinition` | `int Week`、`WeekKind Kind`、`int TurnsSense`、`int TurnsLogic`、`int ClearTarget`、`int PerfectTarget`、`int ExamEarlyEndScore`、`int RivalBaseScore`、`IReadOnlyList<ActionKind> Actions`（`Order` の昇順）、`IReadOnlyList<ClassOptionDefinition> ClassOptions`（`Order` の昇順）、`int GetTurns(Plan plan)`（`Sense` なら `TurnsSense`、それ以外は `TurnsLogic`） |
| `ScenarioDefinition` | `int ScenarioId`、`string Name`、`Difficulty Difficulty`、`int WeekCount`、`int ParamCap`、`int RetryCount`、`IReadOnlyList<WeekDefinition> Weeks`（週 1 から）、`WeekDefinition GetWeek(int week)`（1 以上 `WeekCount` 以下。範囲外は `ArgumentOutOfRangeException`） |
| `ExamPatternDefinition` | `int ExamPatternId`、`int FirstTurnSecondRatePerTenThousand`、`int[] GetTurnCounts(ExamKind kind, Plan plan)`（新しい長さ 3 の配列 `{1番, 2番, 3番}`。`Mid` は `Mid*`、`Final` かつ `Sense` は `FinalSense*`、`Final` かつそれ以外は `FinalLogic*`。`kind` が `Nothing` なら `ArgumentOutOfRangeException`） |
| `IdolDefinition` | `int IdolId`、`string Name`、`Rarity Rarity`、`Plan Plan`、`RecommendKind Recommend`、`int MaxHp`、`int ExamPatternId`、`ParamKind FirstParam`・`SecondParam`・`ThirdParam`、`int UniqueCardId`、`IReadOnlyList<int> RecommendCardIds`（`IdolRecommendDeck` の `Order` の昇順）、`int GetInitialParam(ParamKind kind)`（`Nothing` は `ArgumentOutOfRangeException`。`GetBonusPerThousand` も同じ）、`int GetBonusPerThousand(ParamKind kind)`、`ParamKind GetExamParam(int rank)`（1 → `FirstParam`、2 → `SecondParam`、3 → `ThirdParam`、それ以外は `ArgumentOutOfRangeException`） |
| `SupportAbilityDefinition` | `SupportAbilityKind Kind`、`ParamKind Param`、`int ValueAtLv1`、`int ValueAtMax` |
| `SupportCardDefinition` | `int SupportCardId`、`string Name`、`Rarity Rarity`、`SupportType Type`、`Plan Plan`、`IReadOnlyList<SupportAbilityDefinition> Abilities`（`Order` の昇順）、`int MaxLevelAtFullLimitBreak`（同じレアの `SupportLevelCap` の `LimitBreak` = 4 の `MaxLevel`） |

`D/ProduceCatalog.cs`:

```csharp
public sealed class ProduceCatalog
{
    /// <summary>行から組み立てる。問題があれば InvalidOperationException（メッセージに問題を全部、改行区切りで入れる）。</summary>
    public static ProduceCatalog Create(ProduceMasterRows rows, ICardCatalog cards);   // rows か cards が null なら ArgumentNullException

    public ICardCatalog Cards { get; }
    public ScenarioDefinition GetScenario(int scenarioId);          // 以下 4 つ、無ければ KeyNotFoundException
    public IdolDefinition GetIdol(int idolId);
    public SupportCardDefinition GetSupportCard(int supportCardId);
    public ExamPatternDefinition GetExamPattern(int examPatternId);
    public IReadOnlyList<int> CommonDeckCardIds { get; }            // CommonDeckCard の Order の昇順
    public int GetOfferWeight(Rarity rarity);                       // OfferRarityWeight の Weight
    public int GetMaxLevel(Rarity rarity, int limitBreak);          // SupportLevelCap。無ければ ArgumentOutOfRangeException
    public int GetExamRankParamAll(ExamKind kind, int rank);        // ExamRankReward の ParamAll。rank が 1〜3 でない、または行が無ければ 0
    public int ScoreBonusA { get; }                                 // ScoreBonusSetting の APerThousand
    public int GetScoreBonusB(int rank);                            // 1 → BFirst、2 → BSecond、3 → BThird。それ以外は ArgumentOutOfRangeException
    public IReadOnlyList<EvaluationScoreBandRow> EvaluationScoreBands { get; }   // Order の昇順
    public IReadOnlyList<EvaluationRankRow> EvaluationRanks { get; }             // MinValue の昇順（F から SSPlus）
}
```

`Create` の検査。**問題は見つけた分をすべて集めて、最後に 1 つの `InvalidOperationException` にする**（途中で投げない。同じメッセージは 1 回だけ）。メッセージは `"<対象>: <内容>"` の形で、下の表の「必ず含む語」をメッセージに入れる（ほかの文言は自由）。テストは「必ず含む語」だけを見る。

| # | 検査 | 必ず含む語 |
|---|---|---|
| V1 | 主キーの重複（`Scenario`: ScenarioId／`ScenarioWeek`: ScenarioId+Week／`WeekActionSlot`・`ClassOption`: ScenarioId+Week+Order／`ExamPattern`／`Idol`／`SupportCard`／`SupportLevelCap`: Rarity+LimitBreak／`CommonDeckCard`: Order／`IdolRecommendDeck`: Recommend+Order／`SupportAbility`: SupportCardId+Order／`ExamRankReward`: ExamKind+Rank／`OfferRarityWeight`: Rarity／`EvaluationScoreBand`: Order／`EvaluationRank`: Rank） | 表の名前（例 `Scenario 1`）と `重複` |
| V2 | シナリオ: `WeekCount` ≥ 1、`ParamCap` ≥ 1、`RetryCount` ≥ 0。週が 1〜`WeekCount` にちょうど 1 つずつある（`WeekCount` を超える週・足りない週は問題）。最後の週が `FinalExam` | 最後の週: `FinalExam` |
| V3 | 週: `Kind` が `Nothing` でない。`ActionKind` が `Lesson`・`Class`・`Supply` 以外（`Nothing`・`Outing`・`Consult`）なら問題。同じ種類の枠が 1 週に 2 つ以上あれば問題。`Action` の週は枠が 1 つ以上。`Finishing` の週は枠がちょうど 1 つで `Lesson`。試験の週に枠があれば問題。`Lesson` の枠がある週は `TurnsSense`・`TurnsLogic`・`ClearTarget`・`PerfectTarget` がすべて 1 以上。`Class` の枠がある週は `ClassOption` が 1 つ以上、`Class` の枠が無い週に `ClassOption` があれば問題。試験の週は `RivalBaseScore` ≥ 1 | 未対応の行動: `未対応`。そのほかは週を特定する語（例 `Scenario 2 week 3`） |
| V4 | `ExamPattern`: `Mid`・`FinalSense`・`FinalLogic` のそれぞれで、1 番のターン数 ≥ 2、2 番 ≥ 1、3 番 ≥ 1。`FirstTurnSecondRatePerTenThousand` が 0〜10000。率が 1 以上なら、それぞれで 2 番のターン数 ≥ 2 | `ExamPattern 1`（ID を入れる） |
| V5 | `Idol`: `Plan` が `Sense`・`Logic` のどちらか、`Rarity` が `Nothing` でない、`MaxHp` ≥ 1、初期パラメータ・ボーナスが 0 以上。`ExamPatternId` が存在する。`FirstParam`・`SecondParam`・`ThirdParam` が 3 つのパラメータの並べ替えになっている（`Nothing` や重複は問題）。`UniqueCardId` が `cards.CardIds` にある。`IdolRecommendDeck` が `Recommend` に対して `Order` 1〜3 の 3 行で、その `CardId` がすべて `cards.CardIds` にある | `Idol 1`、審査の順: `審査の順`、固有カード: `UniqueCardId` |
| V6 | `SupportCard`: `Rarity`・`Plan` が `Nothing` でない。同じレアの `SupportLevelCap` が `LimitBreak` 0〜4 にそろう。`SupportAbility`: 参照する `SupportCardId` が存在する、`Kind` が `Nothing` でない、`MaxHp` の行は `Param` が `Nothing`、それ以外は `Param` が 1〜3、`ValueAtLv1` ≥ 0、`ValueAtMax` ≥ `ValueAtLv1`。`SupportLevelCap.MaxLevel` ≥ 2 | `SupportCard 6`（ID を入れる） |
| V7 | `CommonDeckCard`: 1 行以上、`CardId` が `cards.CardIds` にある | `CommonDeckCard` |
| V8 | `OfferRarityWeight`: `N`・`R`・`SR`・`SSR` の 4 行がそろい、`Weight` ≥ 1 | `OfferRarityWeight` |
| V9 | `ScoreBonusSetting` がちょうど 1 行、各値が 0 以上。`EvaluationScoreBand` が 1 行以上で、`Order` の昇順に並べたとき先頭の `MinScore` が 0、`MinScore` が単調増加（等しいのも問題）、`ScalePerThousand`・`Fixed` が 0 以上 | `ScoreBonusSetting`、`EvaluationScoreBand` |
| V10 | `EvaluationRank`: `F`〜`SSPlus` の 13 行がそろい、`F` の `MinValue` が 0、ランク順に `MinValue` が単調増加。`ExamRankReward`: `Mid`・`Final` それぞれに `Rank` 1〜3 がそろい `ParamAll` ≥ 0 | `EvaluationRank`、`ExamRankReward` |

**検査の範囲**: `Create` は、エンジンが前提にする**構造**（主キー、連続する週、参照の存在、並べ替え、行がそろうこと）と、0 除算・負の値で計算が壊れる列だけを見る。値の大小の妥当性（報酬の量・確率の範囲など）はマスタの `IValidatable`（[produce-tables.md](../wiki/architecture/master/produce-tables.md)）の担当で、ここでは重ねない。

### 3.6 計算式（`D/ProduceFormulas.cs`。`public static class ProduceFormulas`）

整数だけで計算する。途中の掛け算は `long`、結果を `int` に戻す（`FixedMath.ToInt` を使う）。

```csharp
public static int AddParam(int current, int amount, int cap);     // 上限で止める加算（produce.md 4.3）。amount < 0 は ArgumentOutOfRangeException。min(cap, current + amount)
public static int Heal(int hp, int amount, int maxHp);            // 最大体力で止める。amount < 0 は ArgumentOutOfRangeException。min(maxHp, hp + amount)
public static int RestAmount(int maxHp);                          // maxHp * 70 / 100 の切り捨て
public static int BonusAmount(int amount, int bonusPerThousand);  // floor(amount * bonus / 1000)。どちらかが負なら ArgumentOutOfRangeException
public static int AbilityValue(int valueAtLv1, int valueAtMax, int level, int maxLevel);
    // idol-support.md 2.3: valueAtLv1 + floor((valueAtMax − valueAtLv1) * (level − 1) / (maxLevel − 1))
    // level が 1〜maxLevel の外、maxLevel < 2、valueAtMax < valueAtLv1 は ArgumentOutOfRangeException
public static int[] DistributeGain(bool finishing, ParamKind target, int gained, int clearTarget, ParamKind examFirstParam);
    // produce.md 4.3 の 1。長さ 3 の配列（添字は ProduceParams.ToIndex）。
    // 通常: target の添字に gained。仕上げ: toTarget = min(gained, clearTarget) を target に、excess = gained − toTarget を 3 等分（excess / 3 を全パラメータに、excess % 3 を examFirstParam に）
    // gained < 0、target か examFirstParam が Nothing は ArgumentOutOfRangeException
public static int PerfectRecover(int maxTurns, int lastTurn);     // max(0, maxTurns − lastTurn) * 2
public static int ScoreBonusPercent(ProduceCatalog catalog, int paramValue, int rank);
    // produce.md 5.2: floor(a * paramValue / 1000) + b[rank]。信頼度補正は MVP で 0 なので掛けない。rank が 1〜3 以外は ArgumentOutOfRangeException
public static IReadOnlyList<int> NpcScores(int rivalBaseScore);   // 長さ 5。i 番目 = floor(rivalBaseScore * ProduceRules.NpcPercent(i) / 100)
public static int ExamRank(int score, IReadOnlyList<int> npcScores);  // 1 + (npcScores のうち score より大きいものの数)。同点はプレイヤーが上
public static EvaluationResult Evaluate(ProduceCatalog catalog, int paramSum, int finalRank, int finalScore);
```

```csharp
public readonly struct EvaluationResult
{
    public EvaluationResult(int paramPart, int rankPart, int scorePart, ProduceRank rank);
    public int ParamPart { get; }
    public int RankPart { get; }
    public int ScorePart { get; }
    public int Value => ParamPart + RankPart + ScorePart;
    public ProduceRank Rank { get; }
}
```

`Evaluate`（produce.md 6章）: `ParamPart = paramSum * 2300 / 1000`（切り捨て）。`RankPart = ProduceRules.FinalRankScore(finalRank)`（`finalRank` が 0 は最終試験を受けていない）。`ScorePart`: `EvaluationScoreBands` のうち `MinScore` が `finalScore` 以下で最大のバンドを選び、`floor(finalScore * ScalePerThousand / 1000) + Fixed`。`Rank`: `EvaluationRanks` のうち `MinValue` が `Value` 以下で最大のもの。`paramSum` か `finalScore` が負、`finalRank` が負は `ArgumentOutOfRangeException`。

### 3.7 コマンド・記録・入力（タスク 4）

`D/ProduceCommand.cs`:

```csharp
public readonly struct ProduceCommand
{
    public ProduceCommand(OperationKind kind, int arg1, int arg2, int arg3);
    public OperationKind Kind { get; }
    public int Arg1 { get; }
    public int Arg2 { get; }
    public int Arg3 { get; }

    // 週の選択（produce-domain.md 5章: 引数1 = 行動の枠番号、引数2 = 選択肢の番号（無いとき -1）、引数3 = パラメータ（ParamKind の数値。レッスン以外は -1））
    public static ProduceCommand ChooseLesson(int slotIndex, ParamKind param);    // (ChooseWeekAction, slotIndex, -1, (int)param)
    public static ProduceCommand ChooseClass(int slotIndex, int optionIndex);     // (ChooseWeekAction, slotIndex, optionIndex, -1)
    public static ProduceCommand ChooseOther(int slotIndex);                      // (ChooseWeekAction, slotIndex, -1, -1)。支給・休む
    public static ProduceCommand OfferPick(int offerIndex);                       // (OfferPick, offerIndex, 0, 0)
    public static ProduceCommand OfferReroll();                                   // (OfferReroll, 0, 0, 0)
    public static ProduceCommand OfferRemove(int offerIndex);                     // (OfferRemove, offerIndex, 0, 0)
    public static ProduceCommand OfferDecline();                                  // (OfferDecline, 0, 0, 0)
    public static ProduceCommand ExamRetry();                                     // (ExamRetry, 0, 0, 0)
    public static ProduceCommand ExamNoRetry();                                   // (ExamNoRetry, 0, 0, 0)
    public static ProduceCommand UseCard(int instanceId);                         // (UseCard, instanceId, 0, 0)
    public static ProduceCommand UseDrink(int slotIndex);                         // (UseDrink, slotIndex, 0, 0)
    public static ProduceCommand Skip();                                          // (Skip, 0, 0, 0)
}

public sealed class OperationRecord
{
    public OperationRecord(int number, ProduceCommand command, int week, int attempt, int turn);
    public int Number { get; }               // 0 から連番
    public ProduceCommand Command { get; }
    public int Week { get; }                 // 受け付ける前の週番号
    public int Attempt { get; }              // 受け付ける前の ProduceState.Attempt（試験の外は 0）
    public int Turn { get; }                 // 受け付ける前のレッスン・試験のターン番号。レッスン・試験の外は 0
}
```

`D/ProduceInputs.cs`:

```csharp
public sealed class SupportCardInput
{
    public SupportCardInput(int cardId, int level, int limitBreak);   // level < 1、limitBreak が 0〜4 の外は ArgumentOutOfRangeException
    public int CardId { get; }
    public int Level { get; }
    public int LimitBreak { get; }
}

public sealed class ProduceStartInput
{
    public ProduceStartInput(int scenarioId, int idolId, int playerLevel, IReadOnlyList<SupportCardInput> supportCards);
    // supportCards が null → ArgumentNullException。playerLevel < 1 → ArgumentOutOfRangeException。6 枚以上、または要素が null → ArgumentException（型は完全一致。テストは Assert.Throws<その型>）。0 枚は許す。配列は複製して持つ
    public int ScenarioId { get; }
    public int IdolId { get; }
    public int PlayerLevel { get; }          // カードの UnlockLv の絞り込みに使う（produce.md 3.5）
    public IReadOnlyList<SupportCardInput> SupportCards { get; }   // 枠の順
}
```

`D/FormationValidator.cs`:

```csharp
public static class FormationValidator
{
    /// <summary>編成の検証の 5・6（idol-support.md 2.1）。1〜4（所持の解決）と 7 はサーバー。</summary>
    public static FormationCheck Validate(ProduceCatalog catalog, int idolId, IReadOnlyList<int> supportCardIds);
    // 5. 同じカード ID が 2 枚以上 → DuplicateCardId（先に見る）
    // 6. サポートカードの Plan が Free でなく、育成アイドルの Plan と違う → PlanMismatch
    // idolId・カード ID が無ければ KeyNotFoundException。0 枚は Ok
}
```

### 3.8 カードの選択肢と審査ターン（タスク 5）

```csharp
public static class OfferPool
{
    /// <summary>候補（produce.md 3.5）。cards.CardIds の昇順で、次をすべて満たすカード ID。
    /// (Plan が idol.Plan か Free) かつ (UnlockLv ≤ playerLevel) かつ (Type が Trouble でない) かつ (Unique でない、または deckCardIds に含まれない)</summary>
    public static IReadOnlyList<int> GetCandidates(ProduceCatalog catalog, IdolDefinition idol, int playerLevel, IEnumerable<int> deckCardIds);

    /// <summary>枠の順に最大 ProduceRules.OfferSlotCount 枚を引く（produce-domain.md 4章 の Offer）。</summary>
    public static IReadOnlyList<int> Draw(ProduceCatalog catalog, IReadOnlyList<int> candidates, IRandom random);
}
```

`Draw` の手順（枠ごと。`picked` = ここまでに引いたカード ID の集合）:

1. `available` = `candidates` から `picked` に入っているものを除いたもの（`candidates` の並びのまま）
2. `rarities` = `{N, R, SR, SSR}`（この順）のうち、`available` に定義のレアリティが一致するカードが 1 枚以上あるもの。`rarities` が空ならここで終了（以降の枠も引かない）
3. `rarity` = `rarities[RandomOps.WeightedIndex(random, rarities の GetOfferWeight)]`
4. `pool` = `available` のうちレアリティが `rarity` のもの。`picked` に `pool[RandomOps.NextInt(random, pool.Count)]` を足す
5. 結果は引いた順のカード ID の配列

```csharp
public static class ExamTurnBuilder
{
    /// <summary>審査ターンの列（produce.md 5.1）。paramValues は試験の開始時のパラメータ（添字は ProduceParams.ToIndex）。</summary>
    public static IReadOnlyList<ExamTurn> Build(ProduceCatalog catalog, IdolDefinition idol, ExamKind kind, IReadOnlyList<int> paramValues, IRandom random);
}
```

`Build` の手順:

1. `counts = catalog.GetExamPattern(idol.ExamPatternId).GetTurnCounts(kind, idol.Plan)`（添字 0 = 1 番、1 = 2 番、2 = 3 番）
2. `headRank` = `RandomOps.Chance(random, パターンの FirstTurnSecondRatePerTenThousand)` が真なら 2、偽なら 1。**率が 0 でも必ず 1 回引く**
3. 残りの数: `r1 = counts[0] − 1 − (headRank == 1 ? 1 : 0)`、`r2 = counts[1] − 1 − (headRank == 2 ? 1 : 0)`、`r3 = counts[2] − 1`
4. `middle` = 順位 1 を `r1` 個、順位 2 を `r2` 個、順位 3 を `r3` 個、この順に並べたリストを `RandomOps.Shuffle(random, middle)`
5. 順位の列 = `[headRank] + middle + [3, 2, 1]`
6. 各順位 `k` について `ExamTurn(idol.GetExamParam(k), ProduceFormulas.ScoreBonusPercent(catalog, paramValues[ProduceParams.ToIndex(idol.GetExamParam(k))], k))`

### 3.9 状態とエンジン（タスク 6・7）

`D/ProduceState.cs`（名前空間 `Uta.Shared.Domain.Produce`）。

```csharp
public sealed class ExamReport
{
    public ExamReport(ExamKind kind, int attempt, int score, int rank, IReadOnlyList<int> npcScores, bool passed);
    public ExamKind Kind { get; }  public int Attempt { get; }  public int Score { get; }
    public int Rank { get; }       public IReadOnlyList<int> NpcScores { get; }  public bool Passed { get; }
}

public sealed class ExamBackup    // 試験の開始直前の「育成」の区分（produce.md 5.1.1）
{
    public ExamBackup(IReadOnlyList<int> parameters, int hp, IReadOnlyList<CardInstance> deck, int nextInstanceId);
    public IReadOnlyList<int> Parameters { get; }   // 長さ 3（ProduceParams の添字）
    public int Hp { get; }  public IReadOnlyList<CardInstance> Deck { get; }  public int NextInstanceId { get; }
}

public sealed class ProduceResult
{
    public ProduceResult(ExamOutcome outcome, int evaluationValue, ProduceRank rank, int finalScore, int finalRank);
    public ExamOutcome Outcome { get; }  public int EvaluationValue { get; }  public ProduceRank Rank { get; }
    public int FinalScore { get; }       // 最終試験を受けていなければ 0
    public int FinalRank { get; }        // 同上
}

public sealed class ProduceState
{
    internal ProduceState(...);   // エンジンだけが作る。引数は自由。ただし ProduceCatalog・ProduceEngine・定義の型への参照を持たない（値と配列と LessonState だけ。順 6 が平らに保存できるようにするため）。ExamBackup を作るときは配列・デッキを複製する
    public ProduceStartInput Input { get; }
    public ProducePhase Phase { get; internal set; }
    public int Week { get; internal set; }                     // 1 から
    public int Attempt { get; internal set; }                  // 今の試験の挑戦回数（0 始まり）。Exam / ExamResult の間だけ 0 以上の意味を持ち、試験の外では 0（produce-domain.md 3章「レッスンは常に 0」と同じ）。直近の試験の挑戦回数は LastExam.Attempt
    public int NextOperationNumber { get; internal set; }
    public int NextInstanceId { get; internal set; }
    public int Hp { get; internal set; }
    public int MaxHp { get; }
    public int ExamRetryLeft { get; internal set; }
    public int GetParam(ParamKind kind);                       // 内部は int[3]
    public int GetBonusPerThousand(ParamKind kind);            // 開始時に決めた合算値（内部は int[3]）
    public IReadOnlyList<CardInstance> Deck { get; }
    public IReadOnlyList<int> ClassOptionParams { get; }       // 授業の選択肢ごとの ParamKind の数値。授業のある週の WeekSelect だけ値あり、ほかは空
    public IReadOnlyList<int> OfferCards { get; }              // CardOffer のときだけ値あり
    public LessonState Lesson { get; }                         // Lesson / Exam のときだけ。ほかは null
    public ExamBackup ExamBackup { get; }                      // Exam / ExamResult のときだけ。ほかは null
    public ExamReport LastExam { get; }                        // 直近に終わった試験。まだなければ null
    public ExamReport MidExam { get; }                         // 中間試験を終えたあとの結果（最後の挑戦）。まだなければ null
    public ProduceResult Result { get; }                       // Finished のときだけ
    public IReadOnlyList<OperationRecord> Operations { get; }
    // 書き換える操作はすべて internal
}
```

`D/ProduceStateComparer.cs`（`public static class`）: `IReadOnlyList<string> Describe(ProduceState state)` と `IReadOnlyList<string> Diff(ProduceState expected, ProduceState actual)`。形は `LessonStateComparer`（[計画](2026-11-01-shared-lesson-phases.md) 3.2）と同じ。**enum は数値、整数は `ToString(CultureInfo.InvariantCulture)`、真偽は `1`/`0`、列は `,` 連結（空の列は空文字）、`null` は `none`**。`Describe` の行は次の順（1 項目 1 行）:

| 順 | 行 | 値 |
|---|---|---|
| 1 | `scenario=` `idol=` `playerLv=` | それぞれの値 |
| 2 | `supports=` | `SupportCards` の順に `cardId:level:limitBreak` |
| 3 | `phase=` `week=` `attempt=` `nextOperation=` `nextInstanceId=` `retryLeft=` | それぞれの値（この順） |
| 4 | `singing=` `rhythm=` `expression=` `hp=` `maxHp=` | それぞれの値（この順） |
| 5 | `bonus=` | 歌唱・律動・表現の順の `GetBonusPerThousand` |
| 6 | `deck=` | `Deck` の順に `instanceId:cardId:enhanced` |
| 7 | `classParams=` `offer=` | `ClassOptionParams`、`OfferCards` |
| 8 | `backup=` | `none`、または `歌唱,律動,表現,hp,nextInstanceId;<deck と同じ形>` |
| 9 | `lastExam=` `midExam=` | `none`、または `kind,attempt,score,rank,passed;<NpcScores を ,連結>` |
| 10 | `result=` | `none`、または `outcome,evaluationValue,rank,finalScore,finalRank` |
| 11 | `operations=` | 件数 |
| 12 | `operation.0=` … | 件ごとに `number,kind,arg1,arg2,arg3,week,attempt,turn` |
| 13 | `lesson=none` | `Lesson` が null のとき。そうでなければこの 1 行の代わりに、`LessonStateComparer.Describe(Lesson)` の各行の先頭に `lesson.` を付けて並べる |

`Diff` は `Describe` の結果を先頭から比べ、長いほうの行数まで回し、足りない側は `(なし)`、`string.Equals(a, b, StringComparison.Ordinal)` が偽の行ごとに `"期待 " + 期待の行 + " / 実際 " + 実際の行` を返す。

`D/ProduceEngine.cs`:

```csharp
public sealed class ProduceEngine
{
    public ProduceEngine(ProduceCatalog catalog);   // null なら ArgumentNullException。内部で new LessonEngine(catalog.Cards) を持つ

    /// <summary>プロデュースを始め、週 1 の入力待ちまで進める。</summary>
    public ProduceState Start(ProduceStartInput input, IRandomStreamSource streams);

    /// <summary>1 回の操作を受け、次の入力待ちまで進める。Accepted 以外のときは状態も記録も変えない。</summary>
    public ProduceApplyResult Apply(ProduceState state, ProduceCommand command, IRandomStreamSource streams);

    /// <summary>週の選択の枠（クライアントの表示にも使う）。Phase が WeekSelect でなければ InvalidOperationException。</summary>
    public IReadOnlyList<WeekChoice> GetWeekChoices(ProduceState state);

    /// <summary>授業の選択肢。Phase が WeekSelect でなければ InvalidOperationException。授業の枠が無い週は空。</summary>
    public IReadOnlyList<ClassOptionView> GetClassOptions(ProduceState state);

    /// <summary>レッスン・試験中のカードの使用可否。Lesson が null なら NotPlayerPhase。</summary>
    public UseCheckResult CanUse(ProduceState state, int instanceId);
}

public readonly struct WeekChoice
{
    public WeekChoice(int slotIndex, WeekChoiceKind kind, bool selectable);
    public int SlotIndex { get; }  public WeekChoiceKind Kind { get; }  public bool Selectable { get; }
}

public readonly struct ClassOptionView
{
    public ClassOptionView(int index, int hpCost, ParamKind param, int paramGain, bool givesCard, bool selectable);
    public int Index { get; }  public int HpCost { get; }  public ParamKind Param { get; }
    public int ParamGain { get; }  public bool GivesCard { get; }  public bool Selectable { get; }
}
```

`WeekChoice` と `ClassOptionView` は `D/ProduceEngine.cs` に置く。

## 4. タスク

どのタスクも、終わったとき `dotnet build Uta.slnx`（警告 0・エラー 0）と `dotnet test --project shared/Uta.Shared.Domain.Tests` が通る状態にする。テストの追加の前に、既存のテストがすべて通ることを確かめる。

### タスク 1: 土台（enum・行の型・定数・乱数の供給元）

- 触る: `shared/Uta.Shared.Domain/Lesson/CardDefinition.cs`、`shared/Uta.Shared.Domain/Rng/IRandomStreamSource.cs`、`D/ProduceEnums.cs`、`D/ProduceRows.cs`、`D/ProduceRules.cs`、`shared/Uta.Shared.Domain.Tests/Lesson/TestCardCatalog.cs`、`T/TestRandomStreamSource.cs`、`T/TestRandomStreamSourceTests.cs`
- 手順:
  1. 3.1・3.2・3.3・3.4 のとおりに書く。`ICardCatalog` に `CardIds` を足す
  2. `TestCardCatalog` に `public IReadOnlyList<int> CardIds { get; }` を足す。コンストラクタで全カードを足し終えたあとに、`cards.Keys` を昇順に並べた配列を入れる（`(IEnumerable<CardDefinition> extra)` のコンストラクタは `: this()` のあとに `AddAll(extra)` するので、`AddAll` の最後で毎回作り直す）
  3. `T/TestRandomStreamSource.cs`: `public sealed class TestRandomStreamSource : IRandomStreamSource`。`TestRandomStreamSource(ulong masterSeed)`。`Get(key)` は、キー（`key.ToUInt64()`）ごとに `RandomStreams.Create(masterSeed, key)` を 1 回だけ作って保持し、同じオブジェクトを返す。`Get` を呼んだキーを呼んだ順に覚え、`public IReadOnlyList<RandomStreamKey> Requests { get; }` で返す（同じキーを何度呼んでも、呼ぶたびに足す）
- 境界: `Requests` は `Get` を呼んだ回数だけ増える
- 確かめ方: 下の 5.1 のテスト

### タスク 2: マスタの定義とカタログ

- 触る: `D/ProduceMasterRows.cs`、`D/ProduceDefinitions.cs`、`D/ProduceCatalog.cs`、`T/TestProduceMaster.cs`、`T/ProduceCatalogTests.cs`
- 手順:
  1. 3.5 のとおりに書く。`Create` は、行をキーごとに引ける辞書にして、V1〜V10 を実装する。問題を集めるための `List<string>` を持ち、同じメッセージは足さない
  2. `T/TestProduceMaster.cs`（`public static class TestProduceMaster`）に次を書く:
     - `public static ProduceMasterRows LoadRows()`: `masterdata/csv/` の CSV を読んで行を作る。CSV の置き場は、`AppContext.BaseDirectory` から親フォルダをたどって `masterdata/csv` というフォルダを持つ最初のフォルダを探す（見つからなければ `DirectoryNotFoundException`）。1 ファイルは UTF-8、行は `\n` で分け、各行の末尾の `\r` を除き、**先頭の 4 行（表の名前・出典・型・列名）を読み飛ばし**、空行は無視し、`,` で分ける（引用符は使われていない）。値の変換: `int` は空なら 0、それ以外は `int.Parse`（`CultureInfo.InvariantCulture`）。`bool` は `TRUE`（大小文字を無視）なら真、それ以外は偽。enum は空なら `default`、それ以外は `Enum.Parse<T>(値)`（テストなのでリフレクションを使ってよい）。読むファイルと列の対応は 3.2 の表のとおり（1 対 1）
     - `public static ProduceCatalog Create(ICardCatalog cards = null)`: `LoadRows()` を `ProduceCatalog.Create(rows, cards ?? new TestCardCatalog())` に渡す
     - `public static ProduceCatalog Create(ProduceMasterRows rows, ICardCatalog cards = null)`
  3. テストは 5.2 のとおり
- 境界: マスタの CSV が V1〜V10 を満たすこと（満たさなければ `LoadRows`＋`Create` のテストが落ちる。そのときは CSV ではなく検査の実装を疑い、計画と食い違うなら止まって報告する）
- 確かめ方: 5.2

### タスク 3: 計算式

- 触る: `D/ProduceFormulas.cs`、`T/ProduceFormulasTests.cs`
- 手順: 3.6 のとおりに書く。テストは 5.3
- 確かめ方: 5.3

### タスク 4: コマンド・記録・入力・編成の検証

- 触る: `D/ProduceCommand.cs`、`D/ProduceInputs.cs`、`D/FormationValidator.cs`、`T/ProduceCommandTests.cs`、`T/FormationValidatorTests.cs`
- 手順: 3.7 のとおりに書く。テストは 5.4
- 確かめ方: 5.4

### タスク 5: カードの選択肢と審査ターン

- 触る: `D/OfferPool.cs`、`D/ExamTurnBuilder.cs`、`T/OfferPoolTests.cs`、`T/ExamTurnBuilderTests.cs`
- 手順: 3.8 のとおりに書く。テストは 5.5
- 確かめ方: 5.5

### タスク 6: 状態とエンジン（試験の手前まで）

**実装担当への渡し方は 2 回に分ける**: 6a = `ProduceState`・`ProduceStateComparer`・`Start`・`BeginWeek`・`GetWeekChoices`・`GetClassOptions`・`CanUse` と、`Apply` の骨組み（`Apply` は `throw new NotSupportedException("タスク 6b")`）。テストは `ProduceEngineStartTests` と、`ProduceStateComparerTests` のうち `Describe_InitialState_ContainsExpectedLines`・`Diff_SameState_Empty`、`ProduceEngineWeekTests` のうち `GetWeekChoices_Scenario1Week1_LessonAndUnselectableRest`。6b = `Apply` とその下の処理（週の選択・レッスン・カードの選択肢・`EndWeek`）と残りのテスト。6a 終了時もビルドとテストが通ること。

- 触る: `D/ProduceState.cs`、`D/ProduceStateComparer.cs`、`D/ProduceEngine.cs`、`T/ProduceBot.cs`、`T/ProduceEngineStartTests.cs`、`T/ProduceEngineWeekTests.cs`、`T/ProduceEngineLessonTests.cs`、`T/ProduceStateComparerTests.cs`
- 手順: 3.9 のシグネチャで、下のアルゴリズムを実装する。**この段階では、試験に関わる次の 4 つを、中身が `throw new NotSupportedException("試験はタスク 7")` のメソッドとして宣言しておく**: `BeginWeek` の試験の分岐、`StartExam(state, streams)`、`FinishExam(state, streams)`、局面 `ExamResult` の処理（`ExamRetry`・`ExamNoRetry` の `Apply` の分岐）。局面 `Exam` の `Skip` などでレッスンが終わったときの `FinishExam` 呼び出しも宣言どおりに書く。タスク 7 でこれらの中身を置き換える。`T/ProduceBot.cs` は 6a で作る（`Run` の `Apply` を使う部分は 6b で通る）`ProduceState` のコンストラクタが取る引数・内部の持ち方は自由（上の公開プロパティを満たすこと）
- 確かめ方: 5.6

**`Start(input, streams)`**（3・4 の `ArgumentException` は、呼び出し側（サーバー）が先に `FormationValidator` と `catalog.GetMaxLevel` で検査してエラーコードに写し、通ったものだけを渡す前提の防御。ふつうは起きない）

1. `input`・`streams` が null なら `ArgumentNullException`
2. `scenario = catalog.GetScenario(input.ScenarioId)`、`idol = catalog.GetIdol(input.IdolId)`、各サポートカードを `catalog.GetSupportCard`（無ければ `KeyNotFoundException`）
3. 各サポートカードで `1 ≤ Level ≤ catalog.GetMaxLevel(Rarity, LimitBreak)` でなければ `ArgumentException`
4. `FormationValidator.Validate(catalog, idol.IdolId, サポートカードの ID の列)` が `Ok` でなければ `ArgumentException`
5. 最大体力 = `idol.MaxHp` + 全サポートカードの全 `MaxHp` アビリティの `AbilityValue(ValueAtLv1, ValueAtMax, Level, card.MaxLevelAtFullLimitBreak)` の合計。体力 = 最大体力
6. ボーナス（千分率、パラメータごと）= `idol.GetBonusPerThousand(p)` + 全サポートカードの `ParamBonus` アビリティ（`Param` が `p`）の `AbilityValue` の合計
7. パラメータ: 各 `p` を `ProduceFormulas.AddParam(0, idol.GetInitialParam(p), scenario.ParamCap)` で作り、その後、サポートカードを枠の順に、各カードのアビリティを `Order` の順に見て、`InitialParam` の行ごとに `AddParam(現在値, AbilityValue(...), scenario.ParamCap)` で足す
8. 初期デッキ（idol-support.md 1.3）: カード ID の並びを、`catalog.CommonDeckCardIds` を昇順に並べたもの、`idol.UniqueCardId`、`idol.RecommendCardIds` を昇順に並べたもの、の順につなぐ。インスタンス ID は 1 から連番、すべて強化なし。`NextInstanceId` = 枚数 + 1
9. `ExamRetryLeft = scenario.RetryCount`、`Week = 1`、`Attempt = 0`、`NextOperationNumber = 0`、記録は空
10. `BeginWeek(state, streams)`。`state` を返す。`Scenario` ストリームは引かない（MVP は引くものがない）

**`BeginWeek(state, streams)`**（週の始まり。`state.Week` の週）

- `week = scenario.GetWeek(state.Week)`
- `Action`: `week.ClassOptions.Count > 0` なら、`random = streams.Get(new RandomStreamKey(RandomStreamKind.Week, state.Week, 0))`、選択肢の順に `(int)ProduceParams.FromIndex(RandomOps.NextInt(random, 3))` を引いて `ClassOptionParams` にする。そうでなければ `streams.Get` を呼ばない。`Phase = WeekSelect`
- `Finishing`: `ClassOptionParams` は空。`Phase = WeekSelect`
- `MidExam`・`FinalExam`: `NotSupportedException`（タスク 7 で置き換え）

**`GetWeekChoices`** (`Phase != WeekSelect` は `InvalidOperationException`)

- `Action` の週: 枠の順に `Lesson`→`WeekChoiceKind.Lesson`、`Class`→`Class`、`Supply`→`Supply`（`selectable` は真）。最後に `Rest`（`slotIndex` = 枠の数、`selectable` = `Hp < MaxHp`）
- `Finishing` の週: `Lesson` 1 つ（`slotIndex` 0、`selectable` は真）。`Rest` は付けない

**`GetClassOptions`**: `Phase != WeekSelect` は `InvalidOperationException`。`week.ClassOptions` の `Order` の順に `ClassOptionView(index, HpCost, (ParamKind)ClassOptionParams[index], ParamGain, GivesCard, selectable)`。`selectable` = `HpCost == 0 || Hp > HpCost`（produce.md 3.4）。授業の枠の無い週は空

**`CanUse`**: `state.Lesson == null` なら `NotPlayerPhase`、そうでなければ `lessonEngine.CanUse(state.Lesson, instanceId)`

**`Apply(state, command, streams)`**

0. `state`・`streams` が null なら `ArgumentNullException`
1. 種類の判定（この順）:
   - `Nothing`、`ConsultExchange`〜`ConsultFinish`、`DiscardDrink`、および `OperationKind` に定義のない数値 → `InvalidArgument`（MVP で受けない。`switch` の `default` もここ）
   - 期待する局面: `ChooseWeekAction` → `WeekSelect`、`OfferPick`・`OfferReroll`・`OfferRemove`・`OfferDecline` → `CardOffer`、`ExamRetry`・`ExamNoRetry` → `ExamResult`、`UseCard`・`UseDrink`・`Skip` → `Lesson` または `Exam`。`state.Phase` が期待と違えば `UnexpectedPhase`（`Finished`・`Nothing` も同じ）
2. `weekBefore = state.Week`、`attemptBefore = state.Attempt`、`turnBefore = state.Lesson == null ? 0 : state.Lesson.Turn` を控える
3. 局面ごとの処理（下）。**処理の途中で例外が出たときの `state` は不定**とする（呼び出し側は、例外なら状態を保存せず、保存済みの状態から取り直す。順 6 がこれを前提にする）。`Accepted` を返す前に、`new OperationRecord(state.NextOperationNumber, command, weekBefore, attemptBefore, turnBefore)` を記録に足し、`NextOperationNumber` を 1 増やす。**拒否するときは、状態を何も変えてから拒否しない**（検査をすべて終えてから書き換える）

**局面 `WeekSelect`**（`ChooseWeekAction`。`choices = GetWeekChoices`）

1. `command.Arg1` が `0 ≤ 値 < choices.Count` でなければ `InvalidArgument`。`choices[Arg1].Selectable` が偽なら `InvalidArgument`
2. 枠の種類ごと:
   - `Lesson`: `Arg3` が `ParamKind` の 1〜3 でなければ `InvalidArgument`。`StartLesson(state, streams, 週が Finishing なら LessonKind.Finishing、そうでなければ LessonKind.Normal, (ParamKind)Arg3)`
   - `Class`: `Arg2` が `0 ≤ 値 < ClassOptions.Count` でなければ `InvalidArgument`。`GetClassOptions()[Arg2].Selectable` が偽なら `InvalidArgument`。受け付けたら: `Hp −= HpCost`。パラメータ `(ParamKind)ClassOptionParams[Arg2]` を `AddParam(現在値, ParamGain, ParamCap)`（パラメータボーナスは掛けない。produce.md 3.4）。`ClassOptionParams` を空にする。`GivesCard` なら `DrawOffer(state, streams)`、そうでなければ `EndWeek(state, streams)`
   - `Supply`: `ClassOptionParams` を空にして `DrawOffer(state, streams)`
   - `Rest`: `Hp = Heal(Hp, RestAmount(MaxHp), MaxHp)`。`ClassOptionParams` を空にして `EndWeek(state, streams)`

**`StartLesson(state, streams, kind, param)`**

1. `week = scenario.GetWeek(state.Week)`、`idol = catalog.GetIdol(Input.IdolId)`。`ClassOptionParams` を空にする
2. `setup = new LessonSetup(kind, param, week.GetTurns(idol.Plan), week.ClearTarget, week.PerfectTarget)`
3. `lesson = new LessonState(setup, state.Deck, state.Hp, state.MaxHp)`、`random = streams.Get(new RandomStreamKey(RandomStreamKind.Lesson, state.Week, 0))`、`lessonEngine.Start(lesson, random)`
4. `state.Lesson = lesson`、`Phase = Lesson`。`lesson.IsFinished` なら `FinishLesson(state, streams)`

**局面 `Lesson`・`Exam`**（`UseCard`・`UseDrink`・`Skip`）

1. `LessonCommand` に変換（`UseCard` → `LessonCommand.UseCard(Arg1)`、`UseDrink` → `LessonCommand.UseDrink(Arg1)`、`Skip` → `LessonCommand.Skip()`）
2. `random = streams.Get(new RandomStreamKey(RandomStreamKind.Lesson, state.Week, state.Phase == Exam ? state.Attempt : 0))`、`result = lessonEngine.Apply(state.Lesson, lc, random)`。`result != LessonApplyResult.Accepted` なら `InvalidArgument`
3. `state.Lesson.IsFinished` なら、`Phase == Lesson` は `FinishLesson`、`Exam` は `FinishExam`（タスク 7）

**`FinishLesson(state, streams)`**（produce.md 4.3）

1. `lesson = state.Lesson`、`result = LessonEngine.GetResult(lesson)`、`setup = lesson.Setup`、`idol`、`scenario`
2. `alloc = DistributeGain(setup.Kind == LessonKind.Finishing, setup.TargetParam, result.Gained, setup.ClearTarget, idol.FirstParam)`
3. パラメータごとに `add = alloc[i] + BonusAmount(alloc[i], state.GetBonusPerThousand(p))`、`param = AddParam(param, add, scenario.ParamCap)`
4. `Hp = result.Hp`。`result.Reason == LessonEndReason.Perfect` なら `Hp = Heal(Hp, PerfectRecover(setup.MaxTurns, result.LastTurn), MaxHp)`。`Finishing` なら `Hp = Heal(Hp, MaxHp * 70 / 100（RestAmount と同じ式）, MaxHp)`
5. `state.Lesson = null`
6. `result.Gained >= setup.ClearTarget` なら `DrawOffer(state, streams)`、そうでなければ `EndWeek(state, streams)`

**`DrawOffer(state, streams)`**

1. `candidates = OfferPool.GetCandidates(catalog, idol, Input.PlayerLevel, Deck の CardId)`
2. `random = streams.Get(new RandomStreamKey(RandomStreamKind.Offer, state.Week, 0))`、`cards = OfferPool.Draw(catalog, candidates, random)`
3. `cards.Count == 0` なら `EndWeek(state, streams)`（カードの選択肢の局面に入らない。produce.md 3.5）。そうでなければ `OfferCards = cards`、`Phase = CardOffer`

**局面 `CardOffer`**

- `OfferPick`: `Arg1` が `0 ≤ 値 < OfferCards.Count` でなければ `InvalidArgument`。`new CardInstance(NextInstanceId, OfferCards[Arg1], false)` を `Deck` の末尾に足し、`NextInstanceId` を 1 増やす。`OfferCards` を空にして `EndWeek`
- `OfferDecline`: `OfferCards` を空にして `EndWeek`
- `OfferReroll`・`OfferRemove`: 常に `InvalidArgument`（再抽選・除去の残り回数は MVP では 0。produce.md 3.5）

**`EndWeek(state, streams)`**（入力待ちではない）

1. `state.Week >= scenario.WeekCount` なら `InvalidOperationException`（最終週は試験で終わるので来ない）
2. `OfferCards`・`ClassOptionParams` を空にする。`Week` を 1 増やす。`BeginWeek(state, streams)`

### タスク 7: 試験・再挑戦・終了

- 触る: `D/ProduceEngine.cs`、`T/ProduceEngineExamTests.cs`
- 手順: タスク 6 の `NotSupportedException` を、次の処理に置き換える。5.7 のテスト
- 確かめ方: 5.7

**`BeginWeek` の試験の分岐**: `Attempt = 0`、`ExamBackup = new ExamBackup(パラメータ 3 つ, Hp, Deck, NextInstanceId)`、`StartExam(state, streams)`

**`StartExam(state, streams)`**

1. `week`、`idol`、`examKind = week.Kind == WeekKind.MidExam ? ExamKind.Mid : ExamKind.Final`
2. `random = streams.Get(new RandomStreamKey(RandomStreamKind.Lesson, state.Week, state.Attempt))`
3. `turns = ExamTurnBuilder.Build(catalog, idol, examKind, 今のパラメータ 3 つ, random)`
4. `setup = new LessonSetup(week.Kind == MidExam ? LessonKind.MidExam : LessonKind.FinalExam, ParamKind.Nothing, turns.Count, 0, 0, new ExamSetup(turns, week.ExamEarlyEndScore))`
5. `lesson = new LessonState(setup, state.Deck, state.Hp, state.MaxHp)`、`lessonEngine.Start(lesson, random)`（審査ターンを引いた同じ `random` を続けて使う）
6. `state.Lesson = lesson`、`Phase = Exam`。`lesson.IsFinished` なら `FinishExam`

**`FinishExam(state, streams)`**

0. `week = scenario.GetWeek(state.Week)`、`examKind = week.Kind == WeekKind.MidExam ? ExamKind.Mid : ExamKind.Final`
1. `result = LessonEngine.GetResult(state.Lesson)`、`npc = NpcScores(week.RivalBaseScore)`、`rank = ExamRank(result.Gained, npc)`、`passed = rank <= ProduceRules.PassRank`
2. `report = new ExamReport(examKind, state.Attempt, result.Gained, rank, npc, passed)`。`state.LastExam = report`。`Hp = result.Hp`。`state.Lesson = null`
3. `ExamRetryLeft > 0` なら `Phase = ExamResult` で終わる（合否にかかわらず。produce.md 5.1.1）。そうでなければ `ConcludeExam(state, streams)`

**局面 `ExamResult`**

- `ExamRetry`: `ExamBackup` から戻す（パラメータ 3 つ・`Hp`・`Deck`・`NextInstanceId`。`Deck` は複製して持つ）。`ExamRetryLeft` を 1 減らし、`Attempt` を 1 増やし、`LastExam` はそのまま。`StartExam`
- `ExamNoRetry`: `ConcludeExam`

**`ConcludeExam(state, streams)`**（produce.md 5.1.1 の表）

1. `report = state.LastExam`、`examKind = report.Kind`。`state.Attempt = 0`（試験を出る）
2. `Mid`: `state.MidExam = report`。合格なら、全パラメータに `AddParam(現在値, catalog.GetExamRankParamAll(Mid, report.Rank), ParamCap)` を足し、`ExamBackup = null`、`EndWeek`。不合格なら `FinishProduce(state, ExamOutcome.MidFailed)`
3. `Final`: 合格なら全パラメータに `AddParam(現在値, GetExamRankParamAll(Final, report.Rank), ParamCap)` を足して `FinishProduce(state, FinalPassed)`、不合格なら `FinishProduce(state, FinalFailed)`

**`FinishProduce(state, outcome)`**

1. `finalTaken = outcome != MidFailed`。`finalRank = finalTaken ? LastExam.Rank : 0`、`finalScore = finalTaken ? LastExam.Score : 0`
2. `eval = Evaluate(catalog, パラメータの合計, finalRank, finalScore)`
3. `state.Result = new ProduceResult(outcome, eval.Value, eval.Rank, finalScore, finalRank)`、`ExamBackup = null`、`Phase = Finished`

### タスク 8: 再生のテスト

- 触る: `T/ProduceReplayTests.cs`
- 手順: 5.8 のテストを書く。実装は変えない（テストが落ちたら、実装の誤りか計画の誤りかを切り分けて報告する）
- 確かめ方: 5.8

## 5. テスト

テストの名前は `Method_Condition_Result` の形。xunit v3（`Xunit`、`[Fact]`・`[Theory]`）。**乱数の出力の具体的な値を期待値に書かない**（出力は構造・件数・不変条件・再生の一致で確かめる）。実行するコマンドは CLAUDE.md の「ビルド・テスト・実行」にあるものだけ: `dotnet build Uta.slnx`、`dotnet test --project shared/Uta.Shared.Domain.Tests`。

### 5.1 タスク 1（`T/TestRandomStreamSourceTests.cs`）

- `Get_SameKey_ReturnsSameObject`: 同じキーで 2 回 `Get` → `ReferenceEquals` が真
- `Get_SameKey_ContinuesSequence`: seed 42、キー `(Week, 3, 0)`。`Get` した乱数から 2 回続けて引いた値が、`RandomStreams.Create(42, 同じキー)` から 2 回引いた値と一致
- `Get_DifferentKeys_DifferentValues`: `(Week, 1, 0)` と `(Week, 2, 0)` の最初の値が違う
- `Requests_RecordsEveryCallInOrder`: 3 回 `Get`（キー A、B、A）→ `Requests` がその順の 3 件
- `TestCardCatalog_CardIds_AscendingAndComplete`: `CardIds` が昇順で、`1`（F01）・`107`（S07）・`207`（L07）・`900` を含む。`new TestCardCatalog(extra)` の `extra` のカード ID も含む

### 5.2 タスク 2（`T/ProduceCatalogTests.cs`）

`TestProduceMaster.Create()` で作ったカタログを `catalog` とする。

- `Create_RealMaster_ScenarioShape`: `GetScenario(1)` は `WeekCount` 3・`ParamCap` 1000・`RetryCount` 3、`Weeks.Count` 3。`GetScenario(2).Weeks.Count` は 13。シナリオ 2 の週 2 は `Kind == Action`・`Actions == [Class, Supply]`・`ClassOptions` が `(8, 40, true)`・`(4, 30, true)`・`(0, 15, true)`。週 6 は `MidExam`・`ExamEarlyEndScore` 900・`RivalBaseScore` 900。週 13 は `FinalExam`・`RivalBaseScore` 2250。シナリオ 1 の週 1 は `GetTurns(Plan.Sense)` が 5、`GetTurns(Plan.Logic)` が 6、`ClearTarget` 25、`PerfectTarget` 50
- `Create_RealMaster_IdolAndSupport`: `GetIdol(1)` は `Plan.Sense`・`MaxHp` 30・`FirstParam` Singing・`SecondParam` Rhythm・`ThirdParam` Expression・`UniqueCardId` 107・`RecommendCardIds == [104, 101, 101]`・`GetInitialParam(Singing)` 30・`GetBonusPerThousand(Singing)` 50・`GetExamParam(2)` が Rhythm。`GetSupportCard(9)` は `Rarity.SSR`・`MaxLevelAtFullLimitBreak` 60・`Abilities.Count` 2、1 つ目は `(InitialParam, Singing, 15, 65)`
- `Create_RealMaster_Tables`: `CommonDeckCardIds == [1, 1, 2, 3, 3]`、`GetOfferWeight(Rarity.SSR)` 10・`(N)` 30、`GetMaxLevel(Rarity.R, 2)` 30、`GetMaxLevel(Rarity.SSR, 4)` 60、`GetExamRankParamAll(Final, 1)` 30・`(Mid, 3)` 5・`(Mid, 4)` 0、`ScoreBonusA` 225、`GetScoreBonusB(1..3)` が 190・150・120、`EvaluationScoreBands.Count` 7（先頭の `MinScore` 0）、`EvaluationRanks.Count` 13（先頭が `F`、末尾が `SSPlus`）。`GetExamPattern(1).GetTurnCounts(Mid, Sense)` が `[4, 3, 2]`、`(Final, Sense)` が `[5, 4, 3]`、`(Final, Logic)` が `[5, 3, 3]`、`FirstTurnSecondRatePerTenThousand` 1500
- `GetScenario_Unknown_Throws`: `KeyNotFoundException`（`GetIdol`・`GetSupportCard`・`GetExamPattern` も 1 本ずつ）
- 検査のテスト。`rows = TestProduceMaster.LoadRows()` を変えて `Create` し、`InvalidOperationException` のメッセージが「必ず含む語」（3.5 の表）をすべて含むこと:
  - `Create_DuplicateScenario_Throws`: `Scenarios` に同じ `ScenarioId` 1 の行をもう 1 つ足す → `Scenario 1`・`重複`
  - `Create_LastWeekNotFinalExam_Throws`: シナリオ 1 の週 3 を `Kind = Action`（ほかの列は同じ）にする → `FinalExam`
  - `Create_OutingSlot_Throws`: シナリオ 2 の週 2 の `WeekActionSlots` に `Order` 3・`Outing` の行を足す → `未対応`
  - `Create_IdolExamOrderNotPermutation_Throws`: `Idol` 1 の `SecondParam` を `Singing` にする → `Idol 1`・`審査の順`
  - `Create_MissingOfferWeight_Throws`: `OfferRarityWeights` から `SSR` の行を除く → `OfferRarityWeight`
  - `Create_ExamPatternFirstTurnsTooSmall_Throws`: `ExamPattern` 1 の `MidFirstTurns` を 1 にする → `ExamPattern 1`
  - `Create_UnknownUniqueCard_Throws`: `Idol` 1 の `UniqueCardId` を 99999 にする → `Idol 1`・`UniqueCardId`
  - `Create_MultipleProblems_ReportsAll`: 上の `LastWeekNotFinalExam` と `OutingSlot` を同時に起こす → メッセージに `FinalExam` と `未対応` の両方

### 5.3 タスク 3（`T/ProduceFormulasTests.cs`。カタログは `TestProduceMaster.Create()`）

- `AddParam`: `(990, 20, 1000)` → 1000、`(10, 20, 1000)` → 30、`(5, 0, 1000)` → 5、`amount` が −1 → `ArgumentOutOfRangeException`
- `Heal`: `(25, 10, 30)` → 30、`(5, 10, 30)` → 15
- `RestAmount`: 30 → 21、35 → 24、1 → 0
- `BonusAmount`: `(100, 85)` → 8、`(7, 50)` → 0、`(40, 70)` → 2、`(0, 100)` → 0
- `AbilityValue`: `(1, 3, 1, 40)` → 1、`(1, 3, 10, 40)` → 1、`(1, 3, 21, 40)` → 2、`(1, 3, 40, 40)` → 3、`(15, 65, 30, 60)` → 39（15 + floor(50×29/59) = 15 + 24）、`(15, 65, 60, 60)` → 65、`level` 0 と `level` 41（maxLevel 40）は `ArgumentOutOfRangeException`
- `DistributeGain`: 通常 `(false, Rhythm, 40, 25, Singing)` → `[0, 40, 0]`。仕上げ `(true, Singing, 100, 75, Rhythm)` → `[83, 9, 8]`（toTarget 75、excess 25 = 3 等分 8 余り 1 を Rhythm へ）。仕上げ `(true, Singing, 50, 75, Rhythm)` → `[50, 0, 0]`。仕上げ `(true, Singing, 78, 75, Rhythm)` → `[76, 1, 1]`。仕上げ `(true, Singing, 77, 75, Expression)` → `[75, 0, 2]`
- `PerfectRecover`: `(6, 4)` → 4、`(6, 6)` → 0、`(6, 7)` → 0
- `ScoreBonusPercent`: `(300, 1)` → 257、`(600, 2)` → 285、`(1000, 3)` → 345、`(1500, 1)` → 527、`(0, 3)` → 120、rank 0 と 4 は `ArgumentOutOfRangeException`
- `NpcScores`: 2250 → `[1800, 1462, 1125, 787, 450]`、900 → `[720, 585, 450, 315, 180]`、1 → `[0, 0, 0, 0, 0]`
- `ExamRank`（NPC は 2250 のもの）: `1800` → 1（同点は上）、`1799` → 2、`1500` → 2、`0` → 6、`450` → 5、`451` → 5、`449` → 6
- `Evaluate`: `(150, 1, 0)` → `ParamPart` 345・`RankPart` 1700・`ScorePart` 0・`Value` 2045・`Rank` D。`(1000, 2, 5000)` → 2300 + 900 + 1500 = 4700・`Rank` CPlus。`(0, 0, 5001)` → `ScorePart` 1500。`(0, 0, 40001)` → `ScorePart` 3650。`(0, 0, 300000)` → `ScorePart` 5250。`(60, 0, 0)` → `Value` 138・`Rank` F。`(435, 0, 0)` → `Value` 1000・`Rank` E。`(0, 0, 0)` → `Value` 0・`Rank` F。`(0, 4, 0)` → `RankPart` 0

### 5.4 タスク 4

`ProduceCommandTests`: 各ファクトリが 3.7 の表のとおり `(Kind, Arg1, Arg2, Arg3)` を作る（12 個を 1 本の `[Theory]` か個別の `[Fact]` で）。`OperationRecord` が引数を保持する。

`ProduceInputsTests`（`T/ProduceCommandTests.cs` に同居してよい）: `SupportCardInput` は level 0・limitBreak −1・limitBreak 5 で例外。`ProduceStartInput` は `supportCards` が null で `ArgumentNullException`、6 枚で `ArgumentException`、要素が null で `ArgumentException`、`playerLevel` 0 で `ArgumentOutOfRangeException`、0 枚と 5 枚は作れる。

`FormationValidatorTests`（`catalog = TestProduceMaster.Create()`）: idol 1 + `[6, 8]` → `Ok`、idol 1 + `[]` → `Ok`、idol 1 + `[1, 2, 3, 4, 5]` → `Ok`、idol 1 + `[1, 1]` → `DuplicateCardId`、idol 1 + `[7]`（SR・論理）→ `PlanMismatch`、idol 2 + `[9]`（SSR・感性）→ `PlanMismatch`、idol 1 + `[7, 7]` → `DuplicateCardId`（先に見る）、idol 99 → `KeyNotFoundException`、カード 99 → `KeyNotFoundException`。

### 5.5 タスク 5

`OfferPoolTests`: 合成カードを足したカードのカタログを使う。`TestCardCatalog(extra)` の `extra` に、`new CardDefinition(id, plan, type, rarity, unlockLv, false, unique, false, TestCards.Face(TestCards.Consume(0)), null)`（`Lesson/TestCards.cs` の既存の補助）で作る次を渡す:

| ID | plan | type | rarity | unlockLv | unique |
|---|---|---|---|---|---|
| 910 | Sense | Mental | N | 1 | false |
| 911 | Logic | Mental | N | 1 | false |
| 912 | Free | Mental | R | 5 | false |
| 913 | Free | Trouble | N | 1 | false |
| 914 | Free | Mental | R | 1 | true |
| 915 | Free | Active | SSR | 1 | false |
| 916 | Free | Active | SSR | 1 | false |
| 917 | Free | Active | SSR | 1 | false |

`catalog = TestProduceMaster.Create(カタログ)`、`idol = catalog.GetIdol(1)`（感性）。

- `GetCandidates_FiltersAndSorts`: `playerLevel` 1、デッキ `[]` → 昇順で、`910`・`914`・`915`・`1`（F01）を含み、`911`（別プラン）・`912`（Lv 5）・`913`（不調）を含まない
- `GetCandidates_HigherLevel_IncludesUnlocked`: `playerLevel` 5 → `912` を含む
- `GetCandidates_UniqueInDeck_Excluded`: デッキ `[914]` → `914` を含まない。デッキ `[910]` → `914` を含む
- `GetCandidates_LogicIdol_UsesLogicPlan`: `catalog.GetIdol(2)` → `911` を含み `910` を含まない
- `Draw_EmptyCandidates_ReturnsEmpty`
- `Draw_LargePool_ThreeDistinctFromPool`: `GetCandidates` の結果で 3 件、重複なし、すべて候補に含まれる。同じ seed の `Xoshiro256StarStar`（`RandomStreams.Create(seed, 任意のキー)`）を 2 回作って同じ結果
- `Draw_TwoCandidates_ReturnsTwo`: 候補 `[910, 915]` → 2 件で集合が `{910, 915}`
- `Draw_OneRarityOnly_ReturnsThree`: 候補 `[915, 916, 917]`（すべて `SSR`）→ 3 件で集合が `{915, 916, 917}`

`ExamTurnBuilderTests`（`catalog = TestProduceMaster.Create()`、`idol = catalog.GetIdol(1)`（1番 Singing、2番 Rhythm、3番 Expression、パターン 1、率 1500）、`paramValues = [300, 200, 100]`）。seed を 1〜20 で回す:

- `Build_Mid_TotalCountsMatchPattern`: `Mid` は 9 ターン。Singing が 4、Rhythm が 3、Expression が 2（先頭がどちらになっても合計は変わらない）
- `Build_Final_Sense_Has12Turns`: `Final` は 12 ターン（Singing 5・Rhythm 4・Expression 3）
- `Build_Final_Logic_Counts`: `catalog.GetIdol(2)`（論理。1番 Rhythm、2番 Expression、3番 Singing、パターン 2）で `Final` は 11 ターン（Rhythm 5・Expression 4・Singing 2）
- `Build_LastThreeTurns_AreThirdSecondFirst`: 最後の 3 ターンのパラメータが Expression・Rhythm・Singing の順
- `Build_FirstTurn_FollowsFirstTurnDraw`: テスト用の `ScriptedRandom`（`T/ScriptedRandom.cs`。`ScriptedRandom(ulong[] head, IRandom rest)`: 最初の `NextUInt64` は `head` を順に返し、尽きたら `rest.NextUInt64()`）を使う。`head = [10000]`（`NextInt(10000)` の結果が 0 で 1500 未満 = 2 番になる。棄却法のしきい値は 1616 なので 1616 以上の値を使う）、`rest = RandomStreams.Create(1, 任意のキー)` → 先頭が Rhythm。`head = [5000]`（結果 5000 で 1 番）→ 先頭が Singing。どちらも合計のターン数・順位ごとの数は 5.5 の合計と同じ
- `Build_RateZero_FirstTurnAlwaysFirstParam`: idol 2（パターン 2、率 0）で、`head = [5000]`（率 0 なので 1 番。1616 以上の値を使う）と seed 1〜20 の両方で先頭が Rhythm（1 番）。`head = [5000]` のときも先頭の判定で 1 回引くこと（`ScriptedRandom` が 1 つ消費したことを `ScriptedRandom.Consumed == 1`（head から返した数）で確かめる。`Consumed` は `head` から返した回数）
- `Build_ScoreBonusMatchesFormula`: 各ターンの `ScoreBonusPercent` が、そのターンのパラメータが 1・2・3 番のどれかで `ProduceFormulas.ScoreBonusPercent(catalog, paramValues[そのパラメータ], 番)` と一致
- `Build_SameSeed_SameResult`: 同じ seed で 2 回作ると、パラメータの並びとボーナスの並びが一致

### 5.6 タスク 6

共通の補助（`T/ProduceBot.cs`、`public static class ProduceBot`）:

```csharp
public static ProduceCommand Next(ProduceEngine engine, ProduceCatalog catalog, ProduceState state, bool playCards = true, bool retryExam = false);
// 局面ごとに次のコマンドを返す。
//   WeekSelect: choices = engine.GetWeekChoices(state)。choices[0] の種類で
//     Lesson → ProduceCommand.ChooseLesson(0, 育成アイドルの FirstParam)
//     Class  → 選べる選択肢（engine.GetClassOptions）のうち添字が最小のものを ProduceCommand.ChooseClass(0, 添字)
//     Supply・Rest → ProduceCommand.ChooseOther(0)
//   CardOffer: ProduceCommand.OfferPick(0)
//   Lesson・Exam: playCards なら、state.Lesson.Zones.Hand を先頭から見て engine.CanUse が Ok の最初のインスタンス ID を UseCard。なければ（または playCards が偽なら）Skip
//   ExamResult: retryExam なら ExamRetry、そうでなければ ExamNoRetry
//   Finished・Nothing: InvalidOperationException
public static List<ProduceCommand> Run(ProduceEngine engine, ProduceCatalog catalog, ProduceState state, IRandomStreamSource streams,
    bool playCards = true, bool retryExam = false, int maxOperations = 3000);
// Finished になるまで Next → Apply を繰り返し、使ったコマンドを返す。
public static List<ProduceCommand> RunUntil(ProduceEngine engine, ProduceCatalog catalog, ProduceState state, IRandomStreamSource streams,
    Func<ProduceState, bool> stop, bool playCards = true, bool retryExam = false, int maxOperations = 3000);
// 毎回 Apply の前に stop(state) を見て、真か Finished なら返す。Run は RunUntil(stop: _ => false) と同じ。
// どちらも、Apply が Accepted 以外を返したら InvalidOperationException、Apply の回数が maxOperations を超えたら InvalidOperationException
```

`ProduceEngineStartTests`（`catalog = TestProduceMaster.Create()`、`engine = new ProduceEngine(catalog)`、`streams = new TestRandomStreamSource(1)`）:

- `Start_Idol1NoSupport_InitialState`: 入力 `(scenario 1, idol 1, Lv 1, [])`。`Phase == WeekSelect`、`Week` 1、`Attempt` 0、`NextOperationNumber` 0、`ExamRetryLeft` 3、パラメータ 30・20・10、`Hp`＝`MaxHp`＝30、`NextInstanceId` 10、`Deck` が `(インスタンス ID, カード ID)` で `(1,1)(2,1)(3,2)(4,3)(5,3)(6,107)(7,101)(8,101)(9,104)`（すべて強化なし）、`GetBonusPerThousand` が 50・30・20、`ClassOptionParams`・`OfferCards` が空、`Lesson`・`ExamBackup`・`LastExam`・`MidExam`・`Result` が null、`Operations` が空。`streams.Requests` が空（シナリオ 1 の週 1 は授業の枠も試験もないので、乱数を引かない）
- `Start_Idol2_DeckUsesLogicCards`: idol 2 → `Deck` のカード ID が `1,1,2,3,3,207,201,201,204`、パラメータ 10・30・20
- `Start_Idol3_DeckSortedWithinGroup`: idol 3（`Focus`: マスタの順は 103,102,102）→ 末尾 3 枚のカード ID が `102,102,103`。`MaxHp` 32、パラメータ 40・30・20
- `Start_Idol4`: idol 4 → 末尾 3 枚が `203,203,206`、`MaxHp` 35
- `Start_WithSupport6Level1_AddsInitialAndBonus`: idol 1 + サポート `(6, level 1, limitBreak 0)` → 歌唱 38（30 + 8）、歌唱のボーナス 70（50 + 20）
- `Start_WithSupport6MaxLevel`: `(6, 50, 4)` → 歌唱 70（30 + 40）、歌唱のボーナス 110（50 + 60）
- `Start_WithSupport8Level30`: `(8, 30, 0)` → `MaxHp` 34（30 + 4）、表現のボーナス 63（20 + 43）、`Hp` 34
- `Start_WithSupport4Level21`: `(4, 21, 1)`（R・アシスト。`MaxHp` の行 1→3、`MaxLevelAtFullLimitBreak` 40、上限解放 1 の上限レベル 25）→ `MaxHp` 32（1 + floor(2×20/39) = 2 を足す）
- `Start_ParamCapApplied`: シナリオ 1 の `ParamCap` を 35 にした行で作ったカタログ。idol 1 + サポート `(6, 50, 4)` → 歌唱 35（上限で止まる）。律動 20・表現 10
- `Start_LevelAboveCap_Throws`: サポート `(6, level 31, limitBreak 0)`（SR の上限解放 0 の上限は 30）→ `ArgumentException`
- `Start_PlanMismatch_Throws`: idol 1 + サポート `(7, 1, 0)` → `ArgumentException`
- `Start_DuplicateSupport_Throws`: idol 1 + `(1,1,0)(1,1,0)` → `ArgumentException`
- `Start_UnknownScenario_Throws`: scenario 99 → `KeyNotFoundException`
- `Start_Scenario2_Week1_NoRandomDrawn`: シナリオ 2 → `Phase == WeekSelect`、`ClassOptionParams` が空、`streams.Requests` が空

`ProduceEngineWeekTests`（`T/ProduceEngineWeekTests.cs`。補助 `SkipLesson(engine, state, streams)`: `Phase == Lesson` の間 `Skip` を `Apply` し続ける）:

- `GetWeekChoices_Scenario1Week1_LessonAndUnselectableRest`: シナリオ 1 → `[(0, Lesson, true), (1, Rest, false)]`
- `GetWeekChoices_NotWeekSelect_Throws`: レッスン中に呼ぶと `InvalidOperationException`
- `Rest_AtFullHp_Rejected`: `ChooseOther(1)` → `InvalidArgument`、`Describe` が呼ぶ前と同じ、`NextOperationNumber` 0、`Operations` 空
- `Choose_SlotOutOfRange_Rejected`: `ChooseLesson(5, Singing)` と `ChooseLesson(-1, Singing)` → `InvalidArgument`
- `ChooseLesson_ParamNothing_Rejected`: `ChooseLesson(0, ParamKind.Nothing)` → `InvalidArgument`。`Describe` 不変
- `ChooseLesson_Valid_StartsLesson`: `ChooseLesson(0, Singing)` → `Accepted`、`Phase == Lesson`、`Lesson` が null でない、`Lesson.Setup` が `Kind == Normal`・`TargetParam == Singing`・`MaxTurns` 5（感性）・`ClearTarget` 25・`PerfectTarget` 50、`Lesson.Hp` が `Hp` と同じ、`Operations.Count` 1・`Operations[0]` が `Number` 0・`Week` 1・`Attempt` 0・`Turn` 0 でコマンドが同じ、`NextOperationNumber` 1、`streams.Requests` に `(Lesson, 1, 0)` が含まれる
- `Choose_WrongPhase_Rejected`: 週の選択中に `OfferPick(0)`・`ExamRetry()`・`UseCard(1)`・`Skip()` → どれも `UnexpectedPhase`。`Describe` 不変
- `Choose_UnsupportedKind_Rejected`: `new ProduceCommand(OperationKind.DiscardDrink, 0, 0, 0)`・`ConsultFinish`・`Nothing` → どれも `InvalidArgument`
- `Class_Scenario2Week2_Options`: シナリオ 2。`ChooseLesson(0, Singing)` → `SkipLesson`（得点 0 でクリア報酬なし）→ `Week == 2`・`Phase == WeekSelect`。`GetWeekChoices` は `[(0, Class, true), (1, Supply, true), (2, Rest, false)]`。`ClassOptionParams.Count` が 3 で、すべて 1〜3。`GetClassOptions` が `HpCost` 8・4・0、`ParamGain` 40・30・15、`Selectable` がすべて真、`Param` が `ClassOptionParams` と一致。`streams.Requests` に `(Week, 2, 0)` が 1 回含まれる
- `Class_Option0_ChangesHpAndParamAndOffersCard`: 上の続きで `ChooseClass(0, 0)` → `Accepted`。`Hp` が 30 − 8 = 22。選んだパラメータ（実行前に控えた `ClassOptionParams[0]`）が実行前より 40 増える（上限 1000 を超えない）。`Phase == CardOffer`、`OfferCards.Count == 3`、重複なし、`ClassOptionParams` が空
- `Class_Option2_NoCost`: `ChooseClass(0, 2)` → `Hp` 変わらず 30、パラメータが 15 増える
- `Class_UnselectableOption_Rejected`: `LoadRows()` で、シナリオ 2 の週 2 の `ClassOption` の `Order` 1 の `HpCost` を 30 にしたカタログ（`MaxHp` が 30 なので `Hp > HpCost` が偽）。`GetClassOptions()[0].Selectable` が偽、`[1]`・`[2]` が真。`ChooseClass(0, 0)` → `InvalidArgument`、`Describe` 不変。`ChooseClass(0, 3)` → `InvalidArgument`
- `Offer_Pick_AddsCardAndEndsWeek`: 上の `Class_Option0` の続きで `OfferPick(1)` → `Accepted`。`Deck.Count` 10、末尾が `InstanceId` 10・`CardId == 実行前の OfferCards[1]`・強化なし。`NextInstanceId` 11。`OfferCards` が空。`Week == 3`・`Phase == WeekSelect`・`ClassOptionParams.Count == 3`（週 3 は授業の枠がある）
- `Offer_Decline_EndsWeekWithoutCard`: `OfferDecline()` → `Deck.Count` 9、`Week == 3`
- `Offer_PickOutOfRange_Rejected`: `OfferPick(3)`・`OfferPick(-1)` → `InvalidArgument`、`Describe` 不変
- `Offer_RerollAndRemove_Rejected`: `OfferReroll()`・`OfferRemove(0)` → `InvalidArgument`、`Describe` 不変
- `Supply_OffersCards`: シナリオ 2 の週 2 で `ChooseOther(1)` → `Phase == CardOffer`、`OfferCards.Count == 3`、`Hp` 変わらず。`streams.Requests` に `(Offer, 2, 0)` が含まれる
- `Rest_RecoversSeventyPercent`: 週 2 で `ChooseClass(0, 0)`（Hp 22）→ `OfferDecline` → 週 3（`[Lesson, Class, Rest]`。`Rest` の `Selectable` が真）→ `ChooseOther(2)` → `Accepted`。`Hp` が `min(30, 22 + 21)` = 30。`Week == 4`・`WeekSelect`

`ProduceEngineLessonTests`（`T/ProduceEngineLessonTests.cs`）:

- `Lesson_SkipOnly_EndsAfterMaxTurnsWithoutReward`: シナリオ 1。`ChooseLesson(0, Singing)` → `Skip` を繰り返す。5 回目の `Skip` で `Phase == WeekSelect`（`Week == 2`）、`Lesson` が null、パラメータは変わらず 30・20・10、`Hp` は 30。`OfferCards` が空（得点 0 でクリア目標 25 に届かない）、`Operations.Count` が 6。`Operations` の `Turn` が 0, 1, 2, 3, 4, 5 の順（`Skip` を受け付ける前のターン番号）。週 2 の `GetWeekChoices` は `[(0, Lesson, true)]`（仕上げの週。`Rest` はない）
- `Lesson_Finishing_ChoiceIsSlotZeroOnly`: 上の続きで `ChooseLesson(1, Singing)` → `InvalidArgument`。`ChooseLesson(0, Singing)` → `Accepted`、`Lesson.Setup.Kind == Finishing`・`MaxTurns` 9・`ClearTarget` 75・`PerfectTarget` 300
- `Lesson_BotPlay_ResultAppliedAsSpecified`: seed 1〜5 と、シナリオ 1・idol 1、シナリオ 1・idol 2 で回す。週 1 の `ChooseLesson(0, Singing)`（idol 2 は `Rhythm`）を受け付けた直後に `lesson = state.Lesson` を控え、`ProduceBot.Next`（`playCards: true`）→ `Apply` を `state.Phase != Lesson` になるまで繰り返す。そのあと `result = LessonEngine.GetResult(lesson)` を使い、実行前に控えたパラメータ・ボーナスから期待値を求めて、次を確かめる: 対象のパラメータが `before + gained + BonusAmount(gained, bonus)`（上限 1000）に、ほかのパラメータは変わらない。`Hp` が `result.Hp`（`Reason == Perfect` なら `Heal(result.Hp, PerfectRecover(lesson.Setup.MaxTurns, result.LastTurn), MaxHp)`）。`result.Gained >= 25` なら `Phase == CardOffer` で `OfferCards.Count` が 1〜3、そうでなければ `Week == 2`
- `Lesson_Command_UseDrink_Rejected`: レッスン中に `UseDrink(0)` → `InvalidArgument`、`Describe` 不変
- `Lesson_Command_InvalidCard_Rejected`: レッスン中に `UseCard(9999)` → `InvalidArgument`、`Describe` 不変
- `Lesson_Command_ChooseWeekInLesson_Rejected`: レッスン中に `ChooseLesson(0, Singing)` → `UnexpectedPhase`
- `Lesson_ClearReward_OfferCardsMatchPool`: seed 1〜20 で、シナリオ 1・idol 1 の週 1 のレッスンを `ProduceBot`（`playCards: true`）で最後まで遊ぶ。終わった直後に `Phase == CardOffer` だった seed ごとに、`OfferCards` のすべてについて、カタログ上の `Plan` が `Sense` か `Free`、`Type` が `Trouble` でなく、`OfferPool.GetCandidates(...)` の結果に含まれることを確かめる（`CardOffer` になる seed が無くても失敗にしない）

`ProduceStateComparerTests`:

- `Describe_InitialState_ContainsExpectedLines`: `Start` 直後の `Describe` に、`scenario=1`・`idol=1`・`phase=1`・`week=1`・`deck=1:1:0,2:1:0,3:2:0,4:3:0,5:3:0,6:107:0,7:101:0,8:101:0,9:104:0`・`lesson=none`・`operations=0`・`result=none` の行が含まれる
- `Diff_SameState_Empty`: 同じ入力で 2 回 `Start` して `Diff` が空
- `Diff_DifferentPhase_ReportsPhaseLine`: 同じ入力で 2 回 `Start` し、片方だけ `ChooseLesson(0, Singing)` を `Apply` する → `Diff` が空でなく、`期待 phase=1 / 実際 phase=3` を含む
- `Describe_InLesson_IncludesPrefixedLessonLines`: レッスン中の `Describe` に `lesson.turn=` で始まる行がある

### 5.7 タスク 7（`T/ProduceEngineExamTests.cs`）

補助: `ModifyScenario1FinalRival(rival)`: `LoadRows()` でシナリオ 1 の週 3 の `RivalBaseScore` を `rival` に差し替えたカタログを返す。`SkipToExam(...)`: シナリオ 1・idol 1 で `ChooseLesson(0, Singing)`＋`Skip` 繰り返し、仕上げも同じで、`Phase == Exam` まで進める（得点 0）。

- `Exam_FinalWeek_StartsAutomaticallyAfterFinishing`: `SkipToExam` 後、`Phase == Exam`、`Week == 3`、`Attempt` 0、`Lesson.Setup.Kind == FinalExam`・`Lesson.Setup.Exam.Turns.Count == 12`・`EarlyEndScore` 0、`ExamBackup` が null でなく `Parameters` が `[30, 20, 10]`・`Hp` 30・`Deck.Count` 9・`NextInstanceId` 10、`streams.Requests` に `(Lesson, 3, 0)` が含まれる
- `Exam_ScoreBonusUsesParamsAtStart`: `Lesson.Setup.Exam.Turns` の各ターンの `ScoreBonusPercent` が `ProduceFormulas.ScoreBonusPercent(catalog, パラメータ, idol 1 でそのパラメータが何番か)` と一致
- `Exam_SkipOnly_RivalHigh_GoesToExamResult`: 既定のマスタ（週 3 の `RivalBaseScore` 2250）。スキップだけで 12 回。`Phase == ExamResult`、`Lesson` が null、`LastExam` が `Kind Final`・`Attempt 0`・`Score 0`・`Rank 6`・`Passed false`・`NpcScores == [1800, 1462, 1125, 787, 450]`、`ExamRetryLeft` 3、`ExamBackup` が null でない、`Result` が null
- `Exam_Retry_RestoresAndChangesStream`: 上の続きで `ExamRetry()` → `Accepted`、`Phase == Exam`、`Attempt` 1、`ExamRetryLeft` 2、`Lesson` が新しく、`Setup.Exam.Turns.Count` 12、パラメータが `ExamBackup.Parameters` と同じ、`Hp` が `ExamBackup.Hp` と同じ、`streams.Requests` に `(Lesson, 3, 1)` が含まれる。`Operations` の最後の 1 件の `Attempt` が 0（受け付ける前の値）
- `Exam_NoRetry_Fail_FinishesWithEvaluation`: `ExamNoRetry()` → `Phase == Finished`、`Result.Outcome == FinalFailed`、`FinalRank` 6、`FinalScore` 0、`EvaluationValue` 138、`Result.Rank == F`、`ExamBackup` が null、`Lesson` が null
- `Exam_RetriesExhausted_FinishesWithoutResultPhase`: 失敗するたびに `ExamRetry()` を 3 回 → 4 回目の失敗（スキップ 12 回）で `ExamRetryLeft` 0 のまま `Phase == Finished`（`ExamResult` を経ない）、`Result.Outcome == FinalFailed`、`LastExam.Attempt` 3、`state.Attempt` 0
- `Exam_Pass_StillOffersRetry_ThenNoRetryFinishes`: `rival` 1 のカタログ（NPC が全員 0 点で、スコア 0 はプレイヤーが上位 = 1 位）。スキップ 12 回 → `Phase == ExamResult`、`LastExam.Rank` 1・`Passed` 真。`ExamNoRetry()` → `Phase == Finished`、`Result.Outcome == FinalPassed`、パラメータが 60・50・40（最終 1 位の `ParamAll` 30 を足す）、`FinalRank` 1、`EvaluationValue` が 2045（345 + 1700 + 0）、`Result.Rank == D`
- `Exam_Pass_NoRetryLeft_FinishesImmediately`: `rival` 1、かつシナリオ 1 の `RetryCount` を 0 にしたカタログ → スキップ 12 回で `Phase == Finished`、`FinalPassed`
- `Exam_Pass_RankRewardCappedByParamCap`: `rival` 1、シナリオ 1 の `ParamCap` を 40 にしたカタログ。`ExamNoRetry()` 後、パラメータが 40・40・40（歌唱 30 → 上限 40、律動 20 + 30 → 40、表現 10 + 30 → 40）、`EvaluationValue` が `120 × 2300 / 1000 + 1700` = 276 + 1700 = 1976、`Result.Rank == E`
- `Exam_Mid_Pass_GoesToNextWeekWithReward`: シナリオ 2、週 6 の `RivalBaseScore` を 1 にしたカタログ（`ExamEarlyEndScore` は 900 のまま）。`ProduceBot.RunUntil` で `Phase == Exam` かつ `Week == 6` まで、`playCards: false`、`retryExam: false` で進める（週 1〜5 を通る）。`Lesson.Setup.Kind == MidExam`・`Turns.Count == 9`・`EarlyEndScore == 900`。スキップを 9 回 → `Phase == ExamResult`、`LastExam.Passed` 真・`Rank` 1。`ExamNoRetry()` → `Phase == WeekSelect`、`Week == 7`、パラメータが、`ExamBackup` を控えておいた値より各 20 増える（上限 1000 を超えない）、`MidExam` が `LastExam` と同じオブジェクト、`ExamBackup` が null
- `Exam_Mid_Fail_FinishesWithMidFailed`: 週 6 の `RivalBaseScore` を 100000 にしたカタログ。同じ流れ → `ExamNoRetry()` 後 `Phase == Finished`、`Result.Outcome == MidFailed`、`FinalRank` 0・`FinalScore` 0、`EvaluationValue` が `(パラメータの合計) × 2300 / 1000`（切り捨て）、`Result.Rank == F`
- `Exam_WrongPhaseCommands_Rejected`: `ExamResult` のとき `UseCard(1)`・`Skip()`・`OfferPick(0)`・`ChooseOther(0)` → `UnexpectedPhase`。`Exam` のとき `ExamRetry()` → `UnexpectedPhase`。`Finished` のとき `Skip()` → `UnexpectedPhase`。すべて `Describe` 不変
- `Exam_HpFromExamResult`: スキップだけの試験の前後で、`lesson`（`Lesson` の参照を控える）の `Hp` が `state.Hp` と一致
- `Exam_PrecededByFinishing_DistributesAndRecovers`: シナリオ 1 の仕上げ（週 2。この後に試験が始まる）を `ProduceBot` で最後まで遊び、`lesson` を控えて、`DistributeGain(true, Singing, gained, 75, idol.FirstParam)` と `BonusAmount` から各パラメータの期待値を求めて一致を確かめる。`Hp` が `Heal(Heal(result.Hp, Perfect なら PerfectRecover, MaxHp), RestAmount(MaxHp), MaxHp)` と一致。seed 1〜5 で回す
- `Exam_BotPlay_Scenario1_Finishes`: seed 1〜5 で、`playCards: true` の `ProduceBot.Run`（シナリオ 1・idol 1）が `Finished` になり、`state.Result` が null でない。試験でカードを使う分岐を通すためのテスト

### 5.8 タスク 8（`T/ProduceReplayTests.cs`）

`Replay(catalog, input, seed, commands)`: 新しい `TestRandomStreamSource(seed)` で `Start` し、`commands` を順に `Apply`（すべて `Accepted`）して最終の `ProduceState` を返す。

- `Replay_Scenario1_BotPlay_SameStateAsOriginal`: seed 1・2・3 × idol 1・2。`ProduceBot.Run`（`playCards: true`、`retryExam: false`）でコマンド列と最終状態を得て、`Replay` の結果との `ProduceStateComparer.Diff` が空。`Phase == Finished`。`Operations.Count == コマンド数`。`Operations` の `Number` が 0 から連番
- `Replay_Scenario2_BotPlay_SameStateAsOriginal`: シナリオ 2（初級 13 週）で同じ（seed 1・2・3 × idol 1・2・3・4。idol 3 は感性 SR、idol 4 は論理 SSR）。`retryExam: false`。`Phase == Finished`。`state.Week` が 13 か、中間試験の不合格で早く終わる（どちらでもよい）。`Result` が null でない
- `Replay_WithRetries_SameStateAsOriginal`: シナリオ 1・idol 1（感性）・`retryExam: true`・`playCards: false`（スキップだけ。毎回不合格 → 再挑戦 3 回 → `Finished`）。コマンド数が 67（週 1: 選択 1 + スキップ 5、週 2: 選択 1 + スキップ 9、試験 12 × 4 = 48、`ExamRetry` 3）。`Replay` と一致。`Result.Outcome == FinalFailed`
- `Replay_EveryStep_MatchesOriginalAfterEachCommand`: シナリオ 2・seed 1・idol 1。元の実行は `ProduceBot.Next` と `Apply` を手で回し（`Run` を使わない）、各コマンドを `Apply` した直後の `Describe` を覚える。`Replay` の途中でも同じタイミングで `Describe` を取り、全ステップで行の並びが一致（途中の状態も再現される）
- `Replay_RejectedCommandsInserted_FinalStateUnchanged`: シナリオ 1・idol 1・seed 1 の `ProduceBot` の列を、`Replay` の補助とは別に手で流す。コマンドの 1 件目・2 件目・3 件目を `Apply` した直後に、それぞれ拒否されるコマンド（`OfferPick(0)`・`UseCard(9999)`・`ExamRetry()`）を `Apply` して、結果が `Accepted` でないこと、その前後で `Describe` が同じことを確かめる。最後まで流した状態が、挟まない `Replay` の結果と `Diff` が空

## 6. 未決事項

なし。実装担当が設計判断に出会ったら、止まって報告する。

## 7. wiki への反映（2026-11-01 に済み）

[produce-domain.md](../wiki/architecture/produce-domain.md)（5章の表の崩れの修正、決めたことの確定、実装メモ）、[lesson-domain-types.md](../wiki/architecture/lesson-domain-types.md)（`CardIds`）、[glossary.md](../wiki/glossary.md)（挑戦回数）、[game-spec.md](../wiki/overview/game-spec.md)（順 4 を done に）。

## 8. 実装結果の補足

- `EvaluationScoreBandRow` のコンストラクタの引数名は `@fixed`（`fixed` が予約語のため）
- `ProduceCatalog.Create` は、計画の V1〜V10 に加えて、参照の存在（存在しないシナリオを指す週など）も検査する
- タプルをキーにした辞書は使わず `long` のキーにした（コードレビューの指摘。IL2CPP の AOT 対策）
- 新しい `.cs` に `.meta` を足した（Unity が `shared/` を取り込むため）
