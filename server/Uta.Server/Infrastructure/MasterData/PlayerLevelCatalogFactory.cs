using Uta.Shared.Domain.Player;

namespace Uta.Server.Infrastructure;

/// <summary>マスタ PlayerLevelExp から PlayerLevelCatalog を作る（produce-server.md 4.1）。All は主キー（Level）の昇順なので並べ替えない。</summary>
public static class PlayerLevelCatalogFactory
{
    public static PlayerLevelCatalog Create(Uta.MasterData.MemoryDatabase master) =>
        PlayerLevelCatalog.Create(master.PlayerLevelExpTable.All.Select(x => x.FromExp).ToArray());
}
