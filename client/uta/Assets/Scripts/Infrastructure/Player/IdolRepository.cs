using System;
using System.Collections.Generic;
using Uta.Client.Domain.Player;

namespace Uta.Client.Infrastructure.Player
{
    public sealed class IdolRepository : IIdolRepository
    {
        private readonly List<IdolEntity> items = new List<IdolEntity>();

        public IReadOnlyList<IdolEntity> FindAll()
        {
            var list = new List<IdolEntity>(items);
            list.Sort((a, b) => a.IdolId.CompareTo(b.IdolId));
            return list;
        }

        public IdolEntity Find(int idolId)
        {
            foreach (var item in items)
            {
                if (item.IdolId == idolId)
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
                if (items[i].IdolId == idol.IdolId)
                {
                    items[i] = idol;
                    return;
                }
            }

            items.Add(idol);
        }

        public void ReplaceAll(IReadOnlyList<IdolEntity> idols)
        {
            if (idols == null)
            {
                throw new ArgumentNullException(nameof(idols));
            }

            var copy = new List<IdolEntity>(idols);
            items.Clear();
            items.AddRange(copy);
        }
    }
}
