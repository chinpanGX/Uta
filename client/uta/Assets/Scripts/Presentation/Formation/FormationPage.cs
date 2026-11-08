using TMPro;
using UnityEngine;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Common.Parts;

namespace Uta.Client.Presentation.Formation
{
    /// <summary>編成のページ（Prefab FormationPage のルート）。フィールドだけ。IFormationView の実装は formation（C#）が足す。</summary>
    public sealed class FormationPage : PageBase
    {
        [SerializeField] private HeaderView header;
        [SerializeField] private TMP_Text idolText;
        [SerializeField] private ButtonView selectIdolButton;
        [SerializeField] private FormationSlotRowView[] slots; // 長さ 5。Slot0〜Slot4 の順
        [SerializeField] private TMP_Text scenarioText;
        [SerializeField] private ButtonView changeScenarioButton;
        [SerializeField] private ButtonView startButton;
    }
}
