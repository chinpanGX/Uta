using System.Text.Json.Nodes;
using Uta.Server.Application.Common;
using Uta.Server.Application.Player;
using Uta.Server.Domain.Gacha;
using VolTech.Server.Transaction;

namespace Uta.Server.Application.Gacha;

public sealed record DrawResult(IReadOnlyList<GachaResult> Results, int GachaCurrency, int ExchangePoints);

public sealed class DrawUseCase(
    IGachaCatalog catalog, IPlayerRepository players, IGachaRepository gachas, ItemGranter granter, IRandomSource randomSource)
{
    public async Task<DrawResult> ExecuteAsync(TxContext tx, Ulid playerId, string requestId, int gachaId, int count, int expectedCost)
    {
        if (count != 1 && count != 10)
        {
            throw new UseCaseException(UseCaseError.GachaInvalidCount, "count must be 1 or 10");
        }

        var gacha = catalog.Find(gachaId)
            ?? throw new UseCaseException(UseCaseError.GachaNotFound, "gacha not found");
        var cost = count == 1 ? gacha.SingleCost : gacha.TenCost;
        if (expectedCost != cost)
        {
            throw new UseCaseException(UseCaseError.GachaCostMismatch, "cost mismatch");
        }

        var remaining = await players.TrySpendGachaCurrencyAsync(tx, playerId, cost)
            ?? throw new UseCaseException(UseCaseError.GachaNotEnoughCurrency, "not enough gacha currency");

        var random = randomSource.Create();
        var results = new List<GachaResult>();
        for (var slot = 1; slot <= count; slot++)
        {
            var table = (count == 10 && slot == 10) ? gacha.Guaranteed : gacha.Normal;
            var item = table.Items[GachaPicker.Pick(random, table.Weights)];
            var result = (await granter.GrantAsync(tx, playerId, item.Kind, item.ItemId)) with { Slot = slot };
            await gachas.AddHistoryAsync(tx, new GachaHistoryRow(
                Ulid.NewUlid(), requestId, playerId, gachaId, slot, table.GachaTableId,
                (int)result.Kind, result.ItemId, (int)result.Outcome, result.Shards, result.SupportPoints));
            results.Add(result);
        }

        var points = await gachas.AddExchangePointsAsync(tx, playerId, gachaId, count);

        tx.WriteGameLog(GameLog.Event("gacha", "gacha_draw")
            .Target(gachaId.ToString())
            .Field("count", count).Field("cost", cost)
            .Field("gacha_currency_after", remaining).Field("exchange_points_after", points)
            .Field("results", new JsonArray(results.Select(x => (JsonNode?)x.ToLogJson()).ToArray())));

        return new DrawResult(results, remaining, points);
    }
}
