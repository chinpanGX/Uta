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

        public Button Button => button;

        public void SetLabel(string text)
        {
            label.text = text;
        }

        /// <summary>見た目は Button の ColorTint が出す。</summary>
        public void SetInteractable(bool value)
        {
            button.interactable = value;
        }

        public void SetColor(Color color)
        {
            background.color = color;
        }
    }
}
