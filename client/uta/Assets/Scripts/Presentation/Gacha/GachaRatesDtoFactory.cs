using System.Collections.Generic;
using Uta.Client.Application.Gacha;
using Uta.MasterData;

namespace Uta.Client.Presentation.Gacha
{
    /// <summary>排出率のモーダルの行を作る。状態を持たず、リポジトリに書かず、通信・遷移・View に触れない。</summary>
    public sealed class GachaRatesDtoFactory
    {
        private readonly MemoryDatabase memoryDatabase;
        private readonly GachaRateCalculator gachaRateCalculator;

        public GachaRatesDtoFactory(MemoryDatabase memoryDatabase, GachaRateCalculator gachaRateCalculator)
        {
            this.memoryDatabase = memoryDatabase;
            this.gachaRateCalculator = gachaRateCalculator;
        }

        /// <summary>通常枠の合計・内訳、確定枠の合計・内訳の順（見出しは isHeading = true）。</summary>
        public IReadOnlyList<GachaLineRowViewDto> Create(int gachaId)
        {
            var master = memoryDatabase.GachaTable.FindByGachaId(gachaId);
            var lines = new List<GachaLineRowViewDto>();
            AddFrame(lines, "通常枠（単発・10連の1〜9枠目）", "通常枠の内訳", gachaRateCalculator.Calculate(master.NormalTableId));
            AddFrame(lines, "確定枠（10連の10枠目）", "確定枠の内訳", gachaRateCalculator.Calculate(master.GuaranteedTableId));
            return lines;
        }

        private static void AddFrame(List<GachaLineRowViewDto> lines, string groupsHeading, string itemsHeading, GachaRateResult rates)
        {
            lines.Add(new GachaLineRowViewDto(groupsHeading, true));
            foreach (var group in rates.Groups)
            {
                lines.Add(new GachaLineRowViewDto(
                    group.Rarity + " " + GachaTexts.Kind(group.Kind) + " " + GachaTexts.Percent(group.BasisPoints),
                    false));
            }

            lines.Add(new GachaLineRowViewDto(itemsHeading, true));
            foreach (var item in rates.Items)
            {
                lines.Add(new GachaLineRowViewDto(
                    item.Rarity + " " + GachaTexts.Kind(item.Kind) + " " + item.Name + " " + GachaTexts.Percent(item.BasisPoints),
                    false));
            }
        }
    }
}
