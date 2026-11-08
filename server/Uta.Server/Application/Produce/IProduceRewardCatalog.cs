using Uta.Server.Domain.Produce;
using Uta.Shared.Domain.Produce;

namespace Uta.Server.Application.Produce;

public interface IProduceRewardCatalog
{
    /// <summary>ランクの報酬。F〜SSPlus 以外（Nothing）は InvalidOperationException。</summary>
    ProduceReward Get(ProduceRank rank);
}
