using NUnit.Framework;
using Uta.Client.Application.Support;
using Uta.Client.Infrastructure.Support;
using Uta.Shared.Contracts.Support;

namespace Uta.Client.Tests
{
    public sealed class SupportMapperTests
    {
        [Test]
        public void ToLevelUpResult_Maps()
        {
            var result = SupportMapper.ToLevelUpResult(new LevelUpResponse(4, 900, 950));

            Assert.AreEqual(SupportResultKind.Succeeded, result.Kind);
            Assert.AreEqual(4, result.Level);
            Assert.AreEqual(900, result.SupportPoints);
            Assert.AreEqual(950, result.Gold);
        }

        [Test]
        public void ToLimitBreakResult_Maps()
        {
            var result = SupportMapper.ToLimitBreakResult(new LimitBreakResponse(1, 45));

            Assert.AreEqual(SupportResultKind.Succeeded, result.Kind);
            Assert.AreEqual(1, result.LimitBreak);
            Assert.AreEqual(45, result.MaxLevel);
        }
    }
}
