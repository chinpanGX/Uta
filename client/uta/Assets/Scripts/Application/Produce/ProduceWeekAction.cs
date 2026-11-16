using Uta.Shared.Domain.Produce;

namespace Uta.Client.Application.Produce
{
    public sealed class ProduceWeekAction
    {
        public ProduceWeekAction(int slotIndex, WeekChoiceKind kind, bool selectable)
        {
            SlotIndex = slotIndex;
            Kind = kind;
            Selectable = selectable;
        }

        public int SlotIndex { get; }
        public WeekChoiceKind Kind { get; }
        public bool Selectable { get; }
    }
}
