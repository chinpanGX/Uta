using System;
using TMPro;
using UnityEngine;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Common.Parts;

namespace Uta.Client.Presentation.Gacha
{
    /// <summary>ガチャのページ（Prefab GachaPage のルート）。</summary>
    public sealed class GachaPage : PageBase, IGachaView
    {
        [SerializeField] private HeaderView header;
        [SerializeField] private TMP_Text currencyText;
        [SerializeField] private TMP_Text lineupText;
        [SerializeField] private TMP_Text exchangeText;
        [SerializeField] private ButtonView ratesButton;
        [SerializeField] private ButtonView singleButton;
        [SerializeField] private ButtonView tenButton;

        public event Action SingleClicked;
        public event Action TenClicked;
        public event Action RatesClicked;
        public event Action BackClicked;

        private void Awake()
        {
            header.BackButton.Button.onClick.AddListener(() => BackClicked?.Invoke());
            ratesButton.Button.onClick.AddListener(() => RatesClicked?.Invoke());
            singleButton.Button.onClick.AddListener(() => SingleClicked?.Invoke());
            tenButton.Button.onClick.AddListener(() => TenClicked?.Invoke());
        }

        public void SetCurrency(string text)
        {
            currencyText.text = text;
        }

        public void SetExchange(string text)
        {
            exchangeText.text = text;
        }

        public void SetCosts(string singleLabel, string tenLabel)
        {
            singleButton.SetLabel(singleLabel);
            tenButton.SetLabel(tenLabel);
        }

        public void SetLineup(string text)
        {
            lineupText.text = text;
        }
    }
}
