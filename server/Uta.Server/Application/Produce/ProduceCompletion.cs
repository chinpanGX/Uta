using Uta.Server.Application.Common;
using Uta.Server.Domain.Player;
using Uta.Server.Domain.Produce;
using Uta.Shared.Domain.Produce;
using VolTech.Server.Transaction;

namespace Uta.Server.Application.Produce;

public sealed class ProduceCompletion(
    IPlayerRepository players, IProduceSessionRepository sessions, IProduceRewardCatalog rewards, IPlayerLevelCatalog levels)
{
    /// <summary>Phase が Finished の state の報酬・Lv・produce_results・ゲームログを 1 回分書く。セッションの status は呼び出し側が更新する。</summary>
    public async Task<ProduceEndSummary> CompleteAsync(TxContext tx, Ulid playerId, Ulid sessionId, ProduceState state)
    {
        var result = state.Result ?? throw new InvalidOperationException("result missing");
        var reward = rewards.Get(result.Rank);
        var player = await players.FindAsync(tx, playerId) ?? throw new InvalidOperationException("player not found");
        var (level, exp) = PlayerLevelRules.AddExp(player.Level, player.Exp, reward.Exp, levels.RequiredExps);
        await players.GrantProduceRewardAsync(tx, playerId, reward, level, exp);
        await sessions.InsertResultAsync(tx, new ProduceResultRecord(
            sessionId, playerId, ProduceSessionStatus.Completed, result.Outcome, result.EvaluationValue, result.Rank,
            result.FinalScore, result.FinalRank, reward.GachaCurrency, reward.Gold, reward.SupportPoints, reward.Exp));
        tx.WriteGameLog(GameLog.Event("produce", "produce_reward")
            .Target(sessionId.ToString())
            .Field("rank", (int)result.Rank)
            .Field("evaluation_value", result.EvaluationValue)
            .Field("gacha_currency", reward.GachaCurrency)
            .Field("gold", reward.Gold)
            .Field("support_points", reward.SupportPoints)
            .Field("exp", reward.Exp)
            .Field("level_after", level)
            .Field("exp_after", exp));
        return new ProduceEndSummary(
            ProduceSessionStatus.Completed, result.Outcome, result.EvaluationValue, result.Rank, result.FinalScore, result.FinalRank,
            reward.GachaCurrency, reward.Gold, reward.SupportPoints, reward.Exp, level, exp);
    }
}
