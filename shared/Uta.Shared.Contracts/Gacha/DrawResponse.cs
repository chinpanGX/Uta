using MessagePack;

namespace Uta.Shared.Contracts.Gacha
{
    [MessagePackObject]
    public sealed class DrawResponse
    {
        [SerializationConstructor]
        public DrawResponse(DrawResultDto[] results, int gachaCurrency, int exchangePoints)
        {
            Results = results;
            GachaCurrency = gachaCurrency;
            ExchangePoints = exchangePoints;
        }

        [Key(0)]
        public DrawResultDto[] Results { get; }

        /// <summary>残り。</summary>
        [Key(1)]
        public int GachaCurrency { get; }

        /// <summary>このガチャの交換pt。</summary>
        [Key(2)]
        public int ExchangePoints { get; }
    }
}
