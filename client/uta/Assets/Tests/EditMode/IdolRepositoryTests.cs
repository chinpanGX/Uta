using System;
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
        public void ReplaceAll_ReplacesEverything()
        {
            var repo = new IdolRepository();
            repo.Upsert(Idol("Z", 1));

            repo.ReplaceAll(new[] { Idol("A", 5), Idol("B", 4) });

            Assert.IsNull(repo.Find("Z"));
            Assert.AreEqual(2, repo.FindAll().Count);
            Assert.AreEqual("B", repo.FindAll()[0].UniqueId);
        }

        [Test]
        public void Null_Arguments_Throw()
        {
            var repo = new IdolRepository();
            Assert.Throws<ArgumentNullException>(() => repo.Upsert(null));
            Assert.Throws<ArgumentNullException>(() => repo.ReplaceAll(null));
            Assert.Throws<ArgumentNullException>(() => new IdolEntity(null, 1, 0, 0, 0));
        }
    }
}
