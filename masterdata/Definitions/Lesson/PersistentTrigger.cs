using MasterMemory.Addons;

namespace Uta.MasterData
{
    public enum PersistentTrigger
    {
        [InputName("Nothing")] Nothing = 0,
        [InputName("LessonStart")] LessonStart = 1,
        [InputName("TurnStart")] TurnStart = 2,
        [InputName("AfterTurnStart")] AfterTurnStart = 3,
        [InputName("EveryNTurns")] EveryNTurns = 4,
        [InputName("OnCostHpDecrease")] OnCostHpDecrease = 5,
        [InputName("OnCardUse")] OnCardUse = 6,
        [InputName("OnActiveCardUse")] OnActiveCardUse = 7,
        [InputName("OnMentalCardUse")] OnMentalCardUse = 8,
        [InputName("AfterCardUse")] AfterCardUse = 9,
        [InputName("AfterActiveCardUse")] AfterActiveCardUse = 10,
        [InputName("AfterMentalCardUse")] AfterMentalCardUse = 11,
        [InputName("OnSkip")] OnSkip = 12,
        [InputName("TurnEnd")] TurnEnd = 13,
    }
}
