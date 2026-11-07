using System;
using Cysharp.Threading.Tasks;
using Uta.Client.Application.Navigation;
using Uta.Client.Application.Player;
using VContainer.Unity;

namespace Uta.Client.Presentation.Home
{
    public sealed class HomePresenter : IStartable, IDisposable
    {
        private readonly HomeView view;
        private readonly PlayerCache cache;
        private readonly ISceneNavigator navigator;

        public HomePresenter(HomeView view, PlayerCache cache, ISceneNavigator navigator)
        {
            this.view = view;
            this.cache = cache;
            this.navigator = navigator;
        }

        public void Start()
        {
            var s = cache.Current;
            if (s == null)
            {
                navigator.GoToTitleAsync().Forget();
                return;
            }

            var text = "Lv " + s.Level + "  Exp " + s.Exp
                + "\nGold " + s.Gold
                + "\nSupport pt " + s.SupportPoints
                + "\nGacha " + s.GachaCurrency
                + "\nIdols " + s.Idols.Count + "  Support cards " + s.SupportCards.Count;
            if (s.DiscardedProduce)
            {
                text += "\n(previous produce was discarded)";
            }

            view.SetSummary(text);
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
