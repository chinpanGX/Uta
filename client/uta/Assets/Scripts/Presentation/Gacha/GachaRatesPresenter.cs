using Cysharp.Threading.Tasks;
using UnityScreenNavigator;
using VContainer;

namespace Uta.Client.Presentation.Gacha
{
    /// <summary>仮（タスク 3a）。本体はタスク 3b で書く。</summary>
    [AssetAddress(nameof(GachaRatesModal))]
    public sealed class GachaRatesPresenter : IScreenWithArgs<GachaRatesArgs>
    {
        [Inject]
        public GachaRatesPresenter(GachaRatesModal gachaRatesModal, GachaRatesArgs gachaRatesArgs, GachaRatesDtoFactory gachaRatesDtoFactory, IScreenNavigator screenNavigator)
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
    }
}
