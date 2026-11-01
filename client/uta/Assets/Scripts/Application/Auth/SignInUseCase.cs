using System;
using Cysharp.Threading.Tasks;

namespace Uta.Client.Application.Auth
{
    public sealed class SignInUseCase
    {
        private readonly IDeviceAuthPort port;

        public SignInUseCase(IDeviceAuthPort port)
        {
            this.port = port;
        }

        /// <summary>
        /// 先に GetAccountStateAsync を 1 回だけ呼ぶ（EnsureSignedInAsync の後に呼ぶと、常に Registered になる）。
        /// 状態が Corrupted なら、通信せずに InvalidOperationException（メッセージは "account corrupted"）を投げる（保存したデータには触らない）。
        /// それ以外は EnsureSignedInAsync を呼び、先に取った状態が NotRegistered だったなら true（サインアップした）、Registered だったなら false を返す。
        /// </summary>
        public async UniTask<bool> ExecuteAsync()
        {
            var state = await port.GetAccountStateAsync();
            if (state == AccountState.Corrupted)
            {
                throw new InvalidOperationException("account corrupted");
            }

            await port.EnsureSignedInAsync();
            return state == AccountState.NotRegistered;
        }

        public UniTask ReissueAsync()
        {
            return port.ReissueTokenAsync();
        }

        public UniTask<bool> CanReceiveServerErrorAsync()
        {
            return port.CanReceiveServerErrorAsync();
        }
    }
}
