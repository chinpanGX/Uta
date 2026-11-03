using MasterMemory;
using MasterMemory.Addons;
using MessagePack;

namespace Uta.MasterData
{
    [MemoryTable("gachaTableEntry"), MessagePackObject(true)]
    public class GachaTableEntry : IValidatable<GachaTableEntry>
    {
        [PrimaryKey(keyOrder: 0)]
        [Exists(typeof(GachaDetailTable), nameof(GachaDetailTable.GachaTableId))]
        public int GachaTableId { get; init; }

        [PrimaryKey(keyOrder: 1)]
        public int Order { get; init; }

        public ItemKind ItemKind { get; init; }

        [Exists(typeof(Idol), nameof(Idol.IdolId), AllowEmpty = true)]
        public int IdolId { get; init; }

        [Exists(typeof(SupportCard), nameof(SupportCard.SupportCardId), AllowEmpty = true)]
        public int SupportCardId { get; init; }

        public int Weight { get; init; }

        void IValidatable<GachaTableEntry>.Validate(IValidator<GachaTableEntry> validator)
        {
            validator.Validate(x => x.ItemKind != ItemKind.Nothing, "gachaTableEntry: ItemKind が Nothing");
            validator.Validate(
                x => (x.ItemKind == ItemKind.Idol && x.IdolId != 0 && x.SupportCardId == 0)
                    || (x.ItemKind == ItemKind.SupportCard && x.SupportCardId != 0 && x.IdolId == 0),
                "gachaTableEntry: ItemKind と IdolId・SupportCardId の埋まり方が合わない");
            validator.Validate(x => x.Order >= 1, "gachaTableEntry: Order が 1 未満");
            validator.Validate(x => x.Weight >= 1, "gachaTableEntry: Weight が 1 未満");
        }
    }
}
