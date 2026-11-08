using TMPro;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Common.Parts;

namespace Uta.Client.Editor.Prefabs.Parts
{
    /// <summary>共通部品 Tab（タブ 1 つ。文字と選択の下線）を作る。</summary>
    public sealed class TabBuilder : IPrefabBuilder
    {
        public int Order
        {
            get { return 130; }
        }

        public string AssetPath
        {
            get { return PrefabBuildUtil.PartPath("Tab"); }
        }

        public void Build(PrefabBuildSession session)
        {
            var root = session.CreateRoot("Tab");
            var image = PrefabBuildUtil.AddImage(root, UiTheme.Header, true);
            var button = PrefabBuildUtil.AddButton(root, image);
            PrefabBuildUtil.SetLayout(root, minHeight: UiTheme.TabHeight, preferredHeight: UiTheme.TabHeight, flexibleWidth: 1f);
            var view = root.AddComponent<TabView>();

            var labelRect = PrefabBuildUtil.AddChild(root.transform, "Label", RectSpec.Stretch(8f, 8f, 0f, 8f));
            var label = PrefabBuildUtil.AddText(labelRect.gameObject, "タブ",
                new TextSpec(UiTheme.FontBody, UiTheme.Text, TextAlignmentOptions.Center, false, true));

            var underlineRect = PrefabBuildUtil.AddChild(root.transform, "Underline", RectSpec.Bottom(8f));
            var underline = PrefabBuildUtil.AddImage(underlineRect.gameObject, UiTheme.Accent, false);
            // 選ばれたタブだけ C# が有効にする
            underline.enabled = false;

            PrefabBuildUtil.SetRef(view, "button", button);
            PrefabBuildUtil.SetRef(view, "label", label);
            PrefabBuildUtil.SetRef(view, "underline", underline);
            PrefabBuildUtil.SavePrefab(root, AssetPath);
        }
    }
}
