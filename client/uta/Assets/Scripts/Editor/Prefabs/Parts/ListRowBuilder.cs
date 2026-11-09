using TMPro;
using UnityEngine;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Common.Parts;

namespace Uta.Client.Editor.Prefabs.Parts
{
    /// <summary>共通部品 ListRow（一覧の行。名前・補足・右の値）を作る。</summary>
    public sealed class ListRowBuilder : IPrefabBuilder
    {
        public int Order
        {
            get { return 150; }
        }

        public string AssetPath
        {
            get { return PrefabBuildUtil.PartPath("ListRow"); }
        }

        public void Build(PrefabBuildSession session)
        {
            var root = session.CreateRoot("ListRow");
            var image = PrefabBuildUtil.AddImage(root, UiTheme.Panel, true);
            var button = PrefabBuildUtil.AddButton(root, image);
            PrefabBuildUtil.SetLayout(root, minHeight: UiTheme.RowHeight, preferredHeight: UiTheme.RowHeight);
            PrefabBuildUtil.AddHorizontal(root, new LayoutSpec(UiTheme.Spacing, UiTheme.Padding, UiTheme.Padding, 16, 16,
                TextAnchor.MiddleLeft, true, true, false, true));
            var view = root.AddComponent<ListRowView>();

            var column = PrefabBuildUtil.AddChild(root.transform, "TextColumn");
            PrefabBuildUtil.AddVertical(column.gameObject, new LayoutSpec(4f, 0, 0, 0, 0, TextAnchor.MiddleLeft, true, true, true, false));
            PrefabBuildUtil.SetLayout(column.gameObject, flexibleWidth: 1f);

            var titleText = PrefabBuildUtil.AddLabel(column, "TitleText", "名前",
                new TextSpec(UiTheme.FontBody, UiTheme.Text, TextAlignmentOptions.MidlineLeft, false, true));
            var subText = PrefabBuildUtil.AddLabel(column, "SubText", "補足",
                new TextSpec(UiTheme.FontSmall, UiTheme.TextMuted, TextAlignmentOptions.MidlineLeft, false, true));
            var rightText = PrefabBuildUtil.AddLabel(root.transform, "RightText", "",
                new TextSpec(UiTheme.FontSmall, UiTheme.Text, TextAlignmentOptions.MidlineRight, false, true));
            PrefabBuildUtil.SetLayout(rightText.gameObject, preferredWidth: 240f, flexibleWidth: 0f);

            PrefabBuildUtil.SetRef(view, "button", button);
            PrefabBuildUtil.SetRef(view, "background", image);
            PrefabBuildUtil.SetRef(view, "titleText", titleText);
            PrefabBuildUtil.SetRef(view, "subText", subText);
            PrefabBuildUtil.SetRef(view, "rightText", rightText);
            PrefabBuildUtil.SavePrefab(root, AssetPath);
        }
    }
}
