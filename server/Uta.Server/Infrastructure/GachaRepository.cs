using Dapper;
using Uta.Server.Application.Gacha;
using VolTech.Server.Transaction;

namespace Uta.Server.Infrastructure;

public sealed class GachaRepository : IGachaRepository
{
    public async Task<int> AddExchangePointsAsync(TxContext tx, Ulid playerId, int gachaId, int points)
    {
        return await tx.Connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "INSERT INTO gacha_exchange_points (player_id, gacha_id, points) VALUES (@playerId, @gachaId, @points) " +
            "ON CONFLICT (player_id, gacha_id) DO UPDATE SET points = gacha_exchange_points.points + @points " +
            "RETURNING points",
            new { playerId = playerId.ToString(), gachaId, points },
            tx.Transaction,
            cancellationToken: tx.CancellationToken));
    }

    public async Task<int?> TrySpendExchangePointsAsync(TxContext tx, Ulid playerId, int gachaId, int points)
    {
        return await tx.Connection.QuerySingleOrDefaultAsync<int?>(new CommandDefinition(
            "UPDATE gacha_exchange_points SET points = points - @cost " +
            "WHERE player_id = @playerId AND gacha_id = @gachaId AND points >= @cost " +
            "RETURNING points",
            new { playerId = playerId.ToString(), gachaId, cost = points },
            tx.Transaction,
            cancellationToken: tx.CancellationToken));
    }

    public async Task<IReadOnlyDictionary<int, int>> GetExchangePointsAsync(TxContext tx, Ulid playerId)
    {
        var rows = await tx.Connection.QueryAsync<Row>(new CommandDefinition(
            "SELECT gacha_id AS GachaId, points AS Points FROM gacha_exchange_points WHERE player_id = @playerId",
            new { playerId = playerId.ToString() },
            tx.Transaction,
            cancellationToken: tx.CancellationToken));
        return rows.ToDictionary(x => x.GachaId, x => x.Points);
    }

    public async Task AddHistoryAsync(TxContext tx, GachaHistoryRow row)
    {
        await tx.Connection.ExecuteAsync(new CommandDefinition(
            "INSERT INTO gacha_histories " +
            "(history_id, request_id, player_id, gacha_id, slot, gacha_table_id, item_kind, item_id, outcome, shards, support_points) " +
            "VALUES (@historyId, @requestId, @playerId, @gachaId, @slot, @gachaTableId, @itemKind, @itemId, @outcome, @shards, @supportPoints)",
            new
            {
                historyId = row.HistoryId.ToString(),
                requestId = row.RequestId,
                playerId = row.PlayerId.ToString(),
                gachaId = row.GachaId,
                slot = row.Slot,
                gachaTableId = row.GachaTableId,
                itemKind = row.ItemKind,
                itemId = row.ItemId,
                outcome = row.Outcome,
                shards = row.Shards,
                supportPoints = row.SupportPoints,
            },
            tx.Transaction,
            cancellationToken: tx.CancellationToken));
    }

    private sealed class Row
    {
        public int GachaId { get; set; }
        public int Points { get; set; }
    }
}
