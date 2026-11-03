using NUnit.Framework;
using Uta.Client.Infrastructure.Player;
using Uta.Shared.Contracts.Player;

namespace Uta.Client.Infrastructure.Tests
{
    public sealed class PlayerGatewayTests
    {
        [Test]
        public void ToSnapshot_CopiesAllFields()
        {
            var response = new SignInResponse(
                3, 40, 500, 60, 700, true, false,
                new[] { new OwnedIdolDto(1, 2, 3, 4), new OwnedIdolDto(5, 6, 7, 8) },
                new[]
                {
                    new OwnedSupportCardDto("01ARZ3NDEKTSV4RRFFQ69G5FAV", 10, 11, 12),
                    new OwnedSupportCardDto("01ARZ3NDEKTSV4RRFFQ69G5FAW", 20, 21, 22),
                });

            var snapshot = PlayerGateway.ToSnapshot(response);

            Assert.AreEqual(3, snapshot.Level);
            Assert.AreEqual(40, snapshot.Exp);
            Assert.AreEqual(500, snapshot.Gold);
            Assert.AreEqual(60, snapshot.SupportPoints);
            Assert.AreEqual(700, snapshot.GachaCurrency);
            Assert.IsTrue(snapshot.HasActiveProduce);
            Assert.IsFalse(snapshot.DiscardedProduce);
            Assert.AreEqual(2, snapshot.Idols.Count);
            Assert.AreEqual(5, snapshot.Idols[1].IdolId);
            Assert.AreEqual(6, snapshot.Idols[1].Training);
            Assert.AreEqual(7, snapshot.Idols[1].Awakening);
            Assert.AreEqual(8, snapshot.Idols[1].Shards);
            Assert.AreEqual(2, snapshot.SupportCards.Count);
            Assert.AreEqual("01ARZ3NDEKTSV4RRFFQ69G5FAW", snapshot.SupportCards[1].SupportCardUid);
            Assert.AreEqual(20, snapshot.SupportCards[1].CardId);
            Assert.AreEqual(21, snapshot.SupportCards[1].Level);
            Assert.AreEqual(22, snapshot.SupportCards[1].LimitBreak);
        }

        [Test]
        public void ToSnapshot_NullArrays_BecomeEmpty()
        {
            var response = new SignInResponse(1, 0, 0, 0, 0, false, false, null, null);

            var snapshot = PlayerGateway.ToSnapshot(response);

            Assert.IsNotNull(snapshot.Idols);
            Assert.IsNotNull(snapshot.SupportCards);
            Assert.AreEqual(0, snapshot.Idols.Count);
            Assert.AreEqual(0, snapshot.SupportCards.Count);
        }
    }
}
