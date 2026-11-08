using NUnit.Framework;
using Uta.Client.Application.Idol;
using Uta.Client.Application.Player;
using Uta.Client.Domain.Idol;
using Uta.Client.Domain.Player;
using Uta.Client.Infrastructure.Idol;
using Uta.Client.Infrastructure.Player;
using Uta.Client.Presentation.Home;
using Uta.MasterData;

namespace Uta.Client.Tests
{
    public sealed class HomeDtoFactoryTests
    {
        private PlayerRepository players;
        private IdolRepository idols;
        private DiscardNoticeState notice;

        [SetUp]
        public void SetUp()
        {
            players = new PlayerRepository();
            idols = new IdolRepository();
            notice = new DiscardNoticeState();
        }

        private HomeDtoFactory Create(MemoryDatabase db)
        {
            return new HomeDtoFactory(players, new FeaturedIdolPicker(idols, db), notice, db);
        }

        private static PlayerEntity Player(int level = 1, int gachaCurrency = 0, bool hasActiveProduce = false, bool discardedProduce = false)
        {
            return new PlayerEntity("P1", level, 0, 0, 0, gachaCurrency, hasActiveProduce, discardedProduce);
        }

        [Test]
        public void Create_NoPlayer_ReturnsNull()
        {
            var factory = Create(TestMasterData.Build(_ => { }));

            Assert.IsNull(factory.Create());
        }

        [Test]
        public void Create_FormatsProfileAndResume()
        {
            players.Upsert(Player(12, 3250, true));
            var factory = Create(TestMasterData.Build(_ => { }));

            var dto = factory.Create();

            Assert.AreEqual("プレイヤーLv 12　ガチャ通貨 3,250", dto.ProfileText);
            Assert.IsTrue(dto.ResumeVisible);
            Assert.AreEqual("再開", dto.PrimaryLabel);
        }

        [Test]
        public void Create_NoActiveProduce_ShowsProduceLabel()
        {
            players.Upsert(Player());
            var factory = Create(TestMasterData.Build(_ => { }));

            var dto = factory.Create();

            Assert.IsFalse(dto.ResumeVisible);
            Assert.AreEqual("プロデュース", dto.PrimaryLabel);
        }

        [Test]
        public void Create_StageFromPicker()
        {
            players.Upsert(Player());
            var db = TestMasterData.Build(b => b.Append(new[]
            {
                TestMasterData.Idol(1, "a", Rarity.SR),
                TestMasterData.Idol(2, "テストアイドル", Rarity.SSR),
            }));
            idols.Upsert(new IdolEntity("U1", 1, 0, 0, 0));
            idols.Upsert(new IdolEntity("U2", 2, 0, 0, 0));

            var dto = Create(db).Create();

            Assert.AreEqual(2, dto.StageIdolId);
            Assert.AreEqual("テストアイドル", dto.StageIdolName);
        }

        [Test]
        public void Create_NoIdols_StageZero()
        {
            players.Upsert(Player());
            var factory = Create(TestMasterData.Build(_ => { }));

            var dto = factory.Create();

            Assert.AreEqual(0, dto.StageIdolId);
            Assert.IsNull(dto.StageIdolName);
        }

        [Test]
        public void Create_ShowDiscardNotice_WhenDiscardedAndNotShown()
        {
            players.Upsert(Player(discardedProduce: true));
            var factory = Create(TestMasterData.Build(_ => { }));

            Assert.IsTrue(factory.Create().ShowDiscardNotice);
        }

        [Test]
        public void Create_HidesDiscardNotice_AfterShown()
        {
            players.Upsert(Player(discardedProduce: true));
            notice.MarkShown();
            var factory = Create(TestMasterData.Build(_ => { }));

            Assert.IsFalse(factory.Create().ShowDiscardNotice);
        }

        [Test]
        public void Create_NotDiscarded_HidesDiscardNotice()
        {
            players.Upsert(Player());
            var factory = Create(TestMasterData.Build(_ => { }));

            Assert.IsFalse(factory.Create().ShowDiscardNotice);
        }
    }
}
