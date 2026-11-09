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
        private readonly ITitleView titleView;
        private readonly IDeviceAuthUseCase deviceAuthUseCase;
        private readonly IPlayerUseCase playerUseCase;
        private readonly ISceneNavigator sceneNavigator;
        private bool running;
        private bool disposed;

        [Inject]
        public TitlePresenter(TitlePage titlePage, IDeviceAuthUseCase deviceAuthUseCase, IPlayerUseCase playerUseCase, ISceneNavigator sceneNavigator)
            : this((ITitleView)titlePage, deviceAuthUseCase, playerUseCase, sceneNavigator)
        {
        }

        public TitlePresenter(ITitleView titleView, IDeviceAuthUseCase deviceAuthUseCase, IPlayerUseCase playerUseCase, ISceneNavigator sceneNavigator)
        {
            this.titleView = titleView;
            this.deviceAuthUseCase = deviceAuthUseCase;
            this.playerUseCase = playerUseCase;
            this.sceneNavigator = sceneNavigator;
        }

        public UniTask InitializeAsync()
        {
            titleView.StartClicked += OnStartClicked;
            titleView.SetStatus("タップしてスタート");
            titleView.SetStartInteractable(true);
            return UniTask.CompletedTask;
        }

        public void Dispose()
        {
            disposed = true;
            titleView.StartClicked -= OnStartClicked;
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
                titleView.SetStartInteractable(false);
                titleView.SetStatus("接続中…");
                Debug.Log("[Uta] startup: start");
                try
                {
                    var registered = await deviceAuthUseCase.SignInAsync();
                    if (disposed)
                    {
                        return;
                    }

                    Debug.Log("[Uta] " + StartupLogs.Device(registered));
                    var entered = await playerUseCase.EnterAsync(registered);
                    if (disposed)
                    {
                        return;
                    }

                    Debug.Log("[Uta] " + StartupLogs.SignUp(entered));
                    Debug.Log("[Uta] " + StartupLogs.SignIn(entered));
                    await sceneNavigator.GoToHomeAsync();
                    return;
                }
                catch (Exception e)
                {
                    if (disposed)
                    {
                        return;
                    }

                    Debug.Log("[Uta] " + StartupLogs.Failed(e));
                    titleView.SetStatus("接続に失敗しました: " + e.Message + "\nタップしてもう一度");
                    titleView.SetStartInteractable(true);
                }
            }
            finally
            {
                running = false;
            }
        }
    }
}
