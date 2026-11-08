using System;
using NUnit.Framework;
using Uta.Client.Domain.Idol;
using Uta.Client.Domain.Player;
using Uta.Client.Domain.SupportCard;
using Uta.Client.Infrastructure.Idol;
using Uta.Client.Infrastructure.Player;
using Uta.Client.Infrastructure.SupportCard;
using Uta.Shared.Contracts.Common;
using Uta.Shared.Contracts.Player;

namespace Uta.Client.Tests
{
    public sealed class PlayerDiffApplierTests
    {
        private const string CardA = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
        private const string CardB = "01ARZ3NDEKTSV4RRFFQ69G5FAW";

        private PlayerRepository players;
        private IdolRepository idols;
        private SupportCardRepository supportCards;
        private PlayerDiffApplier applier;

        [SetUp]
        public void SetUp()
        {
            players = new PlayerRepository();
            idols = new IdolRepository();
            supportCards = new SupportCardRepository();
            applier = new PlayerDiffApplier(players, idols, supportCards);
            players.Upsert(new PlayerEntity("P1", 1, 2, 3, 4, 5, true, true));
            idols.Upsert(new IdolEntity("I1", 10, 0, 0, 0));
            supportCards.Upsert(new SupportCardEntity(CardA, 100, 1, 0));
            supportCards.Upsert(new SupportCardEntity(CardB, 200, 1, 0));
        }

        private static PlayerDiffDto Diff(bool hasBalance, OwnedIdolDto[] idolDtos, OwnedSupportCardDto[] cardDtos, string[] removed)
        {
            return new PlayerDiffDto(hasBalance, 9, 8, 7, 6, 5 + 100, idolDtos, cardDtos, removed);
        }

        [Test]
        public void Apply_ReplacesBalance_WhenHasBalance()
        {
            applier.Apply(Diff(true, null, null, null));

            var p = players.Find();
            Assert.AreEqual("P1", p.UniqueId);
            Assert.AreEqual(9, p.Level);
            Assert.AreEqual(8, p.Exp);
            Assert.AreEqual(7, p.Gold);
            Assert.AreEqual(6, p.SupportPoints);
            Assert.AreEqual(105, p.GachaCurrency);
        }

        [Test]
        public void Apply_KeepsBalance_WhenNoBalance()
        {
            var before = players.Find();

            applier.Apply(Diff(false, null, null, null));

            Assert.AreSame(before, players.Find());
        }

        [Test]
        public void Apply_KeepsProduceFlags()
        {
            applier.Apply(Diff(true, null, null, null));

            Assert.IsTrue(players.Find().HasActiveProduce);
            Assert.IsTrue(players.Find().DiscardedProduce);
        }

        [Test]
        public void Apply_UpsertsIdolAndSupportCard()
        {
            var diff = Diff(
                false,
                new[] { new OwnedIdolDto(10, 1, 2, 3, "I1"), new OwnedIdolDto(20, 0, 0, 0, "I2") },
                new[] { new OwnedSupportCardDto(CardA, 100, 5, 1), new OwnedSupportCardDto("01ARZ3NDEKTSV4RRFFQ69G5FAX", 300, 1, 0) },
                null);

            applier.Apply(diff);

            Assert.AreEqual(2, idols.FindAll().Count);
            Assert.AreEqual(1, idols.Find("I1").Training);
            Assert.AreEqual(20, idols.Find("I2").MasterId);
            Assert.AreEqual(3, supportCards.FindAll().Count);
            Assert.AreEqual(5, supportCards.Find(CardA).Level);
            Assert.AreEqual(300, supportCards.Find("01ARZ3NDEKTSV4RRFFQ69G5FAX").MasterId);
        }

        [Test]
        public void Apply_RemovesSupportCard()
        {
            applier.Apply(Diff(false, null, null, new[] { CardB, "NOTHING" }));

            Assert.AreEqual(1, supportCards.FindAll().Count);
            Assert.IsNull(supportCards.Find(CardB));
        }

        [Test]
        public void Apply_IsIdempotent()
        {
            var diff = Diff(
                true,
                new[] { new OwnedIdolDto(20, 0, 0, 0, "I2") },
                new[] { new OwnedSupportCardDto("01ARZ3NDEKTSV4RRFFQ69G5FAX", 300, 1, 0) },
                new[] { CardB });

            applier.Apply(diff);
            applier.Apply(diff);

            Assert.AreEqual(9, players.Find().Level);
            Assert.AreEqual(2, idols.FindAll().Count);
            Assert.AreEqual(2, supportCards.FindAll().Count);
            Assert.IsNull(supportCards.Find(CardB));
        }

        [Test]
        public void Apply_Throws_WhenNotSignedIn()
        {
            var empty = new PlayerDiffApplier(new PlayerRepository(), new IdolRepository(), new SupportCardRepository());

            Assert.Throws<InvalidOperationException>(() => empty.Apply(Diff(true, null, null, null)));
        }

        [Test]
        public void Apply_TreatsNullArraysAsEmpty()
        {
            Assert.DoesNotThrow(() => applier.Apply(Diff(false, null, null, null)));

            Assert.AreEqual(1, idols.FindAll().Count);
            Assert.AreEqual(2, supportCards.FindAll().Count);
        }
    }
}
