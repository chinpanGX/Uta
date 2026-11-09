using Uta.MasterData;
using Uta.Server.Application.Common;
using Uta.Server.Domain.Player;
using Uta.Server.Domain.Support;
using VolTech.Server.Transaction;

namespace Uta.Server.Application.Support;

public sealed record LevelUpResult(int Level, int SupportPoints, int Gold);

public sealed class LevelUpUseCase(IPlayerRepository players, ISupportCardRepository supportCards, MemoryDatabase master)
{
    public async Task<LevelUpResult> ExecuteAsync(TxContext tx, Ulid playerId, Ulid uid, int targetLevel)
    {
        var state = await supportCards.FindAsync(tx, playerId, uid)
            ?? throw new UseCaseException(UseCaseError.SupportNotOwned, "support card not owned");

        var rarity = master.SupportCardTable.FindBySupportCardId(state.MasterId).Rarity;
        var max = master.SupportLevelCapTable.FindByRarityAndLimitBreak((rarity, state.LimitBreak)).MaxLevel;
        if (targetLevel <= state.Level || targetLevel > max)
        {
            throw new UseCaseException(UseCaseError.SupportInvalidTargetLevel, "invalid target level");
        }

        // Lv state.Level → targetLevel の必要量は、SupportLevelCost の Level が state.Level 以上 targetLevel 未満の行の和
        var costs = master.SupportLevelCostTable
            .FindRangeByRarityAndLevel((rarity, state.Level), (rarity, targetLevel - 1)).ToList();
        if (costs.Count != targetLevel - state.Level)
        {
            throw new InvalidOperationException($"support level cost not found: {rarity} level {state.Level}..{targetLevel - 1}");
        }

        var supportPoints = costs.Sum(x => x.SupportPoints);
        var gold = costs.Sum(x => x.Gold);
        var balance = await players.TrySpendSupportAndGoldAsync(tx, playerId, supportPoints, gold);
        if (balance is null)
        {
            var current = await players.FindAsync(tx, playerId)
                ?? throw new InvalidOperationException("player not found");
            throw current.SupportPoints < supportPoints
                ? new UseCaseException(UseCaseError.SupportNotEnoughPoints, "not enough support points")
                : new UseCaseException(UseCaseError.PlayerNotEnoughGold, "not enough gold");
        }

        await supportCards.UpdateLevelAsync(tx, playerId, uid, targetLevel);

        tx.WriteGameLog(GameLog.Event("support", "support_level_up")
            .Target(uid.ToString())
            .Field("card_id", state.MasterId).Field("from_level", state.Level).Field("to_level", targetLevel)
            .Field("support_points_spent", supportPoints).Field("gold_spent", gold)
            .Field("support_points_after", balance.SupportPoints).Field("gold_after", balance.Gold));

        return new LevelUpResult(targetLevel, balance.SupportPoints, balance.Gold);
    }
}
