using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Uta.Client.Application.Owned;
using Uta.Client.Domain.Idol;
using Uta.Client.Domain.SupportCard;
using Uta.Client.Infrastructure.Idol;
using Uta.Client.Infrastructure.SupportCard;
using Uta.Shared.Domain.Lesson;
using Uta.Shared.Domain.Produce;
using ZLinq;

namespace Uta.Client.Tests
{
    public sealed class OwnedListFactoryTests
    {
        private IdolRepository idolRepository;
        private SupportCardRepository supportCardRepository;
        private OwnedListFactory ownedListFactory;

        [SetUp]
        public void SetUp()
        {
            idolRepository = new IdolRepository();
            supportCardRepository = new SupportCardRepository();
            ownedListFactory = new OwnedListFactory(idolRepository, supportCardRepository, OwnedFixtures.CreateMaster());
        }

        private void SetCards(params SupportCardEntity[] cards)
        {
            supportCardRepository.Clear();
            foreach (var card in cards)
            {
                supportCardRepository.Upsert(card);
            }
        }

        private static string Uids(IReadOnlyList<OwnedListItem> items)
        {
            return string.Join(",", items.Select(item => item.SupportCardUid).ToArray());
        }

        [Test]
        public void CreateIdols_OrderedByIdolId()
        {
            idolRepository.Upsert(new IdolEntity("I3", 3, 0, 0, 0));
            idolRepository.Upsert(new IdolEntity("I1", 1, 0, 0, 0));
            idolRepository.Upsert(new IdolEntity("I2", 2, 0, 0, 0));

            var items = ownedListFactory.CreateIdols(null);

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, items.Select(item => item.IdolId).ToArray());
            Assert.IsTrue(items.All(item => item.Enabled));
        }

        [Test]
        public void CreateIdols_Selection_DisablesExcluded()
        {
            OwnedFixtures.SetOwned(idolRepository, supportCardRepository);

            var items = ownedListFactory.CreateIdols(OwnedSelectionCondition.ForIdol(new[] { 2 }));

            Assert.IsTrue(items[0].Enabled);
            Assert.AreEqual(OwnedDisabledKind.None, items[0].DisabledKind);
            Assert.IsFalse(items[1].Enabled);
            Assert.AreEqual(OwnedDisabledKind.AlreadyInFormation, items[1].DisabledKind);
        }

        [Test]
        public void CreateIdols_SupportCondition_Throws()
        {
            Assert.Throws<ArgumentException>(() => ownedListFactory.CreateIdols(OwnedSelectionCondition.ForSupportCard(0, null)));
        }

        [Test]
        public void CreateSupportCards_SortByRarityDescending()
        {
            OwnedFixtures.SetOwned(idolRepository, supportCardRepository);

            var items = ownedListFactory.CreateSupportCards(OwnedSortKind.Rarity, true, null, null, null);

            Assert.AreEqual("01A,01D,01B,01C", Uids(items));
        }

        [Test]
        public void CreateSupportCards_SortByLevelDescending_TieByUidAscending()
        {
            OwnedFixtures.SetOwned(idolRepository, supportCardRepository);

            var items = ownedListFactory.CreateSupportCards(OwnedSortKind.Level, true, null, null, null);

            Assert.AreEqual("01B,01A,01C,01D", Uids(items));
        }

        [Test]
        public void CreateSupportCards_SortByAcquiredDescending()
        {
            OwnedFixtures.SetOwned(idolRepository, supportCardRepository);

            var items = ownedListFactory.CreateSupportCards(OwnedSortKind.Acquired, true, null, null, null);

            Assert.AreEqual("01D,01C,01B,01A", Uids(items));
        }

        [Test]
        public void CreateSupportCards_PlanFilter_ExactMatch()
        {
            OwnedFixtures.SetOwned(idolRepository, supportCardRepository);

            var items = ownedListFactory.CreateSupportCards(OwnedSortKind.Rarity, true, Plan.Free, null, null);

            Assert.AreEqual("01A,01D", Uids(items));
        }

