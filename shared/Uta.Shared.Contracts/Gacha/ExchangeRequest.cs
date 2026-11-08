using MessagePack;

namespace Uta.Shared.Contracts.Gacha
{
    [MessagePackObject]
    public sealed class ExchangeRequest
    {
        [SerializationConstructor]
        public ExchangeRequest(int gachaMasterId, int ceilingItemOrder)
        {
            GachaMasterId = gachaMasterId;
            CeilingItemOrder = ceilingItemOrder;
        }

        [Key(0)]
        public int GachaMasterId { get; }

        /// <summary>GachaCeilingItem の Order。</summary>
        [Key(1)]
        public int CeilingItemOrder { get; }
    }
}
