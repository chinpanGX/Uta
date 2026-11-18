using System;
using TMPro;
using UnityEngine;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Common.Parts;

namespace Uta.Client.Presentation.Formation
{
    /// <summary>編成のページ（Prefab FormationPage のルート）。</summary>
    public sealed class FormationPage : PageBase, IFormationView
    {
        [SerializeField] private HeaderView header;
        [SerializeField] private TMP_Text idolText;
        [SerializeField] private ButtonView selectIdolButton;
        [SerializeField] private FormationSlotRowView[] slots; // 長さ 5。Slot0〜Slot4 の順
        [SerializeField] private TMP_Text scenarioText;
        [SerializeField] private ButtonView changeScenarioButton;
        [SerializeField] private ButtonView startButton;

        public event Action IdolClicked;
        public event Action<int> SlotClicked;
        public event Action<int> SlotClearClicked;
        public event Action ScenarioClicked;
        public event Action StartClicked;
        public event Action BackClicked;

        private void Awake()
        {
            header.BackButton.Button.onClick.AddListener(() => BackClicked?.Invoke());
            selectIdolButton.Button.onClick.AddListener(() => IdolClicked?.Invoke());
            for (var i = 0; i < slots.Length; i++)
            {
                var index = i;
                slots[i].SelectButton.Button.onClick.AddListener(() => SlotClicked?.Invoke(index));
                slots[i].ClearButton.Button.onClick.AddListener(() => SlotClearClicked?.Invoke(index));
            }

            changeScenarioButton.Button.onClick.AddListener(() => ScenarioClicked?.Invoke());
            startButton.Button.onClick.AddListener(() => StartClicked?.Invoke());
        }

        /// <summary>dto が null なら ArgumentNullException。枠の数が合わなければ ArgumentException("slot count mismatch: " + 件数)。</summary>
        public void Set(FormationViewDto formationViewDto)
        {
            if (formationViewDto == null)
            {
                throw new ArgumentNullException(nameof(formationViewDto));
            }

            if (formationViewDto.Slots.Count != slots.Length)
            {
                throw new ArgumentException("slot count mismatch: " + formationViewDto.Slots.Count);
            }

            idolText.text = formationViewDto.IdolLabel;
            for (var i = 0; i < slots.Length; i++)
            {
                slots[i].SetLabel(formationViewDto.Slots[i].Label);
                slots[i].ClearButton.SetInteractable(formationViewDto.Slots[i].Filled);
            }

            scenarioText.text = formationViewDto.ScenarioLabel;
            changeScenarioButton.SetInteractable(formationViewDto.CanChangeScenario);
            startButton.SetInteractable(formationViewDto.CanStart);
        }
    }
}
