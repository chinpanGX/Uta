namespace Uta.Server.Domain.Gacha;

public sealed record GachaHistoryRecord(
    Ulid HistoryId, string RequestId, Ulid PlayerId, int GachaId, int Slot, int GachaTableId,
    int ItemKind, int ItemId, int Outcome, int Shards, int SupportPoints);
