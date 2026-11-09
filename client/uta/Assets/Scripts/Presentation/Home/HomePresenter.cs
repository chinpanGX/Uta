using Cysharp.Threading.Tasks;
using UnityScreenNavigator;
using Uta.Client.Application.Player;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Gacha;
using Uta.Client.Presentation.Owned;
using VContainer;

namespace Uta.Client.Presentation.Home
{
    [AssetAddress(nameof(HomePage))]
    public sealed class HomePresenter : IPresenter
    {
        private const string NotReadyMessage = "準備中です。";
        private const string DiscardNoticeMessage = "バージョンが変わったため、進行中のプロデュースは破棄されました。";

        private readonly IHomeView homeView;
        private readonly ICharacterStage characterStage;
        private readonly HomeDtoFactory homeDtoFactory;
        private readonly IPlayerUseCase playerUseCase;
        private readonly DialogOpener dialogOpener;
        private readonly IScreenNavigator screenNavigator;
        private bool running;
        private bool disposed;

        [Inject]
        public HomePresenter(HomePage homePage, HomeDtoFactory homeDtoFactory, IPlayerUseCase playerUseCase, DialogOpener dialogOpener, IScreenNavigator screenNavigator)
            : this((IHomeView)homePage, homePage.CreateStage(), homeDtoFactory, playerUseCase, dialogOpener, screenNavigator)
        {
        }

        public HomePresenter(IHomeView homeView, ICharacterStage characterStage, HomeDtoFactory homeDtoFactory, IPlayerUseCase playerUseCase, DialogOpener dialogOpener, IScreenNavigator screenNavigator)
        {
            this.homeView = homeView;
            this.characterStage = characterStage;
            this.homeDtoFactory = homeDtoFactory;
            this.playerUseCase = playerUseCase;
            this.dialogOpener = dialogOpener;
            this.screenNavigator = screenNavigator;
        }

        public UniTask InitializeAsync()
        {
            homeView.ProduceClicked += OnProduceClicked;
            homeView.GachaClicked += OnGachaClicked;
            homeView.OwnedClicked += OnOwnedClicked;
            Refresh();
            return UniTask.CompletedTask;
        }

        public void DidPushEnter()
        {
            ShowDiscardNoticeIfNeededAsync().Forget();
        }

        public void DidPopEnter()
        {
            Refresh();
        }

        public void Dispose()
        {
            disposed = true;
            homeView.ProduceClicked -= OnProduceClicked;
            homeView.GachaClicked -= OnGachaClicked;
            homeView.OwnedClicked -= OnOwnedClicked;
        }

        /// <summary>プレイヤーが無ければ何もしない。破棄の知らせを出す条件なら、出した印を付けてからダイアログを出す。</summary>
        public async UniTask ShowDiscardNoticeIfNeededAsync()
        {
            var dto = homeDtoFactory.Create();
            if (dto == null || !dto.ShowDiscardNotice)
            {
                return;
            }

            playerUseCase.MarkDiscardNoticeShown();
            await dialogOpener.ShowInfoAsync(DiscardNoticeMessage);
        }

        public UniTask OpenProduceAsync()
        {
            return ShowNotReadyAsync();   // 4 本目が書き換える
        }

        public async UniTask OpenGachaAsync()
        {
            if (running)
            {
                return;
            }

            running = true;
            try
            {
                await screenNavigator.PushPageAsync<GachaPresenter>(playAnimation: false);
            }
            finally
            {
                running = false;
            }
        }

        public async UniTask OpenOwnedAsync()
        {
            if (running)
            {
                return;
            }

            running = true;
            try
            {
                await screenNavigator.PushPageAsync<OwnedPresenter, OwnedArgs>(OwnedArgs.Browse(), playAnimation: false);
            }
            finally
            {
                running = false;
            }
        }

        private void OnProduceClicked()
        {
            OpenProduceAsync().Forget();
        }

        private void OnGachaClicked()
        {
            OpenGachaAsync().Forget();
        }

        private void OnOwnedClicked()
        {
            OpenOwnedAsync().Forget();
        }

        private async UniTask ShowNotReadyAsync()
        {
            if (running)
            {
                return;
            }

            running = true;
            try
            {
                await dialogOpener.ShowInfoAsync(NotReadyMessage);
                if (disposed)
                {
                    return;
                }
            }
            finally
            {
                running = false;
            }
        }

        private void Refresh()
        {
            var dto = homeDtoFactory.Create();
            if (dto == null)
            {
                return;
            }

            homeView.SetProfile(dto.ProfileText);
            homeView.SetResume(dto.ResumeVisible, dto.PrimaryLabel);
            if (dto.StageIdolId == 0)
            {
                characterStage.Clear();
            }
            else
            {
                characterStage.ShowIdol(dto.StageIdolId, dto.StageIdolName);
            }
        }
    }
}
