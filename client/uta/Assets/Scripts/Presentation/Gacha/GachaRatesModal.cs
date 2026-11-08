using UnityEngine;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Common.Parts;

namespace Uta.Client.Presentation.Gacha
{
    /// <summary>排出率のモーダル（Prefab GachaRatesModal のルート）。フィールドだけ。IGachaRatesView の実装は gacha（C#）が足す。</summary>
    public sealed class GachaRatesModal : ModalBase
    {
        [SerializeField] private RectTransform listContent;
        [SerializeField] private GachaLineRowView headingRowPrefab;
        [SerializeField] private GachaLineRowView lineRowPrefab;
        [SerializeField] private ButtonView closeButton;
    }
}
