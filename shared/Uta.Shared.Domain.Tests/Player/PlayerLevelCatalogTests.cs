using System;
using Uta.Shared.Domain.Player;
using Xunit;

namespace Uta.Shared.Domain.Tests.Player
{
    public class PlayerLevelCatalogTests
    {
        private static PlayerLevelCatalog Table()
        {
            return PlayerLevelCatalog.Create(new[] { 0, 40, 120 });
        }

        [Fact]
        public void GetLevel_Boundaries_ReturnsLevel()
        {
            var catalog = Table();
            Assert.Equal(1, catalog.GetLevel(0));
            Assert.Equal(1, catalog.GetLevel(39));
            Assert.Equal(2, catalog.GetLevel(40));
            Assert.Equal(2, catalog.GetLevel(119));
            Assert.Equal(3, catalog.GetLevel(120));
        }

        [Fact]
        public void GetLevel_OverMaxExp_ReturnsMaxLevel()
        {
            var catalog = Table();
            Assert.Equal(3, catalog.GetLevel(121));
            Assert.Equal(3, catalog.GetLevel(int.MaxValue));
        }

        [Fact]
        public void GetLevel_Negative_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Table().GetLevel(-1));
        }

        [Fact]
        public void MaxLevelAndMaxExp_FromTable()
        {
            var catalog = Table();
            Assert.Equal(3, catalog.MaxLevel);
            Assert.Equal(120, catalog.MaxExp);
        }

        [Fact]
        public void Create_Empty_Throws()
        {
            Assert.Throws<ArgumentException>(() => PlayerLevelCatalog.Create(new int[0]));
        }

        [Fact]
        public void Create_FirstNotZero_Throws()
        {
            Assert.Throws<ArgumentException>(() => PlayerLevelCatalog.Create(new[] { 10, 40 }));
        }

        [Fact]
        public void Create_NotStrictlyIncreasing_Throws()
        {
            Assert.Throws<ArgumentException>(() => PlayerLevelCatalog.Create(new[] { 0, 40, 40 }));
        }

        [Fact]
        public void Create_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => PlayerLevelCatalog.Create(null));
        }

        [Fact]
        public void Create_CopiesInput()
        {
            var source = new[] { 0, 40, 120 };
            var catalog = PlayerLevelCatalog.Create(source);
            source[2] = 50;
            Assert.Equal(2, catalog.GetLevel(100));
        }

        [Fact]
        public void Create_SingleLevel()
        {
            var catalog = PlayerLevelCatalog.Create(new[] { 0 });
            Assert.Equal(1, catalog.MaxLevel);
            Assert.Equal(0, catalog.MaxExp);
            Assert.Equal(1, catalog.GetLevel(0));
            Assert.Equal(1, catalog.GetLevel(5));
        }
    }
}
