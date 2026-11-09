using NUnit.Framework;
using Uta.Client.Application.Idol;
using Uta.Client.Domain.Idol;
using Uta.Client.Infrastructure.Idol;
using Uta.MasterData;

namespace Uta.Client.Tests
{
    public sealed class FeaturedIdolPickerTests
    {
        private static IdolRepository Owned(params int[] ids)
        {
            var repository = new IdolRepository();
            foreach (var id in ids)
            {
                repository.Upsert(new IdolEntity("U" + id, id, 0, 0, 0));
            }

            return repository;
        }

        [Test]
        public void Pick_ReturnsHighestRarityThenSmallestId()
        {
            var db = TestMasterData.Build(b => b.Append(new[]
            {
                TestMasterData.Idol(1, "a", Rarity.SR),
                TestMasterData.Idol(2, "b", Rarity.SSR),
                TestMasterData.Idol(3, "c", Rarity.SSR),
                TestMasterData.Idol(4, "d", Rarity.R),
            }));
            var picker = new FeaturedIdolPicker(Owned(4, 3, 1, 2), db);

            Assert.AreEqual(2, picker.Pick());
        }

        [Test]
        public void Pick_SingleIdol_ReturnsIt()
        {
            var db = TestMasterData.Build(b => b.Append(new[] { TestMasterData.Idol(5, "e", Rarity.R) }));
            var picker = new FeaturedIdolPicker(Owned(5), db);

            Assert.AreEqual(5, picker.Pick());
        }

        [Test]
        public void Pick_NoIdols_ReturnsZero()
        {
            var db = TestMasterData.Build(_ => { });
            var picker = new FeaturedIdolPicker(Owned(), db);

            Assert.AreEqual(0, picker.Pick());
        }
    }
}
