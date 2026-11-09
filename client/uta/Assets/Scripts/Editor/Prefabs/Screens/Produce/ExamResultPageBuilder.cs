using TMPro;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Produce;

namespace Uta.Client.Editor.Prefabs.Screens
{
    /// <summary>試験結果のページ（Screens/Produce/ExamResultPage.prefab）を作る。Addressables の Screens に登録する。</summary>
    public sealed class ExamResultPageBuilder : IPrefabBuilder
    {
        public int Order => 2000;

        public string AssetPath => PrefabBuildUtil.ScreenPath("Produce", "ExamResultPage");

        public void Build(PrefabBuildSession session)
        {
            var page = PrefabBuildUtil.CreatePage<ExamResultPage>(session, "ExamResultPage", out var body);
            ScreenLayout.SetupBody(body);
            var main = ScreenLayout.AddMain(body);

            var title = ScreenLayout.AddLabel(main, "TitleText", "試験結果", ScreenLayout.Title, 100f);
            var rankingContent = PrefabBuildUtil.AddScrollList(main, "RankingScroll", out _);
            var outcome = ScreenLayout.AddLabel(main, "OutcomeText", "",
                new TextSpec(UiTheme.FontTitle, UiTheme.Accent, TextAlignmentOptions.Center, false, true), 100f);
            var retry = PrefabBuildUtil.AddButtonPart(main, "RetryButton", "再挑戦", ButtonStyle.Secondary);
            var next = PrefabBuildUtil.AddButtonPart(main, "NextButton", "次へ", ButtonStyle.Primary);

            PrefabBuildUtil.SetRef(page, "titleText", title);
            PrefabBuildUtil.SetRef(page, "rankingContent", rankingContent);
            PrefabBuildUtil.SetRef(page, "rankingRowPrefab", ScreenLayout.LoadPart<ExamRankingRowView>("ExamRankingRow"));
            PrefabBuildUtil.SetRef(page, "outcomeText", outcome);
            PrefabBuildUtil.SetRef(page, "retryButton", retry);
            PrefabBuildUtil.SetRef(page, "nextButton", next);

            PrefabBuildUtil.SavePrefab(page.gameObject, AssetPath);
            PrefabBuildUtil.RegisterScreen(AssetPath);
        }
    }
}
