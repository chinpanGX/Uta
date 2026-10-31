namespace Uta.Shared.Domain.Lesson
{
    // マスタ・保存の値に使うので、enum の数値は変えない。追加は末尾に足す。

    /// <summary>プラン（lesson-card.md 3.1）。</summary>
    public enum Plan
    {
        Free = 0,
        Sense = 1,
        Logic = 2,
    }

    /// <summary>カードの種別。</summary>
    public enum CardType
    {
        Active = 0,
        Mental = 1,
        Trouble = 2,
    }

    public enum Rarity
    {
        N = 0,
        R = 1,
        SR = 2,
        SSR = 3,
    }

    /// <summary>パラメータの種類（歌唱 / 律動 / 表現）。</summary>
    public enum ParamKind
    {
        Singing = 0,
        Rhythm = 1,
        Expression = 2,
    }

    /// <summary>コストの種類（lesson-card.md 5章）。</summary>
    public enum CostKind
    {
        /// <summary>元気→体力の順に払う。補正を受ける</summary>
        Consume = 0,
        /// <summary>体力から直接払う。補正を受けない</summary>
        HpDirect = 1,
        GoodConditionTurns = 2,
        Focus = 3,
        Impression = 4,
        Motivation = 5,
        ExcellentTurns = 6,
    }

    /// <summary>条件の対象（lesson-card.md 6.1）。</summary>
    public enum ConditionSubject
    {
        GoodConditionTurns = 0,
        ExcellentTurns = 1,
        Focus = 2,
        Impression = 3,
        Motivation = 4,
        Genki = 5,
        /// <summary>体力の割合（%）。最大体力に対して比べる</summary>
        HpPercent = 6,
        /// <summary>何ターン目か</summary>
        TurnNumber = 7,
        /// <summary>得たパラメータの、クリア目標に対する割合（%）</summary>
        ClearTargetPercent = 8,
    }

    public enum Comparator
    {
        GreaterOrEqual = 0,
        LessOrEqual = 1,
        Equal = 2,
    }

    /// <summary>効果の種類（lesson-card.md 7章）。引数の意味は lesson-domain-types.md 2.1。</summary>
    public enum EffectOp
    {
        Param = 0,
        ParamByRatio = 1,
        ParamBonus = 2,
        AddGenki = 3,
        AddFixedGenki = 4,
        RecoverHp = 5,
        ScaleResource = 6,
        SetResource = 7,
        AddTurns = 8,
        AddStack = 9,
        AddPercent = 10,
        Draw = 11,
        AddUses = 12,
        ReplaceHand = 13,
        EnhanceHand = 14,
        CreateCard = 15,
        Schedule = 16,
        Persistent = 17,
    }

    /// <summary>ParamByRatio が参照する値。</summary>
    public enum RatioSource
    {
        Impression = 0,
        Genki = 1,
        Motivation = 2,
    }

    /// <summary>ScaleResource / SetResource の対象。</summary>
    public enum ResourceKind
    {
        Genki = 0,
    }

    /// <summary>ステータス（lesson-card.md 8章）。元気と体力は LessonState に持つ。</summary>
    public enum StatusKind
    {
        /// <summary>好調（ターン）</summary>
        GoodCondition = 0,
        /// <summary>絶好調（ターン）</summary>
        Excellent = 1,
        /// <summary>集中（値）</summary>
        Focus = 2,
        /// <summary>集中強化（%）</summary>
        FocusBoost = 3,
        /// <summary>好印象（値）。ターン開始時に1減る</summary>
        Impression = 4,
        /// <summary>好印象強化（%）</summary>
        ImpressionBoost = 5,
        /// <summary>やる気（値）</summary>
        Motivation = 6,
        /// <summary>消費体力減少（ターン）</summary>
        CostHalf = 7,
        /// <summary>消費体力増加（ターン）</summary>
        CostDouble = 8,
        /// <summary>消費体力削減（値）</summary>
        CostReduce = 9,
        /// <summary>元気増加無効（ターン）</summary>
        GenkiBlock = 10,
        /// <summary>低下状態無効（回数）</summary>
        DebuffGuard = 11,
    }

    /// <summary>持続効果が反応するタイミング（lesson-card.md 10章）。</summary>
    public enum PersistentTrigger
    {
        LessonStart = 0,
        TurnStart = 1,
        AfterTurnStart = 2,
        OnCostHpDecrease = 3,
        OnCardUse = 4,
        OnActiveCardUse = 5,
        OnMentalCardUse = 6,
        AfterCardUse = 7,
        AfterActiveCardUse = 8,
        AfterMentalCardUse = 9,
        OnSkip = 10,
        TurnEnd = 11,
    }

    /// <summary>レッスンの種類。</summary>
    public enum LessonKind
    {
        Normal = 0,
        Special = 1,
        Finishing = 2,
        MidExam = 3,
        FinalExam = 4,
    }

    /// <summary>処理の局面（lesson-card.md 10章・11章）。</summary>
    public enum LessonPhase
    {
        NotStarted = 0,
        LessonStart = 1,
        TurnStart = 2,
        Player = 3,
        TurnEnd = 4,
        Finished = 5,
    }
}
