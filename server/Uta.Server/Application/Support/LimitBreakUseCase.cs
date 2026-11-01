using Uta.Server.Application.Common;
using VolTech.Server.Transaction;

namespace Uta.Server.Application.Support;

public sealed record LimitBreakResult(int LimitBreak, int MaxLevel);

public sealed class LimitBreakUseCase(ISupportCardRepository supportCards, ISupportCatalog catalog)
{
    public async Task<LimitBreakResult> ExecuteAsync(TxContext tx, Ulid playerId, Ulid uid, Ulid materialUid)
    {
        var target = await supportCards.FindAsync(tx, playerId, uid)
            ?? throw new UseCaseException(UseCaseError.SupportNotOwned, "support card not owned");

        if (materialUid == uid)
        {
            throw new UseCaseException(UseCaseError.SupportInvalidMaterial, "material is the same card");
        }

        if (!await supportCards.TryDeleteMaterialAsync(tx, playerId, materialUid, target.CardId))
        {
            throw new UseCaseException(UseCaseError.SupportInvalidMaterial, "invalid material");
        }

        // 例外でトランザクションごと戻り、素材の削除も戻る
        var limitBreak = await supportCards.TryLimitBreakAsync(tx, playerId, uid)
            ?? throw new UseCaseException(UseCaseError.SupportMaxLimitBreak, "already at max limit break");

        tx.WriteGameLog(GameLog.Event("support", "support_limit_break")
            .Target(uid.ToString())
            .Field("card_id", target.CardId).Field("material_uid", materialUid.ToString()).Field("limit_break", limitBreak));

        return new LimitBreakResult(limitBreak, catalog.GetMaxLevel(target.CardId, limitBreak));
    }
}
