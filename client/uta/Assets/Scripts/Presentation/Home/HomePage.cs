using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Common.Parts;

namespace Uta.Client.Presentation.Home
{
    /// <summary>ホームのページ（Prefab HomePage のルート）。</summary>
    public sealed class HomePage : PageBase, IHomeView
    {
        [SerializeField] private TMP_Text profileText;
        [SerializeField] private RawImage stageImage;          // キャラ表示のステージ。MVP はテクスチャなし。将来 RenderTexture を割り当てる（screen-design 7.8）
        [SerializeField] private TMP_Text stageLabel;
        [SerializeField] private GameObject resumePanel;
        [SerializeField] private ButtonView primaryButton;
        [SerializeField] private ButtonView navProduceButton;
        [SerializeField] private ButtonView navGachaButton;
        [SerializeField] private ButtonView navOwnedButton;

        public event Action ProduceClicked;
        public event Action GachaClicked;
        public event Action OwnedClicked;

        private void Awake()
        {
            primaryButton.Button.onClick.AddListener(() => ProduceClicked?.Invoke());
            navProduceButton.Button.onClick.AddListener(() => ProduceClicked?.Invoke());
            navGachaButton.Button.onClick.AddListener(() => GachaClicked?.Invoke());
            navOwnedButton.Button.onClick.AddListener(() => OwnedClicked?.Invoke());
        }

        public void SetProfile(string text)
        {
            profileText.text = text;
        }

        public void SetResume(bool visible, string primaryLabel)
        {
            resumePanel.SetActive(visible);
            primaryButton.SetLabel(primaryLabel);
        }

        /// <summary>
        /// キャラ表示のステージを作る（IHomeView には置かない。HomePresenter の [Inject] のコンストラクターだけが呼ぶ）。
        /// MVP は new PlaceholderCharacterStage(stageLabel)。3D に替えるときは、ここだけを ModelCharacterStage（stageImage に RenderTexture を入れる）に差し替える。
        /// </summary>
        public ICharacterStage CreateStage()
        {
            return new PlaceholderCharacterStage(stageLabel);
        }
    }
}
