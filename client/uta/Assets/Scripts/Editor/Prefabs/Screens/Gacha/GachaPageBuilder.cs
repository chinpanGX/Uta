using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Gacha;

namespace Uta.Client.Editor.Prefabs.Screens
{
    /// <summary>ガチャのページ（Screens/Gacha/GachaPage.prefab）を作る。Addressables の Screens に登録する。</summary>
    public sealed class GachaPageBuilder : IPrefabBuilder
    {
        public int Order => 2000;

        public string AssetPath => PrefabBuildUtil.ScreenPath("Gacha", "GachaPage");

        public void Build(PrefabBuildSession session)
        {
            var page = PrefabBuildUtil.CreatePage<GachaPage>(session, "GachaPage", out var body);
            ScreenLayout.SetupBody(body);
            var header = PrefabBuildUtil.AddHeader(body, "ガチャ");
            var main = ScreenLayout.AddMain(body);

            var currency = ScreenLayout.AddLabel(main, "CurrencyText", "ガチャ通貨 -", ScreenLayout.LineRight, 70f);
            var lineup = ScreenLayout.AddLabel(main, "LineupText", "SSR 育成アイドル: -", ScreenLayout.BlockCenter, 0f);
            PrefabBuildUtil.SetLayout(lineup.gameObject, minHeight: 70f, flexibleHeight: 1f);
            var exchange = ScreenLayout.AddLabel(main, "ExchangeText", "交換pt - / -", ScreenLayout.LineCenter, 70f);
            var rates = PrefabBuildUtil.AddButtonPart(main, "RatesButton", "排出率", ButtonStyle.Secondary);
            var drawRow = ScreenLayout.AddRow(main, "DrawRow", UiTheme.ButtonHeight);
            var single = PrefabBuildUtil.AddButtonPart(drawRow, "SingleButton", "単発");
            var ten = PrefabBuildUtil.AddButtonPart(drawRow, "TenButton", "10連");

            PrefabBuildUtil.SetRef(page, "header", header);
            PrefabBuildUtil.SetRef(page, "currencyText", currency);
            PrefabBuildUtil.SetRef(page, "lineupText", lineup);
            PrefabBuildUtil.SetRef(page, "exchangeText", exchange);
            PrefabBuildUtil.SetRef(page, "ratesButton", rates);
            PrefabBuildUtil.SetRef(page, "singleButton", single);
            PrefabBuildUtil.SetRef(page, "tenButton", ten);

            PrefabBuildUtil.SavePrefab(page.gameObject, AssetPath);
            PrefabBuildUtil.RegisterScreen(AssetPath);
        }
    }
}
