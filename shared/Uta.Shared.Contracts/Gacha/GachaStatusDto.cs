using MessagePack;

namespace Uta.Shared.Contracts.Gacha
{
    [MessagePackObject]
    public sealed class GachaStatusDto
    {
        [SerializationConstructor]
        public GachaStatusDto(int gachaMasterId, int exchangePoints)
        {
            GachaMasterId = gachaMasterId;
            ExchangePoints = exchangePoints;
        }

        [Key(0)]
        public int GachaMasterId { get; }

        [Key(1)]
        public int ExchangePoints { get; }
    }
}
