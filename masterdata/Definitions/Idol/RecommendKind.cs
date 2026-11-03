using MasterMemory.Addons;

namespace Uta.MasterData
{
    public enum RecommendKind
    {
        [InputName("Nothing")] Nothing = 0,
        [InputName("GoodCondition")] GoodCondition = 1,
        [InputName("Focus")] Focus = 2,
        [InputName("Impression")] Impression = 3,
        [InputName("Motivation")] Motivation = 4,
    }
}
