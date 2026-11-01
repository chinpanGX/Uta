using Uta.MasterData;
using Uta.Server.Application.Gacha;
using Uta.Server.Domain.Gacha;

namespace Uta.Server.Infrastructure;

public sealed class MasterGachaCatalog : IGachaCatalog
{
    private readonly IReadOnlyList<GachaDefinition> all;

    public MasterGachaCatalog(MemoryDatabase db)
    {
        var entries = db.GachaTableEntryTable.All
            .GroupBy(x => x.GachaTableId)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Order).ToList());
        var ceilings = db.GachaCeilingItemTable.All
            .GroupBy(x => x.GachaId)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Order).ToList());

        GachaPool CreatePool(int tableId) => new GachaPool(
            tableId,
            entries[tableId]
                .Select(x => new GachaPoolItem((GrantedItemKind)(int)x.ItemKind, ItemIdOf(x.ItemKind, x.IdolId, x.SupportCardId), x.Weight))
                .ToList());

        all = db.GachaTable.All
            .OrderBy(x => x.GachaId)
            .Select(x => new GachaDefinition(
                x.GachaId, x.SingleCost, x.TenCost, x.CeilingPoints,
                CreatePool(x.NormalTableId),
                CreatePool(x.GuaranteedTableId),
                (ceilings.TryGetValue(x.GachaId, out var items) ? items : [])
                    .Select(c => new CeilingItem(c.Order, (GrantedItemKind)(int)c.ItemKind, ItemIdOf(c.ItemKind, c.IdolId, c.SupportCardId)))
                    .ToList()))
            .ToList();
    }

    public IReadOnlyList<GachaDefinition> All => all;

    public GachaDefinition? Find(int gachaId) => all.FirstOrDefault(x => x.GachaId == gachaId);

    private static int ItemIdOf(ItemKind kind, int idolId, int supportCardId) => kind == ItemKind.Idol ? idolId : supportCardId;
}
