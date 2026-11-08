using TMPro;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Produce;

namespace Uta.Client.Editor.Prefabs.Screens
{
    /// <summary>レッスン・試験のページ（Screens/Produce/LessonPage.prefab）を作る。Addressables の Screens に登録する。</summary>
    public sealed class LessonPageBuilder : IPrefabBuilder
    {
        public int Order => 2000;

        public string AssetPath => PrefabBuildUtil.ScreenPath("Produce", "LessonPage");

        public void Build(PrefabBuildSession session)
        {
            var page = PrefabBuildUtil.CreatePage<LessonPage>(session, "LessonPage", out var body);
            ScreenLayout.SetupBody(body);
            // ヘッダーの子は足さない（BackButton・TitleText・RightSlot のまま。残りのターンは Main の先頭に置く）
            var header = ProduceLayout.AddHeaderWithoutBack(body, "レッスン");
            var menu = ProduceLayout.AddMenuButton(header);
            var main = ScreenLayout.AddMain(body);

            var turn = ScreenLayout.AddLabel(main, "TurnText", "残り 0 / 0", ScreenLayout.LineRight, 60f);
            var progress = ScreenLayout.AddLabel(main, "ProgressText", "", ScreenLayout.Block, 170f);
            var status = ScreenLayout.AddLabel(main, "StatusText", "",
                new TextSpec(UiTheme.FontSmall, UiTheme.Text, TextAlignmentOptions.TopLeft, true, false), 220f);
            var lastResult = ScreenLayout.AddLabel(main, "LastResultText", "",
                new TextSpec(UiTheme.FontSmall, UiTheme.Accent, TextAlignmentOptions.MidlineLeft, false, true), 50f);
            var piles = PrefabBuildUtil.AddButtonPart(main, "PilesButton", "山札 0　捨札 0　除外 0", ButtonStyle.Secondary);
            var handContent = PrefabBuildUtil.AddScrollList(main, "HandScroll", out _);
            var skip = PrefabBuildUtil.AddButtonPart(main, "SkipButton", "スキップ", ButtonStyle.Primary);

            PrefabBuildUtil.SetRef(page, "header", header);
            PrefabBuildUtil.SetRef(page, "turnText", turn);
            PrefabBuildUtil.SetRef(page, "menuButton", menu);
            PrefabBuildUtil.SetRef(page, "progressText", progress);
            PrefabBuildUtil.SetRef(page, "statusText", status);
            PrefabBuildUtil.SetRef(page, "lastResultText", lastResult);
            PrefabBuildUtil.SetRef(page, "pilesButton", piles);
            PrefabBuildUtil.SetRef(page, "handContent", handContent);
            PrefabBuildUtil.SetRef(page, "handRowPrefab", ScreenLayout.LoadPart<SkillCardRowView>("SkillCardRow"));
            PrefabBuildUtil.SetRef(page, "skipButton", skip);

            PrefabBuildUtil.SavePrefab(page.gameObject, AssetPath);
            PrefabBuildUtil.RegisterScreen(AssetPath);
        }
    }
}
