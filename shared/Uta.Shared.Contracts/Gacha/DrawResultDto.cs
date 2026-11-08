using MessagePack;

namespace Uta.Shared.Contracts.Gacha
{
    [MessagePackObject]
    public sealed class DrawResultDto
    {
        [SerializationConstructor]
        public DrawResultDto(int slot, ItemKind itemKind, int itemMasterId, GachaOutcome outcome, int shards, int supportPoints, string supportCardUniqueId)
        {
            Slot = slot;
            ItemKind = itemKind;
            ItemMasterId = itemMasterId;
            Outcome = outcome;
            Shards = shards;
            SupportPoints = supportPoints;
            SupportCardUniqueId = supportCardUniqueId;
        }

        /// <summary>1〜10。交換は 0。</summary>
        [Key(0)]
        public int Slot { get; }

        [Key(1)]
        public ItemKind ItemKind { get; }

        /// <summary>育成アイドルなら IdolId、サポートカードなら SupportCardId。</summary>
        [Key(2)]
        public int ItemMasterId { get; }

        [Key(3)]
        public GachaOutcome Outcome { get; }

        /// <summary>重複した育成アイドルのかけら。</summary>
        [Key(4)]
        public int Shards { get; }

        /// <summary>完凸後の重複の強化pt。</summary>
        [Key(5)]
        public int SupportPoints { get; }

        /// <summary>増えたサポートカードの所持ID。無ければ空文字（null にしない）。</summary>
        [Key(6)]
        public string SupportCardUniqueId { get; }
    }
}
