using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Common.Parts;

namespace Uta.Client.Presentation.Home
{
    /// <summary>ホームのページ（Prefab HomePage のルート）。フィールドだけ。IHomeView の実装は foundation（C#）が足す。</summary>
    public sealed class HomePage : PageBase
    {
        [SerializeField] private TMP_Text profileText;
        [SerializeField] private RawImage stageImage;          // キャラ表示のステージ。MVP はテクスチャなし。将来 RenderTexture を割り当てる（screen-design 7.8）
        [SerializeField] private TMP_Text stageLabel;
        [SerializeField] private GameObject resumePanel;
        [SerializeField] private ButtonView primaryButton;
        [SerializeField] private ButtonView navProduceButton;
        [SerializeField] private ButtonView navGachaButton;
        [SerializeField] private ButtonView navOwnedButton;
    }
}
