using Uta.Shared.Domain.Rng;

namespace Uta.Server.Domain.Produce;

/// <summary>
/// プロデュース 1 回分の乱数ストリームを管理する <see cref="IRandomStreamSource"/>。
/// 保存済みのストリームはその状態から続け、無いキーはマスターシードから作る。
/// </summary>
public sealed class SessionRandomStreams : IRandomStreamSource
{
    private readonly ulong masterSeed;
    private readonly SortedDictionary<ulong, (RandomStreamKey Key, Xoshiro256StarStar Random)> streams = [];

    public SessionRandomStreams(ulong masterSeed, IEnumerable<RandomStreamState> saved)
    {
        this.masterSeed = masterSeed;
        foreach (var state in saved)
        {
            streams[state.Key.ToUInt64()] = (state.Key, new Xoshiro256StarStar(state.S0, state.S1, state.S2, state.S3, state.DrawCount));
        }
    }

    public IRandom Get(RandomStreamKey key)
    {
        var id = key.ToUInt64();
        if (!streams.TryGetValue(id, out var entry))
        {
            entry = (key, RandomStreams.Create(masterSeed, key));
            streams[id] = entry;
        }

        return entry.Random;
    }

    /// <summary>Get した・読み込んだ全部の現在の状態を、key.ToUInt64() の昇順で返す。</summary>
    public IReadOnlyList<RandomStreamState> Export() =>
        streams.Values
            .Select(x => new RandomStreamState(x.Key, x.Random.S0, x.Random.S1, x.Random.S2, x.Random.S3, x.Random.DrawCount))
            .ToList();
}
