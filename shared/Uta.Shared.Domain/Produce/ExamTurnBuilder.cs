using System.Collections.Generic;
using Uta.Shared.Domain.Lesson;
using Uta.Shared.Domain.Rng;

namespace Uta.Shared.Domain.Produce
{
    /// <summary>審査ターンの列を作る（produce.md 5.1）。</summary>
    public static class ExamTurnBuilder
    {
        /// <summary>paramValues は試験の開始時のパラメータ（添字は ProduceParams.ToIndex）。</summary>
        public static IReadOnlyList<ExamTurn> Build(ProduceCatalog catalog, IdolDefinition idol, ExamKind kind, IReadOnlyList<int> paramValues, IRandom random)
        {
            var pattern = catalog.GetExamPattern(idol.ExamPatternId);
            var counts = pattern.GetTurnCounts(kind, idol.Plan);

            // 率が 0 でも必ず 1 回引く
            var headRank = RandomOps.Chance(random, pattern.FirstTurnSecondRatePerTenThousand) ? 2 : 1;

            var r1 = counts[0] - 1 - (headRank == 1 ? 1 : 0);
            var r2 = counts[1] - 1 - (headRank == 2 ? 1 : 0);
            var r3 = counts[2] - 1;

            var middle = new List<int>();
            for (var i = 0; i < r1; i++)
            {
                middle.Add(1);
            }

            for (var i = 0; i < r2; i++)
            {
                middle.Add(2);
            }

            for (var i = 0; i < r3; i++)
            {
                middle.Add(3);
            }

            RandomOps.Shuffle(random, middle);

            var ranks = new List<int>();
            ranks.Add(headRank);
            ranks.AddRange(middle);
            ranks.Add(3);
            ranks.Add(2);
            ranks.Add(1);

            var turns = new ExamTurn[ranks.Count];
            for (var i = 0; i < turns.Length; i++)
            {
                var k = ranks[i];
                var param = idol.GetExamParam(k);
                var value = paramValues[ProduceParams.ToIndex(param)];
                turns[i] = new ExamTurn(param, ProduceFormulas.ScoreBonusPercent(catalog, value, k));
            }

            return turns;
        }
    }
}
