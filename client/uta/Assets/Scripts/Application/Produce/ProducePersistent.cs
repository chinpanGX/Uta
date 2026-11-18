using Uta.Shared.Domain.Lesson;

namespace Uta.Client.Application.Produce
{
    public sealed class ProducePersistent
    {
        public ProducePersistent(PersistentTrigger trigger, int remainingTurns, int remainingCount)
        {
            Trigger = trigger;
            RemainingTurns = remainingTurns;
            RemainingCount = remainingCount;
        }

        public PersistentTrigger Trigger { get; }
        public int RemainingTurns { get; }
        public int RemainingCount { get; }
    }
}
