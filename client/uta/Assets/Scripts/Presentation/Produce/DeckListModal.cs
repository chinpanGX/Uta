using UnityEngine;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Common.Parts;

namespace Uta.Client.Presentation.Produce
{
    /// <summary>山札・捨札・除外の一覧のモーダル（Prefab DeckListModal のルート）。フィールドだけ。IDeckListView の実装は lesson（C#）が足す。</summary>
    public sealed class DeckListModal : ModalBase
    {
        [SerializeField] private RectTransform listContent; // 縦スクロールの Content
        [SerializeField] private DeckSectionHeaderRowView sectionHeaderPrefab; // Parts/DeckSectionHeaderRow.prefab
        [SerializeField] private DeckCardRowView cardRowPrefab; // Parts/DeckCardRow.prefab
        [SerializeField] private ButtonView closeButton;
    }
}
