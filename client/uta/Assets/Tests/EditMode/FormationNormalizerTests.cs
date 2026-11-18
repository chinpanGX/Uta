using System;
using NUnit.Framework;
using Uta.Client.Application.Produce;
using Uta.Client.Domain.SupportCard;
using Uta.Client.Infrastructure.SupportCard;
using Uta.MasterData;

namespace Uta.Client.Tests
{
    public sealed class FormationNormalizerTests
    {
        // idol 1 = Sense（TestMasterData.Idol の既定）。カード 10 = Sense、20 = Logic、30 = Free。
        private static FormationNormalizer Build(out SupportCardRepository repository)
        {
            var db = TestMasterData.Build(b =>
            {
                b.Append(new[] { TestMasterData.Idol(1, "i", Rarity.SR) });
                b.Append(new[]
                {
                    new SupportCard { SupportCardId = 10, Name = "a", Plan = Plan.Sense },
                    new SupportCard { SupportCardId = 20, Name = "b", Plan = Plan.Logic },
                    new SupportCard { SupportCardId = 30, Name = "c", Plan = Plan.Free },
                });
            });
            repository = new SupportCardRepository();
            repository.Upsert(new SupportCardEntity("U10", 10, 1, 0));
            repository.Upsert(new SupportCardEntity("U20", 20, 1, 0));
            repository.Upsert(new SupportCardEntity("U30", 30, 1, 0));
            return new FormationNormalizer(repository, db);
        }

        [Test]
        public void RemoveIncompatible_ClearsIncompatiblePlan()
        {
            var normalizer = Build(out _);

            var result = normalizer.RemoveIncompatible(1, new[] { "U10", "U20", "U30", null, null }, out _);

            CollectionAssert.AreEqual(new[] { "U10", null, "U30", null, null }, result);
        }

        [Test]
        public void RemoveIncompatible_ClearsUnknownUid()
        {
            var normalizer = Build(out _);

            var result = normalizer.RemoveIncompatible(1, new[] { "U10", "ZZ", null, null, null }, out _);

            CollectionAssert.AreEqual(new[] { "U10", null, null, null, null }, result);
        }

        [Test]
        public void RemoveIncompatible_CountsRemoved()
        {
            var normalizer = Build(out _);

            normalizer.RemoveIncompatible(1, new[] { "U10", "U20", "ZZ", null, null }, out var removed);

            Assert.AreEqual(2, removed);
        }

        [Test]
        public void RemoveIncompatible_NoIdol_ClearsOnlyUnknown()
        {
            var normalizer = Build(out _);

            var result = normalizer.RemoveIncompatible(0, new[] { "U10", "U20", "ZZ", null, null }, out var removed);

            CollectionAssert.AreEqual(new[] { "U10", "U20", null, null, null }, result);
            Assert.AreEqual(1, removed);
        }

        [Test]
        public void RemoveIncompatible_Null_Throws()
        {
            var normalizer = Build(out _);

            Assert.Throws<ArgumentNullException>(() => normalizer.RemoveIncompatible(1, null, out _));
        }
    }
}
