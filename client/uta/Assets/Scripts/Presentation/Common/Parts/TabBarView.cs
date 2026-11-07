using UnityEngine;

namespace Uta.Client.Presentation.Common.Parts
{
    /// <summary>TabBar 部品の View。tabs は部品の Prefab では空で、画面の生成で入れる。</summary>
    public sealed class TabBarView : MonoBehaviour
    {
        [SerializeField] private TabView[] tabs;
    }
}
