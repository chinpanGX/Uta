using System;
using System.Collections.Generic;
using System.Linq;

namespace Uta.Shared.Domain.Lesson
{
    /// <summary>
    /// マスタの行から組み立てた、全カードの定義。グループ ID の解決と入れ子の組み立てはここで行い、
    /// クライアントとサーバーで食い違わないようにする（repository-layout.md 4章）。
    /// </summary>
    public sealed class CardCatalog : ICardCatalog
    {
        // 効果のグループをたどる深さの上限（面から呼ぶ最初のグループを 1 と数え、これに入ろうとしたらエラー）
        private const int MaxEffectDepth = 5;

        private readonly Dictionary<int, CardDefinition> cards;

        private CardCatalog(Dictionary<int, CardDefinition> cards)
        {
            this.cards = cards;
            CardIds = cards.Keys.OrderBy(x => x).ToArray();
        }

        /// <summary>昇順</summary>
        public IReadOnlyList<int> CardIds { get; }

        /// <summary>行から全カードを組み立てる。問題があれば InvalidOperationException（メッセージに問題を全部入れる）。</summary>
        public static CardCatalog Create(
            IReadOnlyList<CardRow> cards,
            IReadOnlyList<CardFaceRow> faces,
            IReadOnlyList<CardCostRow> costs,
            IReadOnlyList<EffectRow> effects,
            IReadOnlyList<ConditionRow> conditions)
        {
            if (cards == null)
            {
                throw new ArgumentNullException(nameof(cards));
            }

            if (faces == null)
            {
                throw new ArgumentNullException(nameof(faces));
            }

            if (costs == null)
            {
                throw new ArgumentNullException(nameof(costs));
            }

            if (effects == null)
            {
                throw new ArgumentNullException(nameof(effects));
            }

            if (conditions == null)
            {
                throw new ArgumentNullException(nameof(conditions));
            }

            var builder = new Builder(faces, costs, effects, conditions);
            var result = new Dictionary<int, CardDefinition>();
            var seen = new HashSet<int>();
            foreach (var row in cards)
            {
                if (!seen.Add(row.CardId))
                {
                    builder.AddError("カード " + row.CardId + " が重複");
                    continue;
                }

                var card = builder.BuildCard(row);
                if (card != null)
                {
                    result.Add(row.CardId, card);
                }
            }

            // 重複したカードは作らない
            foreach (var error in builder.DuplicatedCardIds(cards))
            {
                result.Remove(error);
            }

            foreach (var card in result.Values.OrderBy(x => x.Id))
            {
                foreach (var error in CardDefinitionValidator.Validate(card))
                {
                    builder.AddError(error);
                }
            }

            if (builder.Errors.Count > 0)
            {
                throw new InvalidOperationException(string.Join("\n", builder.Errors));
            }

            return new CardCatalog(result);
        }

        /// <summary>無ければ KeyNotFoundException</summary>
        public CardDefinition Get(int cardId)
        {
            if (!cards.TryGetValue(cardId, out var card))
            {
                throw new KeyNotFoundException("カード " + cardId + " がない");
            }

            return card;
        }

        private sealed class Builder
        {
            private readonly Dictionary<int, List<CardFaceRow>> facesByCard = new Dictionary<int, List<CardFaceRow>>();
            private readonly Dictionary<long, List<CardCostRow>> costsByFace = new Dictionary<long, List<CardCostRow>>();
            private readonly Dictionary<int, List<EffectRow>> effectsByGroup = new Dictionary<int, List<EffectRow>>();
            private readonly Dictionary<int, List<ConditionRow>> conditionsByGroup = new Dictionary<int, List<ConditionRow>>();

            public Builder(
                IReadOnlyList<CardFaceRow> faces,
                IReadOnlyList<CardCostRow> costs,
                IReadOnlyList<EffectRow> effects,
                IReadOnlyList<ConditionRow> conditions)
            {
                foreach (var row in faces)
                {
                    Add(facesByCard, row.CardId, row);
                }

                foreach (var row in costs)
                {
                    Add(costsByFace, FaceKey(row.CardId, row.Face), row);
                }

                foreach (var row in effects)
                {
                    Add(effectsByGroup, row.EffectGroupId, row);
                }

                foreach (var row in conditions)
                {
                    Add(conditionsByGroup, row.ConditionGroupId, row);
                }
            }

            public List<string> Errors { get; } = new List<string>();

            public void AddError(string message)
            {
                if (!Errors.Contains(message))
                {
                    Errors.Add(message);
                }
            }

            public IEnumerable<int> DuplicatedCardIds(IReadOnlyList<CardRow> cards)
            {
                return cards.GroupBy(x => x.CardId).Where(x => x.Count() > 1).Select(x => x.Key).ToArray();
            }

