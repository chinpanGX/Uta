using System;
using System.Collections.Generic;
using System.Linq;
using Uta.MasterData;

namespace Uta.Client.Application.Gacha
{
    /// <summary>排出率の計算。外部に触れない具象（ルートのスコープにシングルトン）。</summary>
    public sealed class GachaRateCalculator
    {
        private readonly MemoryDatabase memoryDatabase;

        public GachaRateCalculator(MemoryDatabase memoryDatabase)
        {
            this.memoryDatabase = memoryDatabase;
        }

        /// <summary>gachaTableId の表の排出率を計算する。重みを合計してから切り捨てる（gacha.md 3.1）。</summary>
        public GachaRateResult Calculate(int gachaTableId)
        {
            var entries = memoryDatabase.GachaTableEntryTable.All
                .Where(e => e.GachaTableId == gachaTableId)
                .OrderBy(e => e.Order)
                .ToList();
            long total = 0;
            foreach (var e in entries)
            {
                total += e.Weight;
            }

            if (entries.Count == 0 || total == 0)
            {
                return new GachaRateResult(Array.Empty<GachaRateGroup>(), Array.Empty<GachaItemRate>());
            }

            var items = new List<GachaItemRate>();
            var groupWeights = new Dictionary<(Rarity Rarity, ItemKind Kind), long>();
            foreach (var e in entries)
            {
                int itemId;
                string name;
                Rarity rarity;
                if (e.ItemKind == ItemKind.Idol)
                {
                    itemId = e.IdolId;
                    var idol = memoryDatabase.IdolTable.FindByIdolId(itemId);
                    rarity = idol.Rarity;
                    name = idol.Name;
                }
                else if (e.ItemKind == ItemKind.SupportCard)
                {
                    itemId = e.SupportCardId;
                    var card = memoryDatabase.SupportCardTable.FindBySupportCardId(itemId);
                    rarity = card.Rarity;
                    name = card.Name;
                }
                else
                {
                    throw new ArgumentException("unsupported ItemKind: " + e.ItemKind);
                }

                items.Add(new GachaItemRate(rarity, e.ItemKind, itemId, name, (int)(e.Weight * 10000L / total)));
                var key = (rarity, e.ItemKind);
                groupWeights.TryGetValue(key, out var sum);
                groupWeights[key] = sum + e.Weight;
            }

            var groups = groupWeights
                .OrderByDescending(p => (int)p.Key.Rarity)
                .ThenBy(p => (int)p.Key.Kind)
                .Select(p => new GachaRateGroup(p.Key.Rarity, p.Key.Kind, (int)(p.Value * 10000L / total)))
                .ToList();
            var sortedItems = items
                .OrderByDescending(i => (int)i.Rarity)
                .ThenBy(i => (int)i.Kind)
                .ThenBy(i => i.ItemId)
                .ToList();
            return new GachaRateResult(groups, sortedItems);
        }
    }
}
