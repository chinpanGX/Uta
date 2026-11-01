using VolTech.Server.Transaction;

namespace Uta.Server.Application.Support;

public interface ISupportCardRepository
{
    /// <summary>レベル 1・上限解放 0 の行を追加する。</summary>
    Task InsertAsync(TxContext tx, Ulid playerId, Ulid uid, int cardId);

    /// <summary>support_card_uid の昇順。</summary>
    Task<IReadOnlyList<OwnedSupportCard>> ListAsync(TxContext tx, Ulid playerId);
}
