using Cysharp.Threading.Tasks;
using UnityScreenNavigator;
using Uta.Client.Application.Support;
using Uta.Client.Presentation.Common;
using VContainer;

namespace Uta.Client.Presentation.Owned
{
    /// <summary>仮の実装（タスク 4 で置き換える）。</summary>
    [AssetAddress(nameof(SupportDetailPage))]
    public sealed class SupportDetailPresenter : IScreenWithArgs<SupportDetailArgs>
    {
        private readonly ISupportDetailView supportDetailView;
        private readonly SupportDetailArgs supportDetailArgs;
        private readonly SupportDetailDtoFactory supportDetailDtoFactory;
        private readonly ISupportUseCase supportUseCase;
        private readonly DialogOpener dialogOpener;
        private readonly IScreenNavigator screenNavigator;

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
            return UniTask.CompletedTask;
        }

        public void DidPopEnter()
        {
        }

        public void Dispose()
        {
        }

        public UniTask LevelUpAsync(LevelUpStepKind step)
        {
            return UniTask.CompletedTask;
        }

        public UniTask LimitBreakAsync()
        {
            return UniTask.CompletedTask;
        }

        public UniTask BackAsync()
        {
            return UniTask.CompletedTask;
        }
    }
}
