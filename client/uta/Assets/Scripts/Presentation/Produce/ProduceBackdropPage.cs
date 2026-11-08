using TMPro;
using UnityEngine;
using Uta.Client.Presentation.Common;

namespace Uta.Client.Presentation.Produce
{
    /// <summary>プロデュースの背景のページ（Prefab ProduceBackdropPage のルート。Produce シーンの底のページ）。フィールドだけ。</summary>
    public sealed class ProduceBackdropPage : PageBase
    {
        [SerializeField] private TMP_Text headerText;
    }
}
