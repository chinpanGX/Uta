using Uta.Server.Domain.Gacha;
using Uta.Server.Infrastructure;

namespace Uta.Server.Tests.MasterData;

public class MasterGachaCatalogTests
{
    [Fact]
    public void Catalog_MatchesMasterCsv()
    {
        var catalog = new MasterGachaCatalog(MasterDataLoader.Load());

        var gacha = Assert.Single(catalog.All);
        Assert.Equal(250, gacha.SingleCost);
        Assert.Equal(2500, gacha.TenCost);
        Assert.Equal(200, gacha.CeilingPoints);
        Assert.Equal(10000, gacha.Normal.Weights.Sum());
        Assert.Equal(14, gacha.Normal.Weights.Count);
        Assert.Equal(2200, gacha.Guaranteed.Weights.Sum());
        Assert.Equal(7, gacha.Guaranteed.Weights.Count);
        Assert.DoesNotContain(gacha.Guaranteed.Items, x => x.Kind == GrantedItemKind.Idol && (x.ItemId == 1 || x.ItemId == 2));
        Assert.DoesNotContain(gacha.Guaranteed.Items, x => x.Kind == GrantedItemKind.SupportCard && x.ItemId >= 1 && x.ItemId <= 5);
        Assert.Equal(2, gacha.CeilingItems.Count);
        Assert.Equal((1, GrantedItemKind.Idol, 4), (gacha.CeilingItems[0].Order, gacha.CeilingItems[0].Kind, gacha.CeilingItems[0].ItemId));
        Assert.Equal((2, GrantedItemKind.SupportCard, 9), (gacha.CeilingItems[1].Order, gacha.CeilingItems[1].Kind, gacha.CeilingItems[1].ItemId));
    }

    [Fact]
    public void DuplicateConversion_ReturnsAmountByRarity()
    {
        var catalog = new MasterDuplicateConversionCatalog(MasterDataLoader.Load());

        Assert.Equal(5, catalog.GetAmount(GrantedItemKind.Idol, 1));
        Assert.Equal(25, catalog.GetAmount(GrantedItemKind.Idol, 3));
        Assert.Equal(50, catalog.GetAmount(GrantedItemKind.Idol, 4));
        Assert.Equal(300, catalog.GetAmount(GrantedItemKind.SupportCard, 1));
        Assert.Equal(1000, catalog.GetAmount(GrantedItemKind.SupportCard, 6));
        Assert.Equal(3000, catalog.GetAmount(GrantedItemKind.SupportCard, 9));
    }
}
