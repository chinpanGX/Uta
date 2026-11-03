using Uta.Client.Presentation.Produce;
using VContainer;
using VContainer.Unity;

namespace Uta.Client.Presentation
{
    public sealed class ProduceLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<ProduceView>();
            builder.RegisterEntryPoint<ProducePresenter>();
        }
    }
}
