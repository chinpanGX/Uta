using System;
using UnityScreenNavigator;
using VContainer;
using VContainer.Unity;

namespace Uta.Client.Presentation.Common
{
    public static class SceneUiInstaller
    {
        /// <summary>
        /// シーンの LifetimeScope の Configure から呼ぶ。uiRoot が null なら ArgumentNullException(nameof(uiRoot), "UiRoot is not set on the scene's LifetimeScope")。
        /// UiRoot の Pages・Modals・Overlays のプロパティは AssetLoader（Addressables）を設定してから返す。
        /// </summary>
        public static void Install(IContainerBuilder builder, UiRoot uiRoot)
        {
            if (uiRoot == null)
            {
                throw new ArgumentNullException(nameof(uiRoot), "UiRoot is not set on the scene's LifetimeScope");
            }

            builder.RegisterComponent(uiRoot);
            builder.RegisterComponent(uiRoot.Pages);      // PageContainer
            builder.RegisterComponent(uiRoot.Modals);     // ModalContainer（Overlays の ModalContainer は登録しない）
            builder.RegisterComponent(uiRoot.Overlays);   // OverlayContainer
            builder.Register<ScreenNavigator>(Lifetime.Singleton).As<IScreenNavigator>();   // IDisposable なのでスコープの破棄で Dispose される
            builder.Register<PageStack>(Lifetime.Singleton);
            builder.RegisterInstance(new DialogOpener(uiRoot.DialogLayer, uiRoot.DialogPrefab));
            builder.RegisterEntryPoint<SceneUiActivation>();
        }
    }
}
