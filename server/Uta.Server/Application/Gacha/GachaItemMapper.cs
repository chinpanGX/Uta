using Uta.MasterData;
using Uta.Server.Domain.Gacha;

namespace Uta.Server.Application.Gacha;

/// <summary>マスタの排出・天井の行の品目（ItemKind と IdolId・SupportCardId）を、付与する品目の種類と ID に写す。</summary>
public static class GachaItemMapper
{
    public static (GrantedItemKind Kind, int ItemId) ToGrantedItem(GachaTableEntry entry) =>
        ToGrantedItem(entry.ItemKind, entry.IdolId, entry.SupportCardId);

    public static (GrantedItemKind Kind, int ItemId) ToGrantedItem(GachaCeilingItem item) =>
        ToGrantedItem(item.ItemKind, item.IdolId, item.SupportCardId);

    private static (GrantedItemKind Kind, int ItemId) ToGrantedItem(ItemKind kind, int idolId, int supportCardId) =>
        kind switch
        {
            ItemKind.Idol => (GrantedItemKind.Idol, idolId),
            ItemKind.SupportCard => (GrantedItemKind.SupportCard, supportCardId),
            _ => throw new InvalidOperationException($"unexpected item kind: {kind}"),
        };
}
