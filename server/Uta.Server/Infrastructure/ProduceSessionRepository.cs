using Dapper;
using Uta.Server.Application.Produce;
using Uta.Server.Domain.Produce;
using Uta.Server.Infrastructure.Produce;
using VolTech.Server.Transaction;

namespace Uta.Server.Infrastructure;

public sealed class ProduceSessionRepository : IProduceSessionRepository
{
    public async Task<ProduceSessionRecord?> FindActiveAsync(TxContext tx, Ulid playerId)
    {
        var row = await tx.Connection.QuerySingleOrDefaultAsync<Row>(new CommandDefinition(
            "SELECT session_id AS SessionId, app_version AS AppVersion, state_version AS StateVersion, rule_version AS RuleVersion, " +
            "version AS Version, state AS State FROM produce_sessions WHERE player_id = @playerId AND status = 0",
            new { playerId = playerId.ToString() },
            tx.Transaction,
            cancellationToken: tx.CancellationToken));
        if (row is null) return null;

        ProduceSessionData? data = null;
        if (row.StateVersion == ProduceVersions.StateVersion)
        {
            try
            {
                data = ProduceStateSerializer.Deserialize(row.State);
            }
            catch (Exception)
            {
                // state が読めないときは古いセッションと同じに扱う（SignInAsync が起動できなくなるのを防ぐ）
                data = null;
            }
        }

        return new ProduceSessionRecord(Ulid.Parse(row.SessionId), row.AppVersion, row.StateVersion, row.RuleVersion, row.Version, data);
    }

    public async Task InsertAsync(TxContext tx, Ulid sessionId, Ulid playerId, int appVersion, ProduceSessionData data)
    {
        await tx.Connection.ExecuteAsync(new CommandDefinition(
            "INSERT INTO produce_sessions (session_id, player_id, status, week, turn, app_version, state_version, rule_version, version, state) " +
            "VALUES (@sessionId, @playerId, @status, @week, @turn, @appVersion, @stateVersion, @ruleVersion, 0, @state)",
            new
            {
                sessionId = sessionId.ToString(),
                playerId = playerId.ToString(),
                status = (int)ProduceSessionStatus.InProgress,
                week = data.Snapshot.Week,
                turn = data.Snapshot.Lesson?.Turn ?? 0,
                appVersion,
                stateVersion = ProduceVersions.StateVersion,
                ruleVersion = ProduceVersions.RuleVersion,
                state = ProduceStateSerializer.Serialize(data),
            },
            tx.Transaction,
            cancellationToken: tx.CancellationToken));
    }

    public async Task<bool> TryUpdateAsync(TxContext tx, Ulid sessionId, int expectedVersion, ProduceSessionStatus status, ProduceSessionData data)
    {
        var affected = await tx.Connection.ExecuteAsync(new CommandDefinition(
            "UPDATE produce_sessions SET status = @status, week = @week, turn = @turn, state = @state, version = version + 1, updated_at = now() " +
            "WHERE session_id = @sessionId AND version = @expectedVersion",
            new
            {
                sessionId = sessionId.ToString(),
                expectedVersion,
                status = (int)status,
                week = data.Snapshot.Week,
                turn = data.Snapshot.Lesson?.Turn ?? 0,
                state = ProduceStateSerializer.Serialize(data),
            },
            tx.Transaction,
            cancellationToken: tx.CancellationToken));
        return affected == 1;
    }

    public async Task<bool> TrySetStatusAsync(TxContext tx, Ulid sessionId, int expectedVersion, ProduceSessionStatus status)
    {
        var affected = await tx.Connection.ExecuteAsync(new CommandDefinition(
            "UPDATE produce_sessions SET status = @status, version = version + 1, updated_at = now() " +
            "WHERE session_id = @sessionId AND version = @expectedVersion",
            new { sessionId = sessionId.ToString(), expectedVersion, status = (int)status },
            tx.Transaction,
            cancellationToken: tx.CancellationToken));
        return affected == 1;
    }

    public async Task InsertResultAsync(TxContext tx, ProduceResultRecord result)
    {
        await tx.Connection.ExecuteAsync(new CommandDefinition(
            "INSERT INTO produce_results (session_id, player_id, ending, exam_outcome, evaluation_value, produce_rank, final_score, final_rank, " +
            "gacha_currency, gold, support_points, exp) " +
            "VALUES (@sessionId, @playerId, @ending, @examOutcome, @evaluationValue, @produceRank, @finalScore, @finalRank, " +
            "@gachaCurrency, @gold, @supportPoints, @exp)",
            new
            {
                sessionId = result.SessionId.ToString(),
                playerId = result.PlayerId.ToString(),
                ending = (int)result.Ending,
                examOutcome = (int)result.ExamOutcome,
                evaluationValue = result.EvaluationValue,
                produceRank = (int)result.Rank,
                finalScore = result.FinalScore,
                finalRank = result.FinalRank,
                gachaCurrency = result.GachaCurrency,
                gold = result.Gold,
                supportPoints = result.SupportPoints,
                exp = result.Exp,
            },
            tx.Transaction,
            cancellationToken: tx.CancellationToken));
    }

    private sealed class Row
    {
        public string SessionId { get; set; } = "";
        public int AppVersion { get; set; }
        public int StateVersion { get; set; }
        public int RuleVersion { get; set; }
        public int Version { get; set; }
        public byte[] State { get; set; } = [];
    }
}
