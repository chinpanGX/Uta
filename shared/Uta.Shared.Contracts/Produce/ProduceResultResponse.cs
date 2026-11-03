using MessagePack;
using Uta.Shared.Domain.Produce;

namespace Uta.Shared.Contracts.Produce
{
    [MessagePackObject]
    public sealed class ProduceResultResponse
    {
        [SerializationConstructor]
        public ProduceResultResponse(ProduceEnding ending, ExamOutcome examOutcome, int evaluationValue, ProduceRank rank, int finalScore, int finalRank, int gachaCurrency, int gold, int supportPoints, int exp, int playerLevel, int playerExp)
        {
            Ending = ending;
            ExamOutcome = examOutcome;
            EvaluationValue = evaluationValue;
            Rank = rank;
            FinalScore = finalScore;
            FinalRank = finalRank;
            GachaCurrency = gachaCurrency;
            Gold = gold;
            SupportPoints = supportPoints;
            Exp = exp;
            PlayerLevel = playerLevel;
            PlayerExp = playerExp;
        }

        [Key(0)]
        public ProduceEnding Ending { get; }

        [Key(1)]
        public ExamOutcome ExamOutcome { get; }

        [Key(2)]
        public int EvaluationValue { get; }

        [Key(3)]
        public ProduceRank Rank { get; }

        [Key(4)]
        public int FinalScore { get; }

        [Key(5)]
        public int FinalRank { get; }

        [Key(6)]
        public int GachaCurrency { get; }

        [Key(7)]
        public int Gold { get; }

        [Key(8)]
        public int SupportPoints { get; }

        [Key(9)]
        public int Exp { get; }

        [Key(10)]
        public int PlayerLevel { get; }

        [Key(11)]
        public int PlayerExp { get; }
    }
}
