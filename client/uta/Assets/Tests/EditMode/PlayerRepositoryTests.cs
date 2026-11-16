using System;
using NUnit.Framework;
using Uta.Client.Domain.Player;
using Uta.Client.Infrastructure.Player;

namespace Uta.Client.Tests
{
    public sealed class PlayerRepositoryTests
    {
        private static PlayerEntity Player(int exp)
        {
            return new PlayerEntity("P1", exp, 2, 3, 4, false, false);
        }

        [Test]
        public void Find_Empty_ReturnsNull()
        {
            Assert.IsNull(new PlayerRepository().Find());
        }

        [Test]
        public void Upsert_ReplacesCurrent()
        {
            var repo = new PlayerRepository();
            repo.Upsert(Player(1));
            repo.Upsert(Player(2));

            Assert.AreEqual(2, repo.Find().Exp);
        }

        [Test]
        public void Upsert_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new PlayerRepository().Upsert(null));
        }

        [Test]
        public void Entity_NullUniqueId_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new PlayerEntity(null, 0, 0, 0, 0, false, false));
        }
    }
}
