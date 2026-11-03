using System;
using UnityEngine;
using UnityEngine.UI;

namespace Uta.Client.Presentation.Produce
{
    public sealed class ProduceView : MonoBehaviour
    {
        [SerializeField] private Button homeButton;

        public event Action HomeClicked;

        private void Awake()
        {
            homeButton.onClick.AddListener(OnHome);
        }

        public void SetInteractable(bool value)
        {
            homeButton.interactable = value;
        }

        private void OnHome()
        {
            HomeClicked?.Invoke();
        }
    }
}
