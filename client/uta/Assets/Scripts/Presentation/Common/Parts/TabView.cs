using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Uta.Client.Presentation.Common.Parts
{
    /// <summary>Tab 部品の View。</summary>
    public sealed class TabView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Image underline;
    }
}
