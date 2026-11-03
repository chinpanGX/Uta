using MasterMemory.Addons;

namespace Uta.MasterData
{
    public enum StatusKind
    {
        [InputName("Nothing")] Nothing = 0,
        [InputName("GoodCondition")] GoodCondition = 1,
        [InputName("Excellent")] Excellent = 2,
        [InputName("Focus")] Focus = 3,
        [InputName("FocusBoost")] FocusBoost = 4,
        [InputName("Impression")] Impression = 5,
        [InputName("ImpressionBoost")] ImpressionBoost = 6,
        [InputName("Motivation")] Motivation = 7,
        [InputName("CostHalf")] CostHalf = 8,
        [InputName("CostDouble")] CostDouble = 9,
        [InputName("CostReduce")] CostReduce = 10,
        [InputName("ShieldGainBlock")] ShieldGainBlock = 11,
        [InputName("DebuffGuard")] DebuffGuard = 12,
    }
}
