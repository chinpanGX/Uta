using Uta.Server.Application.Common;
using Uta.Server.Domain.Produce;
using Uta.Shared.Domain.Produce;
using VolTech.Server.Transaction;

namespace Uta.Server.Application.Produce;

public sealed class ProduceOperationUseCase(
    IProduceSessionRepository sessions, ProduceEngine engine, ProduceCatalog catalog,
    ProduceViewBuilder viewBuilder, ProduceCompletion completion)
{
    public async Task<ProduceView> ExecuteAsync(TxContext tx, Ulid playerId, int appVersion, int operationNumber, ProduceCommand command)
    {
        var session = await sessions.FindActiveAsync(tx, playerId)
            ?? throw new UseCaseException(UseCaseError.ProduceNotInProgress, "not in progress");
        if (!ProduceVersions.IsCurrent(session, appVersion) || session.Data is null)
        {
            // 何も書かない（業務エラーはトランザクションを巻き戻す。破棄は次の SignInAsync / StartAsync が行う）
            throw new UseCaseException(UseCaseError.ProduceSessionDiscarded, "session discarded");
        }

        var state = session.Data.Snapshot.ToState(catalog);
        if (operationNumber != state.NextOperationNumber)
        {
            throw new UseCaseException(UseCaseError.ProduceStaleOperation, "stale operation number");
        }

        var streams = new SessionRandomStreams(session.Data.MasterSeed, session.Data.Streams);
        var lastExam = state.LastExam;
        var normalized = ProduceCommandNormalizer.Normalize(engine, state, command);
        var applied = engine.Apply(state, normalized, streams);
        switch (applied)
        {
            case ProduceApplyResult.UnexpectedPhase:
                throw new UseCaseException(UseCaseError.ProduceUnexpectedPhase, "unexpected phase");
            case ProduceApplyResult.InvalidArgument:
                throw new UseCaseException(UseCaseError.ProduceInvalidArgument, "invalid argument");
        }

        ProduceEndSummary? end = null;
        var status = ProduceSessionStatus.InProgress;
        if (state.Phase == ProducePhase.Finished)
        {
            end = await completion.CompleteAsync(tx, playerId, session.SessionId, state);
            status = ProduceSessionStatus.Completed;
        }

        var data = new ProduceSessionData(session.Data.MasterSeed, streams.Export(), ProduceSnapshot.From(state, catalog.Cards));
        if (!await sessions.TryUpdateAsync(tx, session.SessionId, session.Version, status, data))
        {
            throw new UseCaseException(UseCaseError.ProduceStaleOperation, "stale");
        }

        var examResult = ReferenceEquals(lastExam, state.LastExam) ? null : state.LastExam;
        return viewBuilder.Build(state, examResult, end);
    }
}
