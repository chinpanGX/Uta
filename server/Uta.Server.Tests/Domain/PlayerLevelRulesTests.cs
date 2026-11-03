using Uta.Server.Domain.Produce;

namespace Uta.Server.Tests.Domain;

public class PlayerLevelRulesTests
{
    [Fact]
    public void AddExp_NotEnough_StaysSameLevel()
    {
        Assert.Equal((1, 10), PlayerLevelRules.AddExp(1, 0, 10, [40, 80]));
    }

    [Fact]
    public void AddExp_ExactlyReaches_LevelsUpWithZeroExp()
    {
        Assert.Equal((2, 0), PlayerLevelRules.AddExp(1, 30, 10, [40, 80]));
    }

    [Fact]
    public void AddExp_Overflows_CarriesOver()
    {
        Assert.Equal((2, 50), PlayerLevelRules.AddExp(1, 30, 60, [40, 80]));
    }

    [Fact]
    public void AddExp_MultipleLevels()
    {
        Assert.Equal((3, 10), PlayerLevelRules.AddExp(1, 0, 130, [40, 80, 120]));
    }

    [Fact]
    public void AddExp_ReachingMaxLevel_DiscardsOverflow()
    {
        Assert.Equal((3, 0), PlayerLevelRules.AddExp(2, 70, 1000, [40, 80]));
    }

    [Fact]
    public void AddExp_AtMaxLevel_DoesNotIncrease()
    {
        Assert.Equal((3, 0), PlayerLevelRules.AddExp(3, 0, 50, [40, 80]));
    }

    [Fact]
    public void AddExp_ZeroGain_NoChange()
    {
        Assert.Equal((1, 5), PlayerLevelRules.AddExp(1, 5, 0, [40]));
    }
}
