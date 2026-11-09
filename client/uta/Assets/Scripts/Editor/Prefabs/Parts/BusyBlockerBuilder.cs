using Uta.Client.Presentation.Common;

namespace Uta.Client.Editor.Prefabs.Parts
{
    /// <summary>共通部品 BusyBlocker（通信中に全面の透明な Image で入力を止める）を作る。</summary>
    public sealed class BusyBlockerBuilder : IPrefabBuilder
    {
        public int Order
        {
            get { return 180; }
        }

        public string AssetPath
        {
            get { return PrefabBuildUtil.PartPath("BusyBlocker"); }
        }

        public void Build(PrefabBuildSession session)
        {
            var root = session.CreateRoot("BusyBlocker");
            var image = PrefabBuildUtil.AddImage(root, UiTheme.Transparent, true);
            // 通信中だけ BusyIndicator が有効にする
            image.enabled = false;
            var indicator = root.AddComponent<BusyIndicator>();
            PrefabBuildUtil.SetRef(indicator, "blocker", image);
            PrefabBuildUtil.SavePrefab(root, AssetPath);
        }
    }
}
