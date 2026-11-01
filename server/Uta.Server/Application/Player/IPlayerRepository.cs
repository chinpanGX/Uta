using VolTech.Server.Transaction;

namespace Uta.Server.Application.Player;

public interface IPlayerRepository
{
    /// <summary>行が無ければ作る。作ったら true、既にあれば false。</summary>
    Task<bool> InsertIfAbsentAsync(TxContext tx, Ulid playerId);

    /// <summary>行が無ければ null。</summary>
    Task<PlayerSnapshot?> FindAsync(TxContext tx, Ulid playerId);

    /// <summary>通貨を加算する（初期付与）。</summary>
    Task GrantCurrenciesAsync(TxContext tx, Ulid playerId, int gachaCurrency, int gold, int supportPoints);

    /// <summary>ガチャ通貨を減らし、残りを返す。足りなければ null（更新は 0 行）。</summary>
    Task<int?> TrySpendGachaCurrencyAsync(TxContext tx, Ulid playerId, int cost);

    Task AddSupportPointsAsync(TxContext tx, Ulid playerId, int points);
}
