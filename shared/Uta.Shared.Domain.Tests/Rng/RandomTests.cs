using System.Collections.Generic;
using System.Linq;
using Uta.Shared.Domain.Rng;
using Xunit;

namespace Uta.Shared.Domain.Tests.Rng
{
    public sealed class RandomTests
    {
        [Fact]
        public void SplitMix64_Seed1234567_MatchesPublishedValues()
        {
            // 公開されている参照値（Python の独立した実装でも同じ値になることを確かめた）
            var state = 1234567UL;
            var expected = new[]
            {
                6457827717110365317UL,
                3203168211198807973UL,
                9817491932198370423UL,
                4593380528125082431UL,
                16408922859458223821UL,
            };

            foreach (var value in expected)
            {
                Assert.Equal(value, SplitMix64.Next(ref state));
            }
        }

        [Fact]
        public void Xoshiro256StarStar_State1234_MatchesReference()
        {
            var random = new Xoshiro256StarStar(1, 2, 3, 4);
            var expected = new[] { 11520UL, 0UL, 1509978240UL, 1215971899390074240UL, 1216172134540287360UL };

            foreach (var value in expected)
            {
                Assert.Equal(value, random.NextUInt64());
            }

            Assert.Equal(5, random.DrawCount);
        }

        [Fact]
        public void RandomStreams_Create_MatchesReference()
        {
            // Python の参照実装で計算した値。ストリームの作り方を変えたら、保存済みのセッションが再現できなくなる
            var random = RandomStreams.Create(42, new RandomStreamKey(RandomStreamKind.Lesson, 5, 1));

            Assert.Equal(2821150666123509936UL, random.NextUInt64());
            Assert.Equal(14513443485720801181UL, random.NextUInt64());
            Assert.Equal(15050637097555739426UL, random.NextUInt64());
        }

        [Fact]
        public void RandomStreams_DifferentAttempt_DifferentSequence()
        {
            var first = RandomStreams.Create(42, new RandomStreamKey(RandomStreamKind.Lesson, 5, 0));
            var second = RandomStreams.Create(42, new RandomStreamKey(RandomStreamKind.Lesson, 5, 1));

            Assert.NotEqual(first.NextUInt64(), second.NextUInt64());
        }

        [Fact]
        public void Xoshiro256StarStar_RestoredFromState_ContinuesSameSequence()
        {
            var original = Xoshiro256StarStar.FromSeed(99);
            original.NextUInt64();
            original.NextUInt64();

            var restored = new Xoshiro256StarStar(original.S0, original.S1, original.S2, original.S3, original.DrawCount);

            for (var i = 0; i < 10; i++)
            {
                Assert.Equal(original.NextUInt64(), restored.NextUInt64());
            }

            Assert.Equal(original.DrawCount, restored.DrawCount);
        }

        [Fact]
        public void NextInt_ManyDraws_StaysInRangeAndCoversAllValues()
        {
            var random = Xoshiro256StarStar.FromSeed(7);
            var seen = new HashSet<int>();

            for (var i = 0; i < 1000; i++)
            {
                var value = RandomOps.NextInt(random, 6);
                Assert.InRange(value, 0, 5);
                seen.Add(value);
            }

            Assert.Equal(6, seen.Count);
        }

        [Fact]
        public void Chance_RateZeroOrFull_StillDrawsOnce()
        {
            var random = Xoshiro256StarStar.FromSeed(7);

            Assert.False(RandomOps.Chance(random, 0));
            Assert.True(RandomOps.Chance(random, RandomOps.RateDenominator));
            Assert.Equal(2, random.DrawCount);
        }

        [Fact]
        public void WeightedIndex_ZeroWeight_NeverChosen()
        {
            var random = Xoshiro256StarStar.FromSeed(11);
            var weights = new[] { 60, 0, 40 };

            for (var i = 0; i < 500; i++)
            {
                Assert.NotEqual(1, RandomOps.WeightedIndex(random, weights));
            }
        }

        [Fact]
        public void Shuffle_SameSeed_SameOrderAndIsPermutation()
        {
            var first = Enumerable.Range(1, 20).ToList();
            var second = Enumerable.Range(1, 20).ToList();

            RandomOps.Shuffle(Xoshiro256StarStar.FromSeed(5), first);
            RandomOps.Shuffle(Xoshiro256StarStar.FromSeed(5), second);

            Assert.Equal(first, second);
            Assert.Equal(Enumerable.Range(1, 20), first.OrderBy(x => x));
            Assert.NotEqual(Enumerable.Range(1, 20), first);
        }
    }
}
