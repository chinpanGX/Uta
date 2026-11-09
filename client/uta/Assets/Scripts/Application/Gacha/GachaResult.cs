using Uta.MasterData;

namespace Uta.Client.Application.Gacha
{
    /// <summary>ガチャの結果の 1 枠分。</summary>
    public sealed class GachaResult
    {
        public GachaResult(int slot, ItemKind itemKind, int itemId, bool isNew, int shards, int supportPoints)
        {
            Slot = slot;
            Kind = itemKind;
            ItemId = itemId;
            IsNew = isNew;
            Shards = shards;
            SupportPoints = supportPoints;
        }

        /// <summary>1〜10。</summary>
        public int Slot { get; }

        /// <summary>Idol・SupportCard。</summary>
        public ItemKind Kind { get; }

        /// <summary>育成アイドルなら IdolId、サポートカードなら SupportCardId。</summary>
        public int ItemId { get; }

        /// <summary>応答の Outcome が New（育成アイドルは初めて得た、サポートカードは所持の行が 1 つ増えた）。</summary>
        public bool IsNew { get; }

        /// <summary>育成アイドルの重複で得たかけら（それ以外は 0）。</summary>
        public int Shards { get; }

        /// <summary>サポートカードの完凸後の重複で変換した強化pt（それ以外は 0）。</summary>
        public int SupportPoints { get; }
    }
}
