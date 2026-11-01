using MessagePack;

namespace Uta.Shared.Contracts.Player
{
    [MessagePackObject]
    public sealed class OwnedSupportCardDto
    {
        [SerializationConstructor]
        public OwnedSupportCardDto(string supportCardUid, int cardId, int level, int limitBreak)
        {
            SupportCardUid = supportCardUid;
            CardId = cardId;
            Level = level;
            LimitBreak = limitBreak;
        }

        /// <summary>ULID 26 文字。</summary>
        [Key(0)]
        public string SupportCardUid { get; }

        [Key(1)]
        public int CardId { get; }

        [Key(2)]
        public int Level { get; }

        [Key(3)]
        public int LimitBreak { get; }
    }
}
