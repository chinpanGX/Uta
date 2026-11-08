using NUnit.Framework;
using Uta.Client.Infrastructure.Api;
using Uta.Shared.Contracts.Gacha;
using Uta.Shared.Contracts.Player;
using Uta.Shared.Contracts.Produce;
using Uta.Shared.Contracts.Support;
using VolTech.Client;

namespace Uta.Client.Tests
{
    public sealed class ErrorDefinitionsTests
    {
        [Test]
        public void Create_ContainsAllBusinessErrors()
        {
            var errors = ErrorDefinitions.Create();

            Assert.AreEqual(19, errors.Length);
            Assert.AreEqual(PlayerErrors.NotCreated, errors[0].Code);
            Assert.AreEqual(ErrorAction.Title, errors[0].DefaultAction);
            var stay = new[]
            {
                GachaErrors.NotEnoughCurrency, GachaErrors.NotEnoughPoints, GachaErrors.NotFound, GachaErrors.InvalidCount, GachaErrors.CostMismatch,
                SupportErrors.InvalidTargetLevel, SupportErrors.NotEnoughPoints, SupportErrors.MaxLimitBreak, SupportErrors.NotOwned, SupportErrors.InvalidMaterial,
                PlayerErrors.NotEnoughGold,
                ProduceErrors.StaleOperation, ProduceErrors.UnexpectedPhase, ProduceErrors.InvalidArgument, ProduceErrors.InvalidFormation,
                ProduceErrors.NotInProgress, ProduceErrors.SessionDiscarded, ProduceErrors.AlreadyInProgress,
            };
            Assert.AreEqual(18, stay.Length);
            for (var i = 0; i < stay.Length; i++)
            {
                Assert.AreEqual(stay[i], errors[i + 1].Code);
                Assert.AreEqual(ErrorAction.Stay, errors[i + 1].DefaultAction);
            }
        }

        [Test]
        public void Create_IsAcceptedByErrorPolicyProvider()
        {
            ErrorPolicyProvider provider = null;
            Assert.DoesNotThrow(() => provider = new ErrorPolicyProvider(ErrorDefinitions.Create()));

            Assert.AreEqual(ErrorAction.Title, provider.GetDefaultAction(PlayerErrors.NotCreated));
            Assert.AreEqual(ErrorAction.Stay, provider.GetDefaultAction(GachaErrors.NotEnoughCurrency));
        }
    }
}
