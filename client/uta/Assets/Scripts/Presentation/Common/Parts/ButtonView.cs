using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Uta.Client.Presentation.Common.Parts
{
    /// <summary>Button 部品の View。表示のメソッドは foundation が足す。</summary>
    public sealed class ButtonView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text label;
    }
}
