using Uta.MasterData;
using Uta.Server.Application.Gacha;
using Uta.Server.Domain.Gacha;

namespace Uta.Server.Infrastructure;

public sealed class MasterDuplicateConversionCatalog : IDuplicateConversionCatalog
{
    private readonly Dictionary<(GrantedItemKind, int), int> amounts = new();

    public MasterDuplicateConversionCatalog(MemoryDatabase db)
    {
        var conversions = db.DuplicateConversionTable.All;
        foreach (var idol in db.IdolTable.All)
        {
            amounts[(GrantedItemKind.Idol, idol.IdolId)] = Find(conversions, ItemKind.Idol, idol.Rarity);
        }

        foreach (var card in db.SupportCardTable.All)
        {
            amounts[(GrantedItemKind.SupportCard, card.SupportCardId)] = Find(conversions, ItemKind.SupportCard, card.Rarity);
        }
    }

    public int GetAmount(GrantedItemKind kind, int itemId) =>
        amounts.TryGetValue((kind, itemId), out var amount)
            ? amount
            : throw new InvalidOperationException($"duplicate conversion not found: {kind} {itemId}");

    private static int Find(IEnumerable<DuplicateConversion> conversions, ItemKind kind, Rarity rarity) =>
        conversions.FirstOrDefault(x => x.ItemKind == kind && x.Rarity == rarity)?.Amount
            ?? throw new InvalidOperationException($"duplicate conversion not found: {kind} {rarity}");
}
