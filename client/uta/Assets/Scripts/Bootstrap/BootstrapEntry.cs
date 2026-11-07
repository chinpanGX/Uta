using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Uta.Client.Application.Auth;
using Uta.Client.Application.Navigation;
using Uta.Client.Application.Player;
using Uta.Client.Infrastructure;
using Uta.Client.Infrastructure.Api;
using Uta.Client.Infrastructure.Auth;
using Uta.Client.Infrastructure.Player;
using Uta.Client.Presentation.Startup;
using Uta.Shared.Domain.Lesson;
using VolTech.Client;
using VContainer;
using VContainer.Unity;

namespace Uta.Client.Bootstrap
{
    /// <summary>Bootstrap シーンの起点。マスタを読み、ルートの LifetimeScope を組んで Title へ進む。</summary>
    public sealed class BootstrapEntry : MonoBehaviour
    {
        private void Start()
        {
            RunAsync().Forget();
        }

        private static async UniTaskVoid RunAsync()
        {
            var args = Environment.GetCommandLineArgs();
            var serverUrl = ServerEndpoint.Resolve(args);
            var quitAfterCheck = Array.IndexOf(args, "-uta-quit-after-check") >= 0;

            var view = new GameObject("StartupView").AddComponent<StartupView>();
            DontDestroyOnLoad(view.gameObject);
            var errorDialog = new GameObject("ErrorDialogView").AddComponent<ErrorDialogView>();
            errorDialog.Initialize(quitAfterCheck);
            DontDestroyOnLoad(errorDialog.gameObject);

            CardCatalog cardCatalog;
            try
            {
                cardCatalog = await MasterCardCatalog.LoadFromStreamingAssetsAsync();
            }
            catch (Exception e)
            {
                var message = "master: FAILED " + e.GetType().FullName + ": " + e.Message;
                Debug.LogError("[Uta] " + message);
                view.SetText(message);
                if (quitAfterCheck)
                {
                    UnityEngine.Application.Quit(1);
                }

                return;
            }

            Debug.Log("[Uta] master: ok cards=" + cardCatalog.CardIds.Count);
            view.SetText(string.Empty);

            try
            {
                var accountPath = Path.Combine(UnityEngine.Application.persistentDataPath, ServerEndpoint.AccountFileName(serverUrl));
                var scope = LifetimeScope.Create(builder =>
                {
                    builder.RegisterInstance<ICardCatalog>(cardCatalog);
                    builder.RegisterInstance(new StartupOptions(quitAfterCheck));
                    builder.Register<SceneNavigator>(Lifetime.Singleton).As<ISceneNavigator>();
                    builder.Register<ErrorNavigator>(Lifetime.Singleton).As<IErrorNavigator>();
                    builder.Register<UtaApiClient>(
                        resolver => UtaApiClient.Create(
                            serverUrl,
                            AppInfo.Platform,
                            AppInfo.Version,
                            accountPath,
                            AppInfo.AccountKeySource,
                            new DefaultCommonErrorHandler(errorDialog),
                            quitAfterCheck ? null : resolver.Resolve<IErrorNavigator>()),
                        Lifetime.Singleton);
                    builder.Register<DeviceAuthGateway>(Lifetime.Singleton).As<IDeviceAuthPort>();
                    builder.Register<PlayerGateway>(Lifetime.Singleton).As<IPlayerPort>();
                    builder.Register<PlayerCache>(Lifetime.Singleton);
                    builder.Register<SignInUseCase>(Lifetime.Singleton);
                    builder.Register<EnterPlayerUseCase>(Lifetime.Singleton);
                });
                DontDestroyOnLoad(scope.gameObject);
                await scope.Container.Resolve<ISceneNavigator>().GoToTitleAsync();
            }
            catch (Exception e)
            {
                var message = "bootstrap: FAILED " + e.GetType().FullName + ": " + e.Message;
                Debug.LogError("[Uta] " + message);
                view.SetText(message);
                if (quitAfterCheck)
                {
                    UnityEngine.Application.Quit(1);
                }
            }
        }
    }
}
