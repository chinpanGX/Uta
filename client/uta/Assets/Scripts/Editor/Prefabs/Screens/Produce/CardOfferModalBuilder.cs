using TMPro;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Produce;

namespace Uta.Client.Editor.Prefabs.Screens
{
    /// <summary>スキルカード獲得のモーダル（Screens/Produce/CardOfferModal.prefab）を作る。Addressables の Screens に登録する。</summary>
    public sealed class CardOfferModalBuilder : IPrefabBuilder
    {
        public int Order => 2000;

        public string AssetPath => PrefabBuildUtil.ScreenPath("Produce", "CardOfferModal");

        public void Build(PrefabBuildSession session)
        {
            var modal = PrefabBuildUtil.CreateModal<CardOfferModal>(session, "CardOfferModal", out var panel);

            // 固定の文言（View のフィールドにしない）
            ScreenLayout.AddLabel(panel, "TitleText", "スキルカードを1枚獲得", ScreenLayout.Title, 80f);
            var cardList = ProduceLayout.AddRuntimeList(panel, "CardList", 24f);
            var deckCount = ScreenLayout.AddLabel(panel, "DeckCountText", "デッキ 0 枚",
                new TextSpec(UiTheme.FontSmall, UiTheme.TextMuted, TextAlignmentOptions.MidlineLeft, false, true), 50f);
            var decline = PrefabBuildUtil.AddButtonPart(panel, "DeclineButton", "受け取らない", ButtonStyle.Secondary);

            PrefabBuildUtil.SetRef(modal, "cardList", cardList);
            PrefabBuildUtil.SetRef(modal, "cardRowPrefab", ScreenLayout.LoadPart<SkillCardRowView>("SkillCardRow"));
            PrefabBuildUtil.SetRef(modal, "deckCountText", deckCount);
            PrefabBuildUtil.SetRef(modal, "declineButton", decline);

            PrefabBuildUtil.SavePrefab(modal.gameObject, AssetPath);
            PrefabBuildUtil.RegisterScreen(AssetPath);
        }
    }
}
