using MessagePack;

namespace Uta.Shared.Contracts.Produce
{
    [MessagePackObject]
    public sealed class ResolveCardOfferRequest
    {
        [SerializationConstructor]
        public ResolveCardOfferRequest(int operationNumber, CardOfferActionKind kind, int offerIndex)
        {
            OperationNumber = operationNumber;
            Kind = kind;
            OfferIndex = offerIndex;
        }

        [Key(0)]
        public int OperationNumber { get; }

        [Key(1)]
        public CardOfferActionKind Kind { get; }

        [Key(2)]
        public int OfferIndex { get; }
    }
}
