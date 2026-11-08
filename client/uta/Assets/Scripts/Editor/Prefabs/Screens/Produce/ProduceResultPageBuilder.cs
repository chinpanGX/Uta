using TMPro;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Produce;

namespace Uta.Client.Editor.Prefabs.Screens
{
    /// <summary>プロデュースの最終評価のページ（Screens/Produce/ProduceResultPage.prefab）を作る。Addressables の Screens に登録する。戻るボタンは作らない。</summary>
    public sealed class ProduceResultPageBuilder : IPrefabBuilder
    {
        public int Order => 2000;

        public string AssetPath => PrefabBuildUtil.ScreenPath("Produce", "ProduceResultPage");

        public void Build()
        {
            var page = PrefabBuildUtil.CreatePage<ProduceResultPage>("ProduceResultPage", out var body);
            ScreenLayout.SetupBody(body);
            var main = ScreenLayout.AddMain(body);

            var rank = ScreenLayout.AddLabel(main, "RankText", "ランク -",
                new TextSpec(UiTheme.FontTitle, UiTheme.Accent, TextAlignmentOptions.Center, false, true), 120f);
            var lines = PrefabBuildUtil.AddLabel(main, "LinesText", "", ScreenLayout.Block);
            PrefabBuildUtil.SetLayout(lines.gameObject, flexibleHeight: 1f);
            var home = PrefabBuildUtil.AddButtonPart(main, "HomeButton", "ホームへ", ButtonStyle.Primary);

            PrefabBuildUtil.SetRef(page, "rankText", rank);
            PrefabBuildUtil.SetRef(page, "linesText", lines);
            PrefabBuildUtil.SetRef(page, "homeButton", home);

            PrefabBuildUtil.SavePrefab(page.gameObject, AssetPath);
            PrefabBuildUtil.RegisterScreen(AssetPath);
        }
    }
}
