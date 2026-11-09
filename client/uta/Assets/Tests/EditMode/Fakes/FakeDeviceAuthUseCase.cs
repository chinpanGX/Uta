using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Uta.Client.Application.Auth;

namespace Uta.Client.Tests
{
    /// <summary>IDeviceAuthUseCase の偽（Presenter・Flow のテスト用）。</summary>
    public sealed class FakeDeviceAuthUseCase : IDeviceAuthUseCase
    {
        private readonly CallLog log;

        /// <summary>log が null なら記録しない。</summary>
        public FakeDeviceAuthUseCase(CallLog log = null)
        {
            this.log = log;
        }

        /// <summary>SignInAsync の 1 回ごとの振る舞い。空なら UniTask.FromResult(false)（登録済みの端末）。Dequeue した Func を呼んでその UniTask を返す。</summary>
        public Queue<Func<UniTask<bool>>> SignInResults { get; } = new Queue<Func<UniTask<bool>>>();

        public int TokenInvalidationCount { get; private set; }

        public UniTask<bool> SignInAsync()
        {
            if (log != null)
            {
                log.Add("DeviceSignIn");
            }

            return SignInResults.Count > 0 ? SignInResults.Dequeue()() : UniTask.FromResult(false);
        }

        public UniTask ReissueTokenAsync()
        {
            return UniTask.CompletedTask;
        }

        public UniTask<bool> CanReceiveServerErrorAsync()
        {
            return UniTask.FromResult(true);
        }

        public UniTask InvalidateServerSideTokenAsync()
        {
            TokenInvalidationCount++;
            return UniTask.CompletedTask;
        }
    }
}
