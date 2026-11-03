using MasterMemory.Addons;

namespace Uta.MasterData
{
    public enum CostKind
    {
        [InputName("Nothing")] Nothing = 0,
        [InputName("Consume")] Consume = 1,
        [InputName("HpDirect")] HpDirect = 2,
        [InputName("GoodConditionTurns")] GoodConditionTurns = 3,
        [InputName("ExcellentTurns")] ExcellentTurns = 4,
        [InputName("Focus")] Focus = 5,
        [InputName("Impression")] Impression = 6,
        [InputName("Motivation")] Motivation = 7,
    }
}
