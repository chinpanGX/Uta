using System;
using System.Collections.Generic;
using Uta.Client.Application.Owned;
using Uta.Client.Presentation.Common;
using Uta.Shared.Domain.Lesson;
using Uta.Shared.Domain.Produce;

namespace Uta.Client.Presentation.Owned
{
    /// <summary>所持一覧の表示の値と文字を作る。状態を持たず、リポジトリに書かず、通信・遷移・View に触れない。</summary>
    public sealed class OwnedDtoFactory
    {
        private readonly OwnedListFactory ownedListFactory;

        public OwnedDtoFactory(OwnedListFactory ownedListFactory)
        {
            this.ownedListFactory = ownedListFactory;
        }

        public OwnedViewDto Create(OwnedSelectionCondition condition, OwnedTabKind tab, OwnedSortKind sort, bool descending, Plan? planFilter, SupportType? typeFilter)
        {
            var filtersVisible = condition == null || condition.Kind == OwnedSelectionKind.SupportCard;
            var tabsVisible = condition == null;
            bool controlsVisible;
            if (condition == null)
            {
                controlsVisible = tab == OwnedTabKind.SupportCards;
            }
            else
            {
                controlsVisible = condition.Kind != OwnedSelectionKind.Idol;
            }

            var rows = new List<OwnedRowViewDto>();
            if (tab == OwnedTabKind.Idols)
            {
                foreach (var item in ownedListFactory.CreateIdols(condition))
                {
                    rows.Add(CreateRow(item));
                }
            }
            else
            {
                var items = ownedListFactory.CreateSupportCards(sort, descending, filtersVisible ? planFilter : null, filtersVisible ? typeFilter : null, condition);
                foreach (var item in items)
                {
                    rows.Add(CreateRow(item));
                }
            }

            return new OwnedViewDto(CreateTitle(condition), tabsVisible, controlsVisible, CreateSortLabel(sort), descending ? "降順" : "昇順",
                planFilter.HasValue ? "プラン: " + DisplayText.PlanName(planFilter.Value) : "プラン: 全て",
                typeFilter.HasValue ? "タイプ: " + DisplayText.SupportTypeName(typeFilter.Value) : "タイプ: 全て",
                filtersVisible, rows);
        }

        private static string CreateTitle(OwnedSelectionCondition condition)
        {
            if (condition == null)
            {
                return "所持一覧";
            }

            switch (condition.Kind)
            {
                case OwnedSelectionKind.Idol: return "育成アイドルを選ぶ";
                case OwnedSelectionKind.SupportCard: return "サポートカードを選ぶ";
                case OwnedSelectionKind.LimitBreakMaterial: return "素材にするカードを選ぶ";
                default: throw new ArgumentOutOfRangeException(nameof(condition), "unsupported kind: " + condition.Kind);
            }
        }

        private static string CreateSortLabel(OwnedSortKind sort)
        {
            switch (sort)
            {
                case OwnedSortKind.Rarity: return "並び: レア度";
                case OwnedSortKind.Level: return "並び: Lv";
                case OwnedSortKind.Acquired: return "並び: 入手順";
                default: throw new ArgumentOutOfRangeException(nameof(sort), "unsupported sort: " + sort);
            }
        }

        private static OwnedRowViewDto CreateRow(OwnedListItem item)
        {
            var title = item.Name + " [" + DisplayText.RarityName(item.Rarity) + "]";
            string detail;
            if (item.IsIdol)
            {
                detail = DisplayText.PlanName(item.Plan) + "　かけら " + item.Shards;
            }
            else
            {
                detail = DisplayText.SupportTypeName(item.SupportType) + "　" + DisplayText.PlanName(item.Plan) + "　Lv " + item.Level + "　解放 " + item.LimitBreak;
            }

            switch (item.DisabledKind)
            {
                case OwnedDisabledKind.AlreadyInFormation: detail += "　（編成済み）"; break;
                case OwnedDisabledKind.SameCardInFormation: detail += "　（同じカードは編成できない）"; break;
                case OwnedDisabledKind.PlanMismatch: detail += "　（プランが合わない）"; break;
            }

            return new OwnedRowViewDto(item.IdolId, item.SupportCardUid, title, detail, item.Enabled);
        }
    }
}
