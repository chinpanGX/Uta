using MagicOnion.Server;
using Uta.Server.Application.Common;
using Uta.Shared.Contracts.Gacha;
using Uta.Shared.Contracts.Player;
using Uta.Shared.Contracts.Produce;
using Uta.Shared.Contracts.Support;
using VolTech.Server;
using VolTech.Server.Abstractions;

namespace Uta.Server.Api;

/// <summary>IPlayerService 以外のサービスの実装クラスと、PlayerService.SignInAsync のメソッドに付ける。プレイヤー未作成を PLAYER_NOT_CREATED で弾き、UseCaseException を BusinessException に変換する。</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class GameServiceFilterAttribute : MagicOnionFilterAttribute
{
    public override async ValueTask Invoke(ServiceContext context, Func<ServiceContext, ValueTask> next)
    {
        if (context.FindAuthContext() is { PlayerCreated: false })
        {
            throw new BusinessException(PlayerErrors.NotCreated, "player not created");
        }

        try
        {
            await next(context);
        }
        catch (UseCaseException exception)
        {
            throw new BusinessException(ToErrorCode(exception.Error), exception.Message);
        }
    }

    internal static ErrorCode ToErrorCode(UseCaseError error) => error switch
    {
        UseCaseError.GachaInvalidCount => GachaErrors.InvalidCount,
        UseCaseError.GachaNotFound => GachaErrors.NotFound,
        UseCaseError.GachaCostMismatch => GachaErrors.CostMismatch,
        UseCaseError.GachaNotEnoughCurrency => GachaErrors.NotEnoughCurrency,
        UseCaseError.GachaNotEnoughPoints => GachaErrors.NotEnoughPoints,
        UseCaseError.SupportInvalidTargetLevel => SupportErrors.InvalidTargetLevel,
        UseCaseError.SupportNotEnoughPoints => SupportErrors.NotEnoughPoints,
        UseCaseError.SupportMaxLimitBreak => SupportErrors.MaxLimitBreak,
        UseCaseError.SupportNotOwned => SupportErrors.NotOwned,
        UseCaseError.SupportInvalidMaterial => SupportErrors.InvalidMaterial,
        UseCaseError.PlayerNotEnoughGold => PlayerErrors.NotEnoughGold,
        UseCaseError.ProduceStaleOperation => ProduceErrors.StaleOperation,
        UseCaseError.ProduceUnexpectedPhase => ProduceErrors.UnexpectedPhase,
        UseCaseError.ProduceInvalidArgument => ProduceErrors.InvalidArgument,
        UseCaseError.ProduceInvalidFormation => ProduceErrors.InvalidFormation,
        UseCaseError.ProduceNotInProgress => ProduceErrors.NotInProgress,
        UseCaseError.ProduceSessionDiscarded => ProduceErrors.SessionDiscarded,
        UseCaseError.ProduceAlreadyInProgress => ProduceErrors.AlreadyInProgress,
        _ => throw new ArgumentOutOfRangeException(nameof(error), error, null),
    };
}
