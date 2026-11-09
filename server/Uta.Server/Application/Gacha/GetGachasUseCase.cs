using Uta.MasterData;
using Uta.Server.Domain.Gacha;
using VolTech.Server.Transaction;

namespace Uta.Server.Application.Gacha;

public sealed record GachaEntryResult(int GachaId, int ExchangePoints);

public sealed class GetGachasUseCase(MemoryDatabase master, IGachaRepository gachas)
{
    /// <summary>マスタの GachaTable の順（GachaId 昇順）。交換pt の行が無いガチャは 0。</summary>
    public async Task<IReadOnlyList<GachaEntryResult>> ExecuteAsync(TxContext tx, Ulid playerId)
    {
        var points = await gachas.GetExchangePointsAsync(tx, playerId);
        return master.GachaTable.All
            .Select(x => new GachaEntryResult(x.GachaId, points.TryGetValue(x.GachaId, out var value) ? value : 0))
            .ToList();
    }
}
