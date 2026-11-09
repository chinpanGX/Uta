using System;
using TMPro;
using UnityEngine;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Common.Parts;

namespace Uta.Client.Presentation.Title
{
    /// <summary>タイトルのページ（Prefab TitlePage のルート）。</summary>
    public sealed class TitlePage : PageBase, ITitleView
    {
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private ButtonView startButton;

        public event Action StartClicked;

        private void Awake()
        {
            startButton.Button.onClick.AddListener(() => StartClicked?.Invoke());
        }

        public void SetStatus(string text)
        {
            statusText.text = text;
        }

        public void SetStartInteractable(bool interactable)
        {
            startButton.SetInteractable(interactable);
        }
    }
}
