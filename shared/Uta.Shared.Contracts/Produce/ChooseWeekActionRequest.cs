using MessagePack;
using Uta.Shared.Domain.Lesson;

namespace Uta.Shared.Contracts.Produce
{
    [MessagePackObject]
    public sealed class ChooseWeekActionRequest
    {
        [SerializationConstructor]
        public ChooseWeekActionRequest(int operationNumber, int slotIndex, int optionIndex, ParamKind param)
        {
            OperationNumber = operationNumber;
            SlotIndex = slotIndex;
            OptionIndex = optionIndex;
            Param = param;
        }

        [Key(0)]
        public int OperationNumber { get; }

        [Key(1)]
        public int SlotIndex { get; }

        [Key(2)]
        public int OptionIndex { get; }

        [Key(3)]
        public ParamKind Param { get; }
    }
}
