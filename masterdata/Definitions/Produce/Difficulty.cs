using MasterMemory.Addons;

namespace Uta.MasterData
{
    public enum Difficulty
    {
        [InputName("Nothing")] Nothing = 0,
        [InputName("Beginner")] Beginner = 1,
        [InputName("Intermediate")] Intermediate = 2,
        [InputName("Advanced")] Advanced = 3,
    }
}
