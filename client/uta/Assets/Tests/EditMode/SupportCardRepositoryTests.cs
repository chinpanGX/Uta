using System;
using System.Collections.Generic;
using NUnit.Framework;
using Uta.Client.Domain.SupportCard;
using Uta.Client.Infrastructure.SupportCard;

namespace Uta.Client.Tests
{
    public sealed class SupportCardRepositoryTests
    {
        private static SupportCardEntity Card(string uid, int level = 1)
        {
            return new SupportCardEntity(uid, 10, level, 0);
        }

        [Test]
        public void Find_Missing_ReturnsNull()
        {
            Assert.IsNull(new SupportCardRepository().Find("A"));
        }

        [Test]
        public void Upsert_AddsToEnd_ThenReplacesSameUidInPlace()
        {
            var repo = new SupportCardRepository();
            repo.Upsert(Card("B"));
            repo.Upsert(Card("A"));
            repo.Upsert(Card("B", 7));

            var all = repo.FindAll();

            Assert.AreEqual(2, all.Count);
            Assert.AreEqual("B", all[0].UniqueId);
            Assert.AreEqual(7, all[0].Level);
            Assert.AreEqual("A", all[1].UniqueId);
        }

        [Test]
        public void Remove_Existing_RemovesIt()
        {
            var repo = new SupportCardRepository();
            repo.Upsert(Card("A"));

            repo.Remove("A");

            Assert.AreEqual(0, repo.FindAll().Count);
        }

        [Test]
        public void Remove_Missing_DoesNothing()
        {
            var repo = new SupportCardRepository();
            repo.Upsert(Card("A"));

            repo.Remove("Z");

            Assert.AreEqual(1, repo.FindAll().Count);
        }

        [Test]
        public void Clear_RemovesAll()
        {
            var repo = new SupportCardRepository();
            repo.Upsert(Card("X"));

            repo.Clear();

            Assert.AreEqual(0, repo.FindAll().Count);
        }

        [Test]
        public void UpsertList_ReplacesSameUid_AddsToEnd_KeepsOthers()
        {
            var repo = new SupportCardRepository();
            repo.Upsert(Card("X"));
            repo.Upsert(Card("A"));

            repo.Upsert(new[] { Card("B"), Card("A", 7) });

            var all = repo.FindAll();
            Assert.AreEqual(3, all.Count);
            Assert.AreEqual("X", all[0].UniqueId);
            Assert.AreEqual("A", all[1].UniqueId);
            Assert.AreEqual(7, all[1].Level);
            Assert.AreEqual("B", all[2].UniqueId);
        }

        [Test]
        public void UpsertList_NullElement_ThrowsAndChangesNothing()
        {
            var repo = new SupportCardRepository();
            repo.Upsert(Card("A"));

            Assert.Throws<ArgumentNullException>(() => repo.Upsert(new[] { Card("A", 7), null }));

            Assert.AreEqual(1, repo.FindAll().Count);
            Assert.AreEqual(1, repo.Find("A").Level);
        }

        [Test]
        public void FindAll_ReturnsNewList_NotAffectedByLaterUpsert()
        {
            var repo = new SupportCardRepository();
            repo.Upsert(Card("A"));
            var before = repo.FindAll();

            repo.Upsert(Card("B"));

            Assert.AreEqual(1, before.Count);
        }

        [Test]
        public void Null_Arguments_Throw()
        {
            var repo = new SupportCardRepository();
            Assert.Throws<ArgumentNullException>(() => repo.Upsert((SupportCardEntity)null));
            Assert.Throws<ArgumentNullException>(() => repo.Upsert((IReadOnlyList<SupportCardEntity>)null));
            Assert.Throws<ArgumentNullException>(() => new SupportCardEntity(null, 1, 1, 0));
        }
    }
}
