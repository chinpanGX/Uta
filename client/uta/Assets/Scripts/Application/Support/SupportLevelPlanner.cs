using System;
using System.Linq;
using Uta.Client.Domain.SupportCard;
using ZLinq;

namespace Uta.Client.Application.Support
{
    /// <summary>サポートカードの最大レベル・上限解放の最大・強化の費用・目標レベル（外部に触れない具象）。</summary>
    public sealed class SupportLevelPlanner
    {
        private readonly Uta.MasterData.MemoryDatabase memoryDatabase;

        public SupportLevelPlanner(Uta.MasterData.MemoryDatabase memoryDatabase)
        {
            this.memoryDatabase = memoryDatabase;
        }

        /// <summary>カードのレア度と limitBreak の SupportLevelCap 行の MaxLevel。行が無ければ InvalidOperationException。</summary>
        public int GetMaxLevel(int supportCardId, int limitBreak)
        {
            var rarity = GetRarity(supportCardId);
            var rows = memoryDatabase.SupportLevelCapTable.All
                .Where(r => r.Rarity == rarity && r.LimitBreak == limitBreak)
                .ToArray();
            if (rows.Length == 0)
            {
                throw new InvalidOperationException("SupportLevelCap not found: " + rarity + "," + limitBreak);
            }

            return rows[0].MaxLevel;
        }

        /// <summary>カードのレア度の SupportLevelCap 行の LimitBreak の最大。行が無ければ InvalidOperationException。</summary>
        public int GetMaxLimitBreak(int supportCardId)
        {
            var rarity = GetRarity(supportCardId);
            var rows = memoryDatabase.SupportLevelCapTable.All
                .Where(r => r.Rarity == rarity)
                .ToArray();
            if (rows.Length == 0)
            {
                throw new InvalidOperationException("SupportLevelCap not found: " + rarity);
            }

            var max = rows[0].LimitBreak;
            foreach (var row in rows)
            {
                if (row.LimitBreak > max)
                {
                    max = row.LimitBreak;
                }
            }

            return max;
        }

        /// <summary>fromLevel から toLevel - 1 までの SupportLevelCost 行の合計。toLevel が fromLevel 以下なら 0・0。行が無ければ InvalidOperationException。</summary>
        public LevelUpCost GetLevelUpCost(int supportCardId, int fromLevel, int toLevel)
        {
            var rarity = GetRarity(supportCardId);
            var supportPoints = 0;
            var gold = 0;
            for (var level = fromLevel; level < toLevel; level++)
            {
                var current = level;
                var rows = memoryDatabase.SupportLevelCostTable.All
                    .Where(r => r.Rarity == rarity && r.Level == current)
                    .ToArray();
                if (rows.Length == 0)
                {
                    throw new InvalidOperationException("SupportLevelCost not found: " + rarity + "," + current);
                }

                supportPoints += rows[0].SupportPoints;
                gold += rows[0].Gold;
            }

            return new LevelUpCost(supportPoints, gold);
        }

        /// <summary>
        /// 目標レベル。card.Level が最大以上なら card.Level。Plus1・Plus5 は最大で切った +1／+5（所持が足りなくても返す）。
        /// Max は最大から card.Level + 1 まで下げ、所持で払える最初のレベル。無ければ card.Level。
        /// </summary>
        public int PlanTarget(SupportCardEntity card, int supportPoints, int gold, LevelUpStepKind step)
        {
            var maxLevel = GetMaxLevel(card.MasterId, card.LimitBreak);
            if (card.Level >= maxLevel)
            {
                return card.Level;
            }

            switch (step)
            {
                case LevelUpStepKind.Plus1:
                    return Math.Min(card.Level + 1, maxLevel);
                case LevelUpStepKind.Plus5:
                    return Math.Min(card.Level + 5, maxLevel);
                default:
                    for (var target = maxLevel; target > card.Level; target--)
                    {
                        var cost = GetLevelUpCost(card.MasterId, card.Level, target);
                        if (cost.SupportPoints <= supportPoints && cost.Gold <= gold)
                        {
                            return target;
                        }
                    }

                    return card.Level;
            }
        }

        private Uta.MasterData.Rarity GetRarity(int supportCardId)
        {
            return memoryDatabase.SupportCardTable.FindBySupportCardId(supportCardId).Rarity;
        }
    }
}
