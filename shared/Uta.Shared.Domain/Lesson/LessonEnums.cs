namespace Uta.Shared.Domain.Lesson
{
    // 名前・数値の正は uta_docs/wiki/architecture/lesson-enums.md。マスタ・保存の値に使うので数値は変えない。追加は末尾に足す。

    /// <summary>プラン（lesson-card.md 3.1）。</summary>
    public enum Plan
    {
        Nothing = 0,
        Free = 1,
        Sense = 2,
        Logic = 3,
    }

    /// <summary>カードの種別。</summary>
    public enum CardType
    {
        Nothing = 0,
        Active = 1,
        Mental = 2,
        Trouble = 3,
    }

    public enum Rarity
    {
        Nothing = 0,
        N = 1,
        R = 2,
        SR = 3,
        SSR = 4,
    }

    /// <summary>パラメータの種類（歌唱 / 律動 / 表現）。</summary>
    public enum ParamKind
    {
        Nothing = 0,
        Singing = 1,
        Rhythm = 2,
        Expression = 3,
    }

    /// <summary>コストの種類（lesson-card.md 5章。lesson-enums.md 4章）。</summary>
    public enum CostKind
    {
        Nothing = 0,
        /// <summary>元気→体力の順に払う。補正を受ける</summary>
        Consume = 1,
        /// <summary>体力から直接払う。補正を受けない</summary>
        HpDirect = 2,
        GoodConditionTurns = 3,
        ExcellentTurns = 4,
        Focus = 5,
        Impression = 6,
        Motivation = 7,
    }

    /// <summary>条件の対象（lesson-card.md 6.1。lesson-enums.md 5章）。</summary>
    public enum ConditionSubject
    {
        Nothing = 0,
        GoodConditionTurns = 1,
        ExcellentTurns = 2,
        Focus = 3,
        Impression = 4,
        Motivation = 5,
        Shield = 6,
        /// <summary>体力の割合（%）。最大体力に対して比べる</summary>
        HpPercent = 7,
        /// <summary>何ターン目か</summary>
        TurnNumber = 8,
        /// <summary>得たパラメータの、クリア目標に対する割合（%）</summary>
        ClearTargetPercent = 9,
    }

    /// <summary>比較（lesson-enums.md 5章）。</summary>
    public enum Comparator
    {
        Nothing = 0,
        GreaterOrEqual = 1,
        LessOrEqual = 2,
        Equal = 3,
    }

    /// <summary>効果の種類（lesson-card.md 7章。lesson-enums.md 1章）。列の意味は lesson-domain-types.md 2.1。</summary>
    public enum EffectOp
    {
        Nothing = 0,
        GainParam = 1,
        GainParamByImpression = 2,
        GainParamByShield = 3,
        GainParamByMotivation = 4,
        AddParamBonus = 5,
        AddShield = 6,
        AddFixedShield = 7,
        RecoverHp = 8,
        ScaleShield = 9,
        SetShield = 10,
        AddStatus = 11,
        AddStatusPercent = 12,
        DrawCards = 13,
        AddCardUses = 14,
        ReplaceHand = 15,
        EnhanceHand = 16,
        CreateCard = 17,
        ScheduleEffect = 18,
        AddPersistent = 19,
    }

    /// <summary>ステータス（lesson-card.md 8章。lesson-enums.md 3章）。元気と体力は LessonState に持つ。</summary>
    public enum StatusKind
    {
        Nothing = 0,
        /// <summary>好調（ターン）</summary>
        GoodCondition = 1,
        /// <summary>絶好調（ターン）</summary>
        Excellent = 2,
        /// <summary>集中（値）</summary>
        Focus = 3,
        /// <summary>集中強化（%）</summary>
        FocusBoost = 4,
        /// <summary>好印象（値）。ターン開始時に1減る</summary>
        Impression = 5,
        /// <summary>好印象強化（%）</summary>
        ImpressionBoost = 6,
        /// <summary>やる気（値）</summary>
        Motivation = 7,
        /// <summary>消費体力減少（ターン）</summary>
        CostHalf = 8,
        /// <summary>消費体力増加（ターン）</summary>
        CostDouble = 9,
        /// <summary>消費体力削減（値）</summary>
        CostReduce = 10,
        /// <summary>元気増加無効（ターン）</summary>
        ShieldGainBlock = 11,
        /// <summary>低下状態無効（回数）</summary>
        DebuffGuard = 12,
    }

    /// <summary>持続効果が反応するタイミング（lesson-card.md 10章。lesson-enums.md 2章）。</summary>
    public enum PersistentTrigger
    {
        Nothing = 0,
        LessonStart = 1,
        TurnStart = 2,
        AfterTurnStart = 3,
        EveryNTurns = 4,
        OnCostHpDecrease = 5,
        OnCardUse = 6,
        OnActiveCardUse = 7,
        OnMentalCardUse = 8,
        AfterCardUse = 9,
        AfterActiveCardUse = 10,
        AfterMentalCardUse = 11,
        OnSkip = 12,
        TurnEnd = 13,
    }

    /// <summary>レッスンの種類。</summary>
    public enum LessonKind
    {
        Nothing = 0,
        Normal = 1,
        Special = 2,
        Finishing = 3,
        MidExam = 4,
        FinalExam = 5,
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

    /// <summary>カードの面（通常版 / 強化版）。マスタの cardFace・cardCost の Face 列と同じ名前・数値。</summary>
    public enum CardFaceKind
    {
        Nothing = 0,
        Normal = 1,
        Enhanced = 2,
    }
}
