using UnityEngine;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Common.Parts;

namespace Uta.Client.Editor.Prefabs.Parts
{
    /// <summary>共通部品 TabBar（タブを横に並べる帯。タブは画面の生成が AddTabBar で並べる）を作る。</summary>
    public sealed class TabBarBuilder : IPrefabBuilder
    {
        public int Order
        {
            get { return 140; }
        }

        public string AssetPath
        {
            get { return PrefabBuildUtil.PartPath("TabBar"); }
        }

        public void Build()
        {
            var root = PrefabBuildUtil.CreateRoot("TabBar");
            PrefabBuildUtil.SetRect((RectTransform)root.transform, RectSpec.Top(UiTheme.TabHeight));
            PrefabBuildUtil.AddImage(root, UiTheme.Header, true);
            PrefabBuildUtil.AddHorizontal(root, new LayoutSpec(0f, 0, 0, 0, 0, TextAnchor.MiddleCenter, true, true, true, true));
            // flexibleHeight 0: HLG の伸ばす H が true なので、明示しないと flexibleHeight 1 になり、親の VLG の余りを吸って高くなる
            PrefabBuildUtil.SetLayout(root, minHeight: UiTheme.TabHeight, preferredHeight: UiTheme.TabHeight, flexibleHeight: 0f);
            var view = root.AddComponent<TabBarView>();
            PrefabBuildUtil.SetRefs(view, "tabs", new Object[0]);
            PrefabBuildUtil.SavePrefab(root, AssetPath);
        }
    }
}
