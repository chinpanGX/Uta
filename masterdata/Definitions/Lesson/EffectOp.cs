using MasterMemory.Addons;

namespace Uta.MasterData
{
    public enum EffectOp
    {
        [InputName("Nothing")] Nothing = 0,
        [InputName("GainParam")] GainParam = 1,
        [InputName("GainParamByImpression")] GainParamByImpression = 2,
        [InputName("GainParamByShield")] GainParamByShield = 3,
        [InputName("GainParamByMotivation")] GainParamByMotivation = 4,
        [InputName("AddParamBonus")] AddParamBonus = 5,
        [InputName("AddShield")] AddShield = 6,
        [InputName("AddFixedShield")] AddFixedShield = 7,
        [InputName("RecoverHp")] RecoverHp = 8,
        [InputName("ScaleShield")] ScaleShield = 9,
        [InputName("SetShield")] SetShield = 10,
        [InputName("AddStatus")] AddStatus = 11,
        [InputName("AddStatusPercent")] AddStatusPercent = 12,
        [InputName("DrawCards")] DrawCards = 13,
        [InputName("AddCardUses")] AddCardUses = 14,
        [InputName("ReplaceHand")] ReplaceHand = 15,
        [InputName("EnhanceHand")] EnhanceHand = 16,
        [InputName("CreateCard")] CreateCard = 17,
        [InputName("ScheduleEffect")] ScheduleEffect = 18,
        [InputName("AddPersistent")] AddPersistent = 19,
    }
}
