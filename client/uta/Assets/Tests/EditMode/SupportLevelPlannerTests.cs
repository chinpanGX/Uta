using NUnit.Framework;
using Uta.Client.Application.Support;
using Uta.Client.Domain.SupportCard;

namespace Uta.Client.Tests
{
    public sealed class SupportLevelPlannerTests
    {
        private SupportLevelPlanner planner;

        [SetUp]
        public void SetUp()
        {
            planner = new SupportLevelPlanner(OwnedFixtures.CreateMaster());
        }

        private static SupportCardEntity Card(int level)
        {
            return new SupportCardEntity("u", 10, level, 0);
        }

        [Test]
        public void GetMaxLevel_FromCap()
        {
            Assert.AreEqual(50, planner.GetMaxLevel(10, 2));
        }

        [Test]
        public void GetMaxLimitBreak_IsFour()
        {
            Assert.AreEqual(4, planner.GetMaxLimitBreak(10));
        }

        [Test]
        public void GetLevelUpCost_SumsRows()
        {
            var cost = planner.GetLevelUpCost(10, 3, 8);

            Assert.AreEqual(500, cost.SupportPoints);
            Assert.AreEqual(250, cost.Gold);
        }

        [Test]
        public void GetLevelUpCost_NotAbove_Zero()
        {
            var cost = planner.GetLevelUpCost(10, 8, 8);

            Assert.AreEqual(0, cost.SupportPoints);
            Assert.AreEqual(0, cost.Gold);
        }

        [Test]
        public void Plus1_BelowMax_ReturnsLevelPlusOne()
        {
            Assert.AreEqual(4, planner.PlanTarget(Card(3), 1000, 1000, LevelUpStepKind.Plus1));
        }

        [Test]
        public void Plus5_NearMax_ClampsToMax()
        {
            Assert.AreEqual(40, planner.PlanTarget(Card(38), 1000, 1000, LevelUpStepKind.Plus5));
        }

        [Test]
        public void AtMax_ReturnsCurrentLevel()
        {
            Assert.AreEqual(40, planner.PlanTarget(Card(40), 1000, 1000, LevelUpStepKind.Max));
        }

        [Test]
        public void Plus5_NotAffordable_StillReturnsTarget()
        {
            Assert.AreEqual(8, planner.PlanTarget(Card(3), 0, 1000, LevelUpStepKind.Plus5));
        }

        [Test]
        public void Max_AffordableLimited_ReturnsHighestAffordable()
        {
            Assert.AreEqual(5, planner.PlanTarget(Card(3), 250, 1000, LevelUpStepKind.Max));
        }

        [Test]
        public void Max_CannotAffordAny_ReturnsCurrentLevel()
        {
            Assert.AreEqual(3, planner.PlanTarget(Card(3), 50, 1000, LevelUpStepKind.Max));
        }
    }
}
