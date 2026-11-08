using System;
using System.Collections.Generic;
using UnityEngine;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Common.Parts;

namespace Uta.Client.Presentation.Gacha
{
    /// <summary>ガチャの結果のモーダル（Prefab GachaResultModal のルート）。</summary>
    public sealed class GachaResultModal : ModalBase, IGachaResultView
    {
        [SerializeField] private RectTransform listContent;
        [SerializeField] private GachaLineRowView rowPrefab;
        [SerializeField] private ButtonView retryButton;
        [SerializeField] private ButtonView closeButton;

        public event Action CloseClicked;
        public event Action RetryClicked;

        private void Awake()
        {
            retryButton.Button.onClick.AddListener(() => RetryClicked?.Invoke());
            closeButton.Button.onClick.AddListener(() => CloseClicked?.Invoke());
        }

        public void SetLines(IReadOnlyList<GachaLineRowViewDto> lines)
        {
            UiObjects.ClearChildren(listContent);
            for (var i = 0; i < lines.Count; i++)
            {
                UiObjects.AddRow(rowPrefab, listContent, "Line" + i).SetText(lines[i].Text);
            }
        }
    }
}
