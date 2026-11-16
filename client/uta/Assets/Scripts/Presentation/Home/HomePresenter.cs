using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityScreenNavigator;
using Uta.Client.Application.Player;
using Uta.Client.Application.Produce;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Formation;
using Uta.Client.Presentation.Gacha;
using Uta.Client.Presentation.Owned;
using VContainer;
using ISceneNavigator = Uta.Client.Application.Navigation.ISceneNavigator;

namespace Uta.Client.Presentation.Home
{
    [AssetAddress(nameof(HomePage))]
    public sealed class HomePresenter : IPresenter
    {
        private const string DiscardNoticeMessage = "バージョンが変わったため、進行中のプロデュースは破棄されました。";

        private readonly IHomeView homeView;
        private readonly ICharacterStage characterStage;
        private readonly HomeDtoFactory homeDtoFactory;
        private readonly IPlayerUseCase playerUseCase;
        private readonly DialogOpener dialogOpener;
        private readonly IScreenNavigator screenNavigator;
        private readonly IProduceUseCase produceUseCase;
        private readonly ISceneNavigator sceneNavigator;
        private bool running;
        private bool disposed;

        [Inject]
        public HomePresenter(HomePage homePage, HomeDtoFactory homeDtoFactory, IPlayerUseCase playerUseCase, DialogOpener dialogOpener, IScreenNavigator screenNavigator, IProduceUseCase produceUseCase, ISceneNavigator sceneNavigator)
            : this((IHomeView)homePage, homePage.CreateStage(), homeDtoFactory, playerUseCase, dialogOpener, screenNavigator, produceUseCase, sceneNavigator)
        {
        }

        public HomePresenter(IHomeView homeView, ICharacterStage characterStage, HomeDtoFactory homeDtoFactory, IPlayerUseCase playerUseCase, DialogOpener dialogOpener, IScreenNavigator screenNavigator, IProduceUseCase produceUseCase, ISceneNavigator sceneNavigator)
        {
            this.homeView = homeView;
            this.characterStage = characterStage;
            this.homeDtoFactory = homeDtoFactory;
            this.playerUseCase = playerUseCase;
            this.dialogOpener = dialogOpener;
            this.screenNavigator = screenNavigator;
            this.produceUseCase = produceUseCase;
            this.sceneNavigator = sceneNavigator;
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

        /// <summary>進行中でなければ編成のページを積む。進行中なら「再開／放棄／やめる」を選ばせる。</summary>
        public async UniTask OpenProduceAsync()
        {
            if (running)
            {
                return;
            }

            running = true;
            try
            {
                var dto = homeDtoFactory.Create();
                if (dto == null)
                {
                    return;
                }

                if (!dto.ResumeVisible)
                {
                    await screenNavigator.PushPageAsync<FormationPresenter>(playAnimation: false);
                    return;
                }

                var index = await dialogOpener.ChooseAsync("進行中のプロデュースがあります。", new[] { "再開", "放棄", "やめる" });
                if (disposed)
                {
                    return;
                }

                if (index == 0)
                {
                    await ResumeCoreAsync();
                }
                else if (index == 1)
                {
                    var confirm = await dialogOpener.ConfirmAsync("放棄すると報酬は受け取れません。放棄しますか？");
                    if (disposed)
                    {
                        return;
                    }

                    if (confirm == DialogResult.Positive)
                    {
                        await produceUseCase.AbandonAsync();
                        if (!disposed)
                        {
                            Refresh();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                await dialogOpener.ShowInfoAsync("エラーが発生しました: " + e.Message);
            }
            finally
            {
                running = false;
            }
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

        private async UniTask ResumeCoreAsync()
        {
            var kind = await produceUseCase.ResumeAsync();
            if (disposed)
            {
                return;
            }

            switch (kind)
            {
                case ProduceResumeResultKind.Resumed:
                    await sceneNavigator.GoToProduceAsync();
                    break;
                case ProduceResumeResultKind.NotInProgress:
                    await dialogOpener.ShowInfoAsync("進行中のプロデュースはありません。");
                    if (!disposed)
                    {
                        Refresh();
                    }

                    break;
                case ProduceResumeResultKind.Failed:
                    break;
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
