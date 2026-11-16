using System;
using System.Collections.Generic;
using Uta.Shared.Domain.Produce;

namespace Uta.Client.Application.Produce
{
    public sealed class ProduceExamResult
    {
        public ProduceExamResult(ExamKind kind, int attempt, int score, int rank, IReadOnlyList<int> npcScores, bool passed)
        {
            Kind = kind;
            Attempt = attempt;
            Score = score;
            Rank = rank;
            NpcScores = npcScores ?? throw new ArgumentNullException(nameof(npcScores));
            Passed = passed;
        }

        public ExamKind Kind { get; }
        public int Attempt { get; }
        public int Score { get; }
        public int Rank { get; }
        public IReadOnlyList<int> NpcScores { get; }
        public bool Passed { get; }
    }
}
