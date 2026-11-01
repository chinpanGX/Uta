using System;
using System.Collections.Generic;
using MasterMemory;
using MasterMemory.Addons;
using MessagePack;

namespace Uta.MasterData
{
    public enum Difficulty
    {
        [InputName("Nothing")] Nothing = 0,
        [InputName("Beginner")] Beginner = 1,
        [InputName("Intermediate")] Intermediate = 2,
        [InputName("Advanced")] Advanced = 3,
    }

    public enum WeekKind
    {
        [InputName("Nothing")] Nothing = 0,
        [InputName("Action")] Action = 1,
        [InputName("Finishing")] Finishing = 2,
        [InputName("MidExam")] MidExam = 3,
        [InputName("FinalExam")] FinalExam = 4,
    }

    public enum ActionKind
    {
        [InputName("Nothing")] Nothing = 0,
        [InputName("Lesson")] Lesson = 1,
        [InputName("Class")] Class = 2,
        [InputName("Supply")] Supply = 3,
        [InputName("Outing")] Outing = 4,
        [InputName("Consult")] Consult = 5,
    }

    public enum ExamKind
    {
        [InputName("Nothing")] Nothing = 0,
        [InputName("Mid")] Mid = 1,
        [InputName("Final")] Final = 2,
    }

    public enum ProduceRank
    {
        [InputName("Nothing")] Nothing = 0,
        [InputName("F")] F = 1,
        [InputName("E")] E = 2,
        [InputName("D")] D = 3,
        [InputName("C")] C = 4,
        [InputName("CPlus")] CPlus = 5,
        [InputName("B")] B = 6,
        [InputName("BPlus")] BPlus = 7,
        [InputName("A")] A = 8,
        [InputName("APlus")] APlus = 9,
        [InputName("S")] S = 10,
        [InputName("SPlus")] SPlus = 11,
        [InputName("SS")] SS = 12,
        [InputName("SSPlus")] SSPlus = 13,
    }

    [MemoryTable("scenario"), MessagePackObject(true)]
    public class Scenario : IValidatable<Scenario>
    {
        [PrimaryKey]
        public int ScenarioId { get; init; }

        public string Name { get; init; } = "";

        public Difficulty Difficulty { get; init; }

        public int WeekCount { get; init; }

        public int ParamCap { get; init; }

        public int RetryCount { get; init; }

        void IValidatable<Scenario>.Validate(IValidator<Scenario> validator)
        {
            validator.Validate(x => x.Difficulty != Difficulty.Nothing, "scenario: Difficulty が Nothing");
            validator.Validate(x => x.WeekCount >= 1, "scenario: WeekCount が 1 未満");
            validator.Validate(x => x.ParamCap >= 1, "scenario: ParamCap が 1 未満");
            validator.Validate(x => x.RetryCount >= 0, "scenario: RetryCount が負");
        }
    }

    [MemoryTable("scenarioWeek"), MessagePackObject(true)]
    public class ScenarioWeek : IValidatable<ScenarioWeek>
    {
        [PrimaryKey(keyOrder: 0)]
        [Exists(typeof(Scenario), nameof(Scenario.ScenarioId))]
        public int ScenarioId { get; init; }

        [PrimaryKey(keyOrder: 1)]
        public int Week { get; init; }

        public WeekKind Kind { get; init; }

        public int TurnsSense { get; init; }

        public int TurnsLogic { get; init; }

        public int ClearTarget { get; init; }

        public int PerfectTarget { get; init; }

        public int ExamEarlyEndScore { get; init; }

        public int RivalBaseScore { get; init; }

