using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Uta.Client.Application.Auth;
using Uta.Client.Application.Gacha;
using Uta.Client.Application.Idol;
using Uta.Client.Application.Navigation;
using Uta.Client.Application.Player;
using Uta.Client.Domain.Idol;
using Uta.Client.Domain.Player;
using Uta.Client.Domain.SupportCard;
using Uta.Client.Infrastructure;
using Uta.Client.Infrastructure.Api;
using Uta.Client.Infrastructure.Auth;
using Uta.Client.Infrastructure.Gacha;
using Uta.Client.Infrastructure.Idol;
using Uta.Client.Infrastructure.Player;
using Uta.Client.Infrastructure.SupportCard;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Home;
using Uta.Client.Presentation.Startup;
using Uta.MasterData;
using Uta.Shared.Contracts.Gacha;
using Uta.Shared.Contracts.Player;
using Uta.Shared.Domain.Lesson;
using VolTech.Client;
using VContainer;
using VContainer.Unity;

namespace Uta.Client.Bootstrap
{
    /// <summary>Bootstrap シーンの起点。マスタを読み、ルートの LifetimeScope を組んで Title へ進む。</summary>
    public sealed class BootstrapEntry : MonoBehaviour
    {
        [SerializeField] private UiRoot uiRoot;   // SceneBuilder が入れる

        private void Start()
        {
            RunAsync().Forget();
        }

        /// <summary>起動の失敗のダイアログ。uiRoot が null か破棄済みなら出さずに false。</summary>
        public static bool ShowFailureDialog(UiRoot uiRoot, string message)
        {
            if (uiRoot == null)
            {
                return false;
            }

            // 小さいスコープで USN を使う。失敗のあとは先へ進まないので破棄しない
            var scope = LifetimeScope.Create(builder => SceneUiInstaller.InstallScreens(builder, uiRoot));
            scope.Container.Resolve<DialogOpener>().ShowInfoAsync(message).Forget();
            return true;
        }

        private async UniTaskVoid RunAsync()
        {
            var args = Environment.GetCommandLineArgs();
            var serverUrl = ServerEndpoint.Resolve(args);
            var quitAfterCheck = Array.IndexOf(args, "-uta-quit-after-check") >= 0;

            try
            {
                MemoryDatabase db;
                CardCatalog cardCatalog;
                try
                {
                    db = await MasterDataLoader.LoadFromStreamingAssetsAsync();
                    cardCatalog = CardCatalogFactory.Create(db);
                }
                catch (Exception e)
                {
                    Debug.LogError("[Uta] master: FAILED " + e.GetType().FullName + ": " + e.Message);
                    if (quitAfterCheck)
                    {
                        UnityEngine.Application.Quit(1);
                        return;
                    }

                    ShowFailureDialog(uiRoot, "マスタを読めませんでした: " + e.Message);
                    return;
                }

                Debug.Log("[Uta] master: ok cards=" + cardCatalog.CardIds.Count);

                var sceneUi = new ActiveSceneUi();
                var errorDialog = new ErrorDialogView(quitAfterCheck, sceneUi);
                var accountPath = Path.Combine(UnityEngine.Application.persistentDataPath, ServerEndpoint.AccountFileName(serverUrl));
                var scope = LifetimeScope.Create(builder =>
                {
                    builder.RegisterInstance(db);
                    builder.RegisterInstance<ICardCatalog>(cardCatalog);
                    builder.RegisterInstance(sceneUi);
                    builder.RegisterInstance(new StartupOptions(quitAfterCheck));
                    builder.Register<SceneNavigator>(Lifetime.Singleton).As<ISceneNavigator>();
                    builder.Register<ErrorNavigator>(Lifetime.Singleton).As<IErrorNavigator>();
                    builder.Register<ApiClient>(
                        resolver => ApiClient.Create(
                            serverUrl,
                            AppInfo.Platform,
                            AppInfo.Version,
                            accountPath,
                            AppInfo.AccountKeySource,
                            new DefaultCommonErrorHandler(errorDialog),
                            quitAfterCheck ? null : new ApiActivityBridge(sceneUi),
                            quitAfterCheck ? null : resolver.Resolve<IErrorNavigator>(),
                            resolver.Resolve<CommonResponseApplier>()),
                        Lifetime.Singleton);
                    RegisterUseCases(builder);
                });
                DontDestroyOnLoad(scope.gameObject);
                await scope.Container.Resolve<ISceneNavigator>().GoToTitleAsync();
            }
            catch (Exception e)
            {
                Debug.LogError("[Uta] bootstrap: FAILED " + e.GetType().FullName + ": " + e.Message);
                if (quitAfterCheck)
                {
                    UnityEngine.Application.Quit(1);
                }
                else
                {
                    ShowFailureDialog(uiRoot, "起動に失敗しました: " + e.Message);
                }
            }
        }

        private static void RegisterUseCases(IContainerBuilder builder)
        {
            builder.Register<IPlayerService>(resolver => resolver.Resolve<ApiClient>().Player, Lifetime.Singleton);
            builder.Register<IGachaService>(resolver => resolver.Resolve<ApiClient>().Gacha, Lifetime.Singleton);
            builder.Register<PlayerRepository>(Lifetime.Singleton).As<IPlayerRepository>().As<IReadOnlyPlayerRepository>();
            builder.Register<IdolRepository>(Lifetime.Singleton).As<IIdolRepository>().As<IReadOnlyIdolRepository>();
            builder.Register<SupportCardRepository>(Lifetime.Singleton).As<ISupportCardRepository>().As<IReadOnlySupportCardRepository>();
            builder.Register<PlayerDiffApplier>(Lifetime.Singleton);
            builder.Register<CommonResponseApplier>(Lifetime.Singleton);
            builder.Register<DiscardNoticeState>(Lifetime.Singleton);
            builder.Register<FeaturedIdolPicker>(Lifetime.Singleton);
            builder.Register<HomeDtoFactory>(Lifetime.Singleton);
            builder.Register<DeviceAuthUseCase>(Lifetime.Singleton).As<IDeviceAuthUseCase>();
            builder.Register<PlayerUseCase>(Lifetime.Singleton).As<IPlayerUseCase>();
            builder.Register<GachaRateCalculator>(Lifetime.Singleton);
            builder.Register<GachaUseCase>(Lifetime.Singleton).As<IGachaUseCase>();
        }
    }
}
