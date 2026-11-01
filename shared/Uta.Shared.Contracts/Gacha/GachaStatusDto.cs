using MessagePack;

namespace Uta.Shared.Contracts.Gacha
{
    [MessagePackObject]
    public sealed class GachaStatusDto
    {
        [SerializationConstructor]
        public GachaStatusDto(int gachaId, int exchangePoints)
        {
            GachaId = gachaId;
            ExchangePoints = exchangePoints;
        }

        [Key(0)]
        public int GachaId { get; }

        [Key(1)]
        public int ExchangePoints { get; }
    }
}
