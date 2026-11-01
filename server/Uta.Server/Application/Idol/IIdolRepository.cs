using VolTech.Server.Transaction;

namespace Uta.Server.Application.Idol;

public interface IIdolRepository
{
    /// <summary>行が無ければ追加して true。既にあれば false（何もしない）。</summary>
    Task<bool> TryInsertAsync(TxContext tx, Ulid playerId, int idolId);

    Task<bool> ExistsAsync(TxContext tx, Ulid playerId, int idolId);

    Task AddShardsAsync(TxContext tx, Ulid playerId, int idolId, int shards);

    /// <summary>idol_id の昇順。</summary>
    Task<IReadOnlyList<OwnedIdol>> ListAsync(TxContext tx, Ulid playerId);
}
