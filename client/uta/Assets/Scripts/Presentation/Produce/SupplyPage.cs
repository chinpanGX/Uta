using UnityEngine;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Common.Parts;

namespace Uta.Client.Presentation.Produce
{
    /// <summary>支給のページ（Prefab SupplyPage のルート）。フィールドだけ。ISupplyView の実装は week（C#）が足す。</summary>
    public sealed class SupplyPage : PageBase
    {
        [SerializeField] private HeaderView header;
        [SerializeField] private ButtonView receiveButton;
    }
}
