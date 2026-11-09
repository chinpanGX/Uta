using Uta.Server.Domain.Produce;
using Uta.Shared.Domain.Produce;
using VolTech.Server.Transaction;

namespace Uta.Server.Application.Produce;

public sealed class GetProduceStateUseCase(IProduceSessionRepository sessions, ProduceCatalog catalog, ProduceStateResultFactory resultFactory)
{
    /// <summary>進行中のセッションが無い・古い（読めない）ときは ProduceStateResult.None。何も書かない。</summary>
    public async Task<ProduceStateResult> ExecuteAsync(TxContext tx, Ulid playerId, int appVersion)
    {
        var session = await sessions.FindActiveAsync(tx, playerId);
        if (session is null || !ProduceVersions.IsCurrent(session, appVersion) || session.Data is null)
        {
            return ProduceStateResult.None;
        }

        return resultFactory.Create(session.Data.Snapshot.ToState(catalog), null, null);
    }
}
