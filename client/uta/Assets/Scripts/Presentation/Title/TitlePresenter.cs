using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Uta.Client.Application.Auth;
using Uta.Client.Application.Navigation;
using Uta.Client.Application.Player;
using Uta.Client.Presentation.Startup;
using VContainer.Unity;

namespace Uta.Client.Presentation.Title
{
    public sealed class TitlePresenter : IStartable, IDisposable
    {
        private readonly TitleView view;
        private readonly SignInUseCase deviceSignIn;
        private readonly EnterPlayerUseCase enterPlayer;
        private readonly ConnectivityCheck connectivityCheck;
        private readonly ISceneNavigator navigator;
        private readonly StartupOptions options;
        private bool running;

        public TitlePresenter(TitleView view, SignInUseCase deviceSignIn, EnterPlayerUseCase enterPlayer, ConnectivityCheck connectivityCheck, ISceneNavigator navigator, StartupOptions options)
        {
            this.view = view;
            this.deviceSignIn = deviceSignIn;
            this.enterPlayer = enterPlayer;
            this.connectivityCheck = connectivityCheck;
            this.navigator = navigator;
            this.options = options;
        }

        public void Start()
        {
            view.Tapped += OnTapped;
            view.SetStatus("Tap to start");
            if (options.QuitAfterCheck)
            {
                RunAsync(true).Forget();
            }
        }

        public void Dispose()
        {
            if (view != null)
            {
                view.Tapped -= OnTapped;
            }
        }

        private void OnTapped()
        {
            RunAsync(false).Forget();
        }

        private async UniTaskVoid RunAsync(bool diagnostic)
        {
            if (running)
            {
                return;
            }

            running = true;
            view.SetInteractable(false);
            view.SetStatus("Connecting...");
            var succeeded = false;
            try
            {
                Log("startup: start");
                var registered = await deviceSignIn.ExecuteAsync();
                Log(registered ? "device: registered" : "device: token issued");
                var entered = await enterPlayer.ExecuteAsync(registered);
                Log("sign-up: " + (!entered.SignedUp ? "skipped" : entered.Created ? "created" : "already exists"));
                var s = entered.Snapshot;
                Log("sign-in: ok level=" + s.Level + " exp=" + s.Exp + " gold=" + s.Gold + " supportPoints=" + s.SupportPoints + " gachaCurrency=" + s.GachaCurrency + " idols=" + s.Idols.Count + " supportCards=" + s.SupportCards.Count + " hasActiveProduce=" + s.HasActiveProduce + " discardedProduce=" + s.DiscardedProduce);

                if (diagnostic)
                {
                    succeeded = await connectivityCheck.RunAsync(Log);
                }
                else
                {
                    await navigator.GoToHomeAsync();
                    return;
                }
            }
            catch (Exception e)
            {
                Log("start: FAILED " + e.GetType().FullName + ": " + e.Message);
                if (view != null)
                {
                    view.SetStatus("Failed: " + e.Message + "\nTap to retry");
                }
            }

            if (diagnostic)
            {
                UnityEngine.Application.Quit(succeeded ? 0 : 1);
                return;
            }

            if (view == null)
            {
                return;
            }

            view.SetInteractable(true);
            running = false;
        }

        private static void Log(string line)
        {
            Debug.Log("[Uta] " + line);
        }
    }
}
