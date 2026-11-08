using UnityEngine;
using UnityEngine.UI;
using Uta.Client.Presentation.Title;

namespace Uta.Client.Editor.Prefabs.Screens
{
    /// <summary>タイトルのページ（Screens/Title/TitlePage.prefab）を作る。Addressables の Screens に登録する。</summary>
    public sealed class TitlePageBuilder : IPrefabBuilder
    {
        public int Order => 2000;

        public string AssetPath => PrefabBuildUtil.ScreenPath("Title", "TitlePage");

        public void Build(PrefabBuildSession session)
        {
            var page = PrefabBuildUtil.CreatePage<TitlePage>(session, "TitlePage", out var body);
            ScreenLayout.SetupBody(body);
            var main = ScreenLayout.AddMain(body);
            main.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;

            ScreenLayout.AddFlexibleSpacer(main, "TopSpacer");
            ScreenLayout.AddLabel(main, "TitleText", "Uta", ScreenLayout.Title, 120f);
            ScreenLayout.AddFlexibleSpacer(main, "MiddleSpacer");
            var status = ScreenLayout.AddLabel(main, "StatusText", "タップしてスタート", ScreenLayout.BlockCenter, 160f);
            var start = PrefabBuildUtil.AddButtonPart(main, "StartButton", "スタート");
            ScreenLayout.AddFixedSpacer(main, "BottomSpacer", 120f);

            PrefabBuildUtil.SetRef(page, "statusText", status);
            PrefabBuildUtil.SetRef(page, "startButton", start);

            PrefabBuildUtil.SavePrefab(page.gameObject, AssetPath);
            PrefabBuildUtil.RegisterScreen(AssetPath);
        }
    }
}
