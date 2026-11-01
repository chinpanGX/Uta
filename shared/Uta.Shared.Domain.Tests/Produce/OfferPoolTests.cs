using System.Collections.Generic;
using System.Linq;
using Uta.Shared.Domain.Lesson;
using Uta.Shared.Domain.Produce;
using Uta.Shared.Domain.Rng;
using Uta.Shared.Domain.Tests.Lesson;
using Xunit;

namespace Uta.Shared.Domain.Tests.Produce
{
    public class OfferPoolTests
    {
        private static CardDefinition Card(int id, Plan plan, CardType type, Rarity rarity, int unlockLv, bool unique)
        {
            return new CardDefinition(id, plan, type, rarity, unlockLv, false, unique, false, TestCards.Face(TestCards.Consume(0)), null);
        }

        private static readonly ProduceCatalog Catalog = TestProduceMaster.Create(new TestCardCatalog(new[]
        {
            Card(910, Plan.Sense, CardType.Mental, Rarity.N, 1, false),
            Card(911, Plan.Logic, CardType.Mental, Rarity.N, 1, false),
            Card(912, Plan.Free, CardType.Mental, Rarity.R, 5, false),
            Card(913, Plan.Free, CardType.Trouble, Rarity.N, 1, false),
            Card(914, Plan.Free, CardType.Mental, Rarity.R, 1, true),
            Card(915, Plan.Free, CardType.Active, Rarity.SSR, 1, false),
            Card(916, Plan.Free, CardType.Active, Rarity.SSR, 1, false),
            Card(917, Plan.Free, CardType.Active, Rarity.SSR, 1, false),
        }));

        private static IRandom Random(ulong seed)
        {
            return RandomStreams.Create(seed, new RandomStreamKey(RandomStreamKind.Week, 1, 0));
        }

        [Fact]
        public void GetCandidates_FiltersAndSorts()
        {
            var idol = Catalog.GetIdol(1);
            var result = OfferPool.GetCandidates(Catalog, idol, 1, new int[0]);
            Assert.Equal(result.OrderBy(x => x).ToArray(), result.ToArray());
            Assert.Contains(910, result);
            Assert.Contains(914, result);
            Assert.Contains(915, result);
            Assert.Contains(1, result);
            Assert.DoesNotContain(911, result);
            Assert.DoesNotContain(912, result);
            Assert.DoesNotContain(913, result);
        }

        [Fact]
        public void GetCandidates_HigherLevel_IncludesUnlocked()
        {
            var result = OfferPool.GetCandidates(Catalog, Catalog.GetIdol(1), 5, new int[0]);
            Assert.Contains(912, result);
        }

        [Fact]
        public void GetCandidates_UniqueInDeck_Excluded()
        {
            var idol = Catalog.GetIdol(1);
            Assert.DoesNotContain(914, OfferPool.GetCandidates(Catalog, idol, 1, new[] { 914 }));
            Assert.Contains(914, OfferPool.GetCandidates(Catalog, idol, 1, new[] { 910 }));
        }

        [Fact]
        public void GetCandidates_LogicIdol_UsesLogicPlan()
        {
            var result = OfferPool.GetCandidates(Catalog, Catalog.GetIdol(2), 1, new int[0]);
            Assert.Contains(911, result);
            Assert.DoesNotContain(910, result);
        }

        [Fact]
        public void Draw_EmptyCandidates_ReturnsEmpty()
        {
            Assert.Empty(OfferPool.Draw(Catalog, new int[0], Random(1)));
        }

        [Fact]
        public void Draw_LargePool_ThreeDistinctFromPool()
        {
            var candidates = OfferPool.GetCandidates(Catalog, Catalog.GetIdol(1), 1, new int[0]);
            var first = OfferPool.Draw(Catalog, candidates, Random(7));
            Assert.Equal(3, first.Count);
            Assert.Equal(3, first.Distinct().Count());
            Assert.All(first, id => Assert.Contains(id, candidates));

            var second = OfferPool.Draw(Catalog, candidates, Random(7));
            Assert.Equal(first.ToArray(), second.ToArray());
        }

        [Fact]
        public void Draw_TwoCandidates_ReturnsTwo()
        {
            var result = OfferPool.Draw(Catalog, new[] { 910, 915 }, Random(3));
            Assert.Equal(2, result.Count);
            Assert.Equal(new HashSet<int> { 910, 915 }, new HashSet<int>(result));
        }

        [Fact]
        public void Draw_OneRarityOnly_ReturnsThree()
        {
            var result = OfferPool.Draw(Catalog, new[] { 915, 916, 917 }, Random(3));
            Assert.Equal(3, result.Count);
            Assert.Equal(new HashSet<int> { 915, 916, 917 }, new HashSet<int>(result));
        }
    }
}
