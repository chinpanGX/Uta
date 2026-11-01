namespace Uta.Shared.Domain.Produce
{
    // masterdata/Definitions/Produce.cs・IdolSupport.cs の enum と、名前・数値を同じにする。

    /// <summary>シナリオの難易度。</summary>
    public enum Difficulty
    {
        Nothing = 0,
        Beginner = 1,
        Intermediate = 2,
        Advanced = 3,
    }

    /// <summary>週の種類。</summary>
    public enum WeekKind
    {
        Nothing = 0,
        Action = 1,
        Finishing = 2,
        MidExam = 3,
        FinalExam = 4,
    }

    /// <summary>週の行動の種類。</summary>
    public enum ActionKind
    {
        Nothing = 0,
        Lesson = 1,
        Class = 2,
        Supply = 3,
        Outing = 4,
        Consult = 5,
    }

    /// <summary>試験の種類。</summary>
    public enum ExamKind
    {
        Nothing = 0,
        Mid = 1,
        Final = 2,
    }

    /// <summary>評価ランク。</summary>
    public enum ProduceRank
    {
        Nothing = 0,
        F = 1,
        E = 2,
        D = 3,
        C = 4,
        CPlus = 5,
        B = 6,
        BPlus = 7,
        A = 8,
        APlus = 9,
        S = 10,
        SPlus = 11,
        SS = 12,
        SSPlus = 13,
    }

    /// <summary>おすすめの方向性。</summary>
    public enum RecommendKind
    {
        Nothing = 0,
        GoodCondition = 1,
        Focus = 2,
        Impression = 3,
        Motivation = 4,
    }

    /// <summary>サポートカードのタイプ。</summary>
    public enum SupportType
    {
        Nothing = 0,
        Singing = 1,
        Rhythm = 2,
        Expression = 3,
        Assist = 4,
    }

    /// <summary>サポートカードの特技の種類。</summary>
    public enum SupportAbilityKind
    {
        Nothing = 0,
        InitialParam = 1,
        ParamBonus = 2,
        MaxHp = 3,
    }

    /// <summary>育成の局面（produce.md 10章）。</summary>
    public enum ProducePhase
    {
        Nothing = 0,
        WeekSelect = 1,
        CardOffer = 2,
        Lesson = 3,
        Exam = 4,
        ExamResult = 5,
        Finished = 6,
    }

    /// <summary>操作の種類（produce-domain.md 5章の表のとおり。値を保存に使うので変えない）。</summary>
    public enum OperationKind
    {
        Nothing = 0,
        ChooseWeekAction = 1,
        ConsultExchange = 2,
        ConsultEnhance = 3,
        ConsultRemove = 4,
        ConsultFinish = 5,
        OfferPick = 6,
        OfferReroll = 7,
        OfferRemove = 8,
        OfferDecline = 9,
        DiscardDrink = 10,
        ExamRetry = 11,
        ExamNoRetry = 12,
        UseCard = 13,
        UseDrink = 14,
        Skip = 15,
    }

    /// <summary>操作を受け付けたかどうか。</summary>
    public enum ProduceApplyResult
    {
        Accepted = 0,
        UnexpectedPhase = 1,
        InvalidArgument = 2,
    }

    /// <summary>育成の結果の種類（produce_results の「試験の結果」）。</summary>
    public enum ExamOutcome
    {
        Nothing = 0,
        MidFailed = 1,
        FinalFailed = 2,
        FinalPassed = 3,
    }

    /// <summary>編成の検証の結果。</summary>
    public enum FormationCheck
    {
        Ok = 0,
        DuplicateCardId = 1,
        PlanMismatch = 2,
    }

    /// <summary>週の選択で選べるものの種類。</summary>
    public enum WeekChoiceKind
    {
        Nothing = 0,
        Lesson = 1,
        Class = 2,
        Supply = 3,
        Rest = 4,
    }
}
