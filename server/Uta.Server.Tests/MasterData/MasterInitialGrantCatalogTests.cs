using Uta.Server.Infrastructure;

namespace Uta.Server.Tests.MasterData;

public class MasterInitialGrantCatalogTests
{
    [Fact]
    public void Get_ReturnsInitialValues()
    {
        var grant = new MasterInitialGrantCatalog(MasterDataLoader.Load()).Get();

        Assert.Equal(2500, grant.GachaCurrency);
        Assert.Equal(10000, grant.Gold);
        Assert.Equal(10000, grant.SupportPoints);
        Assert.Equal(new[] { 1, 2 }, grant.IdolIds.ToArray());
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, grant.SupportCardIds.ToArray());
    }
}
