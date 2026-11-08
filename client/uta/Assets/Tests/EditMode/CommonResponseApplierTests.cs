using NUnit.Framework;
using Uta.Client.Domain.Player;
using Uta.Client.Infrastructure.Api;
using Uta.Client.Infrastructure.Idol;
using Uta.Client.Infrastructure.Player;
using Uta.Client.Infrastructure.SupportCard;
using Uta.Shared.Contracts.Common;

namespace Uta.Client.Tests
{
    public sealed class CommonResponseApplierTests
    {
        [Test]
        public void ApplyAsync_AppliesPlayerDiff()
        {
            var players = new PlayerRepository();
            players.Upsert(new PlayerEntity("P1", 1, 2, 3, 4, 5, false, false));
            var applier = new CommonResponseApplier(new PlayerDiffApplier(players, new IdolRepository(), new SupportCardRepository()));

            applier.ApplyAsync(new AppCommonDto(new PlayerDiffDto(true, 9, 8, 7, 6, 105, null, null, null)));

            Assert.AreEqual(9, players.Find().Level);
            Assert.AreEqual(105, players.Find().GachaCurrency);
        }

        [Test]
        public void ApplyAsync_DoesNothing_WhenCommonOrPlayerDiffNull()
        {
            var players = new PlayerRepository();
            var idols = new IdolRepository();
            var supportCards = new SupportCardRepository();
            var applier = new CommonResponseApplier(new PlayerDiffApplier(players, idols, supportCards));

            Assert.DoesNotThrow(() => applier.ApplyAsync(null));
            Assert.DoesNotThrow(() => applier.ApplyAsync(new AppCommonDto(null)));

            Assert.IsNull(players.Find());
            Assert.AreEqual(0, idols.FindAll().Count);
            Assert.AreEqual(0, supportCards.FindAll().Count);
        }
    }
}
