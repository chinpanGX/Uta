using MasterMemory.Addons;

namespace Uta.MasterData
{
    public enum ConditionSubject
    {
        [InputName("Nothing")] Nothing = 0,
        [InputName("GoodConditionTurns")] GoodConditionTurns = 1,
        [InputName("ExcellentTurns")] ExcellentTurns = 2,
        [InputName("Focus")] Focus = 3,
        [InputName("Impression")] Impression = 4,
        [InputName("Motivation")] Motivation = 5,
        [InputName("Shield")] Shield = 6,
        [InputName("HpPercent")] HpPercent = 7,
        [InputName("TurnNumber")] TurnNumber = 8,
        [InputName("ClearTargetPercent")] ClearTargetPercent = 9,
    }
}
