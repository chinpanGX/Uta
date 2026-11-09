using NUnit.Framework;
using Uta.Client.Application.Owned;
using Uta.Client.Application.Support;
using Uta.Client.Infrastructure.Player;
using Uta.Client.Infrastructure.SupportCard;
using Uta.Client.Infrastructure.Idol;
using Uta.Client.Presentation.Owned;
using Uta.Shared.Domain.Produce;

namespace Uta.Client.Tests
{
    public sealed class SupportDetailDtoFactoryTests
    {
        private PlayerRepository playerRepository;
        private SupportCardRepository supportCardRepository;
        private SupportDetailDtoFactory supportDetailDtoFactory;

        [SetUp]
        public void SetUp()
        {
            playerRepository = new PlayerRepository();
            supportCardRepository = new SupportCardRepository();
            OwnedFixtures.SetPlayer(playerRepository);
            OwnedFixtures.SetOwned(new IdolRepository(), supportCardRepository);
            var master = OwnedFixtures.CreateMaster();
            supportDetailDtoFactory = new SupportDetailDtoFactory(playerRepository, supportCardRepository, master, new SupportLevelPlanner(master),
                new OwnedListFactory(new IdolRepository(), supportCardRepository, master));
        }

        [Test]
        public void Create_Default_MatchesBodyStrings()
        {
            var dto = supportDetailDtoFactory.Create("01A");

            Assert.IsTrue(dto.Exists);
            Assert.AreEqual(10, dto.SupportCardId);
            Assert.AreEqual("サポートA|SSR　歌唱　共通|Lv 3 / 40|上限解放 0 / 4", string.Join("|", dto.InfoLines));
            Assert.AreEqual("1 初期歌唱 +" + ProduceFormulas.AbilityValue(10, 50, 3, 60), string.Join("|", dto.AbilityLines));
            Assert.AreEqual("強化pt 1,000　ゴールド 1,000", dto.ResourcesText);
            Assert.AreEqual(3, dto.LevelSteps.Count);
            Assert.AreEqual("+1 → Lv 4（強化pt 100 / G 50）", dto.LevelSteps[0].Label);
            Assert.AreEqual("+5 → Lv 8（強化pt 500 / G 250）", dto.LevelSteps[1].Label);
            Assert.AreEqual("最大 → Lv 13（強化pt 1,000 / G 500）", dto.LevelSteps[2].Label);
            Assert.IsTrue(dto.LevelSteps[0].Enabled);
            Assert.IsTrue(dto.LevelSteps[1].Enabled);
            Assert.IsTrue(dto.LevelSteps[2].Enabled);
            Assert.AreEqual(4, dto.LevelSteps[0].TargetLevel);
            Assert.AreEqual("Lv 3 → Lv 4 に上げます。\n強化pt 100・ゴールド 50 を使います。", dto.LevelSteps[0].ConfirmText);
            Assert.AreEqual("上限解放（素材を選ぶ）", dto.LimitBreakLabel);
            Assert.IsTrue(dto.LimitBreakEnabled);
            Assert.IsTrue(dto.HasMaterial);
        }

        [Test]
        public void Create_CardGone_NotExists()
        {
            var dto = supportDetailDtoFactory.Create("01Z");

            Assert.IsFalse(dto.Exists);
            Assert.AreEqual(0, dto.SupportCardId);
            Assert.AreEqual("このカードは所持していません。", string.Join("|", dto.InfoLines));
            Assert.AreEqual(0, dto.AbilityLines.Count);
            Assert.AreEqual("", dto.ResourcesText);
            Assert.AreEqual("+1", dto.LevelSteps[0].Label);
            Assert.AreEqual("+5", dto.LevelSteps[1].Label);
            Assert.AreEqual("最大", dto.LevelSteps[2].Label);
            Assert.IsFalse(dto.LevelSteps[0].Enabled);
            Assert.IsFalse(dto.LevelSteps[1].Enabled);
            Assert.IsFalse(dto.LevelSteps[2].Enabled);
            Assert.AreEqual("上限解放", dto.LimitBreakLabel);
            Assert.IsFalse(dto.LimitBreakEnabled);
            Assert.IsFalse(dto.HasMaterial);
        }

        [Test]
        public void Create_NoMaterial_HasMaterialFalse()
        {
            var dto = supportDetailDtoFactory.Create("01B");

            Assert.IsTrue(dto.Exists);
            Assert.IsFalse(dto.HasMaterial);
        }

        [Test]
        public void CreateLimitBreakConfirmText_Formats()
        {
            var text = supportDetailDtoFactory.CreateLimitBreakConfirmText("01A", "01D");

            Assert.AreEqual("Lv 1 の「サポートA」を素材にして上限解放します（0 → 1）。\n素材は無くなります。強化に使った強化pt・ゴールドは戻りません。", text);
        }

        [Test]
        public void CreateLimitBreakConfirmText_MaterialGone_Null()
        {
            Assert.IsNull(supportDetailDtoFactory.CreateLimitBreakConfirmText("01A", "zz"));
        }
    }
}
