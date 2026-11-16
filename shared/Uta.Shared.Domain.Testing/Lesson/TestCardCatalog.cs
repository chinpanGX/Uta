using System;
using System.Collections.Generic;
using Uta.Shared.Domain.Lesson;

namespace Uta.Shared.Domain.Tests.Lesson
{
    /// <summary>テストのカードをまとめて返す <see cref="ICardCatalog"/>。</summary>
    public sealed class TestCardCatalog : ICardCatalog
    {
        private readonly Dictionary<int, CardDefinition> cards = new Dictionary<int, CardDefinition>();

        public TestCardCatalog()
        {
            AddAll(Phase1Cards.All());
            AddAll(Phase2Cards.All());
            AddAll(Phase3Cards.All());
            AddAll(Phase4Cards.All());
            AddAll(SyntheticCards.All());
        }

        public TestCardCatalog(IEnumerable<CardDefinition> extra)
            : this()
        {
            AddAll(extra);
        }

        /// <summary>昇順</summary>
        public IReadOnlyList<int> CardIds { get; private set; } = new int[0];

        public CardDefinition Get(int cardId)
        {
            return cards[cardId];
        }

        private void AddAll(IEnumerable<CardDefinition> definitions)
        {
            foreach (var definition in definitions)
            {
                if (cards.ContainsKey(definition.Id))
                {
                    throw new ArgumentException("カード ID が重なっています: " + definition.Id, nameof(definitions));
                }

                cards.Add(definition.Id, definition);
            }

            var ids = new List<int>(cards.Keys);
            ids.Sort();
            CardIds = ids.ToArray();
        }
    }
}
