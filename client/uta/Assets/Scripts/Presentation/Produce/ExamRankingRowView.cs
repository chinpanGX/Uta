using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Uta.Client.Presentation.Produce
{
    /// <summary>試験結果の順位表の行（Prefab ExamRankingRow のルート）。フィールドだけ。</summary>
    [DisallowMultipleComponent]
    public sealed class ExamRankingRowView : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text text;
    }
}
