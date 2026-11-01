using MagicOnion.Server;
using Uta.Server.Application.Common;
using Uta.Shared.Contracts.Gacha;
using Uta.Shared.Contracts.Player;
using VolTech.Server;
using VolTech.Server.Abstractions;

namespace Uta.Server.Api;

/// <summary>IPlayerService 以外の Uta のサービスの実装クラスに付ける。プレイヤー未作成を PLAYER_NOT_CREATED で弾き、UseCaseException を BusinessException に変換する。</summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class UtaServiceAttribute : MagicOnionFilterAttribute
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

    private static ErrorCode ToErrorCode(UseCaseError error) => error switch
    {
        UseCaseError.GachaInvalidCount => GachaErrors.InvalidCount,
        UseCaseError.GachaNotFound => GachaErrors.NotFound,
        UseCaseError.GachaCostMismatch => GachaErrors.CostMismatch,
        UseCaseError.GachaNotEnoughCurrency => GachaErrors.NotEnoughCurrency,
        UseCaseError.GachaNotEnoughPoints => GachaErrors.NotEnoughPoints,
        _ => throw new ArgumentOutOfRangeException(nameof(error), error, null),
    };
}
