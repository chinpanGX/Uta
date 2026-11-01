using System.Collections.Generic;
using Uta.Shared.Domain.Rng;

namespace Uta.Shared.Domain.Tests.Produce
{
    /// <summary>テスト用の <see cref="IRandomStreamSource"/>。キーごとに 1 回だけストリームを作り、同じオブジェクトを返す。</summary>
    public sealed class TestRandomStreamSource : IRandomStreamSource
    {
        private readonly ulong masterSeed;
        private readonly Dictionary<ulong, IRandom> streams = new Dictionary<ulong, IRandom>();
        private readonly List<RandomStreamKey> requests = new List<RandomStreamKey>();

        public TestRandomStreamSource(ulong masterSeed)
        {
            this.masterSeed = masterSeed;
        }

        /// <summary>Get を呼んだキー。呼んだ順。同じキーも呼ぶたびに足す</summary>
        public IReadOnlyList<RandomStreamKey> Requests => requests;

        public IRandom Get(RandomStreamKey key)
        {
            requests.Add(key);
            var id = key.ToUInt64();
            if (!streams.TryGetValue(id, out var random))
            {
                random = RandomStreams.Create(masterSeed, key);
                streams.Add(id, random);
            }

            return random;
        }
    }
}
