using Uta.Server.Domain.Gacha;
using Uta.Shared.Domain.Rng;

namespace Uta.Server.Tests.Domain;

public class GachaRandomTests
{
    [Fact]
    public void FromSeed_SameSeed_SameSequence()
    {
        var a = GachaRandom.FromSeed(12345UL);
        var b = GachaRandom.FromSeed(12345UL);
        var reference = Xoshiro256StarStar.FromSeed(12345UL);

        for (var i = 0; i < 3; i++)
        {
            var x = a.NextUInt64();
            Assert.Equal(x, b.NextUInt64());
            Assert.Equal(x, reference.NextUInt64());
        }
    }

    [Fact]
    public void CreateSecure_TwoInstances_Differ()
    {
        Assert.NotEqual(GachaRandom.CreateSecure().NextUInt64(), GachaRandom.CreateSecure().NextUInt64());
    }
}
