using VolTech.Server.Transaction;

namespace Uta.Server.Application.Produce;

public interface IProduceSessionRepository
{
    /// <summary>進行中のセッション（status = 0）。無ければ null。</summary>
    Task<ProduceSessionRecord?> FindActiveAsync(TxContext tx, Ulid playerId);

    /// <summary>status = 進行中、version = 0 で追加。state_version・rule_version は ProduceVersions の定数、week / turn は data.Snapshot から（Week と Lesson?.Turn ?? 0）。</summary>
    Task InsertAsync(TxContext tx, Ulid sessionId, Ulid playerId, int appVersion, ProduceSessionData data);

    /// <summary>state・status・week・turn を更新し version を 1 増やす（WHERE session_id AND version = expectedVersion）。0 行なら false。</summary>
    Task<bool> TryUpdateAsync(TxContext tx, Ulid sessionId, int expectedVersion, ProduceSessionStatus status, ProduceSessionData data);

    /// <summary>state は変えず status だけ更新し version を 1 増やす。0 行なら false。</summary>
    Task<bool> TrySetStatusAsync(TxContext tx, Ulid sessionId, int expectedVersion, ProduceSessionStatus status);

    Task InsertResultAsync(TxContext tx, ProduceResultRecord result);
}
