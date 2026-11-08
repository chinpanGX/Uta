using NUnit.Framework;
using TMPro;
using UnityEditor;
using Uta.Client.Presentation.Gacha;

namespace Uta.Client.Tests
{
    /// <summary>ガチャの画面と行の Prefab の View のフィールドと子の対応（P2 の計画 3.3）。</summary>
    public sealed class GachaPrefabTests
    {
        private const string GachaPagePath = "Assets/Prefabs/Screens/Gacha/GachaPage.prefab";
        private const string GachaRatesModalPath = "Assets/Prefabs/Screens/Gacha/GachaRatesModal.prefab";
        private const string GachaResultModalPath = "Assets/Prefabs/Screens/Gacha/GachaResultModal.prefab";
        private const string HeadingRowPath = "Assets/Prefabs/Parts/GachaRateHeadingRow.prefab";
        private const string RateRowPath = "Assets/Prefabs/Parts/GachaRateRow.prefab";
        private const string ResultRowPath = "Assets/Prefabs/Parts/GachaResultRow.prefab";

        [Test]
        public void GachaPage_FieldsPointToChildren()
        {
            var view = ScreenPrefabAssert.RootView<GachaPage>(ScreenPrefabAssert.Load(GachaPagePath));

            ScreenPrefabAssert.FieldIsChild(view, "header", "Body/Header");
            ScreenPrefabAssert.FieldIsChild(view, "currencyText", "Body/Main/CurrencyText");
            ScreenPrefabAssert.FieldIsChild(view, "lineupText", "Body/Main/LineupText");
            ScreenPrefabAssert.FieldIsChild(view, "exchangeText", "Body/Main/ExchangeText");
            ScreenPrefabAssert.FieldIsChild(view, "ratesButton", "Body/Main/RatesButton");
            ScreenPrefabAssert.FieldIsChild(view, "singleButton", "Body/Main/DrawRow/SingleButton");
            ScreenPrefabAssert.FieldIsChild(view, "tenButton", "Body/Main/DrawRow/TenButton");
        }

        [Test]
        public void GachaRatesModal_FieldsPointToChildrenAndRows()
        {
            var view = ScreenPrefabAssert.RootView<GachaRatesModal>(ScreenPrefabAssert.Load(GachaRatesModalPath));

            ScreenPrefabAssert.FieldIsChild(view, "listContent", "Panel/List/Viewport/Content");
            ScreenPrefabAssert.FieldIsChild(view, "closeButton", "Panel/CloseButton");
            ScreenPrefabAssert.FieldIsAsset(view, "headingRowPrefab", HeadingRowPath);
            ScreenPrefabAssert.FieldIsAsset(view, "lineRowPrefab", RateRowPath);
        }

        [Test]
        public void GachaResultModal_FieldsPointToChildrenAndRow()
        {
            var view = ScreenPrefabAssert.RootView<GachaResultModal>(ScreenPrefabAssert.Load(GachaResultModalPath));

            ScreenPrefabAssert.FieldIsChild(view, "listContent", "Panel/List/Viewport/Content");
            ScreenPrefabAssert.FieldIsChild(view, "retryButton", "Panel/ButtonRow/RetryButton");
            ScreenPrefabAssert.FieldIsChild(view, "closeButton", "Panel/ButtonRow/CloseButton");
            ScreenPrefabAssert.FieldIsAsset(view, "rowPrefab", ResultRowPath);
        }

        [Test]
        public void GachaRows_TextIsOwnTmp()
        {
            foreach (var path in new[] { HeadingRowPath, RateRowPath, ResultRowPath })
            {
                var row = ScreenPrefabAssert.RootView<GachaLineRowView>(ScreenPrefabAssert.Load(path));
                var property = new SerializedObject(row).FindProperty("text");
                Assert.IsNotNull(property, path + ": field text not found");
                var own = row.GetComponent<TMP_Text>();
                Assert.IsNotNull(own, path + ": root has no TMP_Text");
                Assert.AreEqual(own, property.objectReferenceValue, path);
            }
        }
    }
}
