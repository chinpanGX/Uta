using UnityEngine;
using UnityScreenNavigator.Runtime.Core.Page;

namespace Uta.Client.Presentation.Common
{
    /// <summary>ページの基底。Prefab のルートに付く。子 "Body"（SafeAreaFitter 付き）を持つ。</summary>
    public abstract class PageBase : Page
    {
        [SerializeField] private RectTransform body;

        protected RectTransform Body => body;
    }
}
