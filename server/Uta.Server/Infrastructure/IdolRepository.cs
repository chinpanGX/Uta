using Dapper;
using Uta.Server.Application.Idol;
using VolTech.Server.Transaction;

namespace Uta.Server.Infrastructure;

public sealed class IdolRepository : IIdolRepository
{
    public async Task<bool> TryInsertAsync(TxContext tx, Ulid playerId, int idolId)
    {
        var affected = await tx.Connection.ExecuteAsync(new CommandDefinition(
            "INSERT INTO player_idols (player_id, idol_id) VALUES (@playerId, @idolId) ON CONFLICT (player_id, idol_id) DO NOTHING",
            new { playerId = playerId.ToString(), idolId },
            tx.Transaction,
            cancellationToken: tx.CancellationToken));
        return affected == 1;
    }

    public async Task<bool> ExistsAsync(TxContext tx, Ulid playerId, int idolId)
    {
        return await tx.Connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            "SELECT EXISTS (SELECT 1 FROM player_idols WHERE player_id = @playerId AND idol_id = @idolId)",
            new { playerId = playerId.ToString(), idolId },
            tx.Transaction,
            cancellationToken: tx.CancellationToken));
    }

    public async Task AddShardsAsync(TxContext tx, Ulid playerId, int idolId, int shards)
    {
        await tx.Connection.ExecuteAsync(new CommandDefinition(
            "UPDATE player_idols SET shards = shards + @shards WHERE player_id = @playerId AND idol_id = @idolId",
            new { playerId = playerId.ToString(), idolId, shards },
            tx.Transaction,
            cancellationToken: tx.CancellationToken));
    }

    public async Task<IReadOnlyList<OwnedIdol>> ListAsync(TxContext tx, Ulid playerId)
    {
        var rows = await tx.Connection.QueryAsync<Row>(new CommandDefinition(
            "SELECT idol_id AS IdolId, training AS Training, awakening AS Awakening, shards AS Shards " +
            "FROM player_idols WHERE player_id = @playerId ORDER BY idol_id",
            new { playerId = playerId.ToString() },
            tx.Transaction,
            cancellationToken: tx.CancellationToken));
        return rows.Select(x => new OwnedIdol(x.IdolId, x.Training, x.Awakening, x.Shards)).ToList();
    }

    private sealed class Row
    {
        public int IdolId { get; set; }
        public int Training { get; set; }
        public int Awakening { get; set; }
        public int Shards { get; set; }
    }
}
