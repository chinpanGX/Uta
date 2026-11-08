using TMPro;
using UnityEngine;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Common.Parts;

namespace Uta.Client.Presentation.Produce
{
    /// <summary>週の行動選択のページ（Prefab WeekPage のルート）。フィールドだけ。IWeekView の実装は week（C#）が足す。</summary>
    public sealed class WeekPage : PageBase
    {
        [SerializeField] private HeaderView header; // titleText に週の文字
        [SerializeField] private ButtonView menuButton;
        [SerializeField] private TMP_Text nextExamText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private RectTransform actionList; // 行動のボタンを実行時に並べる親
        [SerializeField] private ButtonView actionButtonPrefab; // Parts/Button.prefab のアセットの参照
    }
}
