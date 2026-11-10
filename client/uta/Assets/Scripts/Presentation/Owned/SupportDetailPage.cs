using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Common.Parts;

namespace Uta.Client.Presentation.Owned
{
    /// <summary>サポートカードの詳細のページ（Prefab SupportDetailPage のルート）。</summary>
    public sealed class SupportDetailPage : PageBase, ISupportDetailView
    {
        [SerializeField] private HeaderView header;
        [SerializeField] private TMP_Text infoText;
        [SerializeField] private TMP_Text resourcesText;
        [SerializeField] private ButtonView plus1Button;
        [SerializeField] private ButtonView plus5Button;
        [SerializeField] private ButtonView maxButton;
        [SerializeField] private ButtonView limitBreakButton;
        [SerializeField] private TMP_Text abilitiesText;

        public event Action Plus1Clicked;
        public event Action Plus5Clicked;
        public event Action MaxClicked;
        public event Action LimitBreakClicked;
        public event Action BackClicked;

        private void Awake()
        {
            header.BackButton.Button.onClick.AddListener(() => BackClicked?.Invoke());
            plus1Button.Button.onClick.AddListener(() => Plus1Clicked?.Invoke());
            plus5Button.Button.onClick.AddListener(() => Plus5Clicked?.Invoke());
            maxButton.Button.onClick.AddListener(() => MaxClicked?.Invoke());
            limitBreakButton.Button.onClick.AddListener(() => LimitBreakClicked?.Invoke());
        }

        public void SetInfo(IReadOnlyList<string> lines)
        {
            infoText.text = string.Join("\n", lines);
        }

        public void SetAbilities(IReadOnlyList<string> lines)
        {
            abilitiesText.text = string.Join("\n", lines);
        }

        public void SetResources(string text)
        {
            resourcesText.text = text;
        }

        public void SetLevelButtons(string plus1, string plus5, string max, bool plus1Enabled, bool plus5Enabled, bool maxEnabled)
        {
            plus1Button.SetLabel(plus1);
            plus1Button.SetInteractable(plus1Enabled);
            plus5Button.SetLabel(plus5);
            plus5Button.SetInteractable(plus5Enabled);
            maxButton.SetLabel(max);
            maxButton.SetInteractable(maxEnabled);
        }

        public void SetLimitBreak(string label, bool enabled)
        {
            limitBreakButton.SetLabel(label);
            limitBreakButton.SetInteractable(enabled);
        }
    }
}
