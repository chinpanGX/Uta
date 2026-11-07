using UnityEngine.UI;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Home;

namespace Uta.Client.Editor.Prefabs.Screens
{
    /// <summary>ホームのページ（Screens/Home/HomePage.prefab）を作る。Addressables の Screens に登録する。</summary>
    public sealed class HomePageBuilder : IPrefabBuilder
    {
        public int Order => 2000;

        public string AssetPath => PrefabBuildUtil.ScreenPath("Home", "HomePage");

        public void Build()
        {
            var page = PrefabBuildUtil.CreatePage<HomePage>("HomePage", out var body);
            ScreenLayout.SetupBody(body);
            var main = ScreenLayout.AddMain(body);

            var profile = ScreenLayout.AddLabel(main, "ProfileText", "プレイヤーLv -　ガチャ通貨 -", ScreenLayout.Line, 80f);

            var stage = PrefabBuildUtil.AddChild(main, "Stage");
            var stageImage = stage.gameObject.AddComponent<RawImage>();
            stageImage.texture = null;
            stageImage.color = UiTheme.Panel;
            stageImage.raycastTarget = false;
            PrefabBuildUtil.MarkModified(stageImage);
            PrefabBuildUtil.SetLayout(stage.gameObject, minHeight: 600f, flexibleHeight: 1f);
            var stageLabel = ScreenLayout.AddLabel(stage, "StageLabel", "キャラ表示", ScreenLayout.Title, 0f);

            var resumePanel = PrefabBuildUtil.AddPanel(main, "ResumePanel");
            PrefabBuildUtil.SetLayout(resumePanel.gameObject, minHeight: 134f, preferredHeight: 134f);
            ScreenLayout.AddLabel(resumePanel, "ResumeText", "プロデュース中", ScreenLayout.LineCenter, 70f);

            var primary = PrefabBuildUtil.AddButtonPart(main, "PrimaryButton", "プロデュース");
            ScreenLayout.SetButtonLabel(primary, "プロデュース", ScreenLayout.Title);
            PrefabBuildUtil.SetLayout(primary.gameObject, minHeight: 200f, preferredHeight: 200f);

            var nav = ScreenLayout.AddRow(main, "Nav", UiTheme.ButtonHeight);
            var navProduce = PrefabBuildUtil.AddButtonPart(nav, "NavProduceButton", "プロデュース", ButtonStyle.Secondary);
            var navGacha = PrefabBuildUtil.AddButtonPart(nav, "NavGachaButton", "ガチャ", ButtonStyle.Secondary);
            var navOwned = PrefabBuildUtil.AddButtonPart(nav, "NavOwnedButton", "アイドル", ButtonStyle.Secondary);

            PrefabBuildUtil.SetRef(page, "profileText", profile);
            PrefabBuildUtil.SetRef(page, "stageImage", stageImage);
            PrefabBuildUtil.SetRef(page, "stageLabel", stageLabel);
            PrefabBuildUtil.SetRef(page, "resumePanel", resumePanel.gameObject);
            PrefabBuildUtil.SetRef(page, "primaryButton", primary);
            PrefabBuildUtil.SetRef(page, "navProduceButton", navProduce);
            PrefabBuildUtil.SetRef(page, "navGachaButton", navGacha);
            PrefabBuildUtil.SetRef(page, "navOwnedButton", navOwned);

            PrefabBuildUtil.SavePrefab(page.gameObject, AssetPath);
            PrefabBuildUtil.RegisterScreen(AssetPath);
        }
    }
}
