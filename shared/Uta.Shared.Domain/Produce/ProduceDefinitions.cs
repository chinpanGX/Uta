using System;
using System.Collections.Generic;
using Uta.Shared.Domain.Lesson;

namespace Uta.Shared.Domain.Produce
{
    /// <summary>授業の選択肢 1 つ（produce.md 3.2）。</summary>
    public sealed class ClassOptionDefinition
    {
        internal ClassOptionDefinition(int hpCost, int paramGain, bool givesCard)
        {
            HpCost = hpCost;
            ParamGain = paramGain;
            GivesCard = givesCard;
        }

        public int HpCost { get; }
        public int ParamGain { get; }
        public bool GivesCard { get; }
    }

    /// <summary>シナリオの 1 週。</summary>
    public sealed class WeekDefinition
    {
        internal WeekDefinition(
            int week,
            WeekKind kind,
            int turnsSense,
            int turnsLogic,
            int clearTarget,
            int perfectTarget,
            int examEarlyEndScore,
            int rivalBaseScore,
            IReadOnlyList<ActionKind> actions,
            IReadOnlyList<ClassOptionDefinition> classOptions)
        {
            Week = week;
            Kind = kind;
            TurnsSense = turnsSense;
            TurnsLogic = turnsLogic;
            ClearTarget = clearTarget;
            PerfectTarget = perfectTarget;
            ExamEarlyEndScore = examEarlyEndScore;
            RivalBaseScore = rivalBaseScore;
            Actions = actions;
            ClassOptions = classOptions;
        }

        public int Week { get; }
        public WeekKind Kind { get; }
        public int TurnsSense { get; }
        public int TurnsLogic { get; }
        public int ClearTarget { get; }
        public int PerfectTarget { get; }
        public int ExamEarlyEndScore { get; }
        public int RivalBaseScore { get; }

        /// <summary>行動の枠。Order の昇順</summary>
        public IReadOnlyList<ActionKind> Actions { get; }

        /// <summary>授業の選択肢。Order の昇順</summary>
        public IReadOnlyList<ClassOptionDefinition> ClassOptions { get; }

        /// <summary>レッスンのターン数。Sense なら TurnsSense、それ以外は TurnsLogic</summary>
        public int GetTurns(Plan plan)
        {
            return plan == Plan.Sense ? TurnsSense : TurnsLogic;
        }
    }

    /// <summary>シナリオ（週構成を含む）。</summary>
    public sealed class ScenarioDefinition
    {
        internal ScenarioDefinition(
            int scenarioId,
            string name,
            Difficulty difficulty,
            int weekCount,
            int paramCap,
            int retryCount,
            IReadOnlyList<WeekDefinition> weeks)
        {
            ScenarioId = scenarioId;
            Name = name;
            Difficulty = difficulty;
            WeekCount = weekCount;
            ParamCap = paramCap;
            RetryCount = retryCount;
            Weeks = weeks;
        }

        public int ScenarioId { get; }
        public string Name { get; }
        public Difficulty Difficulty { get; }
        public int WeekCount { get; }
        public int ParamCap { get; }
        public int RetryCount { get; }

        /// <summary>週 1 から</summary>
        public IReadOnlyList<WeekDefinition> Weeks { get; }

        /// <summary>1 以上 WeekCount 以下。範囲外は ArgumentOutOfRangeException</summary>
        public WeekDefinition GetWeek(int week)
        {
            if (week < 1 || week > WeekCount || week > Weeks.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(week));
            }

