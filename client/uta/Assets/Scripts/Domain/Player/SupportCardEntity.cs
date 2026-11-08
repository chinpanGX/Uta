using System;

namespace Uta.Client.Domain.Player
{
    public sealed class SupportCardEntity
    {
        public SupportCardEntity(string supportCardUid, int cardId, int level, int limitBreak)
        {
            SupportCardUid = supportCardUid ?? throw new ArgumentNullException(nameof(supportCardUid));
            CardId = cardId;
            Level = level;
            LimitBreak = limitBreak;
        }

        /// <summary>ULID 26 文字。</summary>
        public string SupportCardUid { get; }
        public int CardId { get; }
        public int Level { get; }
        public int LimitBreak { get; }
    }
}
