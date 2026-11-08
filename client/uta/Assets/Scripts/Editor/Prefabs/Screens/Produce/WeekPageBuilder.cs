using TMPro;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Common.Parts;
using Uta.Client.Presentation.Produce;

namespace Uta.Client.Editor.Prefabs.Screens
{
    /// <summary>週の行動選択のページ（Screens/Produce/WeekPage.prefab）を作る。Addressables の Screens に登録する。</summary>
    public sealed class WeekPageBuilder : IPrefabBuilder
    {
        public int Order => 2000;

        public string AssetPath => PrefabBuildUtil.ScreenPath("Produce", "WeekPage");

        public void Build()
        {
            var page = PrefabBuildUtil.CreatePage<WeekPage>("WeekPage", out var body);
            ScreenLayout.SetupBody(body);
            var header = ProduceLayout.AddHeaderWithoutBack(body, "週 1 / 16");
            var menu = ProduceLayout.AddMenuButton(header);
            var main = ScreenLayout.AddMain(body);

            var nextExam = ScreenLayout.AddLabel(main, "NextExamText", "",
                new TextSpec(UiTheme.FontSmall, UiTheme.TextMuted, TextAlignmentOptions.MidlineLeft, false, true), 50f);
            var statusPanel = PrefabBuildUtil.AddPanel(main, "StatusPanel");
            var status = ScreenLayout.AddLabel(statusPanel, "StatusText", "歌唱 0 / 0", ScreenLayout.Block, 300f);
            ScreenLayout.AddFlexibleSpacer(main, "Spacer");
            ScreenLayout.AddLabel(main, "ActionsTitle", "今週の行動", ScreenLayout.Line, 60f);
            var actionList = ProduceLayout.AddRuntimeList(main, "ActionList", 24f);

            PrefabBuildUtil.SetRef(page, "header", header);
            PrefabBuildUtil.SetRef(page, "menuButton", menu);
            PrefabBuildUtil.SetRef(page, "nextExamText", nextExam);
            PrefabBuildUtil.SetRef(page, "statusText", status);
            PrefabBuildUtil.SetRef(page, "actionList", actionList);
            PrefabBuildUtil.SetRef(page, "actionButtonPrefab", ScreenLayout.LoadPart<ButtonView>("Button"));

            PrefabBuildUtil.SavePrefab(page.gameObject, AssetPath);
            PrefabBuildUtil.RegisterScreen(AssetPath);
        }
    }
}
