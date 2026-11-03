using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Uta.Client.Application.Auth;
using Uta.Client.Application.Player;
using VContainer.Unity;

namespace Uta.Client.Presentation.Startup
{
    public sealed class StartupPresenter : IStartable
    {
        private readonly SignInUseCase signIn;
        private readonly EnterPlayerUseCase enterPlayer;
        private readonly StartupView view;
        private readonly StartupOptions options;
        private readonly List<string> lines = new List<string>();

        public StartupPresenter(SignInUseCase signIn, EnterPlayerUseCase enterPlayer, StartupView view, StartupOptions options)
        {
            this.signIn = signIn;
            this.enterPlayer = enterPlayer;
            this.view = view;
            this.options = options;
        }

        public void Start()
        {
            RunAsync().Forget();
        }

        private async UniTaskVoid RunAsync()
        {
            var succeeded = false;
            try
            {
                AddLine("startup: start");

                var registered = await signIn.ExecuteAsync();
                AddLine(registered ? "device: registered" : "device: token issued");

                var entered = await enterPlayer.ExecuteAsync(registered);
                AddLine("sign-up: " + (!entered.SignedUp ? "skipped" : entered.Created ? "created" : "already exists"));
                var s = entered.Snapshot;
                AddLine("sign-in: ok level=" + s.Level + " exp=" + s.Exp + " gold=" + s.Gold + " supportPoints=" + s.SupportPoints + " gachaCurrency=" + s.GachaCurrency + " idols=" + s.Idols.Count + " supportCards=" + s.SupportCards.Count + " hasActiveProduce=" + s.HasActiveProduce + " discardedProduce=" + s.DiscardedProduce);

                await signIn.ReissueAsync();
                AddLine("reissue: ok");

                var before = signIn.TokenInvalidationCount;
                await signIn.InvalidateServerSideTokenAsync();
                await enterPlayer.ExecuteAsync(false);
                if (signIn.TokenInvalidationCount <= before)
                {
                    throw new InvalidOperationException("reauth retry: not observed");
                }

                AddLine("reauth retry: ok (invalidations=" + (signIn.TokenInvalidationCount - before) + ")");

                if (await signIn.CanReceiveServerErrorAsync())
                {
                    AddLine("server error delivery: ok");
                    AddLine("connectivity check: OK");
                    succeeded = true;
                }
                else
                {
                    AddLine("server error delivery: FAILED");
                    AddLine("connectivity check: FAILED server error delivery");
                }
            }
            catch (Exception e)
            {
                AddLine("connectivity check: FAILED " + e.GetType().FullName + ": " + e.Message);
            }

            if (options.QuitAfterCheck)
            {
                UnityEngine.Application.Quit(succeeded ? 0 : 1);
            }
        }

        private void AddLine(string line)
        {
            lines.Add(line);
            Debug.Log("[Uta] " + line);
            view.SetText(string.Join("\n", lines));
        }
    }
}
