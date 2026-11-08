using TMPro;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Produce;

namespace Uta.Client.Editor.Prefabs.Screens
{
    /// <summary>山札などの一覧のカードの行（Parts/DeckCardRow.prefab）を作る。Addressables に登録しない。</summary>
    public sealed class DeckCardRowBuilder : IPrefabBuilder
    {
        private const string Name = "DeckCardRow";

        public int Order => 1030;

        public string AssetPath => PrefabBuildUtil.PartPath(Name);

        public void Build()
        {
            var root = PrefabBuildUtil.CreateRoot(Name);
            var text = PrefabBuildUtil.AddText(root, "カード名　消費 4\n説明文",
                new TextSpec(UiTheme.FontSmall, UiTheme.Text, TextAlignmentOptions.TopLeft, true, false));
            // 高さは TMP の preferredHeight（親の VLG が制御する）。最小だけ決める
            PrefabBuildUtil.SetLayout(root, minHeight: 50f);
            var view = root.AddComponent<DeckCardRowView>();

            PrefabBuildUtil.SetRef(view, "text", text);
            PrefabBuildUtil.SavePrefab(root, AssetPath);
        }
    }
}
