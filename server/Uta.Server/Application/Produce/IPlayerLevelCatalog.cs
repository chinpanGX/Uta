namespace Uta.Server.Application.Produce;

public interface IPlayerLevelCatalog
{
    /// <summary>Level の昇順（Lv 1 → 2 が先頭）。</summary>
    IReadOnlyList<int> RequiredExps { get; }
}
