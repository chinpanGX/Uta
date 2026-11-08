using TMPro;
using UnityEngine;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Common.Parts;

namespace Uta.Client.Presentation.Produce
{
    /// <summary>スキルカード獲得のモーダル（Prefab CardOfferModal のルート）。フィールドだけ。ICardOfferView の実装は week（C#）が足す。</summary>
    public sealed class CardOfferModal : ModalBase
    {
        [SerializeField] private RectTransform cardList;
        [SerializeField] private SkillCardRowView cardRowPrefab; // Parts/SkillCardRow.prefab のアセットの参照
        [SerializeField] private TMP_Text deckCountText;
        [SerializeField] private ButtonView declineButton;
    }
}
