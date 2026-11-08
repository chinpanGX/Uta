using TMPro;
using UnityEngine;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Produce;

namespace Uta.Client.Editor.Prefabs.Screens
{
    /// <summary>スキルカード 1 枚の行（Parts/SkillCardRow.prefab。手札・獲得の選択肢で共有）を作る。Addressables に登録しない。</summary>
    public sealed class SkillCardRowBuilder : IPrefabBuilder
    {
        private const string Name = "SkillCardRow";

        public int Order => 1010;

        public string AssetPath => PrefabBuildUtil.PartPath(Name);

        public void Build()
        {
            var root = PrefabBuildUtil.CreateRoot(Name);
            var image = PrefabBuildUtil.AddImage(root, UiTheme.Panel, true);
            var button = PrefabBuildUtil.AddButton(root, image);
            // 高さ: 上下 24 + 52 + 44 + 92 + spacing 8 = 220
            PrefabBuildUtil.SetLayout(root, minHeight: 220f, preferredHeight: 220f);
            PrefabBuildUtil.AddVertical(root, new LayoutSpec(4f, 32, 32, 12, 12, TextAnchor.UpperLeft, true, true, true, false));
            var view = root.AddComponent<SkillCardRowView>();

            var title = PrefabBuildUtil.AddLabel(root.transform, "TitleText", "カード名 [R] アクティブ",
                new TextSpec(UiTheme.FontBody, UiTheme.Text, TextAlignmentOptions.MidlineLeft, false, true));
            PrefabBuildUtil.SetLayout(title.gameObject, minHeight: 52f, preferredHeight: 52f);
            var costs = PrefabBuildUtil.AddLabel(root.transform, "CostsText", "消費 4",
                new TextSpec(UiTheme.FontSmall, UiTheme.Accent, TextAlignmentOptions.MidlineLeft, false, true));
            PrefabBuildUtil.SetLayout(costs.gameObject, minHeight: 44f, preferredHeight: 44f);
            var description = PrefabBuildUtil.AddLabel(root.transform, "DescriptionText", "説明文",
                new TextSpec(UiTheme.FontSmall, UiTheme.TextMuted, TextAlignmentOptions.TopLeft, true, true));
            PrefabBuildUtil.SetLayout(description.gameObject, minHeight: 92f, preferredHeight: 92f);

            PrefabBuildUtil.SetRef(view, "button", button);
            PrefabBuildUtil.SetRef(view, "background", image);
            PrefabBuildUtil.SetRef(view, "titleText", title);
            PrefabBuildUtil.SetRef(view, "costsText", costs);
            PrefabBuildUtil.SetRef(view, "descriptionText", description);
            PrefabBuildUtil.SavePrefab(root, AssetPath);
        }
    }
}
