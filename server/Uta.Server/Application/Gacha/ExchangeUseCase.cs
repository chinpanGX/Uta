using Uta.Server.Application.Common;
using VolTech.Server.Transaction;

namespace Uta.Server.Application.Gacha;

public sealed record ExchangeResult(GachaResult Result, int ExchangePoints);

public sealed class ExchangeUseCase(IGachaCatalog catalog, IGachaRepository gachas, ItemGranter granter)
{
    public async Task<ExchangeResult> ExecuteAsync(TxContext tx, Ulid playerId, string requestId, int gachaId, int ceilingItemOrder)
    {
        var gacha = catalog.Find(gachaId);
        if (gacha is null || gacha.CeilingItems.All(x => x.Order != ceilingItemOrder))
        {
            throw new UseCaseException(UseCaseError.GachaNotFound, "gacha or ceiling item not found");
        }

        var points = await gachas.TrySpendExchangePointsAsync(tx, playerId, gachaId, gacha.CeilingPoints)
            ?? throw new UseCaseException(UseCaseError.GachaNotEnoughPoints, "not enough exchange points");

        var item = gacha.CeilingItems.First(x => x.Order == ceilingItemOrder);
        var result = await granter.GrantAsync(tx, playerId, item.Kind, item.ItemId);
        await gachas.AddHistoryAsync(tx, new GachaHistoryRow(
            Ulid.NewUlid(), requestId, playerId, gachaId, 0, 0,
            (int)result.Kind, result.ItemId, (int)result.Outcome, result.Shards, result.SupportPoints));

        tx.WriteGameLog(GameLog.Event("gacha", "gacha_exchange")
            .Target(gachaId.ToString())
            .Field("ceiling_item_order", ceilingItemOrder).Field("cost_points", gacha.CeilingPoints)
            .Field("exchange_points_after", points).Field("result", result.ToLogJson()));

        return new ExchangeResult(result, points);
    }
}
