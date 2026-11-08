using NUnit.Framework;
using Uta.Client.Infrastructure.Player;
using Uta.Shared.Contracts.Player;

namespace Uta.Client.Tests
{
    public sealed class PlayerMapperTests
    {
        private static SignInResponse NewResponse()
        {
            return new SignInResponse(
                3, 40, 500, 60, 700, true, false,
                new[] { new OwnedIdolDto(1, 2, 3, 4, "U1"), new OwnedIdolDto(5, 6, 7, 8, "U5") },
                new[]
                {
                    new OwnedSupportCardDto("01ARZ3NDEKTSV4RRFFQ69G5FAV", 10, 11, 12),
                    new OwnedSupportCardDto("01ARZ3NDEKTSV4RRFFQ69G5FAW", 20, 21, 22),
                },
                "P1");
        }

        [Test]
        public void ToPlayerEntity_CopiesScalars()
        {
            var player = PlayerMapper.ToPlayerEntity(NewResponse());

            Assert.AreEqual("P1", player.UniqueId);
            Assert.AreEqual(3, player.Level);
            Assert.AreEqual(40, player.Exp);
            Assert.AreEqual(500, player.Gold);
            Assert.AreEqual(60, player.SupportPoints);
            Assert.AreEqual(700, player.GachaCurrency);
            Assert.IsTrue(player.HasActiveProduce);
            Assert.IsFalse(player.DiscardedProduce);
        }

        [Test]
        public void ToIdolEntities_CopiesAllFields()
        {
            var idols = PlayerMapper.ToIdolEntities(NewResponse().Idols);

            Assert.AreEqual(2, idols.Count);
            Assert.AreEqual("U5", idols[1].UniqueId);
            Assert.AreEqual(5, idols[1].MasterId);
            Assert.AreEqual(6, idols[1].Training);
            Assert.AreEqual(7, idols[1].Awakening);
            Assert.AreEqual(8, idols[1].Shards);
        }

        [Test]
        public void ToSupportCardEntities_CopiesAllFields()
        {
            var cards = PlayerMapper.ToSupportCardEntities(NewResponse().SupportCards);

            Assert.AreEqual(2, cards.Count);
            Assert.AreEqual("01ARZ3NDEKTSV4RRFFQ69G5FAW", cards[1].UniqueId);
            Assert.AreEqual(20, cards[1].MasterId);
            Assert.AreEqual(21, cards[1].Level);
            Assert.AreEqual(22, cards[1].LimitBreak);
        }

        [Test]
        public void ToEntities_NullArrays_BecomeEmpty()
        {
            Assert.IsNotNull(PlayerMapper.ToIdolEntities(null));
            Assert.IsNotNull(PlayerMapper.ToSupportCardEntities(null));
            Assert.AreEqual(0, PlayerMapper.ToIdolEntities(null).Count);
            Assert.AreEqual(0, PlayerMapper.ToSupportCardEntities(null).Count);
        }
    }
}
