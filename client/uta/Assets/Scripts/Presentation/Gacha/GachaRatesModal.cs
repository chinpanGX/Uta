using System;
using System.Collections.Generic;
using UnityEngine;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Common.Parts;

namespace Uta.Client.Presentation.Gacha
{
    /// <summary>排出率のモーダル（Prefab GachaRatesModal のルート）。</summary>
    public sealed class GachaRatesModal : ModalBase, IGachaRatesView
    {
        [SerializeField] private RectTransform listContent;
        [SerializeField] private GachaLineRowView headingRowPrefab;
        [SerializeField] private GachaLineRowView lineRowPrefab;
        [SerializeField] private ButtonView closeButton;

        public event Action CloseClicked;

        private void Awake()
        {
            closeButton.Button.onClick.AddListener(() => CloseClicked?.Invoke());
        }

        public void SetLines(IReadOnlyList<GachaLineRowViewDto> lines)
        {
            UiObjects.ClearChildren(listContent);
            for (var i = 0; i < lines.Count; i++)
            {
                var row = UiObjects.AddRow(lines[i].IsHeading ? headingRowPrefab : lineRowPrefab, listContent, "Line" + i);
                row.SetText(lines[i].Text);
            }
        }
    }
}
