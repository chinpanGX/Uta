using MessagePack;
using Uta.Shared.Contracts.Common;
using VolTech.Server.Abstractions;

namespace Uta.Shared.Contracts.Gacha
{
    [MessagePackObject]
    public sealed class ExchangeResponse : IApiResponse<AppCommonDto>
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

        /// <summary>共通レスポンス項目。基盤が入れる。</summary>
        [Key(2)]
        public AppCommonDto Common { get; set; }
    }
}
