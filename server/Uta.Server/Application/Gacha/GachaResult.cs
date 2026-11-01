using System.Text.Json.Nodes;
using Uta.Server.Domain.Gacha;

namespace Uta.Server.Application.Gacha;

public sealed record GachaResult(
    int Slot, GrantedItemKind Kind, int ItemId, GrantOutcome Outcome, int Shards, int SupportPoints, Ulid? SupportCardUid)
{
    /// <summary>ゲームログ用。キーは slot・item_kind・item_id・outcome・shards・support_points・support_card_uid（無ければ null）。</summary>
    public JsonObject ToLogJson() => new JsonObject
    {
        ["slot"] = Slot,
        ["item_kind"] = (int)Kind,
        ["item_id"] = ItemId,
        ["outcome"] = (int)Outcome,
        ["shards"] = Shards,
        ["support_points"] = SupportPoints,
        ["support_card_uid"] = SupportCardUid?.ToString(),
    };
}
