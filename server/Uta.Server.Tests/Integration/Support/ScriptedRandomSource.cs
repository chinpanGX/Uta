using Uta.Server.Application.Common;
using Uta.Shared.Domain.Rng;

namespace Uta.Server.Tests.Integration.Support;

/// <summary>決まった値を先頭から順に返す乱数。複数回 Create されても同じ列を続けて使う。</summary>
public sealed class ScriptedRandomSource(params ulong[] values) : IRandomSource
{
    private readonly Queue<ulong> queue = new Queue<ulong>(values);

    public IRandom Create() => new ScriptedRandom(queue);

    private sealed class ScriptedRandom(Queue<ulong> queue) : IRandom
    {
        public ulong NextUInt64()
        {
            lock (queue)
            {
                if (queue.Count == 0)
                {
                    throw new InvalidOperationException("決めた乱数の列を使い切った");
                }

                return queue.Dequeue();
            }
        }
    }
}
