using System.Linq;
using NUnit.Framework;
using Uta.Client.Application.Gacha;
using Uta.Client.Presentation.Gacha;
using ZLinq;

namespace Uta.Client.Tests
{
    public sealed class GachaRatesDtoFactoryTests
    {
        [Test]
        public void Create_BuildsGroupsThenItems()
        {
            var master = GachaFixtures.CreateMaster();
            var factory = new GachaRatesDtoFactory(master, new GachaRateCalculator(master));

            var lines = factory.Create(GachaFixtures.GachaId);

            var expected = new[]
            {
                ("通常枠（単発・10連の1〜9枠目）", true),
                ("SSR 育成アイドル 10.00%", false),
                ("SSR サポートカード 10.00%", false),
                ("R 育成アイドル 80.00%", false),
                ("通常枠の内訳", true),
                ("SSR 育成アイドル アイドルA 10.00%", false),
                ("SSR サポートカード サポートB 10.00%", false),
                ("R 育成アイドル アイドルC 80.00%", false),
                ("確定枠（10連の10枠目）", true),
                ("SSR 育成アイドル 25.00%", false),
                ("SR サポートカード 75.00%", false),
                ("確定枠の内訳", true),
                ("SSR 育成アイドル アイドルA 25.00%", false),
                ("SR サポートカード サポートD 75.00%", false),
            };
            CollectionAssert.AreEqual(
                expected.Select(x => x.Item1).ToArray(),
                lines.Select(x => x.Text).ToArray());
            CollectionAssert.AreEqual(
                expected.Select(x => x.Item2).ToArray(),
                lines.Select(x => x.IsHeading).ToArray());
        }
    }
}
