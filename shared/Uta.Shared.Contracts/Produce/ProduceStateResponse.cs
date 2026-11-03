using MessagePack;
using Uta.Shared.Domain.Produce;

namespace Uta.Shared.Contracts.Produce
{
    [MessagePackObject]
    public sealed class ProduceStateResponse
    {
        [SerializationConstructor]
        public ProduceStateResponse(int operationNumber, ProducePhase phase, int scenarioId, int idolId, int week, int singing, int rhythm, int expression, int hp, int maxHp, int examRetryLeft, DeckCardDto[] deck, WeekActionDto[] weekActions, ClassOptionDto[] classOptions, int[] cardOffers, LessonDto lesson, ExamResultDto examResult, ProduceResultResponse result)
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
            Deck = deck;
            WeekActions = weekActions;
            ClassOptions = classOptions;
            CardOffers = cardOffers;
            Lesson = lesson;
            ExamResult = examResult;
            Result = result;
        }

        [Key(0)]
        public int OperationNumber { get; }

        [Key(1)]
        public ProducePhase Phase { get; }

        [Key(2)]
        public int ScenarioId { get; }

        [Key(3)]
        public int IdolId { get; }

        [Key(4)]
        public int Week { get; }

        [Key(5)]
        public int Singing { get; }

        [Key(6)]
        public int Rhythm { get; }

        [Key(7)]
        public int Expression { get; }

        [Key(8)]
        public int Hp { get; }

        [Key(9)]
        public int MaxHp { get; }

        [Key(10)]
        public int ExamRetryLeft { get; }

        [Key(11)]
        public DeckCardDto[] Deck { get; }

        [Key(12)]
        public WeekActionDto[] WeekActions { get; }

        [Key(13)]
        public ClassOptionDto[] ClassOptions { get; }

        [Key(14)]
        public int[] CardOffers { get; }

        [Key(15)]
        public LessonDto Lesson { get; }

        [Key(16)]
        public ExamResultDto ExamResult { get; }

        [Key(17)]
        public ProduceResultResponse Result { get; }
    }
}