            return Weeks[week - 1];
        }
    }

    /// <summary>審査の順と審査ターン数のパターン（produce.md 5.1）。</summary>
    public sealed class ExamPatternDefinition
    {
        private readonly int[] mid;
        private readonly int[] finalSense;
        private readonly int[] finalLogic;

        internal ExamPatternDefinition(int examPatternId, int firstTurnSecondRatePerTenThousand, int[] mid, int[] finalSense, int[] finalLogic)
        {
            ExamPatternId = examPatternId;
            FirstTurnSecondRatePerTenThousand = firstTurnSecondRatePerTenThousand;
            this.mid = mid;
            this.finalSense = finalSense;
            this.finalLogic = finalLogic;
        }

        public int ExamPatternId { get; }
        public int FirstTurnSecondRatePerTenThousand { get; }

        /// <summary>新しい長さ 3 の配列 {1番, 2番, 3番}。kind が Nothing なら ArgumentOutOfRangeException</summary>
        public int[] GetTurnCounts(ExamKind kind, Plan plan)
        {
            switch (kind)
            {
                case ExamKind.Mid:
                    return (int[])mid.Clone();
                case ExamKind.Final:
                    return (int[])(plan == Plan.Sense ? finalSense : finalLogic).Clone();
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }
    }

    /// <summary>育成アイドル。</summary>
    public sealed class IdolDefinition
    {
        private readonly int[] initialParams;
        private readonly int[] bonusPerThousand;

        internal IdolDefinition(
            int idolId,
            string name,
            Rarity rarity,
            Plan plan,
            RecommendKind recommend,
            int maxHp,
            int examPatternId,
            ParamKind firstParam,
            ParamKind secondParam,
            ParamKind thirdParam,
            int uniqueCardId,
            IReadOnlyList<int> recommendCardIds,
            int[] initialParams,
            int[] bonusPerThousand)
        {
            IdolId = idolId;
            Name = name;
            Rarity = rarity;
            Plan = plan;
            Recommend = recommend;
            MaxHp = maxHp;
            ExamPatternId = examPatternId;
            FirstParam = firstParam;
            SecondParam = secondParam;
            ThirdParam = thirdParam;
            UniqueCardId = uniqueCardId;
            RecommendCardIds = recommendCardIds;
            this.initialParams = initialParams;
            this.bonusPerThousand = bonusPerThousand;
        }

        public int IdolId { get; }
        public string Name { get; }
        public Rarity Rarity { get; }
        public Plan Plan { get; }
        public RecommendKind Recommend { get; }
        public int MaxHp { get; }
        public int ExamPatternId { get; }
        public ParamKind FirstParam { get; }
        public ParamKind SecondParam { get; }
        public ParamKind ThirdParam { get; }
        public int UniqueCardId { get; }

        /// <summary>おすすめデッキ。Order の昇順</summary>
        public IReadOnlyList<int> RecommendCardIds { get; }

        /// <summary>Nothing は ArgumentOutOfRangeException</summary>
        public int GetInitialParam(ParamKind kind)
        {
            return initialParams[ProduceParams.ToIndex(kind)];
        }

        /// <summary>Nothing は ArgumentOutOfRangeException</summary>
        public int GetBonusPerThousand(ParamKind kind)
        {
            return bonusPerThousand[ProduceParams.ToIndex(kind)];
        }

        /// <summary>審査の順。1 → FirstParam、2 → SecondParam、3 → ThirdParam。それ以外は ArgumentOutOfRangeException</summary>
        public ParamKind GetExamParam(int rank)
        {
            switch (rank)
            {
                case 1:
                    return FirstParam;
                case 2:
                    return SecondParam;
                case 3:
                    return ThirdParam;
                default:
                    throw new ArgumentOutOfRangeException(nameof(rank));
            }
        }
    }

    /// <summary>サポートカードの特技 1 つ（idol-support.md 2.3）。</summary>
    public sealed class SupportAbilityDefinition
    {
        internal SupportAbilityDefinition(SupportAbilityKind kind, ParamKind param, int valueAtLv1, int valueAtMax)
        {
            Kind = kind;
            Param = param;
            ValueAtLv1 = valueAtLv1;
            ValueAtMax = valueAtMax;
        }

        public SupportAbilityKind Kind { get; }
        public ParamKind Param { get; }
        public int ValueAtLv1 { get; }
        public int ValueAtMax { get; }
    }

    /// <summary>サポートカード。</summary>
    public sealed class SupportCardDefinition
    {
        internal SupportCardDefinition(
            int supportCardId,
            string name,
            Rarity rarity,
            SupportType type,
            Plan plan,
            IReadOnlyList<SupportAbilityDefinition> abilities,
            int maxLevelAtFullLimitBreak)
        {
            SupportCardId = supportCardId;
            Name = name;
            Rarity = rarity;
            Type = type;
            Plan = plan;
            Abilities = abilities;
            MaxLevelAtFullLimitBreak = maxLevelAtFullLimitBreak;
        }

        public int SupportCardId { get; }
        public string Name { get; }
        public Rarity Rarity { get; }
        public SupportType Type { get; }
        public Plan Plan { get; }

        /// <summary>特技。Order の昇順</summary>
        public IReadOnlyList<SupportAbilityDefinition> Abilities { get; }

        /// <summary>同じレアの SupportLevelCap の LimitBreak = 4 の MaxLevel</summary>
        public int MaxLevelAtFullLimitBreak { get; }
    }
}
