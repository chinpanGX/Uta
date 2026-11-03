using NUnit.Framework;
using Uta.Client.Infrastructure.Api;
using Uta.Shared.Contracts.Player;
using VolTech.Client;

namespace Uta.Client.Infrastructure.Tests
{
    public sealed class UtaErrorsTests
    {
        [Test]
        public void Create_ContainsPlayerNotCreatedWithTitleAction()
        {
            var errors = UtaErrors.Create();

            Assert.AreEqual(1, errors.Length);
            Assert.AreEqual(PlayerErrors.NotCreated, errors[0].Code);
            Assert.AreEqual(ErrorAction.Title, errors[0].DefaultAction);
        }

        [Test]
        public void Create_IsAcceptedByErrorPolicyProvider()
        {
            ErrorPolicyProvider provider = null;
            Assert.DoesNotThrow(() => provider = new ErrorPolicyProvider(UtaErrors.Create()));

            Assert.AreEqual(ErrorAction.Title, provider.GetDefaultAction(PlayerErrors.NotCreated));
        }
    }
}
