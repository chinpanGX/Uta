using Uta.Server.Domain.Gacha;
using ContractsItemKind = Uta.Shared.Contracts.Gacha.ItemKind;
using MasterItemKind = Uta.MasterData.ItemKind;
using GachaOutcome = Uta.Shared.Contracts.Gacha.GachaOutcome;

namespace Uta.Server.Tests.MasterData;

public class ContractsEnumsMatchTests
{
    [Fact]
    public void ItemKind_MatchesMasterAndApplication()
    {
        var contracts = Enum.GetNames<ContractsItemKind>().ToDictionary(x => x, x => (int)Enum.Parse<ContractsItemKind>(x));
        var master = Enum.GetNames<MasterItemKind>().ToDictionary(x => x, x => (int)Enum.Parse<MasterItemKind>(x));
        Assert.Equal(master, contracts);

        Assert.Equal((int)MasterItemKind.Idol, (int)GrantedItemKind.Idol);
        Assert.Equal((int)MasterItemKind.SupportCard, (int)GrantedItemKind.SupportCard);

        Assert.Equal((int)GachaOutcome.New, (int)GrantOutcome.New);
        Assert.Equal((int)GachaOutcome.Duplicate, (int)GrantOutcome.Duplicate);
    }
}
