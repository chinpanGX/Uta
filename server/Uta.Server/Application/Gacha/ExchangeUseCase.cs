using Uta.MasterData;
using Uta.Server.Application.Common;
using Uta.Server.Domain.Gacha;
using VolTech.Server.Transaction;

namespace Uta.Server.Application.Gacha;

public sealed record ExchangeResult(GachaResult Result, int ExchangePoints);

public sealed class ExchangeUseCase(MemoryDatabase master, IGachaRepository gachas, ItemGranter granter)
{
    public async Task<ExchangeResult> ExecuteAsync(TxContext tx, Ulid playerId, string requestId, int gachaId, int ceilingItemOrder)
    {
        if (!master.GachaTable.TryFindByGachaId(gachaId, out var gacha)
            || !master.GachaCeilingItemTable.TryFindByGachaIdAndOrder((gachaId, ceilingItemOrder), out var ceilingItem))
        {
            throw new UseCaseException(UseCaseError.GachaNotFound, "gacha or ceiling item not found");
        }

        var points = await gachas.TrySpendExchangePointsAsync(tx, playerId, gachaId, gacha.CeilingPoints)
            ?? throw new UseCaseException(UseCaseError.GachaNotEnoughPoints, "not enough exchange points");

        var (kind, itemId) = GachaItemMapper.ToGrantedItem(ceilingItem);
        var result = await granter.GrantAsync(tx, playerId, kind, itemId);
        await gachas.AddHistoryAsync(tx, new GachaHistoryRecord(
            Ulid.NewUlid(), requestId, playerId, gachaId, 0, 0,
            (int)result.Kind, result.ItemId, (int)result.Outcome, result.Shards, result.SupportPoints));

        tx.WriteGameLog(GameLog.Event("gacha", "gacha_exchange")
            .Target(gachaId.ToString())
            .Field("ceiling_item_order", ceilingItemOrder).Field("cost_points", gacha.CeilingPoints)
            .Field("exchange_points_after", points).Field("result", result.ToLogJson()));

        return new ExchangeResult(result, points);
    }
}
