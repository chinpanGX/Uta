using System;
using System.Collections.Generic;
using Uta.Client.Domain.Idol;

namespace Uta.Client.Infrastructure.Idol
{
    public sealed class IdolRepository : IIdolRepository
    {
        private readonly List<IdolEntity> items = new List<IdolEntity>();

        public IReadOnlyList<IdolEntity> FindAll()
        {
            var list = new List<IdolEntity>(items);
            list.Sort((a, b) => a.MasterId.CompareTo(b.MasterId));
            return list;
        }

        public IdolEntity Find(string uniqueId)
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

        public void Upsert(IdolEntity idol)
        {
            if (idol == null)
            {
                throw new ArgumentNullException(nameof(idol));
            }

            for (var i = 0; i < items.Count; i++)
            {
                if (items[i].UniqueId == idol.UniqueId)
                {
                    items[i] = idol;
                    return;
                }
            }

            items.Add(idol);
        }

        public void Upsert(IReadOnlyList<IdolEntity> idols)
        {
            if (idols == null)
            {
                throw new ArgumentNullException(nameof(idols));
            }

            foreach (var item in idols)
            {
                if (item == null)
                {
                    throw new ArgumentNullException(nameof(idols), "要素に null がある");
                }
            }

            foreach (var item in idols)
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
