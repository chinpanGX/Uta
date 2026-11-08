using Cysharp.Threading.Tasks;
using UnityScreenNavigator;
using Uta.Client.Application.Player;
using Uta.Client.Presentation.Common;
using VContainer;

namespace Uta.Client.Presentation.Home
{
    [AssetAddress(nameof(HomePage))]
    public sealed class HomePresenter : IPresenter
    {
        private const string NotReadyMessage = "準備中です。";
        private const string DiscardNoticeMessage = "バージョンが変わったため、進行中のプロデュースは破棄されました。";

        private readonly IHomeView view;
        private readonly ICharacterStage stage;
        private readonly HomeDtoFactory dtoFactory;
        private readonly IPlayerUseCase player;
        private readonly DialogOpener dialogs;
        private bool running;
        private bool disposed;

        [Inject]
        public HomePresenter(HomePage view, HomeDtoFactory dtoFactory, IPlayerUseCase player, DialogOpener dialogs)
            : this((IHomeView)view, view.CreateStage(), dtoFactory, player, dialogs)
        {
        }

        public HomePresenter(IHomeView view, ICharacterStage stage, HomeDtoFactory dtoFactory, IPlayerUseCase player, DialogOpener dialogs)
        {
            this.view = view;
            this.stage = stage;
            this.dtoFactory = dtoFactory;
            this.player = player;
            this.dialogs = dialogs;
        }

        public UniTask InitializeAsync()
        {
            view.ProduceClicked += OnProduceClicked;
            view.GachaClicked += OnGachaClicked;
            view.OwnedClicked += OnOwnedClicked;
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
            view.ProduceClicked -= OnProduceClicked;
            view.GachaClicked -= OnGachaClicked;
            view.OwnedClicked -= OnOwnedClicked;
        }

        /// <summary>プレイヤーが無ければ何もしない。破棄の知らせを出す条件なら、出した印を付けてからダイアログを出す。</summary>
        public async UniTask ShowDiscardNoticeIfNeededAsync()
        {
            var dto = dtoFactory.Create();
            if (dto == null || !dto.ShowDiscardNotice)
            {
                return;
            }

            player.MarkDiscardNoticeShown();
            await dialogs.ShowInfoAsync(DiscardNoticeMessage);
        }

        public UniTask OpenProduceAsync()
        {
            return ShowNotReadyAsync();   // 4 本目が書き換える
        }

        public UniTask OpenGachaAsync()
        {
            return ShowNotReadyAsync();   // 2 本目が書き換える
        }

        public UniTask OpenOwnedAsync()
        {
            return ShowNotReadyAsync();   // 3 本目が書き換える
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
                await dialogs.ShowInfoAsync(NotReadyMessage);
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
            var dto = dtoFactory.Create();
            if (dto == null)
            {
                return;
            }

            view.SetProfile(dto.ProfileText);
            view.SetResume(dto.ResumeVisible, dto.PrimaryLabel);
            if (dto.StageIdolId == 0)
            {
                stage.Clear();
            }
            else
            {
                stage.ShowIdol(dto.StageIdolId, dto.StageIdolName);
            }
        }
    }
}
