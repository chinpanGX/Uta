using TMPro;
using UnityEngine;

namespace Uta.Client.Presentation.Gacha
{
    /// <summary>ガチャの 1 行の文字の View（行の Prefab GachaRateHeadingRow・GachaRateRow・GachaResultRow で共用）。</summary>
    [DisallowMultipleComponent]
    public sealed class GachaLineRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;

        /// <summary>text.text = value。null は ""。</summary>
        public void SetText(string value)
        {
            text.text = value ?? "";
        }
    }
}
