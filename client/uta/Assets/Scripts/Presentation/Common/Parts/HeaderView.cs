using TMPro;
using UnityEngine;

namespace Uta.Client.Presentation.Common.Parts
{
    /// <summary>Header 部品の View（戻るボタン＋タイトル＋右の置き場）。</summary>
    public sealed class HeaderView : MonoBehaviour
    {
        [SerializeField] private ButtonView backButton;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private RectTransform rightSlot;
    }
}
