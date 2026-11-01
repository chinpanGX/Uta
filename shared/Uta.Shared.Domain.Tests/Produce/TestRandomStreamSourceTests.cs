using System.Linq;
using Uta.Shared.Domain.Lesson;
using Uta.Shared.Domain.Rng;
using Uta.Shared.Domain.Tests.Lesson;
using Xunit;

namespace Uta.Shared.Domain.Tests.Produce
{
    public class TestRandomStreamSourceTests
    {
        [Fact]
        public void Get_SameKey_ReturnsSameObject()
        {
            var source = new TestRandomStreamSource(42);
            var key = new RandomStreamKey(RandomStreamKind.Week, 3, 0);

            Assert.True(ReferenceEquals(source.Get(key), source.Get(key)));
        }

        [Fact]
        public void Get_SameKey_ContinuesSequence()
        {
            var key = new RandomStreamKey(RandomStreamKind.Week, 3, 0);
            var source = new TestRandomStreamSource(42);
            var expected = RandomStreams.Create(42, key);

            var random = source.Get(key);
            var first = random.NextUInt64();
            var second = source.Get(key).NextUInt64();

            Assert.Equal(expected.NextUInt64(), first);
            Assert.Equal(expected.NextUInt64(), second);
        }

        [Fact]
        public void Get_DifferentKeys_DifferentValues()
        {
            var source = new TestRandomStreamSource(42);

            var a = source.Get(new RandomStreamKey(RandomStreamKind.Week, 1, 0)).NextUInt64();
            var b = source.Get(new RandomStreamKey(RandomStreamKind.Week, 2, 0)).NextUInt64();

            Assert.NotEqual(a, b);
        }

        [Fact]
        public void Requests_RecordsEveryCallInOrder()
        {
            var source = new TestRandomStreamSource(1);
            var keyA = new RandomStreamKey(RandomStreamKind.Week, 1, 0);
            var keyB = new RandomStreamKey(RandomStreamKind.Offer, 2, 0);

            source.Get(keyA);
            source.Get(keyB);
            source.Get(keyA);

            Assert.Equal(new[] { keyA, keyB, keyA }, source.Requests.ToArray());
        }

        [Fact]
        public void TestCardCatalog_CardIds_AscendingAndComplete()
        {
            var catalog = new TestCardCatalog();

            Assert.Equal(catalog.CardIds.OrderBy(x => x).ToArray(), catalog.CardIds.ToArray());
            Assert.Contains(1, catalog.CardIds);
            Assert.Contains(107, catalog.CardIds);
            Assert.Contains(207, catalog.CardIds);
            Assert.Contains(900, catalog.CardIds);
        }

        [Fact]
        public void TestCardCatalog_CardIds_IncludesExtra()
        {
            var baseFace = new CardFace(new CostDef[0], new ConditionDef[0], new EffectDef[0]);
            var extra = new CardDefinition(99999, Plan.Free, CardType.Active, Rarity.N, 1, false, false, false, baseFace, null);

            var catalog = new TestCardCatalog(new[] { extra });

            Assert.Contains(99999, catalog.CardIds);
            Assert.Equal(catalog.CardIds.OrderBy(x => x).ToArray(), catalog.CardIds.ToArray());
        }
    }
}
