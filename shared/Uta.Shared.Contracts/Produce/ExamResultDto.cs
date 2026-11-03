using MessagePack;
using Uta.Shared.Domain.Produce;

namespace Uta.Shared.Contracts.Produce
{
    [MessagePackObject]
    public sealed class ExamResultDto
    {
        [SerializationConstructor]
        public ExamResultDto(ExamKind kind, int attempt, int score, int rank, int[] npcScores, bool passed)
        {
            Kind = kind;
            Attempt = attempt;
            Score = score;
            Rank = rank;
            NpcScores = npcScores;
            Passed = passed;
        }

        [Key(0)]
        public ExamKind Kind { get; }

        [Key(1)]
        public int Attempt { get; }

        [Key(2)]
        public int Score { get; }

        [Key(3)]
        public int Rank { get; }

        [Key(4)]
        public int[] NpcScores { get; }

        [Key(5)]
        public bool Passed { get; }
    }
}
