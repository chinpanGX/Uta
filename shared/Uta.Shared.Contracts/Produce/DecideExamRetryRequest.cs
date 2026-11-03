using MessagePack;

namespace Uta.Shared.Contracts.Produce
{
    [MessagePackObject]
    public sealed class DecideExamRetryRequest
    {
        [SerializationConstructor]
        public DecideExamRetryRequest(int operationNumber, bool retry)
        {
            OperationNumber = operationNumber;
            Retry = retry;
        }

        [Key(0)]
        public int OperationNumber { get; }

        [Key(1)]
        public bool Retry { get; }
    }
}
