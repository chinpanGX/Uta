using TMPro;
using UnityEngine;

namespace Uta.Client.Presentation.Common.Parts
{
    /// <summary>Dialog の View（USN の Overlay に出る）。ボタンは DialogPresenter が buttonPrefab から実行時に buttonList へ並べる。</summary>
    public sealed class DialogView : ModalBase
    {
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private RectTransform buttonList;
        [SerializeField] private ButtonView buttonPrefab;

        public void SetMessage(string message)
        {
            messageText.text = message;
        }

        public ButtonView AddButton(string name, string label, Color color)
        {
            var button = UnityEngine.Object.Instantiate(buttonPrefab, buttonList, false);
            button.gameObject.name = name;
            button.SetLabel(label);
            button.SetColor(color);
            return button;
        }
    }
}
