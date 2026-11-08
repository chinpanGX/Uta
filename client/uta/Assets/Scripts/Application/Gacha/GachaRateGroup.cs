using Uta.MasterData;

namespace Uta.Client.Application.Gacha
{
    /// <summary>レア度・種別ごとの排出率の合計。</summary>
    public sealed class GachaRateGroup
    {
        public GachaRateGroup(Rarity rarity, ItemKind kind, int basisPoints)
        {
            Rarity = rarity;
            Kind = kind;
            BasisPoints = basisPoints;
        }

        public Rarity Rarity { get; }
        public ItemKind Kind { get; }

        /// <summary>0.01% 単位（10000 = 100%）。</summary>
        public int BasisPoints { get; }
    }
}
