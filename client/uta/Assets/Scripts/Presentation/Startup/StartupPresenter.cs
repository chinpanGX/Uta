using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Uta.Client.Application.Auth;
using VContainer.Unity;

namespace Uta.Client.Presentation.Startup
{
    public sealed class StartupPresenter : IStartable
    {
        private readonly SignInUseCase signIn;
        private readonly StartupView view;
        private readonly StartupOptions options;
        private readonly List<string> lines = new List<string>();

        public StartupPresenter(SignInUseCase signIn, StartupView view, StartupOptions options)
        {
            this.signIn = signIn;
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
                AddLine("sign-in: start");

                var signedUp = await signIn.ExecuteAsync();
                AddLine(signedUp ? "sign-in: SignedUp" : "sign-in: SignedIn");

                await signIn.ReissueAsync();
                AddLine("reissue: ok");

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
