using Cysharp.Threading.Tasks;
using UnityScreenNavigator;
using Uta.Client.Application.Owned;
using Uta.Client.Application.Support;
using Uta.Client.Presentation.Common;
using VContainer;

namespace Uta.Client.Presentation.Owned
{
    [AssetAddress(nameof(SupportDetailPage))]
    public sealed class SupportDetailPresenter : IScreenWithArgs<SupportDetailArgs>
    {
        private readonly ISupportDetailView supportDetailView;
        private readonly SupportDetailArgs supportDetailArgs;
        private readonly SupportDetailDtoFactory supportDetailDtoFactory;
        private readonly ISupportUseCase supportUseCase;
        private readonly DialogOpener dialogOpener;
        private readonly IScreenNavigator screenNavigator;
        private bool running;
        private bool closed;
        private bool disposed;

        [Inject]
        public SupportDetailPresenter(SupportDetailPage supportDetailPage, SupportDetailArgs supportDetailArgs, SupportDetailDtoFactory supportDetailDtoFactory,
            ISupportUseCase supportUseCase, DialogOpener dialogOpener, IScreenNavigator screenNavigator)
            : this((ISupportDetailView)supportDetailPage, supportDetailArgs, supportDetailDtoFactory, supportUseCase, dialogOpener, screenNavigator)
        {
        }

        public SupportDetailPresenter(ISupportDetailView supportDetailView, SupportDetailArgs supportDetailArgs, SupportDetailDtoFactory supportDetailDtoFactory,
            ISupportUseCase supportUseCase, DialogOpener dialogOpener, IScreenNavigator screenNavigator)
        {
            this.supportDetailView = supportDetailView;
            this.supportDetailArgs = supportDetailArgs;
            this.supportDetailDtoFactory = supportDetailDtoFactory;
            this.supportUseCase = supportUseCase;
            this.dialogOpener = dialogOpener;
            this.screenNavigator = screenNavigator;
        }

        public UniTask InitializeAsync()
        {
            supportDetailView.Plus1Clicked += OnPlus1Clicked;
            supportDetailView.Plus5Clicked += OnPlus5Clicked;
            supportDetailView.MaxClicked += OnMaxClicked;
            supportDetailView.LimitBreakClicked += OnLimitBreakClicked;
            supportDetailView.BackClicked += OnBackClicked;
            Refresh();
            return UniTask.CompletedTask;
        }

        /// <summary>素材の選択から戻ったとき。</summary>
        public void DidPopEnter()
        {
            if (disposed)
            {
                return;
            }

            Refresh();
        }

        public void Dispose()
        {
            disposed = true;
            supportDetailView.Plus1Clicked -= OnPlus1Clicked;
            supportDetailView.Plus5Clicked -= OnPlus5Clicked;
            supportDetailView.MaxClicked -= OnMaxClicked;
            supportDetailView.LimitBreakClicked -= OnLimitBreakClicked;
            supportDetailView.BackClicked -= OnBackClicked;
        }

        public async UniTask LevelUpAsync(LevelUpStepKind step)
        {
            if (running || closed || disposed)
            {
                return;
            }

            running = true;
            try
            {
                var uid = supportDetailArgs.SupportCardUniqueId;
                var dto = supportDetailDtoFactory.Create(uid);
                if (!dto.Exists)
                {
                    return;
                }

                var levelStep = dto.LevelSteps[(int)step];
                if (!levelStep.Enabled)
                {
                    return;
                }

                var answer = await dialogOpener.ConfirmAsync(levelStep.ConfirmText);
                if (disposed)
                {
                    return;
                }

                if (answer == DialogResult.Negative)
                {
                    return;
                }

                var levelUpResult = await supportUseCase.LevelUpAsync(uid, levelStep.TargetLevel);
                if (disposed)
                {
                    return;
                }

                string message = null;
                switch (levelUpResult.Kind)
                {
                    case SupportResultKind.Succeeded:
                        Refresh();
                        return;
                    case SupportResultKind.NotEnoughPoints:
                        message = "強化ptが足りません。";
                        break;
                    case SupportResultKind.NotEnoughGold:
                        message = "ゴールドが足りません。";
                        break;
                    case SupportResultKind.InvalidTarget:
                        message = "レベルを上げられません。";
                        break;
                    case SupportResultKind.NotOwned:
                        message = "このカードは所持していません。";
                        break;
                }

                if (message == null)
                {
                    return;
                }

                await dialogOpener.ShowInfoAsync(message);
                if (disposed)
                {
                    return;
                }

                Refresh();
            }
            finally
            {
                running = false;
            }
        }

