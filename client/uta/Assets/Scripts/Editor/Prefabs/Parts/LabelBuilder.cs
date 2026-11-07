using Uta.Client.Presentation.Common;

namespace Uta.Client.Editor.Prefabs.Parts
{
    /// <summary>共通部品 Label（TMP の文字 1 つ）を作る。</summary>
    public sealed class LabelBuilder : IPrefabBuilder
    {
        public int Order
        {
            get { return 100; }
        }

        public string AssetPath
        {
            get { return PrefabBuildUtil.PartPath("Label"); }
        }

        public void Build()
        {
            var root = PrefabBuildUtil.CreateRoot("Label");
            PrefabBuildUtil.AddText(root, "ラベル", TextSpec.Body);
            PrefabBuildUtil.SavePrefab(root, AssetPath);
        }
    }
}
