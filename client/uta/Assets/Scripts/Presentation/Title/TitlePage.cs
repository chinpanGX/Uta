using TMPro;
using UnityEngine;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Common.Parts;

namespace Uta.Client.Presentation.Title
{
    /// <summary>タイトルのページ（Prefab TitlePage のルート）。フィールドだけ。ITitleView の実装は foundation（C#）が足す。</summary>
    public sealed class TitlePage : PageBase
    {
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private ButtonView startButton;
    }
}
