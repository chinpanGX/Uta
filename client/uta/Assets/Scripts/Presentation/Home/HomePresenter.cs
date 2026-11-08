using System;
using Cysharp.Threading.Tasks;
using Uta.Client.Application.Navigation;
using VContainer.Unity;

namespace Uta.Client.Presentation.Home
{
    public sealed class HomePresenter : IStartable, IDisposable
    {
        private readonly HomeView view;
        private readonly ISceneNavigator navigator;

        public HomePresenter(HomeView view, ISceneNavigator navigator)
        {
            this.view = view;
            this.navigator = navigator;
        }

        public void Start()
        {
            view.SetSummary("ホーム（仮）");
            view.ProduceClicked += OnProduce;
        }

        public void Dispose()
        {
            if (view != null)
            {
                view.ProduceClicked -= OnProduce;
            }
        }

        private void OnProduce()
        {
            view.SetInteractable(false);
            navigator.GoToProduceAsync().Forget();
        }
    }
}
