using UnityEngine;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Home;
using VContainer;
using VContainer.Unity;

namespace Uta.Client.Presentation
{
    public sealed class HomeLifetimeScope : LifetimeScope
    {
        [SerializeField] private UiRoot uiRoot;   // SceneBuilder が入れる

        protected override void Configure(IContainerBuilder builder)
        {
            SceneUiInstaller.Install(builder, uiRoot);
            builder.RegisterEntryPoint<HomeEntry>();
        }
    }
}
