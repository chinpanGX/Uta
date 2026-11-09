using NUnit.Framework;
using Uta.Client.Application.Gacha;
using Uta.Client.Infrastructure.Gacha;
using Uta.Shared.Contracts.Gacha;

namespace Uta.Client.Tests
{
    public sealed class GachaMapperTests
    {
        [Test]
        public void ToDrawResult_MapsOutcomeAndSortsBySlot()
        {
            var response = new DrawResponse(
                new[]
                {
                    GachaFixtures.SupportCardResult(2, 2, GachaOutcome.Duplicate, 10),
                    GachaFixtures.IdolResult(1, 1, GachaOutcome.New, 0),
                },
                750,
                47);

            var result = GachaMapper.ToDrawResult(response);

            Assert.AreEqual(DrawResultKind.Succeeded, result.Kind);
            Assert.AreEqual(2, result.Results.Count);
            Assert.AreEqual(1, result.Results[0].Slot);
            Assert.AreEqual(2, result.Results[1].Slot);
            Assert.IsTrue(result.Results[0].IsNew);
            Assert.IsFalse(result.Results[1].IsNew);
            Assert.AreEqual(Uta.MasterData.ItemKind.Idol, result.Results[0].Kind);
            Assert.AreEqual(Uta.MasterData.ItemKind.SupportCard, result.Results[1].Kind);
            Assert.AreEqual(1, result.Results[0].ItemId);
            Assert.AreEqual(2, result.Results[1].ItemId);
            Assert.AreEqual(0, result.Results[0].SupportPoints);
            Assert.AreEqual(10, result.Results[1].SupportPoints);
            Assert.AreEqual(750, result.GachaCurrency);
            Assert.AreEqual(47, result.ExchangePoints);
        }

        [Test]
        public void ToDrawResult_NullResults_ReturnsEmpty()
        {
            var result = GachaMapper.ToDrawResult(new DrawResponse(null, 0, 0));

            Assert.AreEqual(0, result.Results.Count);
        }

        [Test]
        public void ToExchangePoints_FindsRowOrZero()
        {
            var response = new GetGachasResponse(new[] { new GachaStatusDto(2, 5), new GachaStatusDto(1, 37) });

            Assert.AreEqual(37, GachaMapper.ToExchangePoints(response, 1));
            Assert.AreEqual(0, GachaMapper.ToExchangePoints(response, 3));
            Assert.AreEqual(0, GachaMapper.ToExchangePoints(new GetGachasResponse(null), 1));
        }
    }
}
