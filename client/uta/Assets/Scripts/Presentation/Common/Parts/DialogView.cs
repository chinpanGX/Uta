using System;
using TMPro;
using UnityEngine;

namespace Uta.Client.Presentation.Common.Parts
{
    /// <summary>Dialog 部品の View。ボタンは DialogService が buttonPrefab から実行時に buttonList へ並べる。</summary>
    public sealed class DialogView : MonoBehaviour
    {
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private RectTransform buttonList;
        [SerializeField] private ButtonView buttonPrefab;

        public event Action Destroyed;

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

        private void OnDestroy()
        {
            Destroyed?.Invoke();
        }
    }
}