            /// <summary>作れなければ（エラーを足して）null。</summary>
            public CardDefinition BuildCard(CardRow row)
            {
                var normal = FindFace(row.CardId, CardFaceKind.Normal);
                var enhanced = FindFace(row.CardId, CardFaceKind.Enhanced);
                if (normal == null)
                {
                    AddError("カード " + row.CardId + ": 通常版の面がない");
                }

                var baseFace = normal == null ? null : BuildFace(normal.Value);
                var enhancedFace = enhanced == null ? null : BuildFace(enhanced.Value);

                // 面が作れなかったか（エラー文言は重複排除されるので、件数の増減では判定しない）
                if (baseFace == null || (enhanced != null && enhancedFace == null))
                {
                    return null;
                }

                return new CardDefinition(
                    row.CardId,
                    row.Plan,
                    row.Type,
                    row.Rarity,
                    row.UnlockLv,
                    row.OnceInLesson,
                    row.Unique,
                    row.StartInHand,
                    baseFace,
                    enhancedFace);
            }

            private static void Add<TKey, TRow>(Dictionary<TKey, List<TRow>> map, TKey key, TRow row)
            {
                if (!map.TryGetValue(key, out var list))
                {
                    list = new List<TRow>();
                    map.Add(key, list);
                }

                list.Add(row);
            }

            private static long FaceKey(int cardId, CardFaceKind face)
            {
                return ((long)cardId << 8) | (long)(int)face;
            }

            private CardFaceRow? FindFace(int cardId, CardFaceKind kind)
            {
                if (!facesByCard.TryGetValue(cardId, out var list))
                {
                    return null;
                }

                var matched = list.Where(x => x.Face == kind).ToList();
                if (matched.Count > 1)
                {
                    AddError("カード " + cardId + ": " + kind + " の面が重複");
                }

                return matched.Count == 0 ? (CardFaceRow?)null : matched[0];
            }

            private CardFace BuildFace(CardFaceRow row)
            {
                var costs = new List<CostDef>();
                if (costsByFace.TryGetValue(FaceKey(row.CardId, row.Face), out var costRows))
                {
                    foreach (var cost in costRows.OrderBy(x => x.Order))
                    {
                        costs.Add(new CostDef(cost.Kind, cost.Value));
                    }
                }

                IReadOnlyList<ConditionDef> useConditions = new ConditionDef[0];
                if (row.UseConditionGroupId != 0)
                {
                    var built = BuildConditions(row.UseConditionGroupId);
                    if (built == null)
                    {
                        return null;
                    }

                    useConditions = built;
                }

                var effects = BuildEffects(row.EffectGroupId, 1);
                if (effects == null)
                {
                    return null;
                }

                return new CardFace(costs, useConditions, effects);
            }

            private IReadOnlyList<ConditionDef> BuildConditions(int groupId)
            {
                if (!conditionsByGroup.TryGetValue(groupId, out var rows) || rows.Count == 0)
                {
                    AddError("条件グループ " + groupId + " がない");
                    return null;
                }

                var result = new List<ConditionDef>();
                foreach (var row in rows.OrderBy(x => x.Order))
                {
                    result.Add(new ConditionDef(row.Subject, row.Comparator, row.Value));
                }

                return result;
            }

            private IReadOnlyList<EffectDef> BuildEffects(int groupId, int depth)
            {
                if (depth >= MaxEffectDepth)
                {
                    AddError("効果グループ " + groupId + ": 入れ子が深すぎる（循環の疑い）");
                    return null;
                }

                if (!effectsByGroup.TryGetValue(groupId, out var rows) || rows.Count == 0)
                {
                    AddError("効果グループ " + groupId + " がない");
                    return null;
                }

                var result = new List<EffectDef>();
                var failed = false;
                foreach (var row in rows.OrderBy(x => x.Order))
                {
                    var conditions = row.RefConditionGroupId == 0 ? null : BuildConditions(row.RefConditionGroupId);
                    var children = row.RefEffectGroupId == 0 ? null : BuildEffects(row.RefEffectGroupId, depth + 1);
                    if ((row.RefConditionGroupId != 0 && conditions == null) || (row.RefEffectGroupId != 0 && children == null))
                    {
                        failed = true;
                        continue;
                    }

                    result.Add(new EffectDef(
                        row.Op,
                        row.Status,
                        row.Trigger,
                        row.Value,
                        row.Arg1,
                        row.Arg2,
                        row.Arg3,
                        row.RefCardId,
                        conditions,
                        children));
                }

                return failed ? null : result;
            }
        }
    }
}
