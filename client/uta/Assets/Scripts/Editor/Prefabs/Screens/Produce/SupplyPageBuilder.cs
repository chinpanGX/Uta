using TMPro;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Produce;

namespace Uta.Client.Editor.Prefabs.Screens
{
    /// <summary>支給のページ（Screens/Produce/SupplyPage.prefab）を作る。Addressables の Screens に登録する。</summary>
    public sealed class SupplyPageBuilder : IPrefabBuilder
    {
        public int Order => 2000;

        public string AssetPath => PrefabBuildUtil.ScreenPath("Produce", "SupplyPage");

        public void Build()
        {
            var page = PrefabBuildUtil.CreatePage<SupplyPage>("SupplyPage", out var body);
            ScreenLayout.SetupBody(body);
            var header = PrefabBuildUtil.AddHeader(body, "支給");
            var main = ScreenLayout.AddMain(body);

            // 文言は固定（View のフィールドにしない）
            ScreenLayout.AddLabel(main, "MessageText", "スキルカードの選択肢から 1 枚を受け取れます。",
                new TextSpec(UiTheme.FontBody, UiTheme.Text, TextAlignmentOptions.Center, true, false), 200f);
            var receive = PrefabBuildUtil.AddButtonPart(main, "ReceiveButton", "受け取る", ButtonStyle.Primary);
            ScreenLayout.AddFlexibleSpacer(main, "Spacer");

            PrefabBuildUtil.SetRef(page, "header", header);
            PrefabBuildUtil.SetRef(page, "receiveButton", receive);

            PrefabBuildUtil.SavePrefab(page.gameObject, AssetPath);
            PrefabBuildUtil.RegisterScreen(AssetPath);
        }
    }
}
