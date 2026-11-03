using Uta.MasterData;
using Uta.Server.Application.Produce;
using ProduceRank = Uta.Shared.Domain.Produce.ProduceRank;

namespace Uta.Server.Infrastructure;

public sealed class MasterProduceRewardCatalog : IProduceRewardCatalog
{
    private readonly Dictionary<ProduceRank, ProduceReward> rewards;

    public MasterProduceRewardCatalog(MemoryDatabase db)
    {
        rewards = db.EvaluationRankRewardTable.All.ToDictionary(
            x => (ProduceRank)(int)x.Rank,
            x => new ProduceReward(x.GachaCurrency, x.Gold, x.SupportPoints, x.Exp));
    }

    public ProduceReward Get(ProduceRank rank) =>
        rewards.TryGetValue(rank, out var reward) ? reward : throw new InvalidOperationException($"produce reward not found: {rank}");
}
