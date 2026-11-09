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

        public Button Button => button;

        public void SetLabel(string text)
        {
            label.text = text;
        }

        public void SetSelected(bool selected)
        {
            underline.enabled = selected;
        }
    }
}
