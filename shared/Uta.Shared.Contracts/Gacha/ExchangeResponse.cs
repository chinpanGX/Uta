using MessagePack;

namespace Uta.Shared.Contracts.Gacha
{
    [MessagePackObject]
    public sealed class ExchangeResponse
    {
        [SerializationConstructor]
        public ExchangeResponse(DrawResultDto result, int exchangePoints)
        {
            Result = result;
            ExchangePoints = exchangePoints;
        }

        /// <summary>Slot は 0。</summary>
        [Key(0)]
        public DrawResultDto Result { get; }

        /// <summary>交換後の残り。</summary>
        [Key(1)]
        public int ExchangePoints { get; }
    }
}
