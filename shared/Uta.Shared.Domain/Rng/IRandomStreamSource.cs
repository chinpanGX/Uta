namespace Uta.Shared.Domain.Rng
{
    /// <summary>キーごとの乱数ストリームを渡すポート。同じキーには、続きから引ける同じストリームを返す（produce-domain.md 6章）。</summary>
    public interface IRandomStreamSource
    {
        IRandom Get(RandomStreamKey key);
    }
}
