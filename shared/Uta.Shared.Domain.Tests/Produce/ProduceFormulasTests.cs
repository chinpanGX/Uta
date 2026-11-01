using System;
using System.Linq;
using Uta.Shared.Domain.Lesson;
using Uta.Shared.Domain.Produce;
using Xunit;

namespace Uta.Shared.Domain.Tests.Produce
{
    public class ProduceFormulasTests
    {
        private static readonly ProduceCatalog Catalog = TestProduceMaster.Create();

        [Theory]
        [InlineData(990, 20, 1000, 1000)]
        [InlineData(10, 20, 1000, 30)]
        [InlineData(5, 0, 1000, 5)]
        public void AddParam_Values_ReturnsExpected(int current, int amount, int cap, int expected)
        {
            Assert.Equal(expected, ProduceFormulas.AddParam(current, amount, cap));
        }

        [Fact]
        public void AddParam_NegativeAmount_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ProduceFormulas.AddParam(5, -1, 1000));
        }

        [Theory]
        [InlineData(25, 10, 30, 30)]
        [InlineData(5, 10, 30, 15)]
        public void Heal_Values_ReturnsExpected(int hp, int amount, int maxHp, int expected)
        {
            Assert.Equal(expected, ProduceFormulas.Heal(hp, amount, maxHp));
        }

        [Theory]
        [InlineData(30, 21)]
        [InlineData(35, 24)]
        [InlineData(1, 0)]
        public void RestAmount_Values_ReturnsExpected(int maxHp, int expected)
        {
            Assert.Equal(expected, ProduceFormulas.RestAmount(maxHp));
        }

        [Theory]
        [InlineData(100, 85, 8)]
        [InlineData(7, 50, 0)]
        [InlineData(40, 70, 2)]
        [InlineData(0, 100, 0)]
        public void BonusAmount_Values_ReturnsExpected(int amount, int bonus, int expected)
        {
            Assert.Equal(expected, ProduceFormulas.BonusAmount(amount, bonus));
        }

        [Theory]
        [InlineData(1, 3, 1, 40, 1)]
        [InlineData(1, 3, 10, 40, 1)]
        [InlineData(1, 3, 21, 40, 2)]
        [InlineData(1, 3, 40, 40, 3)]
        [InlineData(15, 65, 30, 60, 39)]
        [InlineData(15, 65, 60, 60, 65)]
        public void AbilityValue_Values_ReturnsExpected(int lv1, int max, int level, int maxLevel, int expected)
        {
            Assert.Equal(expected, ProduceFormulas.AbilityValue(lv1, max, level, maxLevel));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(41)]
        public void AbilityValue_LevelOutOfRange_Throws(int level)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ProduceFormulas.AbilityValue(1, 3, level, 40));
        }

        [Fact]
        public void DistributeGain_Normal_AllToTarget()
        {
            Assert.Equal(new[] { 0, 40, 0 }, ProduceFormulas.DistributeGain(false, ParamKind.Rhythm, 40, 25, ParamKind.Singing));
        }

        [Fact]
        public void DistributeGain_Finishing_SplitsExcess()
        {
            Assert.Equal(new[] { 83, 9, 8 }, ProduceFormulas.DistributeGain(true, ParamKind.Singing, 100, 75, ParamKind.Rhythm));
            Assert.Equal(new[] { 50, 0, 0 }, ProduceFormulas.DistributeGain(true, ParamKind.Singing, 50, 75, ParamKind.Rhythm));
            Assert.Equal(new[] { 76, 1, 1 }, ProduceFormulas.DistributeGain(true, ParamKind.Singing, 78, 75, ParamKind.Rhythm));
            Assert.Equal(new[] { 75, 0, 2 }, ProduceFormulas.DistributeGain(true, ParamKind.Singing, 77, 75, ParamKind.Expression));
        }

        [Theory]
        [InlineData(6, 4, 4)]
        [InlineData(6, 6, 0)]
        [InlineData(6, 7, 0)]
        public void PerfectRecover_Values_ReturnsExpected(int maxTurns, int lastTurn, int expected)
        {
            Assert.Equal(expected, ProduceFormulas.PerfectRecover(maxTurns, lastTurn));
        }

        [Theory]
        [InlineData(300, 1, 257)]
        [InlineData(600, 2, 285)]
        [InlineData(1000, 3, 345)]
        [InlineData(1500, 1, 527)]
        [InlineData(0, 3, 120)]
        public void ScoreBonusPercent_Values_ReturnsExpected(int paramValue, int rank, int expected)
        {
            Assert.Equal(expected, ProduceFormulas.ScoreBonusPercent(Catalog, paramValue, rank));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(4)]
        public void ScoreBonusPercent_RankOutOfRange_Throws(int rank)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ProduceFormulas.ScoreBonusPercent(Catalog, 100, rank));
        }

        [Fact]
        public void NpcScores_Values_ReturnsExpected()
        {
            Assert.Equal(new[] { 1800, 1462, 1125, 787, 450 }, ProduceFormulas.NpcScores(2250).ToArray());
            Assert.Equal(new[] { 720, 585, 450, 315, 180 }, ProduceFormulas.NpcScores(900).ToArray());
            Assert.Equal(new[] { 0, 0, 0, 0, 0 }, ProduceFormulas.NpcScores(1).ToArray());
        }

        [Theory]
        [InlineData(1800, 1)]
        [InlineData(1799, 2)]
        [InlineData(1500, 2)]
        [InlineData(0, 6)]
        [InlineData(450, 5)]
        [InlineData(451, 5)]
        [InlineData(449, 6)]
        public void ExamRank_Values_ReturnsExpected(int score, int expected)
        {
            var npc = ProduceFormulas.NpcScores(2250);
            Assert.Equal(expected, ProduceFormulas.ExamRank(score, npc));
        }

        [Fact]
        public void Evaluate_Cases_ReturnsExpected()
        {
            var a = ProduceFormulas.Evaluate(Catalog, 150, 1, 0);
            Assert.Equal(345, a.ParamPart);
            Assert.Equal(1700, a.RankPart);
            Assert.Equal(0, a.ScorePart);
            Assert.Equal(2045, a.Value);
            Assert.Equal(ProduceRank.D, a.Rank);

            var b = ProduceFormulas.Evaluate(Catalog, 1000, 2, 5000);
            Assert.Equal(4700, b.Value);
            Assert.Equal(ProduceRank.CPlus, b.Rank);

            Assert.Equal(1500, ProduceFormulas.Evaluate(Catalog, 0, 0, 5001).ScorePart);
            Assert.Equal(3650, ProduceFormulas.Evaluate(Catalog, 0, 0, 40001).ScorePart);
            Assert.Equal(5250, ProduceFormulas.Evaluate(Catalog, 0, 0, 300000).ScorePart);

            var c = ProduceFormulas.Evaluate(Catalog, 60, 0, 0);
            Assert.Equal(138, c.Value);
            Assert.Equal(ProduceRank.F, c.Rank);

            var d = ProduceFormulas.Evaluate(Catalog, 435, 0, 0);
            Assert.Equal(1000, d.Value);
            Assert.Equal(ProduceRank.E, d.Rank);

            var e = ProduceFormulas.Evaluate(Catalog, 0, 0, 0);
            Assert.Equal(0, e.Value);
            Assert.Equal(ProduceRank.F, e.Rank);

            Assert.Equal(0, ProduceFormulas.Evaluate(Catalog, 0, 4, 0).RankPart);
        }
    }
}
