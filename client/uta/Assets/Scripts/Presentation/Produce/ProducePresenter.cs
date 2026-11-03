using System;
using Cysharp.Threading.Tasks;
using Uta.Client.Application.Navigation;
using VContainer.Unity;

namespace Uta.Client.Presentation.Produce
{
    public sealed class ProducePresenter : IStartable, IDisposable
    {
        private readonly ProduceView view;
        private readonly ISceneNavigator navigator;

        public ProducePresenter(ProduceView view, ISceneNavigator navigator)
        {
            this.view = view;
            this.navigator = navigator;
        }

        public void Start()
        {
            view.HomeClicked += OnHome;
        }

        public void Dispose()
        {
            if (view != null)
            {
                view.HomeClicked -= OnHome;
            }
        }

        private void OnHome()
        {
            view.SetInteractable(false);
            navigator.GoToHomeAsync().Forget();
        }
    }
}
