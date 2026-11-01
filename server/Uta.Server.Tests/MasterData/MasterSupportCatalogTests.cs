using Uta.Server.Application.Support;
using Uta.Server.Infrastructure;

namespace Uta.Server.Tests.MasterData;

public class MasterSupportCatalogTests
{
    [Fact]
    public void GetMaxLevel_ReturnsCapByRarityAndLimitBreak()
    {
        var catalog = new MasterSupportCatalog(MasterDataLoader.Load());

        Assert.Equal(20, catalog.GetMaxLevel(1, 0));
        Assert.Equal(40, catalog.GetMaxLevel(1, 4));
        Assert.Equal(30, catalog.GetMaxLevel(6, 0));
        Assert.Equal(60, catalog.GetMaxLevel(9, 4));
    }

    [Fact]
    public void GetLevelUpCost_SumsRows()
    {
        var catalog = new MasterSupportCatalog(MasterDataLoader.Load());

        Assert.Equal(new LevelUpCost(39, 39), catalog.GetLevelUpCost(1, 1, 2));
        Assert.Equal(new LevelUpCost(39 + 78, 39 + 78), catalog.GetLevelUpCost(1, 1, 3));
        Assert.Equal(new LevelUpCost(117, 117), catalog.GetLevelUpCost(9, 1, 2));
    }
}
