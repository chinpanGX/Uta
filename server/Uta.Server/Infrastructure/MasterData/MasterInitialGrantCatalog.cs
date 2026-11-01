using Uta.MasterData;
using Uta.Server.Application.Player;

namespace Uta.Server.Infrastructure;

public sealed class MasterInitialGrantCatalog : IInitialGrantCatalog
{
    private readonly InitialGrant grant;

    public MasterInitialGrantCatalog(MemoryDatabase db)
    {
        var setting = db.InitialSettingTable.All.Single();                      // Id == 1 の 1 行だけ
        grant = new InitialGrant(
            setting.GachaCurrency, setting.Gold, setting.SupportPoints,
            db.IdolTable.All.Where(x => x.Rarity == Rarity.R).OrderBy(x => x.IdolId).Select(x => x.IdolId).ToList(),
            db.InitialSupportCardTable.All.OrderBy(x => x.Order).Select(x => x.SupportCardId).ToList());
    }

    public InitialGrant Get() => grant;
}
