using System;
using System.Collections.Generic;
using Uta.Client.Domain.Player;

namespace Uta.Client.Infrastructure.Player
{
    public sealed class SupportCardRepository : ISupportCardRepository
    {
        private readonly List<SupportCardEntity> items = new List<SupportCardEntity>();

        public IReadOnlyList<SupportCardEntity> FindAll()
        {
            return new List<SupportCardEntity>(items);
        }

        public SupportCardEntity Find(string uid)
        {
            foreach (var item in items)
            {
                if (item.SupportCardUid == uid)
                {
                    return item;
                }
            }

            return null;
        }

        public void Upsert(SupportCardEntity card)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            for (var i = 0; i < items.Count; i++)
            {
                if (items[i].SupportCardUid == card.SupportCardUid)
                {
                    items[i] = card;
                    return;
                }
            }

            items.Add(card);
        }

        public void Remove(string uid)
        {
            items.RemoveAll(x => x.SupportCardUid == uid);
        }

        public void ReplaceAll(IReadOnlyList<SupportCardEntity> cards)
        {
            if (cards == null)
            {
                throw new ArgumentNullException(nameof(cards));
            }

            var copy = new List<SupportCardEntity>(cards);
            items.Clear();
            items.AddRange(copy);
        }
    }
}
