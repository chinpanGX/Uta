using Uta.Client.Presentation.Owned;

namespace Uta.Client.Editor.Prefabs.Screens
{
    /// <summary>サポートカードの詳細のページ（Screens/Owned/SupportDetailPage.prefab）を作る。Addressables の Screens に登録する。</summary>
    public sealed class SupportDetailPageBuilder : IPrefabBuilder
    {
        public int Order => 2000;

        public string AssetPath => PrefabBuildUtil.ScreenPath("Owned", "SupportDetailPage");

        public void Build()
        {
            var page = PrefabBuildUtil.CreatePage<SupportDetailPage>("SupportDetailPage", out var body);
            ScreenLayout.SetupBody(body);
            var header = PrefabBuildUtil.AddHeader(body, "サポートカード");
            var main = ScreenLayout.AddMain(body);

            var info = ScreenLayout.AddLabel(main, "InfoText", "名前\nSSR　歌唱　共通\nLv 1 / 20\n上限解放 0 / 4", ScreenLayout.Block, 240f);
            var resources = ScreenLayout.AddLabel(main, "ResourcesText", "強化pt -　ゴールド -", ScreenLayout.Line, 70f);

            var plus1 = PrefabBuildUtil.AddButtonPart(main, "Plus1Button", "+1");
            ScreenLayout.SetButtonLabel(plus1, "+1", ScreenLayout.SmallButton);
            var plus5 = PrefabBuildUtil.AddButtonPart(main, "Plus5Button", "+5");
            ScreenLayout.SetButtonLabel(plus5, "+5", ScreenLayout.SmallButton);
            var max = PrefabBuildUtil.AddButtonPart(main, "MaxButton", "最大");
            ScreenLayout.SetButtonLabel(max, "最大", ScreenLayout.SmallButton);
            var limitBreak = PrefabBuildUtil.AddButtonPart(main, "LimitBreakButton", "上限解放（素材を選ぶ）");

            ScreenLayout.AddLabel(main, "AbilityHeaderText", "アビリティ", ScreenLayout.Heading, 70f);
            var abilities = ScreenLayout.AddLabel(main, "AbilitiesText", "1 初期歌唱 +0", ScreenLayout.SmallBlock, 0f);
            PrefabBuildUtil.SetLayout(abilities.gameObject, minHeight: 100f, flexibleHeight: 1f);

            PrefabBuildUtil.SetRef(page, "header", header);
            PrefabBuildUtil.SetRef(page, "infoText", info);
            PrefabBuildUtil.SetRef(page, "resourcesText", resources);
            PrefabBuildUtil.SetRef(page, "plus1Button", plus1);
            PrefabBuildUtil.SetRef(page, "plus5Button", plus5);
            PrefabBuildUtil.SetRef(page, "maxButton", max);
            PrefabBuildUtil.SetRef(page, "limitBreakButton", limitBreak);
            PrefabBuildUtil.SetRef(page, "abilitiesText", abilities);

            PrefabBuildUtil.SavePrefab(page.gameObject, AssetPath);
            PrefabBuildUtil.RegisterScreen(AssetPath);
        }
    }
}