        void IValidatable<ScenarioWeek>.Validate(IValidator<ScenarioWeek> validator)
        {
            validator.Validate(x => x.Kind != WeekKind.Nothing, "scenarioWeek: Kind が Nothing");
            validator.Validate(
                x => x.TurnsSense >= 0 && x.TurnsLogic >= 0 && x.ClearTarget >= 0 && x.PerfectTarget >= 0
                    && x.ExamEarlyEndScore >= 0 && x.RivalBaseScore >= 0,
                "scenarioWeek: 数値の列が負");
            validator.Validate(x => x.PerfectTarget >= x.ClearTarget, "scenarioWeek: PerfectTarget が ClearTarget 未満");
            validator.Validate(
                x => x.Kind != WeekKind.Finishing
                    || (x.TurnsSense >= 1 && x.TurnsLogic >= 1 && x.ClearTarget >= 1 && x.PerfectTarget >= 1),
                "scenarioWeek: Finishing の TurnsSense / TurnsLogic / ClearTarget / PerfectTarget が 1 未満");
            validator.Validate(
                x => (x.Kind != WeekKind.MidExam && x.Kind != WeekKind.FinalExam)
                    || (x.TurnsSense == 0 && x.TurnsLogic == 0 && x.ClearTarget == 0 && x.PerfectTarget == 0 && x.RivalBaseScore >= 1),
                "scenarioWeek: 試験の週の TurnsSense / TurnsLogic / ClearTarget / PerfectTarget が 0 でないか、RivalBaseScore が 1 未満");
            validator.Validate(
                x => x.Kind != WeekKind.FinalExam || x.ExamEarlyEndScore == 0,
                "scenarioWeek: FinalExam の ExamEarlyEndScore が 0 でない");
            validator.Validate(
                x => x.Kind == WeekKind.MidExam || x.Kind == WeekKind.FinalExam || (x.RivalBaseScore == 0 && x.ExamEarlyEndScore == 0),
                "scenarioWeek: 試験以外の週の RivalBaseScore か ExamEarlyEndScore が 0 でない");
        }
    }

    [MemoryTable("weekActionSlot"), MessagePackObject(true)]
    public class WeekActionSlot : IValidatable<WeekActionSlot>
    {
        [PrimaryKey(keyOrder: 0)]
        [Exists(typeof(Scenario), nameof(Scenario.ScenarioId))]
        public int ScenarioId { get; init; }

        [PrimaryKey(keyOrder: 1)]
        public int Week { get; init; }

        [PrimaryKey(keyOrder: 2)]
        public int Order { get; init; }

        public ActionKind Kind { get; init; }

        void IValidatable<WeekActionSlot>.Validate(IValidator<WeekActionSlot> validator)
        {
            validator.Validate(x => x.Kind != ActionKind.Nothing, "weekActionSlot: Kind が Nothing");
            validator.Validate(x => x.Order >= 1, "weekActionSlot: Order が 1 未満");
        }
    }

    [MemoryTable("classOption"), MessagePackObject(true)]
    public class ClassOption : IValidatable<ClassOption>
    {
        [PrimaryKey(keyOrder: 0)]
        [Exists(typeof(Scenario), nameof(Scenario.ScenarioId))]
        public int ScenarioId { get; init; }

        [PrimaryKey(keyOrder: 1)]
        public int Week { get; init; }

        [PrimaryKey(keyOrder: 2)]
        public int Order { get; init; }

        public int HpCost { get; init; }

        public int ParamGain { get; init; }

        public bool GivesCard { get; init; }

        void IValidatable<ClassOption>.Validate(IValidator<ClassOption> validator)
        {
            validator.Validate(x => x.Order >= 1, "classOption: Order が 1 未満");
            validator.Validate(x => x.HpCost >= 0, "classOption: HpCost が負");
            validator.Validate(x => x.ParamGain >= 1, "classOption: ParamGain が 1 未満");
        }
    }

    [MemoryTable("examPattern"), MessagePackObject(true)]
    public class ExamPattern : IValidatable<ExamPattern>
    {
        [PrimaryKey]
        public int ExamPatternId { get; init; }

        public int FirstPercent { get; init; }

        public int SecondPercent { get; init; }

        public int ThirdPercent { get; init; }

        public int MidFirstTurns { get; init; }

