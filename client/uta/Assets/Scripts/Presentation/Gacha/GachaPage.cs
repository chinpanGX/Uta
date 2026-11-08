using TMPro;
using UnityEngine;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Common.Parts;

namespace Uta.Client.Presentation.Gacha
{
    /// <summary>ガチャのページ（Prefab GachaPage のルート）。フィールドだけ。IGachaView の実装は gacha（C#）が足す。</summary>
    public sealed class GachaPage : PageBase
    {
        [SerializeField] private HeaderView header;
        [SerializeField] private TMP_Text currencyText;
        [SerializeField] private TMP_Text lineupText;
        [SerializeField] private TMP_Text exchangeText;
        [SerializeField] private ButtonView ratesButton;
        [SerializeField] private ButtonView singleButton;
        [SerializeField] private ButtonView tenButton;
    }
}
