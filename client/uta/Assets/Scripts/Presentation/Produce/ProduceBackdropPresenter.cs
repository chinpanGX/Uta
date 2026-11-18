using Cysharp.Threading.Tasks;
using UnityScreenNavigator;
using Uta.Client.Application.Produce;
using VContainer;

namespace Uta.Client.Presentation.Produce
{
    [AssetAddress(nameof(ProduceBackdropPage))]
    public sealed class ProduceBackdropPresenter : IPresenter
    {
        private readonly IProduceBackdropView produceBackdropView;
        private readonly ProduceSessionState produceSessionState;
        private readonly ProduceBackdropDtoFactory produceBackdropDtoFactory;

        [Inject]
        public ProduceBackdropPresenter(ProduceBackdropPage produceBackdropPage, ProduceSessionState produceSessionState, ProduceBackdropDtoFactory produceBackdropDtoFactory)
            : this((IProduceBackdropView)produceBackdropPage, produceSessionState, produceBackdropDtoFactory)
        {
        }

        public ProduceBackdropPresenter(IProduceBackdropView produceBackdropView, ProduceSessionState produceSessionState, ProduceBackdropDtoFactory produceBackdropDtoFactory)
        {
            this.produceBackdropView = produceBackdropView;
            this.produceSessionState = produceSessionState;
            this.produceBackdropDtoFactory = produceBackdropDtoFactory;
        }

        public UniTask InitializeAsync()
        {
            Refresh();
            return UniTask.CompletedTask;
        }

        /// <summary>上のページが閉じてこのページに戻ったとき（局面の切り替えの途中）。</summary>
        public void DidPopEnter()
        {
            Refresh();
        }

        public void Dispose()
        {
        }

        private void Refresh()
        {
            if (produceSessionState.Current == null)
            {
                return;
            }

            produceBackdropView.SetHeader(produceBackdropDtoFactory.Create(produceSessionState.Current).Header);
        }
    }
}
