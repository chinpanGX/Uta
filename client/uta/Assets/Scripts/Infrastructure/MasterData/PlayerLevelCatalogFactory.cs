using System;
using System.Linq;
using ZLinq;
using Uta.MasterData;
using Uta.Shared.Domain.Player;

namespace Uta.Client.Infrastructure
{
    /// <summary>マスタ PlayerLevelExp から PlayerLevelCatalog を作る（screen-design.md 7.5）。All は主キー（Level）の昇順なので並べ替えない。</summary>
    public static class PlayerLevelCatalogFactory
    {
        public static PlayerLevelCatalog Create(MemoryDatabase db)
        {
            if (db == null)
            {
                throw new ArgumentNullException(nameof(db));
            }

            return PlayerLevelCatalog.Create(db.PlayerLevelExpTable.All.Select(x => x.FromExp).ToArray());
        }
    }
}
