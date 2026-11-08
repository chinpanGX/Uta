using Uta.MasterData;

namespace Uta.Client.Application.Gacha
{
    /// <summary>カード 1 枚分の排出率。</summary>
    public sealed class GachaItemRate
    {
        public GachaItemRate(Rarity rarity, ItemKind kind, int itemId, string name, int basisPoints)
        {
            Rarity = rarity;
            Kind = kind;
            ItemId = itemId;
            Name = name;
            BasisPoints = basisPoints;
        }

        public Rarity Rarity { get; }
        public ItemKind Kind { get; }
        public int ItemId { get; }
        public string Name { get; }

        /// <summary>0.01% 単位（10000 = 100%）。</summary>
        public int BasisPoints { get; }
    }
}
