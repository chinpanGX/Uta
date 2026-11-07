using UnityEngine;
using UnityEngine.UI;

namespace Uta.Client.Presentation.Common.Parts
{
    /// <summary>一覧の行の基底。行の Prefab のルートに派生を付ける。</summary>
    public abstract class ListRowBase : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image background;
    }
}
