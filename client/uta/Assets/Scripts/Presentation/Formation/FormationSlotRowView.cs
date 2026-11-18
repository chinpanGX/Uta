using TMPro;
using UnityEngine;
using Uta.Client.Presentation.Common.Parts;

namespace Uta.Client.Presentation.Formation
{
    /// <summary>編成のサポートカードの枠の行（Prefab FormationSlotRow のルート）。</summary>
    [DisallowMultipleComponent]
    public sealed class FormationSlotRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private ButtonView selectButton;
        [SerializeField] private ButtonView clearButton;

        public ButtonView SelectButton => selectButton;
        public ButtonView ClearButton => clearButton;

        public void SetLabel(string text)
        {
            labelText.text = text;
        }
    }
}
