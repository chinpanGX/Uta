using Uta.Client.Presentation.Common.Parts;
using Uta.Client.Presentation.Produce;

namespace Uta.Client.Editor.Prefabs.Screens
{
    /// <summary>授業のページ（Screens/Produce/ClassPage.prefab）を作る。Addressables の Screens に登録する。</summary>
    public sealed class ClassPageBuilder : IPrefabBuilder
    {
        public int Order => 2000;

        public string AssetPath => PrefabBuildUtil.ScreenPath("Produce", "ClassPage");

        public void Build(PrefabBuildSession session)
        {
            var page = PrefabBuildUtil.CreatePage<ClassPage>(session, "ClassPage", out var body);
            ScreenLayout.SetupBody(body);
            var header = PrefabBuildUtil.AddHeader(body, "授業");
            var main = ScreenLayout.AddMain(body);

            var optionList = ProduceLayout.AddRuntimeList(main, "OptionList", 24f);
            ScreenLayout.AddFlexibleSpacer(main, "Spacer");

            PrefabBuildUtil.SetRef(page, "header", header);
            PrefabBuildUtil.SetRef(page, "optionList", optionList);
            PrefabBuildUtil.SetRef(page, "optionButtonPrefab", ScreenLayout.LoadPart<ButtonView>("Button"));

            PrefabBuildUtil.SavePrefab(page.gameObject, AssetPath);
            PrefabBuildUtil.RegisterScreen(AssetPath);
        }
    }
}
