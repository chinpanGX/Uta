using UnityEngine;
using UnityScreenNavigator;
using UnityScreenNavigator.Runtime.Core.Modal;
using UnityScreenNavigator.Runtime.Core.Page;
using UnityScreenNavigator.Runtime.Foundation.AssetLoader;
using Uta.Client.Presentation.Common.Parts;

namespace Uta.Client.Presentation.Common
{
    /// <summary>各シーンに 1 つ置く UI の根（Canvas・ページ/モーダル/オーバーレイの層・通信中のブロッカー・ダイアログの層）。</summary>
    [DisallowMultipleComponent]
    public sealed class UiRoot : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private PageContainer pageContainer;
        [SerializeField] private ModalContainer modalContainer;
        [SerializeField] private OverlayContainer overlayContainer;
        [SerializeField] private BusyIndicator busyIndicator;
        [SerializeField] private RectTransform dialogLayer;
        [SerializeField] private DialogView dialogPrefab;

        private AddressableAssetLoader assetLoader;

        public Canvas Canvas => canvas;

        public PageContainer Pages
        {
            get
            {
                EnsureConfigured();
                return pageContainer;
            }
        }

        public ModalContainer Modals
        {
            get
            {
                EnsureConfigured();
                return modalContainer;
            }
        }

        public OverlayContainer Overlays
        {
            get
            {
                EnsureConfigured();
                return overlayContainer;
            }
        }

        public BusyIndicator Busy => busyIndicator;
        public RectTransform DialogLayer => dialogLayer;
        public DialogView DialogPrefab => dialogPrefab;

        private void Awake()
        {
            EnsureConfigured();
        }

        /// <summary>
        /// 冪等。assetLoader が null のときだけ AddressableAssetLoader を作り、3 つのコンテナーに同じインスタンスを入れる。
        /// AssetLoader は USN のコンテナーの実行時だけのプロパティ（シリアライズされない）。
        /// </summary>
        private void EnsureConfigured()
        {
            if (assetLoader != null)
            {
                return;
            }

            // 3 つに入れ終えてからフィールドに入れる（途中で例外が出たら、次の呼び出しでまた試す）
            var loader = new AddressableAssetLoader();
            pageContainer.AssetLoader = loader;
            modalContainer.AssetLoader = loader;
            overlayContainer.Container.AssetLoader = loader;
            assetLoader = loader;
        }
    }
}
