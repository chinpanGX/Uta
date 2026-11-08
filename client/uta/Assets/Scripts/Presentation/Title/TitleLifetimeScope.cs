using UnityEngine;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Title;
using VContainer;
using VContainer.Unity;

namespace Uta.Client.Presentation
{
    public sealed class TitleLifetimeScope : LifetimeScope
    {
        [SerializeField] private UiRoot uiRoot;   // SceneBuilder が入れる

        protected override void Configure(IContainerBuilder builder)
        {
            SceneUiInstaller.Install(builder, uiRoot);
            builder.Register<ConnectivityCheck>(Lifetime.Singleton);
            builder.Register<TitleDiagnostics>(Lifetime.Singleton);
            builder.RegisterEntryPoint<TitleEntry>();
        }
    }
}
