using MessagePack;
using MessagePack.Resolvers;
using NUnit.Framework;
using Uta.Shared.Contracts.Common;
using Uta.Shared.Contracts.Player;

namespace Uta.Client.Tests
{
    public sealed class AppCommonDtoSerializationTests
    {
        [Test]
        public void AppCommonDto_RoundTripsThroughMessagePack()
        {
            var original = new AppCommonDto(new PlayerDiffDto(
                true, 1, 2, 3, 4, 5,
                new[] { new OwnedIdolDto(10, 1, 2, 3, "I1") },
                new[] { new OwnedSupportCardDto("01ARZ3NDEKTSV4RRFFQ69G5FAV", 100, 5, 1) },
                new[] { "01ARZ3NDEKTSV4RRFFQ69G5FAW" }));
            var options = MessagePackSerializerOptions.Standard.WithResolver(StandardResolver.Instance);

            var bytes = MessagePackSerializer.Serialize(original, options);
            var actual = MessagePackSerializer.Deserialize<AppCommonDto>(bytes, options).PlayerDiff;

            Assert.IsTrue(actual.HasBalance);
            Assert.AreEqual(1, actual.Level);
            Assert.AreEqual(2, actual.Exp);
            Assert.AreEqual(3, actual.Gold);
            Assert.AreEqual(4, actual.SupportPoints);
            Assert.AreEqual(5, actual.GachaCurrency);
            Assert.AreEqual(1, actual.Idols.Length);
            Assert.AreEqual(10, actual.Idols[0].MasterId);
            Assert.AreEqual("I1", actual.Idols[0].UniqueId);
            Assert.AreEqual(1, actual.SupportCards.Length);
            Assert.AreEqual(5, actual.SupportCards[0].Level);
            Assert.AreEqual("01ARZ3NDEKTSV4RRFFQ69G5FAW", actual.RemovedSupportCardUniqueIds[0]);
        }
    }
}
