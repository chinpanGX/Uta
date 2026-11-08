using NUnit.Framework;
using Uta.Client.Application.Player;

namespace Uta.Client.Tests
{
    public sealed class DiscardNoticeStateTests
    {
        [Test]
        public void MarkShown_SetsFlag()
        {
            var state = new DiscardNoticeState();
            Assert.IsFalse(state.Shown);

            state.MarkShown();

            Assert.IsTrue(state.Shown);
        }

        [Test]
        public void Reset_ClearsFlag()
        {
            var state = new DiscardNoticeState();
            state.MarkShown();

            state.Reset();

            Assert.IsFalse(state.Shown);
        }
    }
}
