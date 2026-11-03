using System;
using UnityEngine;
using UnityEngine.UI;

namespace Uta.Client.Presentation.Home
{
    public sealed class HomeView : MonoBehaviour
    {
        [SerializeField] private Text summaryText;
        [SerializeField] private Button produceButton;

        public event Action ProduceClicked;

        private void Awake()
        {
            produceButton.onClick.AddListener(OnProduce);
        }

        public void SetSummary(string text)
        {
            summaryText.text = text;
        }

        public void SetInteractable(bool value)
        {
            produceButton.interactable = value;
        }

        private void OnProduce()
        {
            ProduceClicked?.Invoke();
        }
    }
}
