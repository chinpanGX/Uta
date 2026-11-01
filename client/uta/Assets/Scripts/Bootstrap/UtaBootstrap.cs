using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Uta.Client.Application.Auth;
using Uta.Client.Infrastructure;
using Uta.Client.Infrastructure.Auth;
using Uta.Client.Presentation.Startup;
using Uta.Shared.Domain.Lesson;
using VContainer;
using VContainer.Unity;

namespace Uta.Client.Bootstrap
{
    internal static class UtaBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Start()
        {
            RunAsync().Forget();
        }

        private static async UniTaskVoid RunAsync()
        {
            var args = Environment.GetCommandLineArgs();
            var serverUrl = ServerEndpoint.Resolve(args);

            var view = new GameObject("StartupView").AddComponent<StartupView>();
            UnityEngine.Object.DontDestroyOnLoad(view.gameObject);

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
                if (Array.IndexOf(args, "-uta-quit-after-check") >= 0)
                {
                    UnityEngine.Application.Quit(1);
                }
                return;
            }

            Debug.Log("[Uta] master: ok cards=" + cardCatalog.CardIds.Count);

            var scope = LifetimeScope.Create(builder =>
            {
                builder.RegisterInstance(view);
                builder.RegisterInstance<ICardCatalog>(cardCatalog);
                builder.RegisterInstance(new StartupOptions(Array.IndexOf(args, "-uta-quit-after-check") >= 0));
                builder.Register<IDeviceAuthPort>(
                    _ => (IDeviceAuthPort)DeviceAuthGateway.Create(
                        serverUrl,
                        AppInfo.Platform,
                        AppInfo.Version,
                        Path.Combine(UnityEngine.Application.persistentDataPath, ServerEndpoint.AccountFileName(serverUrl)),
                        AppInfo.AccountKeySource),
                    Lifetime.Singleton);
                builder.Register<SignInUseCase>(Lifetime.Singleton);
                builder.RegisterEntryPoint<StartupPresenter>();
            });
            UnityEngine.Object.DontDestroyOnLoad(scope.gameObject);
        }
    }
}
