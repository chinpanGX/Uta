using System.Collections.Generic;
using Uta.Shared.Domain.Lesson;
using Uta.Shared.Domain.Rng;

namespace Uta.Shared.Domain.Produce
{
    /// <summary>スキルカードの選択肢の候補と抽選（produce.md 3.5）。</summary>
    public static class OfferPool
    {
        private static readonly Rarity[] RarityOrder = { Rarity.N, Rarity.R, Rarity.SR, Rarity.SSR };

        /// <summary>
        /// 候補。cards.CardIds の昇順で、(Plan が idol.Plan か Free) かつ (UnlockLv ≤ playerLevel)
        /// かつ (Type が Trouble でない) かつ (Unique でない、または deckCardIds に含まれない) のカード ID。
        /// </summary>
        public static IReadOnlyList<int> GetCandidates(ProduceCatalog catalog, IdolDefinition idol, int playerLevel, IEnumerable<int> deckCardIds)
        {
            var deck = new HashSet<int>(deckCardIds);
            var result = new List<int>();
            var ids = catalog.Cards.CardIds;
            for (var i = 0; i < ids.Count; i++)
            {
                var card = catalog.Cards.Get(ids[i]);
                if (card.Plan != idol.Plan && card.Plan != Plan.Free)
                {
                    continue;
                }

                if (card.UnlockLv > playerLevel || card.Type == CardType.Trouble)
                {
                    continue;
                }

                if (card.Unique && deck.Contains(card.Id))
                {
                    continue;
                }

                result.Add(card.Id);
            }

            return result;
        }

        /// <summary>枠の順に最大 ProduceRules.OfferSlotCount 枚を引く（produce-domain.md 4章の Offer）。</summary>
        public static IReadOnlyList<int> Draw(ProduceCatalog catalog, IReadOnlyList<int> candidates, IRandom random)
        {
            var picked = new List<int>();
            var pickedSet = new HashSet<int>();
            for (var slot = 0; slot < ProduceRules.OfferSlotCount; slot++)
            {
                var available = new List<int>();
                for (var i = 0; i < candidates.Count; i++)
                {
                    if (!pickedSet.Contains(candidates[i]))
                    {
                        available.Add(candidates[i]);
                    }
                }

                var rarities = new List<Rarity>();
                var weights = new List<int>();
                for (var r = 0; r < RarityOrder.Length; r++)
                {
                    if (HasRarity(catalog, available, RarityOrder[r]))
                    {
                        rarities.Add(RarityOrder[r]);
                        weights.Add(catalog.GetOfferWeight(RarityOrder[r]));
                    }
                }

                if (rarities.Count == 0)
                {
                    break;
                }

                var rarity = rarities[RandomOps.WeightedIndex(random, weights)];
                var pool = new List<int>();
                for (var i = 0; i < available.Count; i++)
                {
                    if (catalog.Cards.Get(available[i]).Rarity == rarity)
                    {
                        pool.Add(available[i]);
                    }
                }

                var card = pool[RandomOps.NextInt(random, pool.Count)];
                picked.Add(card);
                pickedSet.Add(card);
            }

            return picked;
        }

        private static bool HasRarity(ProduceCatalog catalog, List<int> ids, Rarity rarity)
        {
            for (var i = 0; i < ids.Count; i++)
            {
                if (catalog.Cards.Get(ids[i]).Rarity == rarity)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
