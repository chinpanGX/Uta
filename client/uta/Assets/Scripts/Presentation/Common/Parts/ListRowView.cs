using TMPro;
using UnityEngine;

namespace Uta.Client.Presentation.Common.Parts
{
    /// <summary>ListRow 部品の View（名前・補足・右の値）。</summary>
    public sealed class ListRowView : ListRowBase
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text subText;
        [SerializeField] private TMP_Text rightText;
    }
}
