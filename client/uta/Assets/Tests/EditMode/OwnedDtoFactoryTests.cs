using System.Linq;
using NUnit.Framework;
using Uta.Client.Application.Owned;
using Uta.Client.Infrastructure.Idol;
using Uta.Client.Infrastructure.SupportCard;
using Uta.Client.Presentation.Owned;
using ZLinq;

namespace Uta.Client.Tests
{
    public sealed class OwnedDtoFactoryTests
    {
        private IdolRepository idolRepository;
        private SupportCardRepository supportCardRepository;
        private OwnedDtoFactory ownedDtoFactory;

        [SetUp]
        public void SetUp()
        {
            idolRepository = new IdolRepository();
            supportCardRepository = new SupportCardRepository();
            OwnedFixtures.SetOwned(idolRepository, supportCardRepository);
            var ownedListFactory = new OwnedListFactory(idolRepository, supportCardRepository, OwnedFixtures.CreateMaster());
            ownedDtoFactory = new OwnedDtoFactory(ownedListFactory);
        }

        private static string Rows(OwnedViewDto dto)
        {
            return string.Join("|", dto.Rows.Select(r => r.Title + "/" + r.Detail + "/" + r.Enabled).ToArray());
        }

        [Test]
        public void Create_Browse_IdolTab()
        {
            var dto = ownedDtoFactory.Create(null, OwnedTabKind.Idols, OwnedSortKind.Rarity, true, null, null);

            Assert.AreEqual("所持一覧", dto.Title);
            Assert.IsTrue(dto.TabsVisible);
            Assert.IsFalse(dto.ControlsVisible);
            Assert.IsTrue(dto.FiltersVisible);
            Assert.AreEqual("並び: レア度", dto.SortLabel);
            Assert.AreEqual("降順", dto.OrderLabel);
            Assert.AreEqual("プラン: 全て", dto.PlanLabel);
            Assert.AreEqual("タイプ: 全て", dto.TypeLabel);
            Assert.AreEqual("アイドルA [SSR]/感性　かけら 3/True|アイドルB [R]/理論　かけら 0/True", Rows(dto));
            Assert.AreEqual(1, dto.Rows[0].IdolId);
            Assert.IsNull(dto.Rows[0].SupportCardUid);
        }

        [Test]
        public void Create_IdolSelection_DisabledSuffix()
        {
            var dto = ownedDtoFactory.Create(OwnedSelectionCondition.ForIdol(new[] { 2 }), OwnedTabKind.Idols, OwnedSortKind.Rarity, true, null, null);

            Assert.AreEqual("育成アイドルを選ぶ", dto.Title);
            Assert.IsFalse(dto.TabsVisible);
            Assert.IsFalse(dto.ControlsVisible);
            Assert.IsFalse(dto.FiltersVisible);
            Assert.AreEqual("アイドルA [SSR]/感性　かけら 3/True|アイドルB [R]/理論　かけら 0　（編成済み）/False", Rows(dto));
        }

        [Test]
        public void Create_Material_HidesFilters()
        {
            var dto = ownedDtoFactory.Create(OwnedSelectionCondition.ForLimitBreakMaterial(10, "01A"), OwnedTabKind.SupportCards, OwnedSortKind.Level, false, null, null);

            Assert.AreEqual("素材にするカードを選ぶ", dto.Title);
            Assert.IsFalse(dto.TabsVisible);
            Assert.IsTrue(dto.ControlsVisible);
            Assert.IsFalse(dto.FiltersVisible);
            Assert.AreEqual("並び: Lv", dto.SortLabel);
            Assert.AreEqual("昇順", dto.OrderLabel);
            Assert.AreEqual("サポートA [SSR]/歌唱　共通　Lv 1　解放 0/True", Rows(dto));
            Assert.AreEqual("01D", dto.Rows[0].SupportCardUid);
        }
    }
}
