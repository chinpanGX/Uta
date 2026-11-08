using UnityEngine;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Produce;

namespace Uta.Client.Editor.Prefabs.Screens
{
    /// <summary>山札・捨札・除外の一覧のモーダル（Screens/Produce/DeckListModal.prefab）を作る。Addressables の Screens に登録する。</summary>
    public sealed class DeckListModalBuilder : IPrefabBuilder
    {
        public int Order => 2000;

        public string AssetPath => PrefabBuildUtil.ScreenPath("Produce", "DeckListModal");

        public void Build()
        {
            var modal = PrefabBuildUtil.CreateModal<DeckListModal>("DeckListModal", out var panel);

            // 固定の文言（View のフィールドにしない）
            ScreenLayout.AddLabel(panel, "TitleText", "山札・捨札・除外", ScreenLayout.Title, 80f);
            var content = PrefabBuildUtil.AddScrollList(panel, "ListScroll", out var scroll);
            PrefabBuildUtil.SetLayout(scroll.gameObject, minHeight: 1200f, preferredHeight: 1200f);
            // Content に付いている VLG をそのまま使い、spacing を 24 から 8 にする
            PrefabBuildUtil.AddVertical(content.gameObject, new LayoutSpec(8f, 0, 0, 0, 0, TextAnchor.UpperCenter, true, true, true, false));
            var close = PrefabBuildUtil.AddButtonPart(panel, "CloseButton", "閉じる", ButtonStyle.Secondary);

            PrefabBuildUtil.SetRef(modal, "listContent", content);
            PrefabBuildUtil.SetRef(modal, "sectionHeaderPrefab", ScreenLayout.LoadPart<DeckSectionHeaderRowView>("DeckSectionHeaderRow"));
            PrefabBuildUtil.SetRef(modal, "cardRowPrefab", ScreenLayout.LoadPart<DeckCardRowView>("DeckCardRow"));
            PrefabBuildUtil.SetRef(modal, "closeButton", close);

            PrefabBuildUtil.SavePrefab(modal.gameObject, AssetPath);
            PrefabBuildUtil.RegisterScreen(AssetPath);
        }
    }
}
