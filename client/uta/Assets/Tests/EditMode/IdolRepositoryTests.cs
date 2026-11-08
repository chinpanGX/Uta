using System;
using NUnit.Framework;
using Uta.Client.Domain.Player;
using Uta.Client.Infrastructure.Player;

namespace Uta.Client.Tests
{
    public sealed class IdolRepositoryTests
    {
        private static IdolEntity Idol(int id, int training = 0)
        {
            return new IdolEntity(id, training, 0, 0);
        }

        [Test]
        public void Find_Missing_ReturnsNull()
        {
            Assert.IsNull(new IdolRepository().Find(1));
        }

        [Test]
        public void FindAll_Empty_ReturnsEmpty()
        {
            Assert.AreEqual(0, new IdolRepository().FindAll().Count);
        }

        [Test]
        public void Upsert_Adds_ThenReplacesSameId()
        {
            var repo = new IdolRepository();
            repo.Upsert(Idol(1, 1));
            repo.Upsert(Idol(2, 1));
            repo.Upsert(Idol(1, 9));

            Assert.AreEqual(2, repo.FindAll().Count);
            Assert.AreEqual(9, repo.Find(1).Training);
        }

        [Test]
        public void FindAll_SortedByIdolId()
        {
            var repo = new IdolRepository();
            repo.Upsert(Idol(3));
            repo.Upsert(Idol(1));
            repo.Upsert(Idol(2));

            var all = repo.FindAll();

            Assert.AreEqual(1, all[0].IdolId);
            Assert.AreEqual(2, all[1].IdolId);
            Assert.AreEqual(3, all[2].IdolId);
        }

        [Test]
        public void FindAll_ReturnsNewList_NotAffectedByLaterUpsert()
        {
            var repo = new IdolRepository();
            repo.Upsert(Idol(1));
            var before = repo.FindAll();

            repo.Upsert(Idol(2));

            Assert.AreEqual(1, before.Count);
            Assert.AreNotSame(before, repo.FindAll());
        }

        [Test]
        public void ReplaceAll_ReplacesEverything()
        {
            var repo = new IdolRepository();
            repo.Upsert(Idol(1));

            repo.ReplaceAll(new[] { Idol(5), Idol(4) });

            Assert.IsNull(repo.Find(1));
            Assert.AreEqual(2, repo.FindAll().Count);
            Assert.AreEqual(4, repo.FindAll()[0].IdolId);
        }

        [Test]
        public void Null_Arguments_Throw()
        {
            var repo = new IdolRepository();
            Assert.Throws<ArgumentNullException>(() => repo.Upsert(null));
            Assert.Throws<ArgumentNullException>(() => repo.ReplaceAll(null));
        }
    }
}
