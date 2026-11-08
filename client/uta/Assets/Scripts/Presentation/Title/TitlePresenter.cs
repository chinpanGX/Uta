using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityScreenNavigator;
using Uta.Client.Application.Auth;
using Uta.Client.Application.Player;
using VContainer;
using ISceneNavigator = Uta.Client.Application.Navigation.ISceneNavigator;

namespace Uta.Client.Presentation.Title
{
    [AssetAddress(nameof(TitlePage))]
    public sealed class TitlePresenter : IPresenter
    {
        private readonly ITitleView view;
        private readonly IDeviceAuthUseCase deviceAuth;
        private readonly IPlayerUseCase player;
        private readonly ISceneNavigator scenes;
        private bool running;
        private bool disposed;

        [Inject]
        public TitlePresenter(TitlePage view, IDeviceAuthUseCase deviceAuth, IPlayerUseCase player, ISceneNavigator scenes)
            : this((ITitleView)view, deviceAuth, player, scenes)
        {
        }

        public TitlePresenter(ITitleView view, IDeviceAuthUseCase deviceAuth, IPlayerUseCase player, ISceneNavigator scenes)
        {
            this.view = view;
            this.deviceAuth = deviceAuth;
            this.player = player;
            this.scenes = scenes;
        }

        public UniTask InitializeAsync()
        {
            view.StartClicked += OnStartClicked;
            view.SetStatus("タップしてスタート");
            view.SetStartInteractable(true);
            return UniTask.CompletedTask;
        }

        public void Dispose()
        {
            disposed = true;
            view.StartClicked -= OnStartClicked;
        }

        private void OnStartClicked()
        {
            StartAsync().Forget();
        }

        public async UniTask StartAsync()
        {
            if (running)
            {
                return;
            }

            running = true;
            try
            {
                view.SetStartInteractable(false);
                view.SetStatus("接続中…");
                Debug.Log("[Uta] startup: start");
                try
                {
                    var registered = await deviceAuth.SignInAsync();
                    if (disposed)
                    {
                        return;
                    }

                    Debug.Log("[Uta] " + StartupLogs.Device(registered));
                    var entered = await player.EnterAsync(registered);
                    if (disposed)
                    {
                        return;
                    }

                    Debug.Log("[Uta] " + StartupLogs.SignUp(entered));
                    Debug.Log("[Uta] " + StartupLogs.SignIn(entered));
                    await scenes.GoToHomeAsync();
                    return;
                }
                catch (Exception e)
                {
                    if (disposed)
                    {
                        return;
                    }

                    Debug.Log("[Uta] " + StartupLogs.Failed(e));
                    view.SetStatus("接続に失敗しました: " + e.Message + "\nタップしてもう一度");
                    view.SetStartInteractable(true);
                }
            }
            finally
            {
                running = false;
            }
        }
    }
}
