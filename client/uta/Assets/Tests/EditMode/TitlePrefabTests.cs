using NUnit.Framework;
using Uta.Client.Presentation.Title;

namespace Uta.Client.Infrastructure.Tests
{
    /// <summary>TitlePage の Prefab の View のフィールドと子の対応（P2 の計画 3.3）。</summary>
    public sealed class TitlePrefabTests
    {
        private const string TitlePagePath = "Assets/Prefabs/Screens/Title/TitlePage.prefab";

        [Test]
        public void TitlePage_FieldsPointToChildren()
        {
            var view = ScreenPrefabAssert.RootView<TitlePage>(ScreenPrefabAssert.Load(TitlePagePath));

            ScreenPrefabAssert.FieldIsChild(view, "statusText", "Body/Main/StatusText");
            ScreenPrefabAssert.FieldIsChild(view, "startButton", "Body/Main/StartButton");
        }
    }
}
