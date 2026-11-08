using System.Linq;
using NUnit.Framework;
using Uta.Client.Application.Gacha;
using Uta.MasterData;

namespace Uta.Client.Tests
{
    public sealed class GachaRateCalculatorTests
    {
        [Test]
        public void Calculate_ItemsToBasisPoints()
        {
            var calculator = new GachaRateCalculator(GachaFixtures.CreateMaster());

            var result = calculator.Calculate(10);

            CollectionAssert.AreEqual(new[] { 1000, 1000, 8000 }, result.Items.Select(i => i.BasisPoints).ToArray());
        }

        [Test]
        public void Calculate_GroupsSumWeightsBeforeTruncating()
        {
            var idols = GachaFixtures.Idols();
            idols.Add(TestMasterData.Idol(5, "アイドルE", Rarity.SSR));
            var master = GachaFixtures.Build(
                idols,
                GachaFixtures.SupportCards(),
                new[]
                {
                    GachaFixtures.IdolEntry(20, 1, 1, 1),
                    GachaFixtures.IdolEntry(20, 2, 5, 1),
                    GachaFixtures.IdolEntry(20, 3, 3, 5),
                });
            var calculator = new GachaRateCalculator(master);

            var result = calculator.Calculate(20);

            var group = result.Groups.Single(g => g.Rarity == Rarity.SSR && g.Kind == ItemKind.Idol);
            Assert.AreEqual(2857, group.BasisPoints);
        }

        [Test]
        public void Calculate_SortedByRarityDescThenKindThenId()
        {
            var calculator = new GachaRateCalculator(GachaFixtures.CreateMaster());

            var normal = calculator.Calculate(10);
            var guaranteed = calculator.Calculate(11);

            Assert.AreEqual(3, normal.Groups.Count);
            Assert.AreEqual((Rarity.SSR, ItemKind.Idol), (normal.Groups[0].Rarity, normal.Groups[0].Kind));
            Assert.AreEqual((Rarity.SSR, ItemKind.SupportCard), (normal.Groups[1].Rarity, normal.Groups[1].Kind));
            Assert.AreEqual((Rarity.R, ItemKind.Idol), (normal.Groups[2].Rarity, normal.Groups[2].Kind));
            CollectionAssert.AreEqual(new[] { 1, 4 }, guaranteed.Items.Select(i => i.ItemId).ToArray());
        }

        [Test]
        public void Calculate_ZeroWeightItem_Included()
        {
            var master = GachaFixtures.Build(
                GachaFixtures.Idols(),
                GachaFixtures.SupportCards(),
                new[] { GachaFixtures.IdolEntry(20, 1, 1, 0), GachaFixtures.IdolEntry(20, 2, 3, 4) });
            var calculator = new GachaRateCalculator(master);

            var result = calculator.Calculate(20);

            Assert.AreEqual(2, result.Items.Count);
            Assert.AreEqual(0, result.Items.Single(i => i.ItemId == 1).BasisPoints);
        }

        [Test]
        public void Calculate_EmptyOrZeroTotal_ReturnsEmpty()
        {
            var missing = new GachaRateCalculator(GachaFixtures.CreateMaster()).Calculate(99);
            var zeroMaster = GachaFixtures.Build(
                GachaFixtures.Idols(),
                GachaFixtures.SupportCards(),
                new[] { GachaFixtures.IdolEntry(20, 1, 1, 0) });
            var zero = new GachaRateCalculator(zeroMaster).Calculate(20);

            Assert.AreEqual(0, missing.Groups.Count);
            Assert.AreEqual(0, missing.Items.Count);
            Assert.AreEqual(0, zero.Groups.Count);
            Assert.AreEqual(0, zero.Items.Count);
        }

        [Test]
        public void Calculate_UnknownId_Throws()
        {
            var master = GachaFixtures.Build(
                GachaFixtures.Idols(),
                GachaFixtures.SupportCards(),
                new[] { GachaFixtures.IdolEntry(20, 1, 99, 1) });
            var calculator = new GachaRateCalculator(master);

            Assert.Catch(() => calculator.Calculate(20));
        }
    }
}
