using Uta.Client.Presentation.Gacha;

namespace Uta.Client.Editor.Prefabs.Screens
{
    /// <summary>排出率のモーダル（Screens/Gacha/GachaRatesModal.prefab）を作る。Addressables の Screens に登録する。</summary>
    public sealed class GachaRatesModalBuilder : IPrefabBuilder
    {
        public int Order => 2000;

        public string AssetPath => PrefabBuildUtil.ScreenPath("Gacha", "GachaRatesModal");

        public void Build(PrefabBuildSession session)
        {
            var modal = PrefabBuildUtil.CreateModal<GachaRatesModal>(session, "GachaRatesModal", out var panel);

            ScreenLayout.AddLabel(panel, "TitleText", "排出率", ScreenLayout.Title, 90f);
            var content = PrefabBuildUtil.AddScrollList(panel, "List", out var scroll);
            PrefabBuildUtil.SetLayout(scroll.gameObject, minHeight: 1200f, preferredHeight: 1200f);
            var close = PrefabBuildUtil.AddButtonPart(panel, "CloseButton", "閉じる", ButtonStyle.Secondary);

            PrefabBuildUtil.SetRef(modal, "listContent", content);
            PrefabBuildUtil.SetRef(modal, "headingRowPrefab", ScreenLayout.LoadPart<GachaLineRowView>("GachaRateHeadingRow"));
            PrefabBuildUtil.SetRef(modal, "lineRowPrefab", ScreenLayout.LoadPart<GachaLineRowView>("GachaRateRow"));
            PrefabBuildUtil.SetRef(modal, "closeButton", close);

            PrefabBuildUtil.SavePrefab(modal.gameObject, AssetPath);
            PrefabBuildUtil.RegisterScreen(AssetPath);
        }
    }
}
