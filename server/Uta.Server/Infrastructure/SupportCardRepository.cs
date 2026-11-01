using Dapper;
using Uta.Server.Application.Support;
using Uta.Server.Domain.Support;
using VolTech.Server.Transaction;

namespace Uta.Server.Infrastructure;

public sealed class SupportCardRepository : ISupportCardRepository
{
    public async Task InsertAsync(TxContext tx, Ulid playerId, Ulid uid, int cardId)
    {
        await tx.Connection.ExecuteAsync(new CommandDefinition(
            "INSERT INTO player_support_cards (support_card_uid, player_id, card_id) VALUES (@uid, @playerId, @cardId)",
            new { uid = uid.ToString(), playerId = playerId.ToString(), cardId },
            tx.Transaction,
            cancellationToken: tx.CancellationToken));
    }

    public async Task<bool> HasMaxLimitBreakAsync(TxContext tx, Ulid playerId, int cardId)
    {
        return await tx.Connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            "SELECT EXISTS (SELECT 1 FROM player_support_cards WHERE player_id = @playerId AND card_id = @cardId AND limit_break >= @max)",
            new { playerId = playerId.ToString(), cardId, max = SupportRules.MaxLimitBreak },
            tx.Transaction,
            cancellationToken: tx.CancellationToken));
    }

    public async Task<IReadOnlyList<OwnedSupportCard>> ListAsync(TxContext tx, Ulid playerId)
    {
        var rows = await tx.Connection.QueryAsync<Row>(new CommandDefinition(
            "SELECT support_card_uid AS Uid, card_id AS CardId, level AS Level, limit_break AS LimitBreak " +
            "FROM player_support_cards WHERE player_id = @playerId ORDER BY support_card_uid",
            new { playerId = playerId.ToString() },
            tx.Transaction,
            cancellationToken: tx.CancellationToken));
        return rows.Select(x => new OwnedSupportCard(Ulid.Parse(x.Uid), x.CardId, x.Level, x.LimitBreak)).ToList();
    }

    private sealed class Row
    {
        public string Uid { get; set; } = "";
        public int CardId { get; set; }
        public int Level { get; set; }
        public int LimitBreak { get; set; }
    }
}
