using TMPro;
using UnityEngine;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Common.Parts;

namespace Uta.Client.Presentation.Produce
{
    /// <summary>試験結果のページ（Prefab ExamResultPage のルート）。フィールドだけ。IExamResultView の実装は lesson（C#）が足す。</summary>
    public sealed class ExamResultPage : PageBase
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private RectTransform rankingContent; // 縦スクロールの Content
        [SerializeField] private ExamRankingRowView rankingRowPrefab; // Parts/ExamRankingRow.prefab
        [SerializeField] private TMP_Text outcomeText;
        [SerializeField] private ButtonView retryButton;
        [SerializeField] private ButtonView nextButton;
    }
}
