using Uta.Server.Application.Common;
using VolTech.Server.Transaction;

namespace Uta.Server.Application.Produce;

/// <summary>古い進行中のセッションを破棄する（status を破棄にし、produce_results に 0 で 1 行残す。報酬なし）。</summary>
public sealed class ProduceSessionDiscarder(IProduceSessionRepository sessions)
{
    public async Task DiscardAsync(TxContext tx, Ulid playerId, ProduceSessionRecord session)
    {
        if (!await sessions.TrySetStatusAsync(tx, session.SessionId, session.Version, ProduceSessionStatus.Discarded))
        {
            throw new UseCaseException(UseCaseError.ProduceStaleOperation, "stale");
        }

        await sessions.InsertResultAsync(tx, ProduceResultRecord.Empty(session.SessionId, playerId, ProduceSessionStatus.Discarded));
    }
}
