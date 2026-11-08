using MessagePack;
using Uta.Shared.Contracts.Common;
using VolTech.Server.Abstractions;

namespace Uta.Shared.Contracts.Gacha
{
    [MessagePackObject]
    public sealed class DrawResponse : IApiResponse<AppCommon>
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

        /// <summary>共通レスポンス項目。基盤が入れる。</summary>
        [Key(3)]
        public AppCommon Common { get; set; }
    }
}
