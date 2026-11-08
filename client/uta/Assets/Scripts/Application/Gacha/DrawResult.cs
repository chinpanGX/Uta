using System;
using System.Collections.Generic;

namespace Uta.Client.Application.Gacha
{
    /// <summary>ガチャを引いた結果。Succeeded のときだけ Results・GachaCurrency・ExchangePoints に値が入る。</summary>
    public sealed class DrawResult
    {
        private DrawResult(DrawResultKind kind, IReadOnlyList<GachaResult> results, int gachaCurrency, int exchangePoints)
        {
            Kind = kind;
            Results = results;
            GachaCurrency = gachaCurrency;
            ExchangePoints = exchangePoints;
        }

        public DrawResultKind Kind { get; }

        /// <summary>Succeeded のとき Slot の昇順。それ以外は空のリスト。</summary>
        public IReadOnlyList<GachaResult> Results { get; }

        /// <summary>Succeeded のとき引いた後の残り（DrawResponse.GachaCurrency）。それ以外は 0。</summary>
        public int GachaCurrency { get; }

        /// <summary>Succeeded のとき引いた後のこのガチャの交換pt。それ以外は 0。</summary>
        public int ExchangePoints { get; }

        public static DrawResult Success(IReadOnlyList<GachaResult> results, int gachaCurrency, int exchangePoints)
        {
            if (results == null)
            {
                throw new ArgumentNullException("results");
            }

            return new DrawResult(DrawResultKind.Succeeded, results, gachaCurrency, exchangePoints);
        }

        public static DrawResult Rejected(DrawResultKind kind)
        {
            if (kind == DrawResultKind.Succeeded)
            {
                throw new ArgumentException("kind must not be Succeeded", nameof(kind));
            }

            return new DrawResult(kind, Array.Empty<GachaResult>(), 0, 0);
        }
    }
}
