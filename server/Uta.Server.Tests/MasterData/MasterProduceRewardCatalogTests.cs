using Uta.Server.Application.Produce;
using Uta.Server.Infrastructure;
using Uta.Shared.Domain.Produce;

namespace Uta.Server.Tests.MasterData;

public class MasterProduceRewardCatalogTests
{
    private static readonly MasterProduceRewardCatalog Catalog = new(MasterDataLoader.Load());

    [Fact]
    public void Get_F_ReturnsRow()
    {
        Assert.Equal(new ProduceReward(50, 1000, 500, 10), Catalog.Get(ProduceRank.F));
    }

    [Fact]
    public void Get_SSPlus_ReturnsRow()
    {
        Assert.Equal(new ProduceReward(800, 20000, 10000, 60), Catalog.Get(ProduceRank.SSPlus));
    }

    [Fact]
    public void Get_AllThirteenRanks_Found()
    {
        for (var rank = ProduceRank.F; rank <= ProduceRank.SSPlus; rank++)
        {
            Assert.NotNull(Catalog.Get(rank));
        }
    }

    [Fact]
    public void Get_Nothing_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Catalog.Get(ProduceRank.Nothing));
    }

    [Fact]
    public void PlayerLevelCatalog_RequiredExps_FromMaster()
    {
        var levels = new MasterPlayerLevelCatalog(MasterDataLoader.Load());

        Assert.Equal(99, levels.RequiredExps.Count);
        Assert.Equal(40, levels.RequiredExps[0]);
        Assert.Equal(3960, levels.RequiredExps[^1]);
    }
}
