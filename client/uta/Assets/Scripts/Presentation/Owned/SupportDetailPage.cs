using TMPro;
using UnityEngine;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Common.Parts;

namespace Uta.Client.Presentation.Owned
{
    /// <summary>サポートカードの詳細のページ（Prefab SupportDetailPage のルート）。フィールドだけ。ISupportDetailView の実装は owned（C#）が足す。</summary>
    public sealed class SupportDetailPage : PageBase
    {
        [SerializeField] private HeaderView header;
        [SerializeField] private TMP_Text infoText;
        [SerializeField] private TMP_Text resourcesText;
        [SerializeField] private ButtonView plus1Button;
        [SerializeField] private ButtonView plus5Button;
        [SerializeField] private ButtonView maxButton;
        [SerializeField] private ButtonView limitBreakButton;
        [SerializeField] private TMP_Text abilitiesText;
    }
}
