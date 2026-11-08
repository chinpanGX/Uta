using System;
using Cysharp.Threading.Tasks;
using Uta.Client.Application.Auth;
using Uta.Client.Application.Player;

namespace Uta.Client.Presentation.Title
{
    /// <summary>
    /// 疎通の診断（トークンの取り直し・再認証の再送・サーバーのエラーの受け取り）。
    /// 診断用（-uta-quit-after-check）。画面の計画で消す。
    /// </summary>
    public sealed class ConnectivityCheck
    {
        private readonly IDeviceAuthUseCase deviceAuth;
        private readonly IPlayerUseCase player;

        public ConnectivityCheck(IDeviceAuthUseCase deviceAuth, IPlayerUseCase player)
        {
            this.deviceAuth = deviceAuth;
            this.player = player;
        }

        /// <returns>全部通れば true。server error delivery が届かなければ false。それ以外の失敗は例外のまま投げる</returns>
        public async UniTask<bool> RunAsync(Action<string> log)
        {
            await deviceAuth.ReissueTokenAsync();
            log("reissue: ok");

            var before = deviceAuth.TokenInvalidationCount;
            await deviceAuth.InvalidateServerSideTokenAsync();
            await player.EnterAsync(false);
            if (deviceAuth.TokenInvalidationCount <= before)
            {
                throw new InvalidOperationException("reauth retry: not observed");
            }

            log("reauth retry: ok (invalidations=" + (deviceAuth.TokenInvalidationCount - before) + ")");

            if (await deviceAuth.CanReceiveServerErrorAsync())
            {
                log("server error delivery: ok");
                log("connectivity check: OK");
                return true;
            }

            log("server error delivery: FAILED");
            log("connectivity check: FAILED server error delivery");
            return false;
        }
    }
}
