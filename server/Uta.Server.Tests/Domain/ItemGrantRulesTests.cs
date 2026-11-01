using Uta.Server.Domain.Gacha;

namespace Uta.Server.Tests.Domain;

public class ItemGrantRulesTests
{
    [Fact]
    public void Decide_ByKindAndState()
    {
        Assert.Equal(GrantOutcome.New, ItemGrantRules.Decide(GrantedItemKind.Idol, false, false));
        Assert.Equal(GrantOutcome.Duplicate, ItemGrantRules.Decide(GrantedItemKind.Idol, true, false));
        Assert.Equal(GrantOutcome.New, ItemGrantRules.Decide(GrantedItemKind.SupportCard, false, false));
        Assert.Equal(GrantOutcome.Duplicate, ItemGrantRules.Decide(GrantedItemKind.SupportCard, false, true));
    }
}
