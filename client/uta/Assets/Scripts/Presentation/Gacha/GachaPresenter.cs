using Cysharp.Threading.Tasks;
using UnityScreenNavigator;
using Uta.Client.Application.Gacha;
using Uta.Client.Presentation.Common;
using VContainer;

namespace Uta.Client.Presentation.Gacha
{
    [AssetAddress(nameof(GachaPage))]
    public sealed class GachaPresenter : IPresenter
    {
        private readonly IGachaView gachaView;
        private readonly GachaDtoFactory gachaDtoFactory;
        private readonly IGachaUseCase gachaUseCase;
        private readonly DialogOpener dialogOpener;
        private readonly IScreenNavigator screenNavigator;
        private int gachaId;
        private int? exchangePoints;
        private bool running;
        private bool disposed;

        [Inject]
        public GachaPresenter(GachaPage gachaPage, GachaDtoFactory gachaDtoFactory, IGachaUseCase gachaUseCase, DialogOpener dialogOpener, IScreenNavigator screenNavigator)
            : this((IGachaView)gachaPage, gachaDtoFactory, gachaUseCase, dialogOpener, screenNavigator)
        {
        }

        public GachaPresenter(IGachaView gachaView, GachaDtoFactory gachaDtoFactory, IGachaUseCase gachaUseCase, DialogOpener dialogOpener, IScreenNavigator screenNavigator)
        {
            this.gachaView = gachaView;
            this.gachaDtoFactory = gachaDtoFactory;
            this.gachaUseCase = gachaUseCase;
            this.dialogOpener = dialogOpener;
            this.screenNavigator = screenNavigator;
        }

        public async UniTask InitializeAsync()
        {
            gachaView.SingleClicked += OnSingleClicked;
            gachaView.TenClicked += OnTenClicked;
            gachaView.RatesClicked += OnRatesClicked;
            gachaView.BackClicked += OnBackClicked;

            gachaId = gachaDtoFactory.Create(null).GachaId;
            exchangePoints = await gachaUseCase.GetExchangePointsAsync(gachaId);
            if (disposed)
            {
                return;
            }

            Show(gachaDtoFactory.Create(exchangePoints));
        }

        public void Dispose()
        {
            disposed = true;
            gachaView.SingleClicked -= OnSingleClicked;
            gachaView.TenClicked -= OnTenClicked;
            gachaView.RatesClicked -= OnRatesClicked;
            gachaView.BackClicked -= OnBackClicked;
        }

        /// <summary>単発は 1、10 連は 10。結果のモーダルの「もう一度」からも呼ばれる。</summary>
        public async UniTask DrawAsync(int count)
        {
            if (running)
            {
                return;
            }

            running = true;
            try
            {
                var answer = await dialogOpener.ConfirmAsync(gachaDtoFactory.CreateDrawConfirmText(count), "引く", "やめる");
                if (disposed)
                {
                    return;
                }

                if (answer == DialogResult.Negative)
                {
                    return;
                }

                var result = await gachaUseCase.DrawAsync(gachaId, count);
                if (disposed)
                {
                    return;
                }

                switch (result.Kind)
                {
                    case DrawResultKind.Succeeded:
                        exchangePoints = result.ExchangePoints;
                        Show(gachaDtoFactory.Create(exchangePoints));
                        await screenNavigator.PushModalAsync<GachaResultPresenter, GachaResultArgs>(
                            new GachaResultArgs(result.Results, () => DrawAsync(count)),
                            playAnimation: false);
                        break;
                    case DrawResultKind.NotEnoughCurrency:
                        await dialogOpener.ShowInfoAsync("ガチャ通貨が足りません。");
                        break;
                    case DrawResultKind.Failed:
                        break;
                }
            }
            finally
            {
                running = false;
            }
        }

        public async UniTask OpenRatesAsync()
        {
            if (running)
            {
                return;
            }

            running = true;
            try
            {
                await screenNavigator.PushModalAsync<GachaRatesPresenter, GachaRatesArgs>(new GachaRatesArgs(gachaId), playAnimation: false);
            }
            finally
            {
                running = false;
            }
        }

        public async UniTask BackAsync()
        {
            if (running)
            {
                return;
            }

            running = true;
            try
            {
                await screenNavigator.PopPageAsync(this, playAnimation: false);
            }
            finally
            {
                running = false;
            }
        }

        private void Show(GachaViewDto gachaViewDto)
        {
            gachaView.SetCurrency(gachaViewDto.CurrencyText);
            gachaView.SetCosts(gachaViewDto.SingleLabel, gachaViewDto.TenLabel);
            gachaView.SetLineup(gachaViewDto.LineupText);
            gachaView.SetExchange(gachaViewDto.ExchangeText);
        }

        private void OnSingleClicked()
        {
            DrawAsync(1).Forget();
        }

        private void OnTenClicked()
        {
            DrawAsync(10).Forget();
        }

        private void OnRatesClicked()
        {
            OpenRatesAsync().Forget();
        }

        private void OnBackClicked()
        {
            BackAsync().Forget();
        }
    }
}
