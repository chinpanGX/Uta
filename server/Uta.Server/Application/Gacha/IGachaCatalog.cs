namespace Uta.Server.Application.Gacha;

public interface IGachaCatalog
{
    /// <summary>GachaId 昇順。</summary>
    IReadOnlyList<GachaDefinition> All { get; }

    GachaDefinition? Find(int gachaId);
}
