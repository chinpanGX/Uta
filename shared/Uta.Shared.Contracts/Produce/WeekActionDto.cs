using MessagePack;
using Uta.Shared.Domain.Produce;

namespace Uta.Shared.Contracts.Produce
{
    [MessagePackObject]
    public sealed class WeekActionDto
    {
        [SerializationConstructor]
        public WeekActionDto(int slotIndex, WeekChoiceKind kind, bool selectable)
        {
            SlotIndex = slotIndex;
            Kind = kind;
            Selectable = selectable;
        }

        [Key(0)]
        public int SlotIndex { get; }

        [Key(1)]
        public WeekChoiceKind Kind { get; }

        [Key(2)]
        public bool Selectable { get; }
    }
}
