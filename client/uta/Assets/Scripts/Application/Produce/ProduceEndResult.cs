using Uta.Shared.Domain.Produce;

namespace Uta.Client.Application.Produce
{
    public sealed class ProduceEndResult
    {
        public ProduceEndResult(ProduceEndKind ending, ExamResultKind examResultKind, int evaluationValue, ProduceRank rank, int finalScore, int finalRank,
            int gachaCurrency, int gold, int supportPoints, int exp, int playerExp)
        {
            Ending = ending;
            ExamResultKind = examResultKind;
            EvaluationValue = evaluationValue;
            Rank = rank;
            FinalScore = finalScore;
            FinalRank = finalRank;
            GachaCurrency = gachaCurrency;
            Gold = gold;
            SupportPoints = supportPoints;
            Exp = exp;
            PlayerExp = playerExp;
        }

        public ProduceEndKind Ending { get; }
        public ExamResultKind ExamResultKind { get; }
        public int EvaluationValue { get; }
        public ProduceRank Rank { get; }
        public int FinalScore { get; }
        public int FinalRank { get; }
        public int GachaCurrency { get; }
        public int Gold { get; }
        public int SupportPoints { get; }
        public int Exp { get; }

        /// <summary>付与したあとの経験値の累計。</summary>
        public int PlayerExp { get; }
    }
}
