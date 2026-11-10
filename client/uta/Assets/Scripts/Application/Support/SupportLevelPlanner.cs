using System;
using System.Collections.Generic;
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
            var costs = GetCostsByLevel(GetRarity(supportCardId));
            var supportPoints = 0;
            var gold = 0;
            for (var level = fromLevel; level < toLevel; level++)
            {
                if (!costs.TryGetValue(level, out var cost))
                {
                    throw new InvalidOperationException("SupportLevelCost not found: " + GetRarity(supportCardId) + "," + level);
                }

                supportPoints += cost.SupportPoints;
                gold += cost.Gold;
            }

            return new LevelUpCost(supportPoints, gold);
        }

        /// <summary>
        /// 目標レベル。card.Level が最大以上なら card.Level。Plus1・Plus5 は最大で切った +1／+5（所持が足りなくても返す）。
        /// Max は card.Level から 1 段ずつ費用を足し、所持（強化pt・ゴールド）に収まるあいだ上げた最後のレベル。1 段も払えなければ card.Level。
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
                    var costs = GetCostsByLevel(GetRarity(card.MasterId));
                    var target = card.Level;
                    var totalSupportPoints = 0;
                    var totalGold = 0;
                    while (target < maxLevel)
                    {
                        if (!costs.TryGetValue(target, out var cost))
                        {
                            throw new InvalidOperationException("SupportLevelCost not found: " + GetRarity(card.MasterId) + "," + target);
                        }

                        totalSupportPoints += cost.SupportPoints;
                        totalGold += cost.Gold;
                        if (totalSupportPoints > supportPoints || totalGold > gold)
                        {
                            break;
                        }

                        target++;
                    }

                    return target;
            }
        }

        /// <summary>レア度の SupportLevelCost 行を Level をキーにして引く。同じ Level が複数あれば先頭の行。</summary>
        private Dictionary<int, LevelUpCost> GetCostsByLevel(Uta.MasterData.Rarity rarity)
        {
            var costs = new Dictionary<int, LevelUpCost>();
            var rows = memoryDatabase.SupportLevelCostTable.All
                .Where(r => r.Rarity == rarity)
                .ToArray();
            foreach (var row in rows)
            {
                if (!costs.ContainsKey(row.Level))
                {
                    costs.Add(row.Level, new LevelUpCost(row.SupportPoints, row.Gold));
                }
            }

            return costs;
        }

        private Uta.MasterData.Rarity GetRarity(int supportCardId)
        {
            return memoryDatabase.SupportCardTable.FindBySupportCardId(supportCardId).Rarity;
        }
    }
}
