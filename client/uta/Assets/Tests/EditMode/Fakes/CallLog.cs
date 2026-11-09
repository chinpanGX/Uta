using System.Collections.Generic;

namespace Uta.Client.Tests
{
    /// <summary>偽の呼ばれた順を文字列で記録する。複数の偽に同じインスタンスを渡し、Entries の並びで確かめる。</summary>
    public sealed class CallLog
    {
        public List<string> Entries { get; } = new List<string>();

        public void Add(string entry)
        {
            Entries.Add(entry);
        }
    }
}
