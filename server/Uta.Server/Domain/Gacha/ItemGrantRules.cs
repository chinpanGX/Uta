namespace Uta.Server.Domain.Gacha;

/// <summary>gacha.md 5章。DB を読まない純粋な関数。</summary>
public static class ItemGrantRules
{
    /// <summary>育成アイドルは所持済みなら Duplicate（かけらに変換）。サポートカードは同じカードの完凸を持っていれば Duplicate（強化ptに変換）、そうでなければ New。</summary>
    public static GrantOutcome Decide(GrantedItemKind kind, bool idolOwned, bool supportCardMaxed) =>
        kind == GrantedItemKind.Idol
            ? (idolOwned ? GrantOutcome.Duplicate : GrantOutcome.New)
            : (supportCardMaxed ? GrantOutcome.Duplicate : GrantOutcome.New);
}
