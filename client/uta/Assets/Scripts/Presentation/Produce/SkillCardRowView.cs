using TMPro;
using UnityEngine;
using Uta.Client.Presentation.Common.Parts;

namespace Uta.Client.Presentation.Produce
{
    /// <summary>スキルカード 1 枚の行（Prefab SkillCardRow のルート。手札・獲得の選択肢で共有）。button・background は ListRowBase。フィールドだけ。</summary>
    [DisallowMultipleComponent]
    public sealed class SkillCardRowView : ListRowBase
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text costsText;
        [SerializeField] private TMP_Text descriptionText;
    }
}
