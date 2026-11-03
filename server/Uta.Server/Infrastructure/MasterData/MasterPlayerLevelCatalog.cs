using Uta.MasterData;
using Uta.Server.Application.Produce;

namespace Uta.Server.Infrastructure;

public sealed class MasterPlayerLevelCatalog : IPlayerLevelCatalog
{
    public MasterPlayerLevelCatalog(MemoryDatabase db)
    {
        RequiredExps = db.PlayerLevelExpTable.All.OrderBy(x => x.Level).Select(x => x.RequiredExp).ToList();
    }

    public IReadOnlyList<int> RequiredExps { get; }
}