        public int MidSecondTurns { get; init; }

        public int MidThirdTurns { get; init; }

        public int FinalSenseFirstTurns { get; init; }

        public int FinalSenseSecondTurns { get; init; }

        public int FinalSenseThirdTurns { get; init; }

        public int FinalLogicFirstTurns { get; init; }

        public int FinalLogicSecondTurns { get; init; }

        public int FinalLogicThirdTurns { get; init; }

        public int FirstTurnSecondRatePerTenThousand { get; init; }

        void IValidatable<ExamPattern>.Validate(IValidator<ExamPattern> validator)
        {
            validator.Validate(x => x.FirstPercent + x.SecondPercent + x.ThirdPercent == 100, "examPattern: Percent の合計が 100 でない");
            validator.Validate(x => x.FirstPercent >= 0 && x.SecondPercent >= 0 && x.ThirdPercent >= 0, "examPattern: Percent が負");
            validator.Validate(
                x => x.FirstPercent < 45 || x.FirstTurnSecondRatePerTenThousand == 0,
                "examPattern: FirstPercent が 45 以上なのに FirstTurnSecondRatePerTenThousand が 0 でない");
            validator.Validate(x => x.MidFirstTurns + x.MidSecondTurns + x.MidThirdTurns == 9, "examPattern: Mid のターン数の合計が 9 でない");
            validator.Validate(
                x => x.FinalSenseFirstTurns + x.FinalSenseSecondTurns + x.FinalSenseThirdTurns == 12,
                "examPattern: FinalSense のターン数の合計が 12 でない");
            validator.Validate(
                x => x.FinalLogicFirstTurns + x.FinalLogicSecondTurns + x.FinalLogicThirdTurns == 11,
                "examPattern: FinalLogic のターン数の合計が 11 でない");
            validator.Validate(
                x => x.MidFirstTurns >= 1 && x.MidSecondTurns >= 1 && x.MidThirdTurns >= 1
                    && x.FinalSenseFirstTurns >= 1 && x.FinalSenseSecondTurns >= 1 && x.FinalSenseThirdTurns >= 1
                    && x.FinalLogicFirstTurns >= 1 && x.FinalLogicSecondTurns >= 1 && x.FinalLogicThirdTurns >= 1,
                "examPattern: ターン数が 1 未満");
            validator.Validate(
                x => x.FirstTurnSecondRatePerTenThousand >= 0 && x.FirstTurnSecondRatePerTenThousand <= 10000,
                "examPattern: FirstTurnSecondRatePerTenThousand が 0〜10000 の外");
        }
    }

    [MemoryTable("examRankReward"), MessagePackObject(true)]
    public class ExamRankReward : IValidatable<ExamRankReward>
    {
        [PrimaryKey(keyOrder: 0)]
        public ExamKind ExamKind { get; init; }

        [PrimaryKey(keyOrder: 1)]
        public int Rank { get; init; }

        public int ParamAll { get; init; }

        void IValidatable<ExamRankReward>.Validate(IValidator<ExamRankReward> validator)
        {
            validator.Validate(x => x.ExamKind != ExamKind.Nothing, "examRankReward: ExamKind が Nothing");
            validator.Validate(x => x.Rank >= 1 && x.Rank <= 3, "examRankReward: Rank が 1〜3 の外");
            validator.Validate(x => x.ParamAll >= 0, "examRankReward: ParamAll が負");
        }
    }

    [MemoryTable("scoreBonusSetting"), MessagePackObject(true)]
    public class ScoreBonusSetting : IValidatable<ScoreBonusSetting>
    {
        [PrimaryKey]
        public int Id { get; init; }

        public int APerThousand { get; init; }

        public int BFirst { get; init; }

        public int BSecond { get; init; }

        public int BThird { get; init; }

