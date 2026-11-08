using Cysharp.Threading.Tasks;
using UnityScreenNavigator;
using VContainer;

namespace Uta.Client.Presentation.Gacha
{
    [AssetAddress(nameof(GachaResultModal))]
    public sealed class GachaResultPresenter : IScreenWithArgs<GachaResultArgs>
    {
        private readonly IGachaResultView gachaResultView;
        private readonly GachaResultArgs gachaResultArgs;
        private readonly GachaResultDtoFactory gachaResultDtoFactory;
        private readonly IScreenNavigator screenNavigator;
        private bool running;

        [Inject]
        public GachaResultPresenter(GachaResultModal gachaResultModal, GachaResultArgs gachaResultArgs, GachaResultDtoFactory gachaResultDtoFactory, IScreenNavigator screenNavigator)
            : this((IGachaResultView)gachaResultModal, gachaResultArgs, gachaResultDtoFactory, screenNavigator)
        {
        }

        public GachaResultPresenter(IGachaResultView gachaResultView, GachaResultArgs gachaResultArgs, GachaResultDtoFactory gachaResultDtoFactory, IScreenNavigator screenNavigator)
        {
            this.gachaResultView = gachaResultView;
            this.gachaResultArgs = gachaResultArgs;
            this.gachaResultDtoFactory = gachaResultDtoFactory;
            this.screenNavigator = screenNavigator;
        }

        public UniTask InitializeAsync()
        {
            gachaResultView.CloseClicked += OnCloseClicked;
            gachaResultView.RetryClicked += OnRetryClicked;
            gachaResultView.SetLines(gachaResultDtoFactory.Create(gachaResultArgs.Results));
            return UniTask.CompletedTask;
        }

        public void Dispose()
        {
            gachaResultView.CloseClicked -= OnCloseClicked;
            gachaResultView.RetryClicked -= OnRetryClicked;
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

        public async UniTask RetryAsync()
        {
            if (running)
            {
                return;
            }

            running = true;
            try
            {
                await screenNavigator.PopModalAsync(this, playAnimation: false);

                // Pop のあとは破棄済みかを見ない。Retry はガチャのページの Presenter の処理で、この Presenter の View に触らない。
                if (gachaResultArgs.Retry != null)
                {
                    await gachaResultArgs.Retry();
                }
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

        private void OnRetryClicked()
        {
            RetryAsync().Forget();
        }
    }
}
