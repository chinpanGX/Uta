using MasterMemory.Addons;

namespace Uta.MasterData
{
    public enum Comparator
    {
        [InputName("Nothing")] Nothing = 0,
        [InputName("GreaterOrEqual")] GreaterOrEqual = 1,
        [InputName("LessOrEqual")] LessOrEqual = 2,
        [InputName("Equal")] Equal = 3,
    }
}
