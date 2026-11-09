using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Uta.Client.Application.Player;
using Uta.Client.Domain.Player;

namespace Uta.Client.Tests
{
    /// <summary>IPlayerUseCase の偽（Presenter・Flow のテスト用）。リポジトリには書かない。</summary>
    public sealed class FakePlayerUseCase : IPlayerUseCase
    {
        private readonly CallLog log;
        private readonly DiscardNoticeState notice;

        /// <summary>log が null なら記録しない。notice があれば MarkDiscardNoticeShown が notice.MarkShown() する。</summary>
        public FakePlayerUseCase(CallLog log = null, DiscardNoticeState notice = null)
        {
            this.log = log;
            this.notice = notice;
            Player = new PlayerEntity("P1", 1, 0, 0, 0, 0, false, false);
        }

        /// <summary>EnterAsync の 1 回ごとの振る舞い。空なら new EnterPlayerResult(afterRegistration, afterRegistration, Player, 0, 0)。</summary>
        public Queue<Func<UniTask<EnterPlayerResult>>> EnterResults { get; } = new Queue<Func<UniTask<EnterPlayerResult>>>();

        /// <summary>RefreshAsync の 1 回ごとの振る舞い。空なら完了済み。</summary>
        public Queue<Func<UniTask>> RefreshResults { get; } = new Queue<Func<UniTask>>();

        /// <summary>既定の EnterAsync が返すエンティティ。</summary>
        public PlayerEntity Player { get; set; }

        public int RefreshCount { get; private set; }

        public UniTask<EnterPlayerResult> EnterAsync(bool afterRegistration)
        {
            if (log != null)
            {
                log.Add("Enter:" + afterRegistration);
            }

            if (EnterResults.Count > 0)
            {
                return EnterResults.Dequeue()();
            }

            return UniTask.FromResult(new EnterPlayerResult(afterRegistration, afterRegistration, Player, 0, 0));
        }

        public UniTask RefreshAsync()
        {
            RefreshCount++;
            if (log != null)
            {
                log.Add("Refresh");
            }

            return RefreshResults.Count > 0 ? RefreshResults.Dequeue()() : UniTask.CompletedTask;
        }

        public void MarkDiscardNoticeShown()
        {
            if (log != null)
            {
                log.Add("MarkDiscardNoticeShown");
            }

            if (notice != null)
            {
                notice.MarkShown();
            }
        }
    }
}
