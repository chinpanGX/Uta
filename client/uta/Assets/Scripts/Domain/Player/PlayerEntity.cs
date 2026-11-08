using System;

namespace Uta.Client.Domain.Player
{
    public sealed class PlayerEntity
    {
        public PlayerEntity(string uniqueId, int level, int exp, int gold, int supportPoints, int gachaCurrency, bool hasActiveProduce, bool discardedProduce)
        {
            UniqueId = uniqueId ?? throw new ArgumentNullException(nameof(uniqueId));
            Level = level;
            Exp = exp;
            Gold = gold;
            SupportPoints = supportPoints;
            GachaCurrency = gachaCurrency;
            HasActiveProduce = hasActiveProduce;
            DiscardedProduce = discardedProduce;
        }

        /// <summary>サーバーの player_id（ULID 26 文字）。</summary>
        public string UniqueId { get; }

        public int Level { get; }
        public int Exp { get; }
        public int Gold { get; }
        public int SupportPoints { get; }
        public int GachaCurrency { get; }
        public bool HasActiveProduce { get; }
        public bool DiscardedProduce { get; }
    }
}
