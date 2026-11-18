using Uta.MasterData;
using Uta.Server.Application.Common;
using Uta.Server.Domain.Player;
using Uta.Server.Domain.Produce;
using Uta.Shared.Domain.Player;
using Uta.Shared.Domain.Produce;
using VolTech.Server.Transaction;

namespace Uta.Server.Application.Produce;

public sealed class ProduceCompletion(
    IPlayerRepository players, IProduceSessionRepository sessions, MemoryDatabase master, PlayerLevelCatalog levelCatalog)
{
    /// <summary>Phase が Finished の state の報酬・経験値・produce_results・ゲームログを 1 回分書く。セッションの status は呼び出し側が更新する。</summary>
    public async Task<ProduceEndResult> CompleteAsync(TxContext tx, Ulid playerId, Ulid sessionId, ProduceState state)
    {
        var result = state.Result ?? throw new InvalidOperationException("result missing");
        var row = master.EvaluationRankRewardTable.FindByRank((Uta.MasterData.ProduceRank)(int)result.Rank);
        var reward = new ProduceReward(row.GachaCurrency, row.Gold, row.SupportPoints, row.Exp);
        var player = await players.FindAsync(tx, playerId) ?? throw new InvalidOperationException("player not found");
        var exp = PlayerExpRules.AddExp(player.Exp, reward.Exp, levelCatalog.MaxExp);
        await players.GrantProduceRewardAsync(tx, playerId, reward, exp);
        await sessions.InsertResultAsync(tx, new ProduceResultRecord(
            sessionId, playerId, ProduceSessionStatus.Completed, result.ResultKind, result.EvaluationValue, result.Rank,
            result.FinalScore, result.FinalRank, reward.GachaCurrency, reward.Gold, reward.SupportPoints, reward.Exp));
        tx.WriteGameLog(GameLog.Event("produce", "produce_reward")
            .Target(sessionId.ToString())
            .Field("rank", (int)result.Rank)
            .Field("evaluation_value", result.EvaluationValue)
            .Field("gacha_currency", reward.GachaCurrency)
            .Field("gold", reward.Gold)
            .Field("support_points", reward.SupportPoints)
            .Field("exp", reward.Exp)
            .Field("level_after", levelCatalog.GetLevel(exp))
            .Field("exp_after", exp));
        return new ProduceEndResult(
            ProduceSessionStatus.Completed, result.ResultKind, result.EvaluationValue, result.Rank, result.FinalScore, result.FinalRank,
            reward.GachaCurrency, reward.Gold, reward.SupportPoints, reward.Exp, exp);
    }
}
