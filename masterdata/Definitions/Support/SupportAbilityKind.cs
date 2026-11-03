using MasterMemory.Addons;

namespace Uta.MasterData
{
    public enum SupportAbilityKind
    {
        [InputName("Nothing")] Nothing = 0,
        [InputName("InitialParam")] InitialParam = 1,
        [InputName("ParamBonus")] ParamBonus = 2,
        [InputName("MaxHp")] MaxHp = 3,
    }
}
