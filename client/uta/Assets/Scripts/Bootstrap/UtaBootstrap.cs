using System;
using System.IO;
using UnityEngine;
using Uta.Client.Application.Auth;
using Uta.Client.Infrastructure.Auth;
using Uta.Client.Presentation.Startup;
using VContainer;
using VContainer.Unity;

namespace Uta.Client.Bootstrap
{
    internal static class UtaBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Start()
        {
            var args = Environment.GetCommandLineArgs();
            var serverUrl = ServerEndpoint.Resolve(args);

            var view = new GameObject("StartupView").AddComponent<StartupView>();
            UnityEngine.Object.DontDestroyOnLoad(view.gameObject);

            var scope = LifetimeScope.Create(builder =>
            {
                builder.RegisterInstance(view);
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
