using MasterMemory.Addons;

namespace Uta.MasterData
{
    public enum ActionKind
    {
        [InputName("Nothing")] Nothing = 0,
        [InputName("Lesson")] Lesson = 1,
        [InputName("Class")] Class = 2,
        [InputName("Supply")] Supply = 3,
        [InputName("Outing")] Outing = 4,
        [InputName("Consult")] Consult = 5,
    }
}
