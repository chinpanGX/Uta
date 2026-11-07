using TMPro;
using UnityEngine;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Common.Parts;

namespace Uta.Client.Presentation.Owned
{
    /// <summary>所持一覧のページ（Prefab OwnedPage のルート）。フィールドだけ。IOwnedView の実装は owned（C#）が足す。</summary>
    public sealed class OwnedPage : PageBase
    {
        [SerializeField] private HeaderView header;
        [SerializeField] private TabBarView tabBar;
        [SerializeField] private RectTransform controls;
        [SerializeField] private RectTransform filterRow;
        [SerializeField] private ButtonView sortButton;
        [SerializeField] private ButtonView orderButton;
        [SerializeField] private ButtonView planButton;
        [SerializeField] private ButtonView typeButton;
        [SerializeField] private TMP_Text emptyText;
        [SerializeField] private RectTransform listContent;
        [SerializeField] private ListRowView rowPrefab;
    }
}
