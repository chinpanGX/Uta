using MessagePack;
using Uta.Shared.Domain.Lesson;

namespace Uta.Shared.Contracts.Produce
{
    [MessagePackObject]
    public sealed class PersistentDto
    {
        [SerializationConstructor]
        public PersistentDto(PersistentTrigger trigger, int remainingTurns, int remainingCount)
        {
            Trigger = trigger;
            RemainingTurns = remainingTurns;
            RemainingCount = remainingCount;
        }

        [Key(0)]
        public PersistentTrigger Trigger { get; }

        [Key(1)]
        public int RemainingTurns { get; }

        [Key(2)]
        public int RemainingCount { get; }
    }
}
