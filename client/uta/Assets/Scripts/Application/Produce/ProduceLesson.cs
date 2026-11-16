using System;
using System.Collections.Generic;
using Uta.Shared.Domain.Lesson;

namespace Uta.Client.Application.Produce
{
    public sealed class ProduceLesson
    {
        public ProduceLesson(LessonKind kind, ParamKind targetParam, int maxTurns, int clearTarget, int perfectTarget,
            IReadOnlyList<ProduceExamTurn> examTurns, int turn, int shield, int usesLeft, int paramGained,
            IReadOnlyList<int> hand, IReadOnlyList<bool> usableHand, IReadOnlyList<int> drawPile, IReadOnlyList<int> discard, IReadOnlyList<int> exiled,
            IReadOnlyList<int> statusValues, IReadOnlyList<ProduceParamBonus> paramBonuses, IReadOnlyList<ProducePersistent> persistents,
            IReadOnlyList<int> lessonEnhanced, IReadOnlyList<int> npcScores)
        {
            Kind = kind;
            TargetParam = targetParam;
            MaxTurns = maxTurns;
            ClearTarget = clearTarget;
            PerfectTarget = perfectTarget;
            ExamTurns = examTurns ?? throw new ArgumentNullException(nameof(examTurns));
            Turn = turn;
            Shield = shield;
            UsesLeft = usesLeft;
            ParamGained = paramGained;
            Hand = hand ?? throw new ArgumentNullException(nameof(hand));
            UsableHand = usableHand ?? throw new ArgumentNullException(nameof(usableHand));
            DrawPile = drawPile ?? throw new ArgumentNullException(nameof(drawPile));
            Discard = discard ?? throw new ArgumentNullException(nameof(discard));
            Exiled = exiled ?? throw new ArgumentNullException(nameof(exiled));
            StatusValues = statusValues ?? throw new ArgumentNullException(nameof(statusValues));
            ParamBonuses = paramBonuses ?? throw new ArgumentNullException(nameof(paramBonuses));
            Persistents = persistents ?? throw new ArgumentNullException(nameof(persistents));
            LessonEnhanced = lessonEnhanced ?? throw new ArgumentNullException(nameof(lessonEnhanced));
            NpcScores = npcScores ?? throw new ArgumentNullException(nameof(npcScores));
        }

        public LessonKind Kind { get; }
        public ParamKind TargetParam { get; }
        public int MaxTurns { get; }
        public int ClearTarget { get; }
        public int PerfectTarget { get; }
        public IReadOnlyList<ProduceExamTurn> ExamTurns { get; }
        public int Turn { get; }
        public int Shield { get; }
        public int UsesLeft { get; }
        public int ParamGained { get; }
        public IReadOnlyList<int> Hand { get; }

        /// <summary>Hand と同じ長さか、空（古いサーバーの応答）。</summary>
        public IReadOnlyList<bool> UsableHand { get; }
        public IReadOnlyList<int> DrawPile { get; }
        public IReadOnlyList<int> Discard { get; }
        public IReadOnlyList<int> Exiled { get; }
        public IReadOnlyList<int> StatusValues { get; }
        public IReadOnlyList<ProduceParamBonus> ParamBonuses { get; }
        public IReadOnlyList<ProducePersistent> Persistents { get; }
        public IReadOnlyList<int> LessonEnhanced { get; }
        public IReadOnlyList<int> NpcScores { get; }

        /// <summary>Kind が MidExam か FinalExam のとき true。</summary>
        public bool IsExam
        {
            get { return Kind == LessonKind.MidExam || Kind == LessonKind.FinalExam; }
        }
    }
}
