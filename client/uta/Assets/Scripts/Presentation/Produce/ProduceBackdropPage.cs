using TMPro;
using UnityEngine;
using Uta.Client.Presentation.Common;

namespace Uta.Client.Presentation.Produce
{
    /// <summary>プロデュースの背景のページ（Prefab ProduceBackdropPage のルート。Produce シーンの底のページ）。</summary>
    public sealed class ProduceBackdropPage : PageBase, IProduceBackdropView
    {
        [SerializeField] private TMP_Text headerText;

        public void SetHeader(string text)
        {
            headerText.text = text;
        }
    }
}
