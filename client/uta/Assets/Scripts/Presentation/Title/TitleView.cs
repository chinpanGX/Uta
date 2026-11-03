using System;
using UnityEngine;
using UnityEngine.UI;

namespace Uta.Client.Presentation.Title
{
    public sealed class TitleView : MonoBehaviour
    {
        [SerializeField] private Button startButton;
        [SerializeField] private Text statusText;

        public event Action Tapped;

        private void Awake()
        {
            startButton.onClick.AddListener(OnClick);
        }

        public void SetStatus(string text)
        {
            statusText.text = text;
        }

        public void SetInteractable(bool value)
        {
            startButton.interactable = value;
        }

        private void OnClick()
        {
            Tapped?.Invoke();
        }
    }
}
