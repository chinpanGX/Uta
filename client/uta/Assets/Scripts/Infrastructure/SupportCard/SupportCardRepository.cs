using System;
using System.Collections.Generic;
using Uta.Client.Domain.SupportCard;

namespace Uta.Client.Infrastructure.SupportCard
{
    public sealed class SupportCardRepository : ISupportCardRepository
    {
        private readonly List<SupportCardEntity> items = new List<SupportCardEntity>();

        public IReadOnlyList<SupportCardEntity> FindAll()
        {
            return new List<SupportCardEntity>(items);
        }

        public SupportCardEntity Find(string uniqueId)
        {
            foreach (var item in items)
            {
                if (item.UniqueId == uniqueId)
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
                if (items[i].UniqueId == card.UniqueId)
                {
                    items[i] = card;
                    return;
                }
            }

            items.Add(card);
        }

        public void Remove(string uniqueId)
        {
            items.RemoveAll(x => x.UniqueId == uniqueId);
        }

        public void Upsert(IReadOnlyList<SupportCardEntity> cards)
        {
            if (cards == null)
            {
                throw new ArgumentNullException(nameof(cards));
            }

            foreach (var item in cards)
            {
                if (item == null)
                {
                    throw new ArgumentNullException(nameof(cards), "要素に null がある");
                }
            }

            foreach (var item in cards)
            {
                Upsert(item);
            }
        }

        public void Clear()
        {
            items.Clear();
        }
    }
}
