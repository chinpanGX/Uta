using Uta.Server.Application.Common;
using Uta.Server.Application.Player;
using Uta.Shared.Domain.Produce;
using VolTech.Server.Transaction;

namespace Uta.Server.Application.Produce;

public sealed class AbandonProduceUseCase(IProduceSessionRepository sessions, IPlayerRepository players)
{
    public async Task<ProduceEndSummary> ExecuteAsync(TxContext tx, Ulid playerId, int appVersion)
    {
        var session = await sessions.FindActiveAsync(tx, playerId)
            ?? throw new UseCaseException(UseCaseError.ProduceNotInProgress, "not in progress");
        if (!ProduceVersions.IsCurrent(session, appVersion))
        {
            // 何も書かない（業務エラーはトランザクションを巻き戻す。破棄は次の SignInAsync / StartAsync が行う）
            throw new UseCaseException(UseCaseError.ProduceSessionDiscarded, "session discarded");
        }

        if (!await sessions.TrySetStatusAsync(tx, session.SessionId, session.Version, ProduceSessionStatus.Abandoned))
        {
            throw new UseCaseException(UseCaseError.ProduceStaleOperation, "stale");
        }

        await sessions.InsertResultAsync(tx, ProduceResultRecord.Empty(session.SessionId, playerId, ProduceSessionStatus.Abandoned));
        var player = await players.FindAsync(tx, playerId) ?? throw new InvalidOperationException("player not found");
        return new ProduceEndSummary(
            ProduceSessionStatus.Abandoned, ExamOutcome.Nothing, 0, ProduceRank.Nothing, 0, 0, 0, 0, 0, 0, player.Level, player.Exp);
    }
}