        public async UniTask LimitBreakAsync()
        {
            if (running || closed || disposed)
            {
                return;
            }

            running = true;
            try
            {
                var uid = supportDetailArgs.SupportCardUniqueId;
                var dto = supportDetailDtoFactory.Create(uid);
                if (!dto.Exists || !dto.LimitBreakEnabled)
                {
                    return;
                }

                if (!dto.HasMaterial)
                {
                    await dialogOpener.ShowInfoAsync("素材にできる同じカードがありません。");
                    return;
                }

                var ownedPresenter = await screenNavigator.PushPageAsync<OwnedPresenter, OwnedArgs>(
                    new OwnedArgs(OwnedSelectionCondition.ForLimitBreakMaterial(dto.SupportCardId, uid)), playAnimation: false);
                var selection = await screenNavigator.WaitForPopAsync<OwnedSelection>(ownedPresenter);
                if (disposed)
                {
                    return;
                }

                if (selection == null || selection.IsCancelled)
                {
                    return;
                }

                var confirmText = supportDetailDtoFactory.CreateLimitBreakConfirmText(uid, selection.SupportCardUid);
                if (confirmText == null)
                {
                    return;
                }

                var answer = await dialogOpener.ConfirmAsync(confirmText);
                if (disposed)
                {
                    return;
                }

                if (answer == DialogResult.Negative)
                {
                    return;
                }

                var limitBreakResult = await supportUseCase.LimitBreakAsync(uid, selection.SupportCardUid);
                if (disposed)
                {
                    return;
                }

                string message = null;
                switch (limitBreakResult.Kind)
                {
                    case SupportResultKind.Succeeded:
                        Refresh();
                        return;
                    case SupportResultKind.MaxLimitBreak:
                        message = "これ以上は解放できません。";
                        break;
                    case SupportResultKind.InvalidMaterial:
                        message = "素材にできません。";
                        break;
                    case SupportResultKind.NotOwned:
                        message = "このカードは所持していません。";
                        break;
                }

                if (message == null)
                {
                    return;
                }

                await dialogOpener.ShowInfoAsync(message);
                if (disposed)
                {
                    return;
                }

                Refresh();
            }
            finally
            {
                running = false;
            }
        }

        public async UniTask BackAsync()
        {
            if (running || closed || disposed)
            {
                return;
            }

            running = true;
            try
            {
                closed = true;
                await screenNavigator.PopPageAsync(this, playAnimation: false);
            }
            finally
            {
                running = false;
            }
        }

        private void Refresh()
        {
            var dto = supportDetailDtoFactory.Create(supportDetailArgs.SupportCardUniqueId);
            supportDetailView.SetInfo(dto.InfoLines);
            supportDetailView.SetAbilities(dto.AbilityLines);
            supportDetailView.SetResources(dto.ResourcesText);
            supportDetailView.SetLevelButtons(
                dto.LevelSteps[0].Label, dto.LevelSteps[1].Label, dto.LevelSteps[2].Label,
                dto.LevelSteps[0].Enabled, dto.LevelSteps[1].Enabled, dto.LevelSteps[2].Enabled);
            supportDetailView.SetLimitBreak(dto.LimitBreakLabel, dto.LimitBreakEnabled);
        }

        private void OnPlus1Clicked()
        {
            LevelUpAsync(LevelUpStepKind.Plus1).Forget();
        }

        private void OnPlus5Clicked()
        {
            LevelUpAsync(LevelUpStepKind.Plus5).Forget();
        }

        private void OnMaxClicked()
        {
            LevelUpAsync(LevelUpStepKind.Max).Forget();
        }

        private void OnLimitBreakClicked()
        {
            LimitBreakAsync().Forget();
        }

        private void OnBackClicked()
        {
            BackAsync().Forget();
        }
    }
}
