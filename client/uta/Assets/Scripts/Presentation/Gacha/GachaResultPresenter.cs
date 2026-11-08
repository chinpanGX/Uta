using Cysharp.Threading.Tasks;
using UnityScreenNavigator;
using VContainer;

namespace Uta.Client.Presentation.Gacha
{
    /// <summary>仮（タスク 3a）。本体はタスク 3b で書く。</summary>
    [AssetAddress(nameof(GachaResultModal))]
    public sealed class GachaResultPresenter : IScreenWithArgs<GachaResultArgs>
    {
        [Inject]
        public GachaResultPresenter(GachaResultModal gachaResultModal, GachaResultArgs gachaResultArgs, GachaResultDtoFactory gachaResultDtoFactory, IScreenNavigator screenNavigator)
        {
        }

        public UniTask InitializeAsync()
        {
            return UniTask.CompletedTask;
        }

        public void Dispose()
        {
        }

        public UniTask CloseAsync()
        {
            return UniTask.CompletedTask;
        }

        public UniTask RetryAsync()
        {
            return UniTask.CompletedTask;
        }
    }
}
