using MessagePack;

namespace Uta.Shared.Contracts.Support
{
    [MessagePackObject]
    public sealed class LimitBreakResponse
    {
        [SerializationConstructor]
        public LimitBreakResponse(int limitBreak, int maxLevel)
        {
            LimitBreak = limitBreak;
            MaxLevel = maxLevel;
        }

        /// <summary>解放後の回数。</summary>
        [Key(0)]
        public int LimitBreak { get; }

        /// <summary>解放後の上限レベル。</summary>
        [Key(1)]
        public int MaxLevel { get; }
    }
}
