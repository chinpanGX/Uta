using UnityEngine;
using UnityScreenNavigator.Runtime.Core.Modal;

namespace Uta.Client.Presentation.Common
{
    /// <summary>モーダルの基底。Prefab のルートに付く。中央の子 "Panel" を持つ。暗幕は USN の DefaultModalBackdrop。</summary>
    public abstract class ModalBase : Modal
    {
        [SerializeField] private RectTransform panel;

        protected RectTransform Panel => panel;
    }
}
