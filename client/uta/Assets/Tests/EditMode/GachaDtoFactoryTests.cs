using System.Linq;
using NUnit.Framework;
using Uta.Client.Infrastructure.Player;
using Uta.Client.Presentation.Gacha;
using ZLinq;

namespace Uta.Client.Tests
{
    public sealed class GachaDtoFactoryTests
    {
        private PlayerRepository playerRepository;

        [SetUp]
        public void SetUp()
        {
            playerRepository = new PlayerRepository();
            playerRepository.Upsert(GachaFixtures.Player(GachaFixtures.Currency));
        }

        private GachaDtoFactory CreateFactory(Uta.MasterData.MemoryDatabase memoryDatabase = null)
        {
            return new GachaDtoFactory(playerRepository, memoryDatabase ?? GachaFixtures.CreateMaster());
        }

        [Test]
        public void Create_FormatsTexts()
        {
            var dto = CreateFactory().Create(37);

            Assert.AreEqual(1, dto.GachaId);
            Assert.AreEqual("ガチャ通貨 3,250", dto.CurrencyText);
            Assert.AreEqual("交換pt 37 / 200", dto.ExchangeText);
            Assert.AreEqual("単発 250", dto.SingleLabel);
            Assert.AreEqual("10連 2,500", dto.TenLabel);
            Assert.AreEqual("SSR 育成アイドル: アイドルA\nSSR サポートカード: サポートB", dto.LineupText);
        }

        [Test]
        public void Create_NullPoints_ShowsDash()
        {
            var dto = CreateFactory().Create(null);

            Assert.AreEqual("交換pt - / 200", dto.ExchangeText);
        }

        [Test]
        public void Create_LineupDistinctAndNoSsr()
        {
            var entries = GachaFixtures.NormalEntries();
            entries.Add(GachaFixtures.IdolEntry(10, 4, 1, 1));
            var distinct = GachaFixtures.Build(GachaFixtures.Idols(), GachaFixtures.SupportCards(), entries);

            Assert.AreEqual("SSR 育成アイドル: アイドルA\nSSR サポートカード: サポートB", CreateFactory(distinct).Create(0).LineupText);

            var noSsr = GachaFixtures.Build(
                GachaFixtures.Idols(),
                GachaFixtures.SupportCards(),
                new[] { GachaFixtures.IdolEntry(10, 1, 3, 1) });

            Assert.AreEqual(string.Empty, CreateFactory(noSsr).Create(0).LineupText);
        }

        [Test]
        public void Create_NoPlayer_ZeroCurrency()
        {
            var factory = new GachaDtoFactory(new PlayerRepository(), GachaFixtures.CreateMaster());

            Assert.AreEqual("ガチャ通貨 0", factory.Create(0).CurrencyText);
        }

        [Test]
        public void CreateDrawConfirmText_UsesCostAndCurrency()
        {
            var factory = CreateFactory();

            Assert.AreEqual("ガチャ通貨 2,500 を使って 10 回引きます。（所持 3,250）", factory.CreateDrawConfirmText(10));
            Assert.AreEqual("ガチャ通貨 250 を使って 1 回引きます。（所持 3,250）", factory.CreateDrawConfirmText(1));
        }
    }
}
