using VolTech.Server.Transaction;

namespace Uta.Server.Application.Support;

public interface ISupportCardRepository
{
    /// <summary>レベル 1・上限解放 0 の行を追加する。</summary>
    Task InsertAsync(TxContext tx, Ulid playerId, Ulid uid, int cardId);

    /// <summary>同じカード ID で上限解放が SupportRules.MaxLimitBreak の行を 1 つでも持っているか。</summary>
    Task<bool> HasMaxLimitBreakAsync(TxContext tx, Ulid playerId, int cardId);

    /// <summary>support_card_uid の昇順。</summary>
    Task<IReadOnlyList<OwnedSupportCard>> ListAsync(TxContext tx, Ulid playerId);

    /// <summary>自分の所持IDの行。無い・他人の行なら null。</summary>
    Task<OwnedSupportCard?> FindAsync(TxContext tx, Ulid playerId, Ulid uid);

    Task UpdateLevelAsync(TxContext tx, Ulid uid, int level);

    /// <summary>素材を消す。自分の所持で、カード ID が cardId の行だけ。消せたら true。</summary>
    Task<bool> TryDeleteMaterialAsync(TxContext tx, Ulid playerId, Ulid uid, int cardId);

    /// <summary>上限解放を 1 増やし、増やした後の回数を返す。既に SupportRules.MaxLimitBreak なら null（更新は 0 行）。</summary>
    Task<int?> TryLimitBreakAsync(TxContext tx, Ulid playerId, Ulid uid);
}