        void IValidatable<ScoreBonusSetting>.Validate(IValidator<ScoreBonusSetting> validator)
        {
            validator.Validate(x => x.Id == 1, "scoreBonusSetting: Id が 1 でない");
            validator.Validate(
                x => x.APerThousand >= 0 && x.BFirst >= 0 && x.BSecond >= 0 && x.BThird >= 0,
                "scoreBonusSetting: 係数が負");
        }
    }

    [MemoryTable("evaluationScoreBand"), MessagePackObject(true)]
    public class EvaluationScoreBand : IValidatable<EvaluationScoreBand>
    {
        [PrimaryKey]
        public int Order { get; init; }

        public int MinScore { get; init; }

        public int ScalePerThousand { get; init; }

        public int Fixed { get; init; }

        void IValidatable<EvaluationScoreBand>.Validate(IValidator<EvaluationScoreBand> validator)
        {
            validator.Validate(x => x.Order >= 1, "evaluationScoreBand: Order が 1 未満");
            validator.Validate(x => x.MinScore >= 0, "evaluationScoreBand: MinScore が負");
            validator.Validate(x => x.ScalePerThousand >= 0, "evaluationScoreBand: ScalePerThousand が負");
            validator.Validate(x => x.Fixed >= 0, "evaluationScoreBand: Fixed が負");
        }
    }

    [MemoryTable("evaluationRank"), MessagePackObject(true)]
    public class EvaluationRank : IValidatable<EvaluationRank>
    {
        [PrimaryKey]
        public ProduceRank Rank { get; init; }

        public int MinValue { get; init; }

        void IValidatable<EvaluationRank>.Validate(IValidator<EvaluationRank> validator)
        {
            validator.Validate(x => x.Rank != ProduceRank.Nothing, "evaluationRank: Rank が Nothing");
            validator.Validate(x => x.MinValue >= 0, "evaluationRank: MinValue が負");
        }
    }

    [ServerOnly]
    [MemoryTable("evaluationRankReward"), MessagePackObject(true)]
    public class EvaluationRankReward : IValidatable<EvaluationRankReward>
    {
        [PrimaryKey]
        public ProduceRank Rank { get; init; }

        public int GachaCurrency { get; init; }

        public int Gold { get; init; }

        public int SupportPoints { get; init; }

        public int Exp { get; init; }

        void IValidatable<EvaluationRankReward>.Validate(IValidator<EvaluationRankReward> validator)
        {
            validator.Validate(x => x.Rank != ProduceRank.Nothing, "evaluationRankReward: Rank が Nothing");
            validator.Validate(
                x => x.GachaCurrency >= 0 && x.Gold >= 0 && x.SupportPoints >= 0 && x.Exp >= 0,
                "evaluationRankReward: 量が負");
        }
    }

    [MemoryTable("offerRarityWeight"), MessagePackObject(true)]
    public class OfferRarityWeight : IValidatable<OfferRarityWeight>
    {
        [PrimaryKey]
        public Rarity Rarity { get; init; }

        public int Weight { get; init; }

        void IValidatable<OfferRarityWeight>.Validate(IValidator<OfferRarityWeight> validator)
        {
            validator.Validate(
                x => x.Rarity == Rarity.N || x.Rarity == Rarity.R || x.Rarity == Rarity.SR || x.Rarity == Rarity.SSR,
                "offerRarityWeight: Rarity が N〜SSR でない");
            validator.Validate(x => x.Weight >= 0, "offerRarityWeight: Weight が負");
        }
    }

    [MemoryTable("playerLevelExp"), MessagePackObject(true)]
    public class PlayerLevelExp : IValidatable<PlayerLevelExp>
    {
        [PrimaryKey]
        public int Level { get; init; }

        public int RequiredExp { get; init; }

        void IValidatable<PlayerLevelExp>.Validate(IValidator<PlayerLevelExp> validator)
        {
            validator.Validate(x => x.Level >= 1, "playerLevelExp: Level が 1 未満");
            validator.Validate(x => x.RequiredExp >= 1, "playerLevelExp: RequiredExp が 1 未満");
        }
    }
}