        [Test]
        public void CreateSupportCards_TypeFilter()
        {
            OwnedFixtures.SetOwned(idolRepository, supportCardRepository);

            var items = ownedListFactory.CreateSupportCards(OwnedSortKind.Rarity, true, null, SupportType.Rhythm, null);

            Assert.AreEqual("01B", Uids(items));
        }

        [Test]
        public void CreateSupportCards_Selection_DisablesByPriority()
        {
            SetCards(
                new SupportCardEntity("s1", 12, 1, 0),
                new SupportCardEntity("s2", 12, 1, 0),
                new SupportCardEntity("s3", 12, 1, 0),
                new SupportCardEntity("s4", 10, 1, 0),
                new SupportCardEntity("s5", 11, 1, 0));

            var items = ownedListFactory.CreateSupportCards(OwnedSortKind.Acquired, false, null, null,
                OwnedSelectionCondition.ForSupportCard(1, new[] { "s1" }));

            Assert.AreEqual(OwnedDisabledKind.AlreadyInFormation, items[0].DisabledKind);
            Assert.AreEqual(OwnedDisabledKind.SameCardInFormation, items[1].DisabledKind);
            Assert.AreEqual(OwnedDisabledKind.SameCardInFormation, items[2].DisabledKind);
            Assert.AreEqual(OwnedDisabledKind.None, items[3].DisabledKind);
            Assert.IsTrue(items[3].Enabled);
            Assert.AreEqual(OwnedDisabledKind.None, items[4].DisabledKind);
            Assert.IsTrue(items[4].Enabled);
            Assert.IsFalse(items[0].Enabled);
        }

        [Test]
        public void CreateSupportCards_Selection_PlanMismatch()
        {
            SetCards(new SupportCardEntity("s1", 12, 1, 0), new SupportCardEntity("s4", 10, 1, 0));

            var items = ownedListFactory.CreateSupportCards(OwnedSortKind.Acquired, false, null, null,
                OwnedSelectionCondition.ForSupportCard(1, null));

            Assert.AreEqual(OwnedDisabledKind.PlanMismatch, items[0].DisabledKind);
            Assert.IsFalse(items[0].Enabled);
            Assert.IsTrue(items[1].Enabled);
        }

        [Test]
        public void CreateSupportCards_Selection_UnknownExcludedUid_Ignored()
        {
            OwnedFixtures.SetOwned(idolRepository, supportCardRepository);

            var items = ownedListFactory.CreateSupportCards(OwnedSortKind.Rarity, true, null, null,
                OwnedSelectionCondition.ForSupportCard(0, new[] { "zz" }));

            Assert.AreEqual(4, items.Count);
            Assert.IsTrue(items.All(item => item.Enabled));
        }

        [Test]
        public void CreateSupportCards_Material_ListsSameCardExceptTarget()
        {
            SetCards(
                new SupportCardEntity("m1", 10, 1, 0),
                new SupportCardEntity("m2", 10, 1, 0),
                new SupportCardEntity("m3", 10, 1, 0),
                new SupportCardEntity("x1", 11, 1, 0));

            var items = ownedListFactory.CreateSupportCards(OwnedSortKind.Level, false, null, null,
                OwnedSelectionCondition.ForLimitBreakMaterial(10, "m2"));

            Assert.AreEqual("m1,m3", Uids(items));
            Assert.IsTrue(items.All(item => item.Enabled));
        }

        [Test]
        public void CreateSupportCards_IdolCondition_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
                ownedListFactory.CreateSupportCards(OwnedSortKind.Rarity, true, null, null, OwnedSelectionCondition.ForIdol()));
        }

        [Test]
        public void CreateSupportCards_Empty_ReturnsEmpty()
        {
            var items = ownedListFactory.CreateSupportCards(OwnedSortKind.Rarity, true, null, null, null);

            Assert.AreEqual(0, items.Count);
        }
    }
}
