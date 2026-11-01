using Uta.Server.Application.Idol;
using Uta.Server.Application.Support;
using VolTech.Server.Transaction;

namespace Uta.Server.Application.Player;

public sealed class SignInUseCase(IPlayerRepository players, IIdolRepository idols, ISupportCardRepository supportCards)
{
    /// <summary>行が無ければ null。</summary>
    public async Task<SignInResult?> ExecuteAsync(TxContext tx, Ulid playerId)
    {
        var player = await players.FindAsync(tx, playerId);
        if (player is null) return null;
        return new SignInResult(player, await idols.ListAsync(tx, playerId), await supportCards.ListAsync(tx, playerId));
    }
}
