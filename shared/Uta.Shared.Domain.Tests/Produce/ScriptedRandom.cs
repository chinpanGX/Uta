using Uta.Shared.Domain.Rng;

namespace Uta.Shared.Domain.Tests.Produce
{
    /// <summary>テスト用の乱数。最初の数回は head を順に返し、尽きたら rest から引く。</summary>
    public sealed class ScriptedRandom : IRandom
    {
        private readonly ulong[] head;
        private readonly IRandom rest;

        public ScriptedRandom(ulong[] head, IRandom rest)
        {
            this.head = head;
            this.rest = rest;
        }

        /// <summary>head から返した回数</summary>
        public int Consumed { get; private set; }

        public ulong NextUInt64()
        {
            if (Consumed < head.Length)
            {
                return head[Consumed++];
            }

            return rest.NextUInt64();
        }
    }
}
