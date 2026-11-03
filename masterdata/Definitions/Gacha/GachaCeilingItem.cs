using MasterMemory;
using MasterMemory.Addons;
using MessagePack;

namespace Uta.MasterData
{
    [MemoryTable("gachaCeilingItem"), MessagePackObject(true)]
    public class GachaCeilingItem : IValidatable<GachaCeilingItem>
    {
        [PrimaryKey(keyOrder: 0)]
        [Exists(typeof(Gacha), nameof(Gacha.GachaId))]
        public int GachaId { get; init; }

        [PrimaryKey(keyOrder: 1)]
        public int Order { get; init; }

        public ItemKind ItemKind { get; init; }

        [Exists(typeof(Idol), nameof(Idol.IdolId), AllowEmpty = true)]
        public int IdolId { get; init; }

        [Exists(typeof(SupportCard), nameof(SupportCard.SupportCardId), AllowEmpty = true)]
        public int SupportCardId { get; init; }

        void IValidatable<GachaCeilingItem>.Validate(IValidator<GachaCeilingItem> validator)
        {
            validator.Validate(x => x.ItemKind != ItemKind.Nothing, "gachaCeilingItem: ItemKind が Nothing");
            validator.Validate(
                x => (x.ItemKind == ItemKind.Idol && x.IdolId != 0 && x.SupportCardId == 0)
                    || (x.ItemKind == ItemKind.SupportCard && x.SupportCardId != 0 && x.IdolId == 0),
                "gachaCeilingItem: ItemKind と IdolId・SupportCardId の埋まり方が合わない");
            validator.Validate(x => x.Order >= 1, "gachaCeilingItem: Order が 1 未満");
        }
    }
}
