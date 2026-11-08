using MessagePack;

namespace Uta.Shared.Contracts.Player
{
    [MessagePackObject]
    public sealed class OwnedSupportCardDto
    {
        [SerializationConstructor]
        public OwnedSupportCardDto(string uniqueId, int masterId, int level, int limitBreak)
        {
            UniqueId = uniqueId;
            MasterId = masterId;
            Level = level;
            LimitBreak = limitBreak;
        }

        /// <summary>ULID 26 文字。</summary>
        [Key(0)]
        public string UniqueId { get; }

        [Key(1)]
        public int MasterId { get; }

        [Key(2)]
        public int Level { get; }

        [Key(3)]
        public int LimitBreak { get; }
    }
}
