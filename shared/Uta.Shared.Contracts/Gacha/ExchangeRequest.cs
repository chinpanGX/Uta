using MessagePack;

namespace Uta.Shared.Contracts.Gacha
{
    [MessagePackObject]
    public sealed class ExchangeRequest
    {
        [SerializationConstructor]
        public ExchangeRequest(int gachaId, int ceilingItemOrder)
        {
            GachaId = gachaId;
            CeilingItemOrder = ceilingItemOrder;
        }

        [Key(0)]
        public int GachaId { get; }

        /// <summary>GachaCeilingItem の Order。</summary>
        [Key(1)]
        public int CeilingItemOrder { get; }
    }
}
