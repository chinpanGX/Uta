using TMPro;
using UnityEngine;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Common.Parts;

namespace Uta.Client.Presentation.Produce
{
    /// <summary>プロデュースの最終評価のページ（Prefab ProduceResultPage のルート）。フィールドだけ。IProduceResultView の実装は lesson（C#）が足す。</summary>
    public sealed class ProduceResultPage : PageBase
    {
        [SerializeField] private TMP_Text rankText;
        [SerializeField] private TMP_Text linesText;
        [SerializeField] private ButtonView homeButton;
    }
}
