using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Uta.Client.Application.Auth;
using Uta.Client.Application.Player;
using VContainer;

namespace Uta.Client.Presentation.Title
{
    /// <summary>診断（-uta-quit-after-check）。画面に出さない。</summary>
    public sealed class TitleDiagnostics
    {
        private readonly IDeviceAuthUseCase deviceAuthUseCase;
        private readonly IPlayerUseCase playerUseCase;
        private readonly ConnectivityCheck connectivityCheck;
        private readonly Action<int> quit;

        [Inject]
        public TitleDiagnostics(IDeviceAuthUseCase deviceAuthUseCase, IPlayerUseCase playerUseCase, ConnectivityCheck connectivityCheck)
            : this(deviceAuthUseCase, playerUseCase, connectivityCheck, code => UnityEngine.Application.Quit(code))
        {
        }

        /// <summary>テスト用。quit は終了の代わり（終了コードを受ける）。</summary>
        public TitleDiagnostics(IDeviceAuthUseCase deviceAuthUseCase, IPlayerUseCase playerUseCase, ConnectivityCheck connectivityCheck, Action<int> quit)
        {
            this.deviceAuthUseCase = deviceAuthUseCase;
            this.playerUseCase = playerUseCase;
            this.connectivityCheck = connectivityCheck;
            this.quit = quit ?? throw new ArgumentNullException(nameof(quit));
        }

        public async UniTask RunAsync()
        {
            var succeeded = false;
            try
            {
                Log("startup: start");
                var registered = await deviceAuthUseCase.SignInAsync();
                Log(StartupLogs.Device(registered));
                var entered = await playerUseCase.EnterAsync(registered);
                Log(StartupLogs.SignUp(entered));
                Log(StartupLogs.SignIn(entered));
                succeeded = await connectivityCheck.RunAsync(Log);
            }
            catch (Exception e)
            {
                Log(StartupLogs.Failed(e));
                succeeded = false;
            }

            quit(succeeded ? 0 : 1);
        }

        private static void Log(string line)
        {
            Debug.Log("[Uta] " + line);
        }
    }
}
