using System;
using System.Collections.Generic;
using System.Linq;
using Uta.Client.Application.Gacha;
using Uta.Shared.Contracts.Gacha;

namespace Uta.Client.Infrastructure.Gacha
{
    /// <summary>DTO → Application の型。</summary>
    public static class GachaMapper
    {
        /// <summary>response.Gachas の中で GachaMasterId == gachaMasterId の最初の行の ExchangePoints。Gachas が null か、行が無ければ 0。</summary>
        public static int ToExchangePoints(GetGachasResponse response, int gachaMasterId)
        {
            if (response.Gachas == null)
            {
                return 0;
            }

            foreach (var status in response.Gachas)
            {
                if (status.GachaMasterId == gachaMasterId)
                {
                    return status.ExchangePoints;
                }
            }

            return 0;
        }

        /// <summary>Success に詰め替える。Results は Slot の昇順に並べ直す（null なら空）。</summary>
        public static DrawResult ToDrawResult(DrawResponse response)
        {
            var results = new List<GachaResult>();
            if (response.Results != null)
            {
                results.AddRange(response.Results
                    .Select(dto => new GachaResult(
                        dto.Slot,
                        (Uta.MasterData.ItemKind)(int)dto.ItemKind,
                        dto.ItemMasterId,
                        dto.Outcome == GachaOutcome.New,
                        dto.Shards,
                        dto.SupportPoints))
                    .OrderBy(r => r.Slot));
            }

            return DrawResult.Success(results, response.GachaCurrency, response.ExchangePoints);
        }
    }
}
