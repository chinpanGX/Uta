using VolTech.Server.Transaction;

namespace Uta.Server.Application.Player;

public sealed class SignUpUseCase(IPlayerRepository players)
{
    public Task<bool> ExecuteAsync(TxContext tx, Ulid playerId) => players.InsertIfAbsentAsync(tx, playerId);
}
