using TMPro;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Produce;

namespace Uta.Client.Editor.Prefabs.Screens
{
    /// <summary>山札などの一覧の見出しの行（Parts/DeckSectionHeaderRow.prefab）を作る。Addressables に登録しない。</summary>
    public sealed class DeckSectionHeaderRowBuilder : IPrefabBuilder
    {
        private const string Name = "DeckSectionHeaderRow";

        public int Order => 1020;

        public string AssetPath => PrefabBuildUtil.PartPath(Name);

        public void Build(PrefabBuildSession session)
        {
            var root = session.CreateRoot(Name);
            var text = PrefabBuildUtil.AddText(root, "山札 0 枚",
                ScreenLayout.Heading);
            PrefabBuildUtil.SetLayout(root, minHeight: 60f, preferredHeight: 60f);
            var view = root.AddComponent<DeckSectionHeaderRowView>();

            PrefabBuildUtil.SetRef(view, "headerText", text);
            PrefabBuildUtil.SavePrefab(root, AssetPath);
        }
    }
}
