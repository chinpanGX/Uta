using System;
using System.Collections.Generic;
using Uta.Shared.Domain.Lesson;
using Uta.Shared.Domain.Produce;

namespace Uta.Client.Application.Produce
{
    public sealed class ProduceStateResult
    {
        public ProduceStateResult(int operationNumber, ProducePhase phase, int scenarioId, int idolId, int week, int singing, int rhythm, int expression,
            int hp, int maxHp, int examRetryLeft, IReadOnlyList<ProduceDeckCard> deck, IReadOnlyList<ProduceWeekAction> weekActions,
            IReadOnlyList<ProduceClassOption> classOptions, IReadOnlyList<int> cardOffers, ProduceLesson lesson, ProduceExamResult examResult, ProduceEndResult result)
        {
            OperationNumber = operationNumber;
            Phase = phase;
            ScenarioId = scenarioId;
            IdolId = idolId;
            Week = week;
            Singing = singing;
            Rhythm = rhythm;
            Expression = expression;
            Hp = hp;
            MaxHp = maxHp;
            ExamRetryLeft = examRetryLeft;
            Deck = deck ?? throw new ArgumentNullException(nameof(deck));
            WeekActions = weekActions ?? throw new ArgumentNullException(nameof(weekActions));
            ClassOptions = classOptions ?? throw new ArgumentNullException(nameof(classOptions));
            CardOffers = cardOffers ?? throw new ArgumentNullException(nameof(cardOffers));
            Lesson = lesson;
            ExamResult = examResult;
            Result = result;
        }

        public int OperationNumber { get; }
        public ProducePhase Phase { get; }
        public int ScenarioId { get; }
        public int IdolId { get; }
        public int Week { get; }
        public int Singing { get; }
        public int Rhythm { get; }
        public int Expression { get; }
        public int Hp { get; }
        public int MaxHp { get; }
        public int ExamRetryLeft { get; }
        public IReadOnlyList<ProduceDeckCard> Deck { get; }
        public IReadOnlyList<ProduceWeekAction> WeekActions { get; }
        public IReadOnlyList<ProduceClassOption> ClassOptions { get; }
        public IReadOnlyList<int> CardOffers { get; }

        /// <summary>無ければ null。</summary>
        public ProduceLesson Lesson { get; }

        /// <summary>無ければ null。</summary>
        public ProduceExamResult ExamResult { get; }

        /// <summary>無ければ null。</summary>
        public ProduceEndResult Result { get; }

        /// <summary>Singing / Rhythm / Expression の値。それ以外は ArgumentOutOfRangeException。</summary>
        public int GetParam(ParamKind param)
        {
            switch (param)
            {
                case ParamKind.Singing:
                    return Singing;
                case ParamKind.Rhythm:
                    return Rhythm;
                case ParamKind.Expression:
                    return Expression;
                default:
                    throw new ArgumentOutOfRangeException(nameof(param), param, null);
            }
        }
    }
}
