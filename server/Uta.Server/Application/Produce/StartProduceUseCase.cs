using Uta.Server.Application.Common;
using Uta.Server.Application.Idol;
using Uta.Server.Application.Player;
using Uta.Server.Application.Support;
using Uta.Server.Domain.Produce;
using Uta.Shared.Domain.Produce;
using VolTech.Server.Transaction;

namespace Uta.Server.Application.Produce;

public sealed class StartProduceUseCase(
    IProduceSessionRepository sessions, IPlayerRepository players, IIdolRepository idols, ISupportCardRepository supportCards,
    ProduceEngine engine, ProduceCatalog catalog, IRandomSource randomSource, ProduceSessionDiscarder discarder, ProduceViewBuilder viewBuilder)
{
    public async Task<ProduceView> ExecuteAsync(
        TxContext tx, Ulid playerId, int appVersion, int idolId, IReadOnlyList<string> supportCardUids, int rentalCandidateId, int scenarioId)
    {
        try
        {
            catalog.GetScenario(scenarioId);
        }
        catch (KeyNotFoundException)
        {
            throw new UseCaseException(UseCaseError.ProduceInvalidArgument, "unknown scenario");
        }

        // IdolId がマスタに無い場合も所持していないので同じ
        if (!await idols.ExistsAsync(tx, playerId, idolId))
        {
            throw new UseCaseException(UseCaseError.ProduceInvalidFormation, "idol not owned");
        }

        if (supportCardUids.Count > ProduceRules.MaxSupportCards)
        {
            throw new UseCaseException(UseCaseError.ProduceInvalidFormation, "too many support cards");
        }

        var uids = new HashSet<Ulid>();
        var owned = new List<OwnedSupportCard>();
        foreach (var text in supportCardUids)
        {
            if (!Ulid.TryParse(text, out var uid))
            {
                throw new UseCaseException(UseCaseError.ProduceInvalidFormation, "invalid support card uid");
            }

            if (!uids.Add(uid))
            {
                throw new UseCaseException(UseCaseError.ProduceInvalidFormation, "duplicate support card uid");
            }

            var card = await supportCards.FindAsync(tx, playerId, uid)
                ?? throw new UseCaseException(UseCaseError.ProduceInvalidFormation, "support card not owned");
            owned.Add(card);
        }

        var cardIds = owned.Select(x => x.MasterId).ToList();
        if (FormationValidator.Validate(catalog, idolId, cardIds) != FormationCheck.Ok)
        {
            throw new UseCaseException(UseCaseError.ProduceInvalidFormation, "invalid formation");
        }

        if (rentalCandidateId != 0)
        {
            throw new UseCaseException(UseCaseError.ProduceInvalidArgument, "rental is not supported");
        }

        var existing = await sessions.FindActiveAsync(tx, playerId);
        if (existing is not null)
        {
            if (ProduceVersions.IsCurrent(existing, appVersion))
            {
                throw new UseCaseException(UseCaseError.ProduceAlreadyInProgress, "already in progress");
            }

            // 破棄も検証と同じトランザクション。同時の StartAsync の部分ユニークインデックス違反は想定しない
            await discarder.DiscardAsync(tx, playerId, existing);
        }

        var player = await players.FindAsync(tx, playerId) ?? throw new InvalidOperationException("player not found");
        var input = new ProduceStartInput(
            scenarioId, idolId, player.Level, owned.Select(x => new SupportCardInput(x.MasterId, x.Level, x.LimitBreak)).ToList());

        var seed = randomSource.Create().NextUInt64();
        var streams = new SessionRandomStreams(seed, []);
        var state = engine.Start(input, streams);

        var data = new ProduceSessionData(seed, streams.Export(), ProduceSnapshot.From(state, catalog.Cards));
        await sessions.InsertAsync(tx, Ulid.NewUlid(), playerId, appVersion, data);
        return viewBuilder.Build(state, null, null);
    }
}
