using VolTech.Server.Transaction;

namespace Uta.Server.Application.Player;

public interface IPlayerRepository
{
    /// <summary>行が無ければ作る。作ったら true、既にあれば false。</summary>
    Task<bool> InsertIfAbsentAsync(TxContext tx, Ulid playerId);

    /// <summary>行が無ければ null。</summary>
    Task<PlayerSnapshot?> FindAsync(TxContext tx, Ulid playerId);
}
