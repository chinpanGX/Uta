using Uta.Server.Application.Common;
using Uta.Server.Application.Idol;
using Uta.Server.Application.Support;
using VolTech.Server.Transaction;

namespace Uta.Server.Application.Player;

public sealed class SignUpUseCase(
    IPlayerRepository players, IIdolRepository idols, ISupportCardRepository supportCards, IInitialGrantCatalog catalog)
{
    public async Task<bool> ExecuteAsync(TxContext tx, Ulid playerId)
    {
        if (!await players.InsertIfAbsentAsync(tx, playerId)) return false;
        var grant = catalog.Get();
        await players.GrantCurrenciesAsync(tx, playerId, grant.GachaCurrency, grant.Gold, grant.SupportPoints);
        foreach (var idolId in grant.IdolIds) await idols.TryInsertAsync(tx, playerId, idolId);
        var uids = new List<string>();
        foreach (var cardId in grant.SupportCardIds)
        {
            var uid = Ulid.NewUlid();
            await supportCards.InsertAsync(tx, playerId, uid, cardId);
            uids.Add(uid.ToString());
        }

        // 初期付与のゲームログを 1 件（server-foundation.md「ゲームログ」）。再送・2 回目以降・失敗のときは書かれない
        tx.WriteGameLog(GameLog.Event("player", "sign_up")
            .Target(playerId.ToString())
            .Field("gacha_currency", grant.GachaCurrency)
            .Field("gold", grant.Gold)
            .Field("support_points", grant.SupportPoints)
            .Field("idol_ids", GameLogJson.Ints(grant.IdolIds))
            .Field("support_card_ids", GameLogJson.Ints(grant.SupportCardIds))
            .Field("support_card_uids", GameLogJson.Strings(uids)));
        return true;
    }
}
