using System;
using System.Collections.Generic;
using System.Linq;
using Uta.Client.Application.Gacha;
using Uta.Shared.Contracts.Gacha;
using ZLinq;

namespace Uta.Client.Infrastructure.Gacha
{
    /// <summary>DTO → Application の型。</summary>
    public static class GachaMapper
    {
        /// <summary>getGachasResponse.Gachas の中で GachaMasterId == gachaMasterId の最初の行の ExchangePoints。Gachas が null か、行が無ければ 0。</summary>
        public static int ToExchangePoints(GetGachasResponse getGachasResponse, int gachaMasterId)
        {
            if (getGachasResponse.Gachas == null)
            {
                return 0;
            }

            foreach (var status in getGachasResponse.Gachas)
            {
                if (status.GachaMasterId == gachaMasterId)
                {
                    return status.ExchangePoints;
                }
            }

            return 0;
        }

        /// <summary>Success に詰め替える。Results は Slot の昇順に並べ直す（null なら空）。</summary>
        public static DrawResult ToDrawResult(DrawResponse drawResponse)
        {
            var results = new List<GachaResult>();
            if (drawResponse.Results != null)
            {
                results.AddRange(drawResponse.Results
                    .Select(dto => new GachaResult(
                        dto.Slot,
                        (Uta.MasterData.ItemKind)(int)dto.ItemKind,
                        dto.ItemMasterId,
                        dto.Outcome == GachaOutcome.New,
                        dto.Shards,
                        dto.SupportPoints))
                    .OrderBy(r => r.Slot)
                    .ToArray());
            }

            return DrawResult.Success(results, drawResponse.GachaCurrency, drawResponse.ExchangePoints);
        }
    }
}
