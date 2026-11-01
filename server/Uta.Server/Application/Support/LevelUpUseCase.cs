using Uta.Server.Application.Common;
using Uta.Server.Application.Player;
using VolTech.Server.Transaction;

namespace Uta.Server.Application.Support;

public sealed record LevelUpResult(int Level, int SupportPoints, int Gold);

public sealed class LevelUpUseCase(IPlayerRepository players, ISupportCardRepository supportCards, ISupportCatalog catalog)
{
    public async Task<LevelUpResult> ExecuteAsync(TxContext tx, Ulid playerId, Ulid uid, int targetLevel)
    {
        var state = await supportCards.FindAsync(tx, playerId, uid)
            ?? throw new UseCaseException(UseCaseError.SupportNotOwned, "support card not owned");

        var max = catalog.GetMaxLevel(state.CardId, state.LimitBreak);
        if (targetLevel <= state.Level || targetLevel > max)
        {
            throw new UseCaseException(UseCaseError.SupportInvalidTargetLevel, "invalid target level");
        }

        var cost = catalog.GetLevelUpCost(state.CardId, state.Level, targetLevel);
        var balance = await players.TrySpendSupportAndGoldAsync(tx, playerId, cost.SupportPoints, cost.Gold);
        if (balance is null)
        {
            var current = await players.FindAsync(tx, playerId)
                ?? throw new InvalidOperationException("player not found");
            throw current.SupportPoints < cost.SupportPoints
                ? new UseCaseException(UseCaseError.SupportNotEnoughPoints, "not enough support points")
                : new UseCaseException(UseCaseError.PlayerNotEnoughGold, "not enough gold");
        }

        await supportCards.UpdateLevelAsync(tx, uid, targetLevel);

        tx.WriteGameLog(GameLog.Event("support", "support_level_up")
            .Target(uid.ToString())
            .Field("card_id", state.CardId).Field("from_level", state.Level).Field("to_level", targetLevel)
            .Field("support_points_spent", cost.SupportPoints).Field("gold_spent", cost.Gold)
            .Field("support_points_after", balance.SupportPoints).Field("gold_after", balance.Gold));

        return new LevelUpResult(targetLevel, balance.SupportPoints, balance.Gold);
    }
}
