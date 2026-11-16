using MessagePack;

namespace Uta.Shared.Contracts.Player
{
    [MessagePackObject]
    public sealed class SignInResponse
    {
        [SerializationConstructor]
        public SignInResponse(int exp, int gold, int supportPoints, int gachaCurrency, bool hasActiveProduce, bool discardedProduce, OwnedIdolDto[] idols, OwnedSupportCardDto[] supportCards, string playerId)
        {
            Exp = exp;
            Gold = gold;
            SupportPoints = supportPoints;
            GachaCurrency = gachaCurrency;
            HasActiveProduce = hasActiveProduce;
            DiscardedProduce = discardedProduce;
            Idols = idols;
            SupportCards = supportCards;
            PlayerId = playerId;
        }

        /// <summary>経験値の累計（produce.md 7章）。Lv はクライアントが PlayerLevelCatalog で求める。</summary>
        [Key(0)]
        public int Exp { get; }

        [Key(1)]
        public int Gold { get; }

        [Key(2)]
        public int SupportPoints { get; }

        [Key(3)]
        public int GachaCurrency { get; }

        /// <summary>プロデュース未実装の間は常に false。</summary>
        [Key(4)]
        public bool HasActiveProduce { get; }

        /// <summary>プロデュース未実装の間は常に false。</summary>
        [Key(5)]
        public bool DiscardedProduce { get; }

        [Key(6)]
        public OwnedIdolDto[] Idols { get; }

        [Key(7)]
        public OwnedSupportCardDto[] SupportCards { get; }

        [Key(8)]
        public string PlayerId { get; }
    }
}
