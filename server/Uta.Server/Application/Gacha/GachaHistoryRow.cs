namespace Uta.Server.Application.Gacha;

public sealed record GachaHistoryRow(
    Ulid HistoryId, string RequestId, Ulid PlayerId, int GachaId, int Slot, int GachaTableId,
    int ItemKind, int ItemId, int Outcome, int Shards, int SupportPoints);
