using Uta.Client.Presentation.Title;
using VContainer;
using VContainer.Unity;

namespace Uta.Client.Presentation
{
    public sealed class TitleLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<TitleView>();
            builder.Register<ConnectivityCheck>(Lifetime.Singleton);
            builder.RegisterEntryPoint<TitlePresenter>();
        }
    }
}
