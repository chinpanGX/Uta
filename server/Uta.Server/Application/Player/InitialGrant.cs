namespace Uta.Server.Application.Player;

public sealed record InitialGrant(
    int GachaCurrency, int Gold, int SupportPoints,
    IReadOnlyList<int> IdolIds,            // R の育成アイドル。IdolId の昇順
    IReadOnlyList<int> SupportCardIds);    // InitialSupportCard の Order の昇順
