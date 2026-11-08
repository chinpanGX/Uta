using NUnit.Framework;
using TMPro;
using UnityEngine.UI;
using Uta.Client.Presentation.Home;

namespace Uta.Client.Infrastructure.Tests
{
    /// <summary>HomePage の Prefab の View のフィールドと子の対応（P2 の計画 3.3）と、ステージの仮の枠。</summary>
    public sealed class HomePrefabTests
    {
        private const string HomePagePath = "Assets/Prefabs/Screens/Home/HomePage.prefab";

        [Test]
        public void HomePage_FieldsPointToChildren()
        {
            var view = ScreenPrefabAssert.RootView<HomePage>(ScreenPrefabAssert.Load(HomePagePath));

            ScreenPrefabAssert.FieldIsChild(view, "profileText", "Body/Main/ProfileText");
            ScreenPrefabAssert.FieldIsChild(view, "stageImage", "Body/Main/Stage");
            ScreenPrefabAssert.FieldIsChild(view, "stageLabel", "Body/Main/Stage/StageLabel");
            ScreenPrefabAssert.FieldIsChild(view, "resumePanel", "Body/Main/ResumePanel");
            ScreenPrefabAssert.FieldIsChild(view, "primaryButton", "Body/Main/PrimaryButton");
            ScreenPrefabAssert.FieldIsChild(view, "navProduceButton", "Body/Main/Nav/NavProduceButton");
            ScreenPrefabAssert.FieldIsChild(view, "navGachaButton", "Body/Main/Nav/NavGachaButton");
            ScreenPrefabAssert.FieldIsChild(view, "navOwnedButton", "Body/Main/Nav/NavOwnedButton");
        }

        [Test]
        public void HomePage_StageIsPlaceholder()
        {
            var prefab = ScreenPrefabAssert.Load(HomePagePath);

            var stage = prefab.transform.Find("Body/Main/Stage");
            Assert.IsNotNull(stage, "Body/Main/Stage not found");
            var image = stage.GetComponent<RawImage>();
            Assert.IsNotNull(image, "Body/Main/Stage has no RawImage");
            Assert.IsNull(image.texture);
            Assert.IsFalse(image.raycastTarget);

            var label = prefab.transform.Find("Body/Main/Stage/StageLabel");
            Assert.IsNotNull(label, "Body/Main/Stage/StageLabel not found");
            Assert.AreEqual("キャラ表示", label.GetComponent<TMP_Text>().text);

            var primary = prefab.transform.Find("Body/Main/PrimaryButton");
            Assert.IsNotNull(primary, "Body/Main/PrimaryButton not found");
            Assert.AreEqual(200f, primary.GetComponent<LayoutElement>().preferredHeight);
        }
    }
}
