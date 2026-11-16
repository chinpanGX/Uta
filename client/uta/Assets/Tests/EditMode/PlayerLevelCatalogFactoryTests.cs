using System.IO;
using NUnit.Framework;
using Uta.Client.Infrastructure;
using Uta.Shared.Domain.Player;

namespace Uta.Client.Tests
{
    public class PlayerLevelCatalogFactoryTests
    {
        private static byte[] ReadBundled()
        {
            return File.ReadAllBytes(UnityEngine.Application.streamingAssetsPath + "/" + MasterDataLoader.RelativePath);
        }

        private static PlayerLevelCatalog CreateCatalog()
        {
            return PlayerLevelCatalogFactory.Create(MasterDataLoader.Load(ReadBundled()));
        }

        [Test]
        public void Create_Bundled_HasMaxLevelAndMaxExp()
        {
            var catalog = CreateCatalog();

            Assert.AreEqual(100, catalog.MaxLevel);
            Assert.AreEqual(198000, catalog.MaxExp);
        }

        [Test]
        public void GetLevel_Bundled_ReturnsLevelFromExp()
        {
            var catalog = CreateCatalog();

            Assert.AreEqual(1, catalog.GetLevel(0));
            Assert.AreEqual(2, catalog.GetLevel(40));
            Assert.AreEqual(99, catalog.GetLevel(197999));
        }
    }
}
