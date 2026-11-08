using System;

namespace Uta.Client.Domain.SupportCard
{
    public sealed class SupportCardEntity
    {
        public SupportCardEntity(string uniqueId, int masterId, int level, int limitBreak)
        {
            UniqueId = uniqueId ?? throw new ArgumentNullException(nameof(uniqueId));
            MasterId = masterId;
            Level = level;
            LimitBreak = limitBreak;
        }

        /// <summary>ULID 26 文字。</summary>
        public string UniqueId { get; }

        /// <summary>マスタのサポートカード ID。</summary>
        public int MasterId { get; }
        public int Level { get; }
        public int LimitBreak { get; }
    }
}
