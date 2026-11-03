using MessagePack;

namespace Uta.Shared.Contracts.Produce
{
    [MessagePackObject]
    public sealed class ExecuteLessonActionRequest
    {
        [SerializationConstructor]
        public ExecuteLessonActionRequest(int operationNumber, LessonActionKind kind, int cardInstanceId, int drinkSlotIndex)
        {
            OperationNumber = operationNumber;
            Kind = kind;
            CardInstanceId = cardInstanceId;
            DrinkSlotIndex = drinkSlotIndex;
        }

        [Key(0)]
        public int OperationNumber { get; }

        [Key(1)]
        public LessonActionKind Kind { get; }

        [Key(2)]
        public int CardInstanceId { get; }

        [Key(3)]
        public int DrinkSlotIndex { get; }
    }
}
