using TMPro;
using UnityEngine;

namespace Uta.Client.Presentation.Gacha
{
    /// <summary>ガチャの 1 行の文字の View（行の Prefab GachaRateHeadingRow・GachaRateRow・GachaResultRow で共用）。フィールドだけ。</summary>
    [DisallowMultipleComponent]
    public sealed class GachaLineRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
    }
}
