using UnityEngine;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Common.Parts;

namespace Uta.Client.Presentation.Gacha
{
    /// <summary>ガチャの結果のモーダル（Prefab GachaResultModal のルート）。フィールドだけ。IGachaResultView の実装は gacha（C#）が足す。</summary>
    public sealed class GachaResultModal : ModalBase
    {
        [SerializeField] private RectTransform listContent;
        [SerializeField] private GachaLineRow rowPrefab;
        [SerializeField] private ButtonView retryButton;
        [SerializeField] private ButtonView closeButton;
    }
}
