using Uta.Server.Domain.Player;

namespace Uta.Server.Tests.Domain;

public class PlayerExpRulesTests
{
    [Fact]
    public void AddExp_Normal_AddsGain()
    {
        Assert.Equal(40, PlayerExpRules.AddExp(0, 40, 198000));
    }

    [Fact]
    public void AddExp_OverMax_StopsAtMax()
    {
        Assert.Equal(198000, PlayerExpRules.AddExp(197990, 60, 198000));
    }

    [Fact]
    public void AddExp_AtMax_StaysAtMax()
    {
        Assert.Equal(198000, PlayerExpRules.AddExp(198000, 10, 198000));
    }

    [Fact]
    public void AddExp_AlreadyOverMax_DoesNotReduce()
    {
        Assert.Equal(200000, PlayerExpRules.AddExp(200000, 10, 198000));
    }

    [Fact]
    public void AddExp_ZeroGain_Unchanged()
    {
        Assert.Equal(5, PlayerExpRules.AddExp(5, 0, 198000));
    }

    [Fact]
    public void AddExp_NegativeGain_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PlayerExpRules.AddExp(0, -1, 198000));
    }
}
