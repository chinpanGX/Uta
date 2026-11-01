namespace Uta.Shared.Domain.Lesson
{
    /// <summary>レッスン・試験の終わり方。</summary>
    public enum LessonEndReason
    {
        Nothing = 0,
        TurnsOver = 1,
        Perfect = 2,
        EarlyEnd = 3,
    }

    /// <summary>レッスン・試験の結果。</summary>
    public readonly struct LessonResult
    {
        public LessonResult(int gained, int lastTurn, LessonEndReason reason, int hp)
        {
            Gained = gained;
            LastTurn = lastTurn;
            Reason = reason;
            Hp = hp;
        }

        /// <summary>得たパラメータ。試験ならスコア</summary>
        public int Gained { get; }
        public int LastTurn { get; }
        public LessonEndReason Reason { get; }
        public int Hp { get; }
    }
}
