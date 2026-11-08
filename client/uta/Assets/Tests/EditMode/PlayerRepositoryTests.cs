using System;
using NUnit.Framework;
using Uta.Client.Domain.Player;
using Uta.Client.Infrastructure.Player;

namespace Uta.Client.Tests
{
    public sealed class PlayerRepositoryTests
    {
        private static PlayerEntity Player(int level)
        {
            return new PlayerEntity("P1", level, 1, 2, 3, 4, false, false);
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

            Assert.AreEqual(2, repo.Find().Level);
        }

        [Test]
        public void Upsert_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new PlayerRepository().Upsert(null));
        }
    }
}
