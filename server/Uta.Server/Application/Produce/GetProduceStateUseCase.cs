using Uta.Shared.Domain.Produce;
using VolTech.Server.Transaction;

namespace Uta.Server.Application.Produce;

public sealed class GetProduceStateUseCase(IProduceSessionRepository sessions, ProduceCatalog catalog, ProduceViewBuilder viewBuilder)
{
    /// <summary>進行中のセッションが無い・古い（読めない）ときは ProduceView.None。何も書かない。</summary>
    public async Task<ProduceView> ExecuteAsync(TxContext tx, Ulid playerId, int appVersion)
    {
        var session = await sessions.FindActiveAsync(tx, playerId);
        if (session is null || !ProduceVersions.IsCurrent(session, appVersion) || session.Data is null)
        {
            return ProduceView.None;
        }

        return viewBuilder.Build(session.Data.Snapshot.ToState(catalog), null, null);
    }
}
