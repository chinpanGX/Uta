using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Uta.Client.Application.Support;

namespace Uta.Client.Tests
{
    /// <summary>ISupportUseCase の偽（Presenter のテスト用）。</summary>
    public sealed class FakeSupportUseCase : ISupportUseCase
    {
        private readonly CallLog log;

        /// <summary>log が null なら記録しない。log があれば入口で Calls と同じ文字列を記録する。</summary>
        public FakeSupportUseCase(CallLog log = null)
        {
            this.log = log;
        }

        /// <summary>LevelUpAsync の 1 回ごとの振る舞い。空なら InvalidOperationException。</summary>
        public Queue<Func<UniTask<LevelUpResult>>> LevelUpResults { get; } = new Queue<Func<UniTask<LevelUpResult>>>();

        /// <summary>LimitBreakAsync の 1 回ごとの振る舞い。空なら InvalidOperationException。</summary>
        public Queue<Func<UniTask<LimitBreakResult>>> LimitBreakResults { get; } = new Queue<Func<UniTask<LimitBreakResult>>>();

        /// <summary>"LevelUp:" + uid + "," + targetLevel／"LimitBreak:" + uid + "," + materialUid。</summary>
        public List<string> Calls { get; } = new List<string>();

        public UniTask<LevelUpResult> LevelUpAsync(string supportCardUniqueId, int targetLevel)
        {
            Record("LevelUp:" + supportCardUniqueId + "," + targetLevel);
            if (LevelUpResults.Count == 0)
            {
                throw new InvalidOperationException("FakeSupportUseCase: no level up result");
            }

            return LevelUpResults.Dequeue()();
        }

        public UniTask<LimitBreakResult> LimitBreakAsync(string supportCardUniqueId, string materialUniqueId)
        {
            Record("LimitBreak:" + supportCardUniqueId + "," + materialUniqueId);
            if (LimitBreakResults.Count == 0)
            {
                throw new InvalidOperationException("FakeSupportUseCase: no limit break result");
            }

            return LimitBreakResults.Dequeue()();
        }

        private void Record(string entry)
        {
            Calls.Add(entry);
            if (log != null)
            {
                log.Add(entry);
            }
        }
    }
}
