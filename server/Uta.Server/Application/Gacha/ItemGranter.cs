using Uta.Server.Application.Idol;
using Uta.Server.Application.Player;
using Uta.Server.Application.Support;
using Uta.Server.Domain.Gacha;
using VolTech.Server.Transaction;

namespace Uta.Server.Application.Gacha;

/// <summary>所持品への書き込み（gacha.md 5章）。Draw と Exchange が使う。Slot は 0 で返す（呼び出し側が with で入れる）。</summary>
public sealed class ItemGranter(
    IIdolRepository idols, ISupportCardRepository supportCards, IPlayerRepository players, IDuplicateConversionCatalog conversions)
{
    public async Task<GachaResult> GrantAsync(TxContext tx, Ulid playerId, GrantedItemKind kind, int itemId)
    {
        if (kind == GrantedItemKind.Idol)
        {
            var owned = await idols.ExistsAsync(tx, playerId, itemId);            // 枠ごとに読む（同じ 10 連で同じアイドルが 2 回出ても正しい）
            if (ItemGrantRules.Decide(kind, owned, false) == GrantOutcome.New)
            {
                await idols.TryInsertAsync(tx, playerId, itemId);
                return new GachaResult(0, kind, itemId, GrantOutcome.New, 0, 0, null);
            }

            var shards = conversions.GetAmount(kind, itemId);
            await idols.AddShardsAsync(tx, playerId, itemId, shards);
            return new GachaResult(0, kind, itemId, GrantOutcome.Duplicate, shards, 0, null);
        }

        var maxed = await supportCards.HasMaxLimitBreakAsync(tx, playerId, itemId);
        if (ItemGrantRules.Decide(kind, false, maxed) == GrantOutcome.Duplicate)
        {
            var points = conversions.GetAmount(kind, itemId);
            await players.AddSupportPointsAsync(tx, playerId, points);
            return new GachaResult(0, kind, itemId, GrantOutcome.Duplicate, 0, points, null);
        }

        var uid = Ulid.NewUlid();
        await supportCards.InsertAsync(tx, playerId, uid, itemId);
        return new GachaResult(0, kind, itemId, GrantOutcome.New, 0, 0, uid);
    }
}
