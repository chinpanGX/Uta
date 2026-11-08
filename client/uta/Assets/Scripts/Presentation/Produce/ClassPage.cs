using UnityEngine;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Common.Parts;

namespace Uta.Client.Presentation.Produce
{
    /// <summary>授業のページ（Prefab ClassPage のルート）。フィールドだけ。IClassView の実装は week（C#）が足す。</summary>
    public sealed class ClassPage : PageBase
    {
        [SerializeField] private HeaderView header;
        [SerializeField] private RectTransform optionList;
        [SerializeField] private ButtonView optionButtonPrefab; // Parts/Button.prefab のアセットの参照
    }
}
