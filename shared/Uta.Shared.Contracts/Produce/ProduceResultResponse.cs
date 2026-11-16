using MessagePack;
using Uta.Shared.Domain.Produce;

namespace Uta.Shared.Contracts.Produce
{
    [MessagePackObject]
    public sealed class ProduceResultResponse
    {
        [SerializationConstructor]
        public ProduceResultResponse(ProduceEnding ending, ExamResultKind examResultKind, int evaluationValue, ProduceRank rank, int finalScore, int finalRank, int gachaCurrency, int gold, int supportPoints, int exp, int playerExp)
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

        [Key(0)]
        public ProduceEnding Ending { get; }

        [Key(1)]
        public ExamResultKind ExamResultKind { get; }

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

        /// <summary>付与したあとのプレイヤーの経験値の累計（放棄・破棄は今の累計）。</summary>
        [Key(10)]
        public int PlayerExp { get; }
    }
}
