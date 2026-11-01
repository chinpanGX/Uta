using Uta.Shared.Domain.Rng;

namespace Uta.Server.Domain.Gacha;

public static class GachaPicker
{
    /// <summary>重みの並び（Order 昇順）から 1 つ選んだ添字を返す。RandomOps.WeightedIndex を呼ぶだけ。</summary>
    public static int Pick(IRandom random, IReadOnlyList<int> weights) => RandomOps.WeightedIndex(random, weights);
}
