using System;
using Cysharp.Threading.Tasks;
using Uta.Client.Application.Auth;
using Uta.Client.Application.Player;

namespace Uta.Client.Presentation.Title
{
    /// <summary>
    /// 疎通の診断（トークンの取り直し・再認証の再送・サーバーのエラーの受け取り）。
    /// 診断用（-uta-quit-after-check）。`TitleDiagnostics` が使う。
    /// </summary>
    public sealed class ConnectivityCheck
    {
        private readonly IDeviceAuthUseCase deviceAuthUseCase;
        private readonly IPlayerUseCase playerUseCase;

        public ConnectivityCheck(IDeviceAuthUseCase deviceAuthUseCase, IPlayerUseCase playerUseCase)
        {
            this.deviceAuthUseCase = deviceAuthUseCase;
            this.playerUseCase = playerUseCase;
        }

        /// <returns>全部通れば true。server error delivery が届かなければ false。それ以外の失敗は例外のまま投げる</returns>
        public async UniTask<bool> RunAsync(Action<string> log)
        {
            await deviceAuthUseCase.ReissueTokenAsync();
            log("reissue: ok");

            var before = deviceAuthUseCase.TokenInvalidationCount;
            await deviceAuthUseCase.InvalidateServerSideTokenAsync();
            await playerUseCase.EnterAsync(false);
            if (deviceAuthUseCase.TokenInvalidationCount <= before)
            {
                throw new InvalidOperationException("reauth retry: not observed");
            }

            log("reauth retry: ok (invalidations=" + (deviceAuthUseCase.TokenInvalidationCount - before) + ")");

            if (await deviceAuthUseCase.CanReceiveServerErrorAsync())
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
