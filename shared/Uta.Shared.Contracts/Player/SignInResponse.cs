using MessagePack;

namespace Uta.Shared.Contracts.Player
{
    [MessagePackObject]
    public sealed class SignInResponse
    {
        [SerializationConstructor]
        public SignInResponse(int level, int exp, int gold, int supportPoints, int gachaCurrency, bool hasActiveProduce, bool discardedProduce, OwnedIdolDto[] idols, OwnedSupportCardDto[] supportCards)
        {
            Level = level;
            Exp = exp;
            Gold = gold;
            SupportPoints = supportPoints;
            GachaCurrency = gachaCurrency;
            HasActiveProduce = hasActiveProduce;
            DiscardedProduce = discardedProduce;
            Idols = idols;
            SupportCards = supportCards;
        }

        [Key(0)]
        public int Level { get; }

        /// <summary>今のレベルの中の経験値（produce.md 7章）。</summary>
        [Key(1)]
        public int Exp { get; }

        [Key(2)]
        public int Gold { get; }

        [Key(3)]
        public int SupportPoints { get; }

        [Key(4)]
        public int GachaCurrency { get; }

        /// <summary>プロデュース未実装の間は常に false。</summary>
        [Key(5)]
        public bool HasActiveProduce { get; }

        /// <summary>プロデュース未実装の間は常に false。</summary>
        [Key(6)]
        public bool DiscardedProduce { get; }

        [Key(7)]
        public OwnedIdolDto[] Idols { get; }

        [Key(8)]
        public OwnedSupportCardDto[] SupportCards { get; }
    }
}
