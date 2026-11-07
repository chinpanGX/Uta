using Uta.Client.Presentation.Home;
using VContainer;
using VContainer.Unity;

namespace Uta.Client.Presentation
{
    public sealed class HomeLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<HomeView>();
            builder.RegisterEntryPoint<HomePresenter>();
        }
    }
}
