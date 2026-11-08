using MessagePack;
using Uta.Shared.Contracts.Player;

namespace Uta.Shared.Contracts.Common
{
    [MessagePackObject]
    public sealed class PlayerDiff
    {
        [SerializationConstructor]
        public PlayerDiff(bool hasBalance, int level, int exp, int gold, int supportPoints, int gachaCurrency, OwnedIdolDto[] idols, OwnedSupportCardDto[] supportCards, string[] removedSupportCardUniqueIds)
        {
            HasBalance = hasBalance;
            Level = level;
            Exp = exp;
            Gold = gold;
            SupportPoints = supportPoints;
            GachaCurrency = gachaCurrency;
            Idols = idols;
            SupportCards = supportCards;
            RemovedSupportCardUniqueIds = removedSupportCardUniqueIds;
        }

        /// <summary>false なら通貨とレベル（次の 5 つ）は変化なし。</summary>
        [Key(0)]
        public bool HasBalance { get; }

        [Key(1)]
        public int Level { get; }

        [Key(2)]
        public int Exp { get; }

        [Key(3)]
        public int Gold { get; }

        [Key(4)]
        public int SupportPoints { get; }

        [Key(5)]
        public int GachaCurrency { get; }

        /// <summary>触ったアイドル（変更後の値）。無ければ空配列。</summary>
        [Key(6)]
        public OwnedIdolDto[] Idols { get; }

        /// <summary>触ったサポートカード（変更後の値）。無ければ空配列。</summary>
        [Key(7)]
        public OwnedSupportCardDto[] SupportCards { get; }

        /// <summary>無くなったサポートカードの UniqueId（ULID 26 文字）。無ければ空配列。</summary>
        [Key(8)]
        public string[] RemovedSupportCardUniqueIds { get; }
    }
}
