using Uta.Server.Application.Common;
using Uta.Server.Application.Idol;
using Uta.Server.Application.Produce;
using Uta.Server.Application.Support;
using VolTech.Server.Transaction;

namespace Uta.Server.Application.Player;

public sealed class SignInUseCase(
    IPlayerRepository players, IIdolRepository idols, ISupportCardRepository supportCards,
    IProduceSessionRepository produceSessions, ProduceSessionDiscarder discarder)
{
    /// <summary>行が無ければ null。appVersion は進行中のプロデュースがあるときだけ使う（無ければ ProduceInvalidArgument）。</summary>
    public async Task<SignInResult?> ExecuteAsync(TxContext tx, Ulid playerId, int? appVersion)
    {
        var player = await players.FindAsync(tx, playerId);
        if (player is null) return null;

        var hasActiveProduce = false;
        var discardedProduce = false;
        var session = await produceSessions.FindActiveAsync(tx, playerId);
        if (session is not null)
        {
            if (appVersion is null)
            {
                throw new UseCaseException(UseCaseError.ProduceInvalidArgument, "x-app-version is required");
            }

            if (ProduceVersions.IsCurrent(session, appVersion.Value))
            {
                hasActiveProduce = true;
            }
            else
            {
                await discarder.DiscardAsync(tx, playerId, session);
                discardedProduce = true;
            }
        }

        return new SignInResult(
            player, await idols.ListAsync(tx, playerId), await supportCards.ListAsync(tx, playerId), hasActiveProduce, discardedProduce);
    }
}
