using VolTech.Server.Transaction;

namespace Uta.Server.Application.Gacha;

public interface IGachaRepository
{
    /// <summary>交換pt を加算し、加算後の値を返す（行が無ければ作る）。</summary>
    Task<int> AddExchangePointsAsync(TxContext tx, Ulid playerId, int gachaId, int points);

    /// <summary>交換pt を減らし、残りを返す。足りない（行が無い場合を含む）なら null。</summary>
    Task<int?> TrySpendExchangePointsAsync(TxContext tx, Ulid playerId, int gachaId, int points);

    /// <summary>プレイヤーの交換pt。行があるガチャだけ（キーは GachaId）。</summary>
    Task<IReadOnlyDictionary<int, int>> GetExchangePointsAsync(TxContext tx, Ulid playerId);

    Task AddHistoryAsync(TxContext tx, GachaHistoryRow row);
}
