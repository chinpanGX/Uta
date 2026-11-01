using VolTech.Server.Transaction;

namespace Uta.Server.Application.Player;

public sealed class SignInUseCase(IPlayerRepository players)
{
    public Task<PlayerSnapshot?> ExecuteAsync(TxContext tx, Ulid playerId) => players.FindAsync(tx, playerId);
}
