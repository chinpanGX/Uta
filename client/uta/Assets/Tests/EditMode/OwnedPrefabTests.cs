using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Uta.Client.Presentation.Common.Parts;
using Uta.Client.Presentation.Owned;

namespace Uta.Client.Tests
{
    /// <summary>所持一覧・サポートカードの詳細の Prefab の View のフィールドと子の対応（P2 の計画 3.3）。</summary>
    public sealed class OwnedPrefabTests
    {
        private const string OwnedPagePath = "Assets/Prefabs/Screens/Owned/OwnedPage.prefab";
        private const string SupportDetailPagePath = "Assets/Prefabs/Screens/Owned/SupportDetailPage.prefab";
        private const string ListRowPath = "Assets/Prefabs/Parts/ListRow.prefab";

        [Test]
        public void OwnedPage_FieldsPointToChildren()
        {
            var view = ScreenPrefabAssert.RootView<OwnedPage>(ScreenPrefabAssert.Load(OwnedPagePath));

            ScreenPrefabAssert.FieldIsChild(view, "header", "Body/Header");
            ScreenPrefabAssert.FieldIsChild(view, "tabBar", "Body/TabBar");
            ScreenPrefabAssert.FieldIsChild(view, "controls", "Body/Main/Controls");
            ScreenPrefabAssert.FieldIsChild(view, "filterRow", "Body/Main/Controls/FilterRow");
            ScreenPrefabAssert.FieldIsChild(view, "sortButton", "Body/Main/Controls/SortRow/SortButton");
            ScreenPrefabAssert.FieldIsChild(view, "orderButton", "Body/Main/Controls/SortRow/OrderButton");
            ScreenPrefabAssert.FieldIsChild(view, "planButton", "Body/Main/Controls/FilterRow/PlanButton");
            ScreenPrefabAssert.FieldIsChild(view, "typeButton", "Body/Main/Controls/FilterRow/TypeButton");
            ScreenPrefabAssert.FieldIsChild(view, "emptyText", "Body/Main/EmptyText");
            ScreenPrefabAssert.FieldIsChild(view, "listContent", "Body/Main/List/Viewport/Content");
            ScreenPrefabAssert.FieldIsAsset(view, "rowPrefab", ListRowPath);
        }

        [Test]
        public void OwnedPage_TabsAreIdolsThenSupport()
        {
            var view = ScreenPrefabAssert.RootView<OwnedPage>(ScreenPrefabAssert.Load(OwnedPagePath));
            var tabBar = view.transform.Find("Body/TabBar");
            Assert.IsNotNull(tabBar, "Body/TabBar not found");
            var barView = tabBar.GetComponent<TabBarView>();
            Assert.IsNotNull(barView, "Body/TabBar has no TabBarView");

            var tabs = new SerializedObject(barView).FindProperty("tabs");
            Assert.IsNotNull(tabs, "TabBarView.tabs not found");
            Assert.AreEqual(2, tabs.arraySize);
            Assert.AreEqual("TabIdols", NameOf(tabs.GetArrayElementAtIndex(0).objectReferenceValue));
            Assert.AreEqual("TabSupport", NameOf(tabs.GetArrayElementAtIndex(1).objectReferenceValue));
        }

        [Test]
        public void OwnedPage_EmptyTextIsInactive()
        {
            var prefab = ScreenPrefabAssert.Load(OwnedPagePath);
            var empty = prefab.transform.Find("Body/Main/EmptyText");
            Assert.IsNotNull(empty, "Body/Main/EmptyText not found");
            Assert.IsFalse(empty.gameObject.activeSelf);
        }

        [Test]
        public void SupportDetailPage_FieldsPointToChildren()
        {
            var view = ScreenPrefabAssert.RootView<SupportDetailPage>(ScreenPrefabAssert.Load(SupportDetailPagePath));

            ScreenPrefabAssert.FieldIsChild(view, "header", "Body/Header");
            ScreenPrefabAssert.FieldIsChild(view, "infoText", "Body/Main/InfoText");
            ScreenPrefabAssert.FieldIsChild(view, "resourcesText", "Body/Main/ResourcesText");
            ScreenPrefabAssert.FieldIsChild(view, "plus1Button", "Body/Main/Plus1Button");
            ScreenPrefabAssert.FieldIsChild(view, "plus5Button", "Body/Main/Plus5Button");
            ScreenPrefabAssert.FieldIsChild(view, "maxButton", "Body/Main/MaxButton");
            ScreenPrefabAssert.FieldIsChild(view, "limitBreakButton", "Body/Main/LimitBreakButton");
            ScreenPrefabAssert.FieldIsChild(view, "abilitiesText", "Body/Main/AbilitiesText");
        }

        private static string NameOf(Object value)
        {
            var component = value as Component;
            return component != null ? component.gameObject.name : null;
        }
    }
}
