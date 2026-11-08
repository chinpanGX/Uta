using Uta.Client.Presentation.Produce;

namespace Uta.Client.Editor.Prefabs.Screens
{
    /// <summary>プロデュースの背景のページ（Screens/Produce/ProduceBackdropPage.prefab）を作る。Addressables の Screens に登録する。</summary>
    public sealed class ProduceBackdropPageBuilder : IPrefabBuilder
    {
        public int Order => 2000;

        public string AssetPath => PrefabBuildUtil.ScreenPath("Produce", "ProduceBackdropPage");

        public void Build()
        {
            var page = PrefabBuildUtil.CreatePage<ProduceBackdropPage>("ProduceBackdropPage", out var body);
            ScreenLayout.SetupBody(body);
            var main = ScreenLayout.AddMain(body);

            var headerText = ScreenLayout.AddLabel(main, "HeaderText", "週 1", ScreenLayout.Block, 160f);
            ScreenLayout.AddFlexibleSpacer(main, "Spacer");

            PrefabBuildUtil.SetRef(page, "headerText", headerText);

            PrefabBuildUtil.SavePrefab(page.gameObject, AssetPath);
            PrefabBuildUtil.RegisterScreen(AssetPath);
        }
    }
}
