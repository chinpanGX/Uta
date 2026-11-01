using MagicOnion;
using MagicOnion.Server;
using Uta.Server.Application.Player;
using Uta.Shared.Contracts.Player;
using VolTech.Server;
using VolTech.Server.Abstractions;
using VolTech.Server.Transaction;

// Contracts は Nullable なし（netstandard2.1）、サーバーは Nullable 有効。IService<T> の With* の null 許容性が合わず CS8644 になる
#pragma warning disable CS8644

namespace Uta.Server.Api;

public sealed class PlayerService(ITransactions transactions, SignUpUseCase signUp, SignInUseCase signIn)
    : ServiceBase<IPlayerService>, IPlayerService
{
    public async UnaryResult<SignUpResponse> SignUpAsync()
    {
        var playerId = Context.GetAuthContext().PlayerId;
        return await transactions.RunAsync(Context, async tx => new SignUpResponse(await signUp.ExecuteAsync(tx, playerId)));
    }

    public async UnaryResult<SignInResponse> SignInAsync()
    {
        var playerId = Context.GetAuthContext().PlayerId;
        return await transactions.RunAsync(Context, async tx =>
        {
            var player = await signIn.ExecuteAsync(tx, playerId)
                ?? throw new BusinessException(PlayerErrors.NotCreated, "player not created");
            return new SignInResponse(player.Level, player.Exp, player.Gold, player.SupportPoints, player.GachaCurrency, false, false);
        });
    }
}
