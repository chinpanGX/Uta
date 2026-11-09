using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Gacha;

namespace Uta.Client.Editor.Prefabs.Screens
{
    /// <summary>ガチャの結果のモーダル（Screens/Gacha/GachaResultModal.prefab）を作る。Addressables の Screens に登録する。</summary>
    public sealed class GachaResultModalBuilder : IPrefabBuilder
    {
        public int Order => 2000;

        public string AssetPath => PrefabBuildUtil.ScreenPath("Gacha", "GachaResultModal");

        public void Build(PrefabBuildSession session)
        {
            var modal = PrefabBuildUtil.CreateModal<GachaResultModal>(session, "GachaResultModal", out var panel);

            ScreenLayout.AddLabel(panel, "TitleText", "結果", ScreenLayout.Title, 90f);
            var content = PrefabBuildUtil.AddScrollList(panel, "List", out var scroll);
            PrefabBuildUtil.SetLayout(scroll.gameObject, minHeight: 1100f, preferredHeight: 1100f);
            var buttonRow = ScreenLayout.AddRow(panel, "ButtonRow", UiTheme.ButtonHeight);
            var retry = PrefabBuildUtil.AddButtonPart(buttonRow, "RetryButton", "もう一度");
            var close = PrefabBuildUtil.AddButtonPart(buttonRow, "CloseButton", "閉じる", ButtonStyle.Secondary);

            PrefabBuildUtil.SetRef(modal, "listContent", content);
            PrefabBuildUtil.SetRef(modal, "rowPrefab", ScreenLayout.LoadPart<GachaLineRowView>("GachaResultRow"));
            PrefabBuildUtil.SetRef(modal, "retryButton", retry);
            PrefabBuildUtil.SetRef(modal, "closeButton", close);

            PrefabBuildUtil.SavePrefab(modal.gameObject, AssetPath);
            PrefabBuildUtil.RegisterScreen(AssetPath);
        }
    }
}
