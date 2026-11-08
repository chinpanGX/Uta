using Cysharp.Threading.Tasks;
using UnityScreenNavigator;
using VContainer;

namespace Uta.Client.Presentation.Gacha
{
    [AssetAddress(nameof(GachaRatesModal))]
    public sealed class GachaRatesPresenter : IScreenWithArgs<GachaRatesArgs>
    {
        private readonly IGachaRatesView gachaRatesView;
        private readonly GachaRatesArgs gachaRatesArgs;
        private readonly GachaRatesDtoFactory gachaRatesDtoFactory;
        private readonly IScreenNavigator screenNavigator;
        private bool running;

        [Inject]
        public GachaRatesPresenter(GachaRatesModal gachaRatesModal, GachaRatesArgs gachaRatesArgs, GachaRatesDtoFactory gachaRatesDtoFactory, IScreenNavigator screenNavigator)
            : this((IGachaRatesView)gachaRatesModal, gachaRatesArgs, gachaRatesDtoFactory, screenNavigator)
        {
        }

        public GachaRatesPresenter(IGachaRatesView gachaRatesView, GachaRatesArgs gachaRatesArgs, GachaRatesDtoFactory gachaRatesDtoFactory, IScreenNavigator screenNavigator)
        {
            this.gachaRatesView = gachaRatesView;
            this.gachaRatesArgs = gachaRatesArgs;
            this.gachaRatesDtoFactory = gachaRatesDtoFactory;
            this.screenNavigator = screenNavigator;
        }

        public UniTask InitializeAsync()
        {
            gachaRatesView.CloseClicked += OnCloseClicked;
            gachaRatesView.SetLines(gachaRatesDtoFactory.Create(gachaRatesArgs.GachaId));
            return UniTask.CompletedTask;
        }

        public void Dispose()
        {
            gachaRatesView.CloseClicked -= OnCloseClicked;
        }

        public async UniTask CloseAsync()
        {
            if (running)
            {
                return;
            }

            running = true;
            try
            {
                await screenNavigator.PopModalAsync(this, playAnimation: false);
            }
            finally
            {
                running = false;
            }
        }

        private void OnCloseClicked()
        {
            CloseAsync().Forget();
        }
    }
}
