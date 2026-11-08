using System;
using System.Linq;
using NUnit.Framework;
using Uta.Client.Application.Gacha;
using Uta.Client.Presentation.Gacha;

namespace Uta.Client.Tests
{
    public sealed class GachaResultDtoFactoryTests
    {
        [Test]
        public void Create_BuildsLines()
        {
            var factory = new GachaResultDtoFactory(GachaFixtures.CreateMaster());
            var results = new[]
            {
                new GachaResult(1, Uta.MasterData.ItemKind.Idol, 1, true, 0, 0),
                new GachaResult(2, Uta.MasterData.ItemKind.Idol, 3, false, 5, 0),
                new GachaResult(3, Uta.MasterData.ItemKind.SupportCard, 2, true, 0, 0),
                new GachaResult(4, Uta.MasterData.ItemKind.SupportCard, 4, false, 0, 1000),
            };

            var lines = factory.Create(results);

            CollectionAssert.AreEqual(
                new[]
                {
                    "1. SSR 育成アイドル アイドルA　NEW",
                    "2. R 育成アイドル アイドルC　かけら +5",
                    "3. SSR サポートカード サポートB　所持に追加",
                    "4. SR サポートカード サポートD　サポート強化pt +1,000",
                },
                lines.Select(x => x.Text).ToArray());
            Assert.IsTrue(lines.All(x => !x.IsHeading));
        }

        [Test]
        public void Create_NullResults_Throws()
        {
            var factory = new GachaResultDtoFactory(GachaFixtures.CreateMaster());

            Assert.Throws<ArgumentNullException>(() => factory.Create(null));
        }
    }
}
