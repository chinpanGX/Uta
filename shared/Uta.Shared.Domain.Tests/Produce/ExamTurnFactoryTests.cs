using System.Collections.Generic;
using System.Linq;
using Uta.Shared.Domain.Lesson;
using Uta.Shared.Domain.Produce;
using Uta.Shared.Domain.Rng;
using Xunit;

namespace Uta.Shared.Domain.Tests.Produce
{
    public class ExamTurnFactoryTests
    {
        private static readonly ProduceCatalog Catalog = TestProduceMaster.Create();
        private static readonly int[] ParamValues = { 300, 200, 100 };

        private static IRandom Random(ulong seed)
        {
            return RandomStreams.Create(seed, new RandomStreamKey(RandomStreamKind.Lesson, 1, 0));
        }

        private static int Count(IReadOnlyList<ExamTurn> turns, ParamKind kind)
        {
            return turns.Count(t => t.Param == kind);
        }

        [Fact]
        public void Create_Mid_TotalCountsMatchPattern()
        {
            var idol = Catalog.GetIdol(1);
            for (ulong seed = 1; seed <= 20; seed++)
            {
                var turns = ExamTurnFactory.Create(Catalog, idol, ExamKind.Mid, ParamValues, Random(seed));
                Assert.Equal(9, turns.Count);
                Assert.Equal(4, Count(turns, ParamKind.Singing));
                Assert.Equal(3, Count(turns, ParamKind.Rhythm));
                Assert.Equal(2, Count(turns, ParamKind.Expression));
            }
        }

        [Fact]
        public void Create_Final_Sense_Has12Turns()
        {
            var idol = Catalog.GetIdol(1);
            for (ulong seed = 1; seed <= 20; seed++)
            {
                var turns = ExamTurnFactory.Create(Catalog, idol, ExamKind.Final, ParamValues, Random(seed));
                Assert.Equal(12, turns.Count);
                Assert.Equal(5, Count(turns, ParamKind.Singing));
                Assert.Equal(4, Count(turns, ParamKind.Rhythm));
                Assert.Equal(3, Count(turns, ParamKind.Expression));
            }
        }

        [Fact]
        public void Create_Final_Logic_Counts()
        {
            var idol = Catalog.GetIdol(2);
            for (ulong seed = 1; seed <= 20; seed++)
            {
                var turns = ExamTurnFactory.Create(Catalog, idol, ExamKind.Final, ParamValues, Random(seed));
                Assert.Equal(11, turns.Count);
                Assert.Equal(5, Count(turns, ParamKind.Rhythm));
                Assert.Equal(4, Count(turns, ParamKind.Expression));
                Assert.Equal(2, Count(turns, ParamKind.Singing));
            }
        }

        [Fact]
        public void Create_LastThreeTurns_AreThirdSecondFirst()
        {
            var idol = Catalog.GetIdol(1);
            for (ulong seed = 1; seed <= 20; seed++)
            {
                var turns = ExamTurnFactory.Create(Catalog, idol, ExamKind.Final, ParamValues, Random(seed));
                var n = turns.Count;
                Assert.Equal(ParamKind.Expression, turns[n - 3].Param);
                Assert.Equal(ParamKind.Rhythm, turns[n - 2].Param);
                Assert.Equal(ParamKind.Singing, turns[n - 1].Param);
            }
        }

        [Fact]
        public void Create_FirstTurn_FollowsFirstTurnDraw()
        {
            var idol = Catalog.GetIdol(1);

            var second = new ScriptedRandom(new ulong[] { 10000 }, Random(1));
            var turnsSecond = ExamTurnFactory.Create(Catalog, idol, ExamKind.Mid, ParamValues, second);
            Assert.Equal(ParamKind.Rhythm, turnsSecond[0].Param);
            Assert.Equal(9, turnsSecond.Count);
            Assert.Equal(4, Count(turnsSecond, ParamKind.Singing));
            Assert.Equal(3, Count(turnsSecond, ParamKind.Rhythm));
            Assert.Equal(2, Count(turnsSecond, ParamKind.Expression));

            var first = new ScriptedRandom(new ulong[] { 5000 }, Random(1));
            var turnsFirst = ExamTurnFactory.Create(Catalog, idol, ExamKind.Mid, ParamValues, first);
            Assert.Equal(ParamKind.Singing, turnsFirst[0].Param);
            Assert.Equal(9, turnsFirst.Count);
            Assert.Equal(4, Count(turnsFirst, ParamKind.Singing));
            Assert.Equal(3, Count(turnsFirst, ParamKind.Rhythm));
            Assert.Equal(2, Count(turnsFirst, ParamKind.Expression));
        }

        [Fact]
        public void Create_RateZero_FirstTurnAlwaysFirstParam()
        {
            var idol = Catalog.GetIdol(2);
            for (ulong seed = 1; seed <= 20; seed++)
            {
                var turns = ExamTurnFactory.Create(Catalog, idol, ExamKind.Final, ParamValues, Random(seed));
                Assert.Equal(ParamKind.Rhythm, turns[0].Param);
            }

            var scripted = new ScriptedRandom(new ulong[] { 5000 }, Random(1));
            var scriptedTurns = ExamTurnFactory.Create(Catalog, idol, ExamKind.Final, ParamValues, scripted);
            Assert.Equal(ParamKind.Rhythm, scriptedTurns[0].Param);
            Assert.Equal(1, scripted.Consumed);
        }

        [Fact]
        public void Create_ScoreBonusMatchesFormula()
        {
            var idol = Catalog.GetIdol(1);
            for (ulong seed = 1; seed <= 20; seed++)
            {
                var turns = ExamTurnFactory.Create(Catalog, idol, ExamKind.Final, ParamValues, Random(seed));
                foreach (var turn in turns)
                {
                    var rank = turn.Param == idol.FirstParam ? 1 : turn.Param == idol.SecondParam ? 2 : 3;
                    var expected = ProduceFormulas.ScoreBonusPercent(Catalog, ParamValues[ProduceParams.ToIndex(turn.Param)], rank);
                    Assert.Equal(expected, turn.ScoreBonusPercent);
                }
            }
        }

        [Fact]
        public void Create_SameSeed_SameResult()
        {
            var idol = Catalog.GetIdol(1);
            var a = ExamTurnFactory.Create(Catalog, idol, ExamKind.Final, ParamValues, Random(5));
            var b = ExamTurnFactory.Create(Catalog, idol, ExamKind.Final, ParamValues, Random(5));
            Assert.Equal(a.Select(t => t.Param).ToArray(), b.Select(t => t.Param).ToArray());
            Assert.Equal(a.Select(t => t.ScoreBonusPercent).ToArray(), b.Select(t => t.ScoreBonusPercent).ToArray());
        }
    }
}
