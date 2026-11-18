using UnityEngine;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Produce;
using VContainer;
using VContainer.Unity;

namespace Uta.Client.Presentation
{
    public sealed class ProduceLifetimeScope : LifetimeScope
    {
        [SerializeField] private UiRoot uiRoot;   // SceneBuilder が入れる

        protected override void Configure(IContainerBuilder builder)
        {
            SceneUiInstaller.Install(builder, uiRoot);
            builder.Register<ProduceFlow>(Lifetime.Singleton);
            builder.Register<ProduceMenu>(Lifetime.Singleton);
            builder.RegisterEntryPoint<ProduceEntry>();
        }
    }
}
