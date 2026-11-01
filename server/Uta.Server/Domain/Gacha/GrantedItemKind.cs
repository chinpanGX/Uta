namespace Uta.Server.Domain.Gacha;

/// <summary>ItemKind（Contracts）と同じ数値。Application はこの型を使い、Api が Contracts の型へ (int) で読み替える。</summary>
public enum GrantedItemKind
{
    Idol = 1,
    SupportCard = 2,
}
