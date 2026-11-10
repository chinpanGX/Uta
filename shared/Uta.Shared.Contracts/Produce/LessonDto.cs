using MessagePack;
using Uta.Shared.Domain.Lesson;

namespace Uta.Shared.Contracts.Produce
{
    [MessagePackObject]
    public sealed class LessonDto
    {
        [SerializationConstructor]
        public LessonDto(LessonKind kind, ParamKind targetParam, int maxTurns, int clearTarget, int perfectTarget, ExamTurnDto[] examTurns, int turn, int shield, int usesLeft, int paramGained, int[] hand, int[] drawPile, int[] discard, int[] exiled, int[] statusValues, ParamBonusDto[] paramBonuses, PersistentDto[] persistents, int[] lessonEnhanced, int[] npcScores, bool[] usableHand)
        {
            Kind = kind;
            TargetParam = targetParam;
            MaxTurns = maxTurns;
            ClearTarget = clearTarget;
            PerfectTarget = perfectTarget;
            ExamTurns = examTurns;
            Turn = turn;
            Shield = shield;
            UsesLeft = usesLeft;
            ParamGained = paramGained;
            Hand = hand;
            DrawPile = drawPile;
            Discard = discard;
            Exiled = exiled;
            StatusValues = statusValues;
            ParamBonuses = paramBonuses;
            Persistents = persistents;
            LessonEnhanced = lessonEnhanced;
            NpcScores = npcScores;
            UsableHand = usableHand;
        }

        [Key(0)]
        public LessonKind Kind { get; }

        [Key(1)]
        public ParamKind TargetParam { get; }

        [Key(2)]
        public int MaxTurns { get; }

        [Key(3)]
        public int ClearTarget { get; }

        [Key(4)]
        public int PerfectTarget { get; }

        [Key(5)]
        public ExamTurnDto[] ExamTurns { get; }

        [Key(6)]
        public int Turn { get; }

        [Key(7)]
        public int Shield { get; }

        [Key(8)]
        public int UsesLeft { get; }

        [Key(9)]
        public int ParamGained { get; }

        [Key(10)]
        public int[] Hand { get; }

        [Key(11)]
        public int[] DrawPile { get; }

        [Key(12)]
        public int[] Discard { get; }

        [Key(13)]
        public int[] Exiled { get; }

        [Key(14)]
        public int[] StatusValues { get; }

        [Key(15)]
        public ParamBonusDto[] ParamBonuses { get; }

        [Key(16)]
        public PersistentDto[] Persistents { get; }

        [Key(17)]
        public int[] LessonEnhanced { get; }

        [Key(18)]
        public int[] NpcScores { get; }

        [Key(19)]
        public bool[] UsableHand { get; }
    }
}
