using Uta.MasterData;
using Uta.Server.Application.Common;
using Uta.Server.Domain.Idol;
using Uta.Server.Domain.Player;
using Uta.Server.Domain.Support;
using VolTech.Server.Transaction;

namespace Uta.Server.Application.Player;

public sealed class SignUpUseCase(
    IPlayerRepository players, IIdolRepository idols, ISupportCardRepository supportCards, MemoryDatabase master)
{
    public async Task<bool> ExecuteAsync(TxContext tx, Ulid playerId)
    {
        if (!await players.InsertIfAbsentAsync(tx, playerId)) return false;
        var setting = master.InitialSettingTable.FindById(1);
        var idolIds = master.IdolTable.All.Where(x => x.Rarity == Rarity.R).Select(x => x.IdolId).ToList();
        var supportCardIds = master.InitialSupportCardTable.All.Select(x => x.SupportCardId).ToList();
        await players.GrantCurrenciesAsync(tx, playerId, setting.GachaCurrency, setting.Gold, setting.SupportPoints);
        foreach (var idolId in idolIds) await idols.TryInsertAsync(tx, playerId, idolId);
        var uids = new List<string>();
        foreach (var cardId in supportCardIds)
        {
            var uid = Ulid.NewUlid();
            await supportCards.InsertAsync(tx, playerId, uid, cardId);
            uids.Add(uid.ToString());
        }

        // 初期付与のゲームログを 1 件（server-foundation.md「ゲームログ」）。再送・2 回目以降・失敗のときは書かれない
        tx.WriteGameLog(GameLog.Event("player", "sign_up")
            .Target(playerId.ToString())
            .Field("gacha_currency", setting.GachaCurrency)
            .Field("gold", setting.Gold)
            .Field("support_points", setting.SupportPoints)
            .Field("idol_ids", GameLogJson.Ints(idolIds))
            .Field("support_card_ids", GameLogJson.Ints(supportCardIds))
            .Field("support_card_uids", GameLogJson.Strings(uids)));
        return true;
    }
}
