using System;

namespace Uta.Client.Domain.Idol
{
    public sealed class IdolEntity
    {
        public IdolEntity(string uniqueId, int masterId, int training, int awakening, int shards)
        {
            UniqueId = uniqueId ?? throw new ArgumentNullException(nameof(uniqueId));
            MasterId = masterId;
            Training = training;
            Awakening = awakening;
            Shards = shards;
        }

        /// <summary>サーバーが採番する ULID 26 文字。</summary>
        public string UniqueId { get; }

        /// <summary>マスタのアイドル ID。</summary>
        public int MasterId { get; }
        public int Training { get; }
        public int Awakening { get; }
        public int Shards { get; }
    }
}
