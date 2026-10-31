namespace Uta.Shared.Domain.Lesson
{
    public enum LessonCommandKind
    {
        UseCard = 0,
        UseDrink = 1,
        Skip = 2,
    }

    /// <summary>レッスン・試験の中のプレイヤー操作（produce.md 10.4）。</summary>
    public readonly struct LessonCommand
    {
        private LessonCommand(LessonCommandKind kind, int arg)
        {
            Kind = kind;
            Arg = arg;
        }

        public LessonCommandKind Kind { get; }

        /// <summary>UseCard はカードのインスタンス ID、UseDrink はドリンクの枠番号</summary>
        public int Arg { get; }

        public static LessonCommand UseCard(int instanceId)
        {
            return new LessonCommand(LessonCommandKind.UseCard, instanceId);
        }

        public static LessonCommand UseDrink(int drinkSlot)
        {
            return new LessonCommand(LessonCommandKind.UseDrink, drinkSlot);
        }

        public static LessonCommand Skip()
        {
            return new LessonCommand(LessonCommandKind.Skip, 0);
        }
    }

    /// <summary>カードを使えるか（P1・P3 の判定）。乱数を使わないので、クライアントの予測にも使える。</summary>
    public enum UseCheckResult
    {
        Ok = 0,
        NotPlayerPhase = 1,
        NotInHand = 2,
        NoUsesLeft = 3,
        UseConditionNotMet = 4,
        CannotPayCost = 5,
    }

    /// <summary>操作を受け付けたか。Accepted 以外は状態を変えていない。</summary>
    public enum LessonApplyResult
    {
        Accepted = 0,
        NotPlayerPhase = 1,
        NotInHand = 2,
        NoUsesLeft = 3,
        UseConditionNotMet = 4,
        CannotPayCost = 5,
        NotSupported = 6,
    }
}
