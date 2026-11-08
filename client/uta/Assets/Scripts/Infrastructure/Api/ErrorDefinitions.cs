using Uta.Shared.Contracts.Gacha;
using Uta.Shared.Contracts.Player;
using Uta.Shared.Contracts.Produce;
using Uta.Shared.Contracts.Support;
using VolTech.Client;

namespace Uta.Client.Infrastructure.Api
{
    public static class ErrorDefinitions
    {
        /// <summary>アプリの業務エラーの一覧。呼ぶたびに新しい配列を返す（static の可変な配列を持たない）。</summary>
        public static ErrorDefinition[] Create()
        {
            return new[]
            {
                new ErrorDefinition(PlayerErrors.NotCreated, ErrorAction.Title),
                new ErrorDefinition(GachaErrors.NotEnoughCurrency, ErrorAction.Stay),
                new ErrorDefinition(GachaErrors.NotEnoughPoints, ErrorAction.Stay),
                new ErrorDefinition(GachaErrors.NotFound, ErrorAction.Stay),
                new ErrorDefinition(GachaErrors.InvalidCount, ErrorAction.Stay),
                new ErrorDefinition(GachaErrors.CostMismatch, ErrorAction.Stay),
                new ErrorDefinition(SupportErrors.InvalidTargetLevel, ErrorAction.Stay),
                new ErrorDefinition(SupportErrors.NotEnoughPoints, ErrorAction.Stay),
                new ErrorDefinition(SupportErrors.MaxLimitBreak, ErrorAction.Stay),
                new ErrorDefinition(SupportErrors.NotOwned, ErrorAction.Stay),
                new ErrorDefinition(SupportErrors.InvalidMaterial, ErrorAction.Stay),
                new ErrorDefinition(PlayerErrors.NotEnoughGold, ErrorAction.Stay),
                new ErrorDefinition(ProduceErrors.StaleOperation, ErrorAction.Stay),
                new ErrorDefinition(ProduceErrors.UnexpectedPhase, ErrorAction.Stay),
                new ErrorDefinition(ProduceErrors.InvalidArgument, ErrorAction.Stay),
                new ErrorDefinition(ProduceErrors.InvalidFormation, ErrorAction.Stay),
                new ErrorDefinition(ProduceErrors.NotInProgress, ErrorAction.Stay),
                new ErrorDefinition(ProduceErrors.SessionDiscarded, ErrorAction.Stay),
                new ErrorDefinition(ProduceErrors.AlreadyInProgress, ErrorAction.Stay),
            };
        }
    }
}
