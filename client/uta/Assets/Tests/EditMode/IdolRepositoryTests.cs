using System;
using System.Collections.Generic;
using NUnit.Framework;
using Uta.Client.Domain.Idol;
using Uta.Client.Infrastructure.Idol;

namespace Uta.Client.Tests
{
    public sealed class IdolRepositoryTests
    {
        private static IdolEntity Idol(string uniqueId, int masterId, int training = 0)
        {
            return new IdolEntity(uniqueId, masterId, training, 0, 0);
        }

        [Test]
        public void Find_Missing_ReturnsNull()
        {
            Assert.IsNull(new IdolRepository().Find("U1"));
        }

        [Test]
        public void FindAll_Empty_ReturnsEmpty()
        {
            Assert.AreEqual(0, new IdolRepository().FindAll().Count);
        }

        [Test]
        public void Upsert_SameUniqueId_Replaces()
        {
            var repo = new IdolRepository();
            repo.Upsert(Idol("A", 1, 1));
            repo.Upsert(Idol("A", 2, 9));

            Assert.AreEqual(1, repo.FindAll().Count);
            Assert.AreEqual(2, repo.Find("A").MasterId);
            Assert.AreEqual(9, repo.Find("A").Training);
        }

        [Test]
        public void Upsert_SameMasterIdDifferentUniqueId_Adds()
        {
            var repo = new IdolRepository();
            repo.Upsert(Idol("A", 1));
            repo.Upsert(Idol("B", 1));

            Assert.AreEqual(2, repo.FindAll().Count);
        }

        [Test]
        public void FindAll_SortedByMasterId()
        {
            var repo = new IdolRepository();
            repo.Upsert(Idol("C", 1));
            repo.Upsert(Idol("A", 3));
            repo.Upsert(Idol("B", 2));

            var all = repo.FindAll();

            Assert.AreEqual("C", all[0].UniqueId);
            Assert.AreEqual("B", all[1].UniqueId);
            Assert.AreEqual("A", all[2].UniqueId);
        }

        [Test]
        public void FindAll_ReturnsNewList_NotAffectedByLaterUpsert()
        {
            var repo = new IdolRepository();
            repo.Upsert(Idol("A", 1));
            var before = repo.FindAll();

            repo.Upsert(Idol("B", 2));

            Assert.AreEqual(1, before.Count);
            Assert.AreNotSame(before, repo.FindAll());
        }

        [Test]
        public void Clear_RemovesAll()
        {
            var repo = new IdolRepository();
            repo.Upsert(Idol("Z", 1));

            repo.Clear();

            Assert.AreEqual(0, repo.FindAll().Count);
        }

        [Test]
        public void UpsertList_ReplacesSameUniqueId_KeepsOthers()
        {
            var repo = new IdolRepository();
            repo.Upsert(Idol("Z", 1));
            repo.Upsert(Idol("A", 1, 1));

            repo.Upsert(new[] { Idol("A", 5, 9), Idol("B", 4) });

            Assert.AreEqual(3, repo.FindAll().Count);
            Assert.IsNotNull(repo.Find("Z"));
            Assert.AreEqual(5, repo.Find("A").MasterId);
            Assert.AreEqual(9, repo.Find("A").Training);
            Assert.IsNotNull(repo.Find("B"));
        }

        [Test]
        public void UpsertList_NullElement_ThrowsAndChangesNothing()
        {
            var repo = new IdolRepository();
            repo.Upsert(Idol("A", 1));

            Assert.Throws<ArgumentNullException>(() => repo.Upsert(new[] { Idol("A", 9), null }));

            Assert.AreEqual(1, repo.Find("A").MasterId);
            Assert.AreEqual(1, repo.FindAll().Count);
        }

        [Test]
        public void Null_Arguments_Throw()
        {
            var repo = new IdolRepository();
            Assert.Throws<ArgumentNullException>(() => repo.Upsert((IdolEntity)null));
            Assert.Throws<ArgumentNullException>(() => repo.Upsert((IReadOnlyList<IdolEntity>)null));
            Assert.Throws<ArgumentNullException>(() => new IdolEntity(null, 1, 0, 0, 0));
        }
    }
}
