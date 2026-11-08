using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Uta.Client.Application.Gacha;

namespace Uta.Client.Presentation.Gacha
{
    public sealed class GachaResultArgs
    {
        /// <summary>results が null なら ArgumentNullException。retry は null を許す（「もう一度」は閉じるだけになる）。</summary>
        public GachaResultArgs(IReadOnlyList<GachaResult> results, Func<UniTask> retry)
        {
            Results = results ?? throw new ArgumentNullException(nameof(results));
            Retry = retry;
        }

        public IReadOnlyList<GachaResult> Results { get; }
        public Func<UniTask> Retry { get; }
    }
}
