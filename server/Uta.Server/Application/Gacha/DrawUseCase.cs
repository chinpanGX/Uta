using System.Text.Json.Nodes;
using Uta.MasterData;
using Uta.Server.Application.Common;
using Uta.Server.Domain.Gacha;
using Uta.Server.Domain.Player;
using VolTech.Server.Transaction;

namespace Uta.Server.Application.Gacha;

public sealed record DrawResult(IReadOnlyList<GachaResult> Results, int GachaCurrency, int ExchangePoints);

public sealed class DrawUseCase(
    MemoryDatabase master, IPlayerRepository players, IGachaRepository gachas, ItemGranter granter, IRandomSource randomSource)
{
    public async Task<DrawResult> ExecuteAsync(TxContext tx, Ulid playerId, string requestId, int gachaId, int count, int expectedCost)
    {
        if (count != 1 && count != 10)
        {
            throw new UseCaseException(UseCaseError.GachaInvalidCount, "count must be 1 or 10");
        }

        if (!master.GachaTable.TryFindByGachaId(gachaId, out var gacha))
        {
            throw new UseCaseException(UseCaseError.GachaNotFound, "gacha not found");
        }

        var cost = count == 1 ? gacha.SingleCost : gacha.TenCost;
        if (expectedCost != cost)
        {
            throw new UseCaseException(UseCaseError.GachaCostMismatch, "cost mismatch");
        }

        var remaining = await players.TrySpendGachaCurrencyAsync(tx, playerId, cost)
            ?? throw new UseCaseException(UseCaseError.GachaNotEnoughCurrency, "not enough gacha currency");

        var normal = master.GachaTableEntryTable
            .FindRangeByGachaTableIdAndOrder((gacha.NormalTableId, int.MinValue), (gacha.NormalTableId, int.MaxValue)).ToList();
        var guaranteed = master.GachaTableEntryTable
            .FindRangeByGachaTableIdAndOrder((gacha.GuaranteedTableId, int.MinValue), (gacha.GuaranteedTableId, int.MaxValue)).ToList();
        var normalWeights = normal.Select(x => x.Weight).ToList();
        var guaranteedWeights = guaranteed.Select(x => x.Weight).ToList();

        var random = randomSource.Create();
        var results = new List<GachaResult>();
        for (var slot = 1; slot <= count; slot++)
        {
            var isGuaranteed = count == 10 && slot == 10;
            var entries = isGuaranteed ? guaranteed : normal;
            var weights = isGuaranteed ? guaranteedWeights : normalWeights;
            var tableId = isGuaranteed ? gacha.GuaranteedTableId : gacha.NormalTableId;
            var (kind, itemId) = GachaItemMapper.ToGrantedItem(entries[GachaPicker.Pick(random, weights)]);
            var result = (await granter.GrantAsync(tx, playerId, kind, itemId)) with { Slot = slot };
            await gachas.AddHistoryAsync(tx, new GachaHistoryRecord(
                Ulid.NewUlid(), requestId, playerId, gachaId, slot, tableId,
                (int)result.Kind, result.ItemId, (int)result.ResultKind, result.Shards, result.SupportPoints));
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
