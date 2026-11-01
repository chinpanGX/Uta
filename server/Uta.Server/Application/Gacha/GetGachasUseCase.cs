using VolTech.Server.Transaction;

namespace Uta.Server.Application.Gacha;

public sealed record GachaStatus(int GachaId, int ExchangePoints);

public sealed class GetGachasUseCase(IGachaCatalog catalog, IGachaRepository gachas)
{
    /// <summary>catalog.All の順（GachaId 昇順）。交換pt の行が無いガチャは 0。</summary>
    public async Task<IReadOnlyList<GachaStatus>> ExecuteAsync(TxContext tx, Ulid playerId)
    {
        var points = await gachas.GetExchangePointsAsync(tx, playerId);
        return catalog.All
            .Select(x => new GachaStatus(x.GachaId, points.TryGetValue(x.GachaId, out var value) ? value : 0))
            .ToList();
    }
}
