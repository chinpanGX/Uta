using Uta.Shared.Domain.Lesson;

namespace Uta.Shared.Domain.Produce
{
    // マスタの行と同じ列を持つ、Domain の行の型。各側の Infrastructure がマスタの値をそのまま詰めて ProduceCatalog.Create に渡す。
    // コンストラクタの引数は CSV の列の順。コンストラクタでは検査しない（検査は ProduceCatalog.Create）。

    /// <summary>scenario テーブルの 1 行。</summary>
    public readonly struct ScenarioRow
    {
        public ScenarioRow(int scenarioId, string name, Difficulty difficulty, int weekCount, int paramCap, int retryCount)
        {
            ScenarioId = scenarioId;
            Name = name;
            Difficulty = difficulty;
            WeekCount = weekCount;
            ParamCap = paramCap;
            RetryCount = retryCount;
        }

        public int ScenarioId { get; }
        public string Name { get; }
        public Difficulty Difficulty { get; }
        public int WeekCount { get; }
        public int ParamCap { get; }
        public int RetryCount { get; }
    }

    /// <summary>scenarioWeek テーブルの 1 行。</summary>
    public readonly struct ScenarioWeekRow
    {
        public ScenarioWeekRow(int scenarioId, int week, WeekKind kind, int turnsSense, int turnsLogic, int clearTarget, int perfectTarget, int examEarlyEndScore, int rivalBaseScore)
        {
            ScenarioId = scenarioId;
            Week = week;
            Kind = kind;
            TurnsSense = turnsSense;
            TurnsLogic = turnsLogic;
            ClearTarget = clearTarget;
            PerfectTarget = perfectTarget;
            ExamEarlyEndScore = examEarlyEndScore;
            RivalBaseScore = rivalBaseScore;
        }

        public int ScenarioId { get; }
        public int Week { get; }
        public WeekKind Kind { get; }
        public int TurnsSense { get; }
        public int TurnsLogic { get; }
        public int ClearTarget { get; }
        public int PerfectTarget { get; }
        public int ExamEarlyEndScore { get; }
        public int RivalBaseScore { get; }
    }

    /// <summary>weekActionSlot テーブルの 1 行。</summary>
    public readonly struct WeekActionSlotRow
    {
        public WeekActionSlotRow(int scenarioId, int week, int order, ActionKind kind)
        {
            ScenarioId = scenarioId;
            Week = week;
            Order = order;
            Kind = kind;
        }

        public int ScenarioId { get; }
        public int Week { get; }
        public int Order { get; }
        public ActionKind Kind { get; }
    }

    /// <summary>classOption テーブルの 1 行。</summary>
    public readonly struct ClassOptionRow
    {
        public ClassOptionRow(int scenarioId, int week, int order, int hpCost, int paramGain, bool givesCard)
        {
            ScenarioId = scenarioId;
            Week = week;
            Order = order;
            HpCost = hpCost;
            ParamGain = paramGain;
            GivesCard = givesCard;
        }

        public int ScenarioId { get; }
        public int Week { get; }
        public int Order { get; }
        public int HpCost { get; }
        public int ParamGain { get; }
        public bool GivesCard { get; }
    }

    /// <summary>examPattern テーブルの 1 行。</summary>
    public readonly struct ExamPatternRow
    {
        public ExamPatternRow(int examPatternId, int firstPercent, int secondPercent, int thirdPercent, int midFirstTurns, int midSecondTurns, int midThirdTurns, int finalSenseFirstTurns, int finalSenseSecondTurns, int finalSenseThirdTurns, int finalLogicFirstTurns, int finalLogicSecondTurns, int finalLogicThirdTurns, int firstTurnSecondRatePerTenThousand)
        {
            ExamPatternId = examPatternId;
            FirstPercent = firstPercent;
            SecondPercent = secondPercent;
            ThirdPercent = thirdPercent;
            MidFirstTurns = midFirstTurns;
            MidSecondTurns = midSecondTurns;
            MidThirdTurns = midThirdTurns;
            FinalSenseFirstTurns = finalSenseFirstTurns;
            FinalSenseSecondTurns = finalSenseSecondTurns;
            FinalSenseThirdTurns = finalSenseThirdTurns;
            FinalLogicFirstTurns = finalLogicFirstTurns;
            FinalLogicSecondTurns = finalLogicSecondTurns;
            FinalLogicThirdTurns = finalLogicThirdTurns;
            FirstTurnSecondRatePerTenThousand = firstTurnSecondRatePerTenThousand;
        }

        public int ExamPatternId { get; }
        public int FirstPercent { get; }
        public int SecondPercent { get; }
        public int ThirdPercent { get; }
        public int MidFirstTurns { get; }
        public int MidSecondTurns { get; }
        public int MidThirdTurns { get; }
        public int FinalSenseFirstTurns { get; }
        public int FinalSenseSecondTurns { get; }
        public int FinalSenseThirdTurns { get; }
        public int FinalLogicFirstTurns { get; }
        public int FinalLogicSecondTurns { get; }
        public int FinalLogicThirdTurns { get; }
        public int FirstTurnSecondRatePerTenThousand { get; }
    }

    /// <summary>examRankReward テーブルの 1 行。</summary>
    public readonly struct ExamRankRewardRow
    {
        public ExamRankRewardRow(ExamKind examKind, int rank, int paramAll)
        {
            ExamKind = examKind;
            Rank = rank;
            ParamAll = paramAll;
        }

        public ExamKind ExamKind { get; }
        public int Rank { get; }
        public int ParamAll { get; }
    }

