using System;
using Uta.Shared.Domain.Lesson;
using Xunit;

namespace Uta.Shared.Domain.Tests.Lesson
{
    public sealed class StatusSetTests
    {
        [Fact]
        public void Get_Nothing_ReturnsZero()
        {
            Assert.Equal(0, new StatusSet().Get(StatusKind.Nothing));
        }

        [Fact]
        public void Restore_NonZeroAtIndexZero_Throws()
        {
            var values = new int[StatusSet.KindCount];
            values[0] = 1;
            var skips = new bool[StatusSet.KindCount];
            Assert.Throws<ArgumentException>(() => new StatusSet(values, skips));
        }

        [Fact]
        public void Restore_WrongLength_Throws()
        {
            var values = new int[StatusSet.KindCount - 1];
            var skips = new bool[StatusSet.KindCount - 1];
            Assert.Throws<ArgumentException>(() => new StatusSet(values, skips));
        }
    }
}
