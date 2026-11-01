using Uta.Server.Domain.Gacha;
using Uta.Shared.Domain.Rng;

namespace Uta.Server.Tests.Domain;

public class GachaPickerTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1559, 0)]
    [InlineData(1560, 1)]
    [InlineData(3119, 1)]
    [InlineData(3120, 2)]
    [InlineData(3799, 2)]
    [InlineData(3800, 3)]
    [InlineData(3999, 3)]
    public void Pick_BoundaryValues_SelectExpectedRows(int r, int expected)
    {
        var weights = new[] { 1560, 1560, 680, 200 };

        var index = GachaPicker.Pick(new FixedRandom((ulong)(r + 4000)), weights);

        Assert.Equal(expected, index);
    }

    [Fact]
    public void Pick_SingleRow_AlwaysZero()
    {
        var index = GachaPicker.Pick(new FixedRandom(5), new[] { 5 });

        Assert.Equal(0, index);
    }

    private sealed class FixedRandom(ulong value) : IRandom
    {
        public ulong NextUInt64() => value;
    }
}