    /// <summary>scoreBonusSetting テーブルの 1 行。</summary>
    public readonly struct ScoreBonusSettingRow
    {
        public ScoreBonusSettingRow(int id, int aPerThousand, int bFirst, int bSecond, int bThird)
        {
            Id = id;
            APerThousand = aPerThousand;
            BFirst = bFirst;
            BSecond = bSecond;
            BThird = bThird;
        }

        public int Id { get; }
        public int APerThousand { get; }
        public int BFirst { get; }
        public int BSecond { get; }
        public int BThird { get; }
    }

    /// <summary>evaluationScoreBand テーブルの 1 行。</summary>
    public readonly struct EvaluationScoreBandRow
    {
        public EvaluationScoreBandRow(int order, int minScore, int scalePerThousand, int @fixed)
        {
            Order = order;
            MinScore = minScore;
            ScalePerThousand = scalePerThousand;
            Fixed = @fixed;
        }

        public int Order { get; }
        public int MinScore { get; }
        public int ScalePerThousand { get; }
        public int Fixed { get; }
    }

    /// <summary>evaluationRank テーブルの 1 行。</summary>
    public readonly struct EvaluationRankRow
    {
        public EvaluationRankRow(ProduceRank rank, int minValue)
        {
            Rank = rank;
            MinValue = minValue;
        }

        public ProduceRank Rank { get; }
        public int MinValue { get; }
    }

    /// <summary>offerRarityWeight テーブルの 1 行。</summary>
    public readonly struct OfferRarityWeightRow
    {
        public OfferRarityWeightRow(Rarity rarity, int weight)
        {
            Rarity = rarity;
            Weight = weight;
        }

        public Rarity Rarity { get; }
        public int Weight { get; }
    }

    /// <summary>idol テーブルの 1 行。</summary>
    public readonly struct IdolRow
    {
        public IdolRow(int idolId, string name, Rarity rarity, Plan plan, RecommendKind recommend, int maxHp, int initialSinging, int initialRhythm, int initialExpression, int bonusSingingPerThousand, int bonusRhythmPerThousand, int bonusExpressionPerThousand, int examPatternId, ParamKind firstParam, ParamKind secondParam, ParamKind thirdParam, int uniqueCardId)
        {
            IdolId = idolId;
            Name = name;
            Rarity = rarity;
            Plan = plan;
            Recommend = recommend;
            MaxHp = maxHp;
            InitialSinging = initialSinging;
            InitialRhythm = initialRhythm;
            InitialExpression = initialExpression;
            BonusSingingPerThousand = bonusSingingPerThousand;
            BonusRhythmPerThousand = bonusRhythmPerThousand;
            BonusExpressionPerThousand = bonusExpressionPerThousand;
            ExamPatternId = examPatternId;
            FirstParam = firstParam;
            SecondParam = secondParam;
            ThirdParam = thirdParam;
            UniqueCardId = uniqueCardId;
        }

        public int IdolId { get; }
        public string Name { get; }
        public Rarity Rarity { get; }
        public Plan Plan { get; }
        public RecommendKind Recommend { get; }
        public int MaxHp { get; }
        public int InitialSinging { get; }
        public int InitialRhythm { get; }
        public int InitialExpression { get; }
        public int BonusSingingPerThousand { get; }
        public int BonusRhythmPerThousand { get; }
        public int BonusExpressionPerThousand { get; }
        public int ExamPatternId { get; }
        public ParamKind FirstParam { get; }
        public ParamKind SecondParam { get; }
        public ParamKind ThirdParam { get; }
        public int UniqueCardId { get; }
    }

    /// <summary>idolRecommendDeck テーブルの 1 行。</summary>
    public readonly struct IdolRecommendDeckRow
    {
        public IdolRecommendDeckRow(RecommendKind recommend, int order, int cardId)
        {
            Recommend = recommend;
            Order = order;
            CardId = cardId;
        }

        public RecommendKind Recommend { get; }
        public int Order { get; }
        public int CardId { get; }
    }

    /// <summary>supportCard テーブルの 1 行。</summary>
    public readonly struct SupportCardRow
    {
        public SupportCardRow(int supportCardId, string name, Rarity rarity, SupportType type, Plan plan)
        {
            SupportCardId = supportCardId;
            Name = name;
            Rarity = rarity;
            Type = type;
            Plan = plan;
        }

        public int SupportCardId { get; }
        public string Name { get; }
        public Rarity Rarity { get; }
        public SupportType Type { get; }
        public Plan Plan { get; }
    }

    /// <summary>supportAbility テーブルの 1 行。</summary>
    public readonly struct SupportAbilityRow
    {
        public SupportAbilityRow(int supportCardId, int order, SupportAbilityKind kind, ParamKind param, int valueAtLv1, int valueAtMax)
        {
            SupportCardId = supportCardId;
            Order = order;
            Kind = kind;
            Param = param;
            ValueAtLv1 = valueAtLv1;
            ValueAtMax = valueAtMax;
        }

        public int SupportCardId { get; }
        public int Order { get; }
        public SupportAbilityKind Kind { get; }
        public ParamKind Param { get; }
        public int ValueAtLv1 { get; }
        public int ValueAtMax { get; }
    }

    /// <summary>supportLevelCap テーブルの 1 行。</summary>
    public readonly struct SupportLevelCapRow
    {
        public SupportLevelCapRow(Rarity rarity, int limitBreak, int maxLevel)
        {
            Rarity = rarity;
            LimitBreak = limitBreak;
            MaxLevel = maxLevel;
        }

        public Rarity Rarity { get; }
        public int LimitBreak { get; }
        public int MaxLevel { get; }
    }

    /// <summary>commonDeckCard テーブルの 1 行。</summary>
    public readonly struct CommonDeckCardRow
    {
        public CommonDeckCardRow(int order, int cardId)
        {
            Order = order;
            CardId = cardId;
        }

        public int Order { get; }
        public int CardId { get; }
    }
}
