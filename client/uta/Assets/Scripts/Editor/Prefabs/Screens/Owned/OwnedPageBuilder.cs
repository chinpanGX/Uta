using UnityEngine;
using Uta.Client.Presentation.Common.Parts;
using Uta.Client.Presentation.Owned;

namespace Uta.Client.Editor.Prefabs.Screens
{
    /// <summary>所持一覧のページ（Screens/Owned/OwnedPage.prefab）を作る。Addressables の Screens に登録する。</summary>
    public sealed class OwnedPageBuilder : IPrefabBuilder
    {
        public int Order => 2000;

        public string AssetPath => PrefabBuildUtil.ScreenPath("Owned", "OwnedPage");

        public void Build()
        {
            var page = PrefabBuildUtil.CreatePage<OwnedPage>("OwnedPage", out var body);
            ScreenLayout.SetupBody(body);
            var header = PrefabBuildUtil.AddHeader(body, "所持一覧");
            var tabBar = PrefabBuildUtil.AddTabBar(body, "TabBar", new[] { "TabIdols", "TabSupport" }, new[] { "育成アイドル", "サポート" });
            var main = ScreenLayout.AddMain(body);

            var controls = PrefabBuildUtil.AddChild(main, "Controls");
            PrefabBuildUtil.AddVertical(controls.gameObject, new LayoutSpec(16f, 0, 0, 0, 0, TextAnchor.UpperCenter, true, true, true, false));
            var sortRow = ScreenLayout.AddRow(controls, "SortRow", 130f);
            var sort = PrefabBuildUtil.AddButtonPart(sortRow, "SortButton", "並び: レア度", ButtonStyle.Secondary);
            var order = PrefabBuildUtil.AddButtonPart(sortRow, "OrderButton", "降順", ButtonStyle.Secondary);
            var filterRow = ScreenLayout.AddRow(controls, "FilterRow", 130f);
            var plan = PrefabBuildUtil.AddButtonPart(filterRow, "PlanButton", "プラン: 全て", ButtonStyle.Secondary);
            var type = PrefabBuildUtil.AddButtonPart(filterRow, "TypeButton", "タイプ: 全て", ButtonStyle.Secondary);

            var empty = ScreenLayout.AddLabel(main, "EmptyText", "ありません", ScreenLayout.LineCenter, 80f);
            empty.gameObject.SetActive(false);
            var content = PrefabBuildUtil.AddScrollList(main, "List", out _);

            PrefabBuildUtil.SetRef(page, "header", header);
            PrefabBuildUtil.SetRef(page, "tabBar", tabBar);
            PrefabBuildUtil.SetRef(page, "controls", controls);
            PrefabBuildUtil.SetRef(page, "filterRow", filterRow);
            PrefabBuildUtil.SetRef(page, "sortButton", sort);
            PrefabBuildUtil.SetRef(page, "orderButton", order);
            PrefabBuildUtil.SetRef(page, "planButton", plan);
            PrefabBuildUtil.SetRef(page, "typeButton", type);
            PrefabBuildUtil.SetRef(page, "emptyText", empty);
            PrefabBuildUtil.SetRef(page, "listContent", content);
            PrefabBuildUtil.SetRef(page, "rowPrefab", ScreenLayout.LoadPart<ListRowView>("ListRow"));

            PrefabBuildUtil.SavePrefab(page.gameObject, AssetPath);
            PrefabBuildUtil.RegisterScreen(AssetPath);
        }
    }
}
