using TMPro;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Common.Parts;

namespace Uta.Client.Editor.Prefabs.Parts
{
    /// <summary>共通部品 Button（単色のボタンと子の文字）を作る。</summary>
    public sealed class ButtonBuilder : IPrefabBuilder
    {
        public int Order
        {
            get { return 110; }
        }

        public string AssetPath
        {
            get { return PrefabBuildUtil.PartPath("Button"); }
        }

        public void Build()
        {
            var root = PrefabBuildUtil.CreateRoot("Button");
            PrefabBuildUtil.SetRect((UnityEngine.RectTransform)root.transform, RectSpec.Center(600f, UiTheme.ButtonHeight));
            var image = PrefabBuildUtil.AddImage(root, UiTheme.ButtonPrimary, true);
            var button = PrefabBuildUtil.AddButton(root, image);
            PrefabBuildUtil.SetLayout(root, minHeight: UiTheme.ButtonHeight, preferredHeight: UiTheme.ButtonHeight);
            var view = root.AddComponent<ButtonView>();

            var labelRect = PrefabBuildUtil.AddChild(root.transform, "Label", RectSpec.Stretch(16f, 16f, 0f, 0f));
            var label = PrefabBuildUtil.AddText(labelRect.gameObject, "ボタン",
                new TextSpec(UiTheme.FontBody, UiTheme.Text, TextAlignmentOptions.Center, false, true));

            PrefabBuildUtil.SetRef(view, "button", button);
            PrefabBuildUtil.SetRef(view, "background", image);
            PrefabBuildUtil.SetRef(view, "label", label);
            PrefabBuildUtil.SavePrefab(root, AssetPath);
        }
    }
}
