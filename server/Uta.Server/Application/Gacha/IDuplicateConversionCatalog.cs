using Uta.Server.Domain.Gacha;

namespace Uta.Server.Application.Gacha;

public interface IDuplicateConversionCatalog
{
    /// <summary>重複したときの変換量。育成アイドルはかけら、サポートカードは完凸後の強化pt。無ければ InvalidOperationException。</summary>
    int GetAmount(GrantedItemKind kind, int itemId);
}
