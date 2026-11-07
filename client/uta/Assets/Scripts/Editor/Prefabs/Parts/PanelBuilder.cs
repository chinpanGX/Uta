using Uta.Client.Presentation.Common;

namespace Uta.Client.Editor.Prefabs.Parts
{
    /// <summary>共通部品 Panel（単色の背景と縦のレイアウト）を作る。</summary>
    public sealed class PanelBuilder : IPrefabBuilder
    {
        public int Order
        {
            get { return 120; }
        }

        public string AssetPath
        {
            get { return PrefabBuildUtil.PartPath("Panel"); }
        }

        public void Build()
        {
            var root = PrefabBuildUtil.CreateRoot("Panel");
            PrefabBuildUtil.AddImage(root, UiTheme.Panel, true);
            PrefabBuildUtil.AddVertical(root, LayoutSpec.Column);
            PrefabBuildUtil.SavePrefab(root, AssetPath);
        }
    }
}
