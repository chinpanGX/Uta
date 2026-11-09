using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Uta.Client.Application.Gacha;

namespace Uta.Client.Tests
{
    /// <summary>IGachaUseCase の偽（Presenter のテスト用）。</summary>
    public sealed class FakeGachaUseCase : IGachaUseCase
    {
        private readonly CallLog log;

        /// <summary>log が null なら記録しない。log があれば DrawAsync の入口で "Draw:" + count を記録する（GetExchangePointsAsync は記録しない）。</summary>
        public FakeGachaUseCase(CallLog log = null)
        {
            this.log = log;
        }

        /// <summary>GetExchangePointsAsync の 1 回ごとの振る舞い。空なら 0。</summary>
        public Queue<Func<UniTask<int?>>> ExchangePointsResults { get; } = new Queue<Func<UniTask<int?>>>();

        /// <summary>DrawAsync の 1 回ごとの振る舞い。空なら InvalidOperationException。</summary>
        public Queue<Func<UniTask<DrawResult>>> DrawResults { get; } = new Queue<Func<UniTask<DrawResult>>>();

        /// <summary>DrawAsync のたびに gachaId + "," + count。</summary>
        public List<string> DrawCalls { get; } = new List<string>();

        public UniTask<int?> GetExchangePointsAsync(int gachaId)
        {
            if (ExchangePointsResults.Count > 0)
            {
                return ExchangePointsResults.Dequeue()();
            }

            return UniTask.FromResult<int?>(0);
        }

        public UniTask<DrawResult> DrawAsync(int gachaId, int count)
        {
            DrawCalls.Add(gachaId + "," + count);
            if (log != null)
            {
                log.Add("Draw:" + count);
            }

            if (DrawResults.Count == 0)
            {
                throw new InvalidOperationException("FakeGachaUseCase: no draw result");
            }

            return DrawResults.Dequeue()();
        }
    }
}
