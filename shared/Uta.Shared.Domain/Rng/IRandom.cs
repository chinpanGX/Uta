namespace Uta.Shared.Domain.Rng
{
    /// <summary>
    /// 決定的な疑似乱数。値の取り出し方（範囲・確率・シャッフル）は <see cref="RandomOps"/> に固定する。
    /// </summary>
    public interface IRandom
    {
        ulong NextUInt64();
    }
}
