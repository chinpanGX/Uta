using TMPro;
using UnityEngine;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Common.Parts;

namespace Uta.Client.Presentation.Produce
{
    /// <summary>レッスン・試験のページ（Prefab LessonPage のルート）。フィールドだけ。ILessonView の実装は lesson（C#）が足す。</summary>
    public sealed class LessonPage : PageBase
    {
        [SerializeField] private HeaderView header; // titleText にレッスンの見出し
        [SerializeField] private TMP_Text turnText;
        [SerializeField] private ButtonView menuButton;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text lastResultText;
        [SerializeField] private ButtonView pilesButton;
        [SerializeField] private RectTransform handContent; // 縦スクロールの Content
        [SerializeField] private SkillCardRowView handRowPrefab; // Parts/SkillCardRow.prefab のアセットの参照
        [SerializeField] private ButtonView skipButton;
    }
}
