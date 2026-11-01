using Dapper;
using Uta.Server.Application.Player;
using VolTech.Server.Transaction;

namespace Uta.Server.Infrastructure;

public sealed class PlayerRepository : IPlayerRepository
{
    public async Task<bool> InsertIfAbsentAsync(TxContext tx, Ulid playerId)
    {
        var affected = await tx.Connection.ExecuteAsync(new CommandDefinition(
            "INSERT INTO players (player_id) VALUES (@playerId) ON CONFLICT (player_id) DO NOTHING",
            new { playerId = playerId.ToString() },
            tx.Transaction,
            cancellationToken: tx.CancellationToken));
        return affected == 1;
    }

    public async Task<PlayerSnapshot?> FindAsync(TxContext tx, Ulid playerId)
    {
        var row = await tx.Connection.QuerySingleOrDefaultAsync<Row>(new CommandDefinition(
            "SELECT level AS Level, exp AS Exp, gold AS Gold, support_points AS SupportPoints, gacha_currency AS GachaCurrency " +
            "FROM players WHERE player_id = @playerId",
            new { playerId = playerId.ToString() },
            tx.Transaction,
            cancellationToken: tx.CancellationToken));
        return row is null ? null : new PlayerSnapshot(row.Level, row.Exp, row.Gold, row.SupportPoints, row.GachaCurrency);
    }

    private sealed class Row
    {
        public int Level { get; set; }
        public int Exp { get; set; }
        public int Gold { get; set; }
        public int SupportPoints { get; set; }
        public int GachaCurrency { get; set; }
    }
}
