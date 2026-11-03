using MasterMemory.Addons;

namespace Uta.MasterData
{
    public enum WeekKind
    {
        [InputName("Nothing")] Nothing = 0,
        [InputName("Action")] Action = 1,
        [InputName("Finishing")] Finishing = 2,
        [InputName("MidExam")] MidExam = 3,
        [InputName("FinalExam")] FinalExam = 4,
    }
}
