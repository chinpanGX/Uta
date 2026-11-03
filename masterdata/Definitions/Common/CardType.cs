using MasterMemory.Addons;

namespace Uta.MasterData
{
    public enum CardType
    {
        [InputName("Nothing")] Nothing = 0,
        [InputName("Active")] Active = 1,
        [InputName("Mental")] Mental = 2,
        [InputName("Trouble")] Trouble = 3,
    }
}
