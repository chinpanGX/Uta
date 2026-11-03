using Uta.Shared.Domain.Produce;

namespace Uta.Server.Application.Produce;

public sealed record ProduceReward(int GachaCurrency, int Gold, int SupportPoints, int Exp);

public interface IProduceRewardCatalog
{
    /// <summary>ランクの報酬。F〜SSPlus 以外（Nothing）は InvalidOperationException。</summary>
    ProduceReward Get(ProduceRank rank);
}

public interface IPlayerLevelCatalog
{
    /// <summary>Level の昇順（Lv 1 → 2 が先頭）。</summary>
    IReadOnlyList<int> RequiredExps { get; }
}
