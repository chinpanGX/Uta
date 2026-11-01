using System;
using System.Collections.Generic;
using MasterMemory;
using MasterMemory.Addons;
using MessagePack;

namespace Uta.MasterData
{
    public enum ItemKind
    {
        [InputName("Nothing")] Nothing = 0,
        [InputName("Idol")] Idol = 1,
        [InputName("SupportCard")] SupportCard = 2,
    }

    [MemoryTable("gacha"), MessagePackObject(true)]
    public class Gacha : IValidatable<Gacha>
    {
        [PrimaryKey]
        public int GachaId { get; init; }

        public int SingleCost { get; init; }

        public int TenCost { get; init; }

        [Exists(typeof(GachaDetailTable), nameof(GachaDetailTable.GachaTableId))]
        public int NormalTableId { get; init; }

        [Exists(typeof(GachaDetailTable), nameof(GachaDetailTable.GachaTableId))]
        public int GuaranteedTableId { get; init; }

        public int CeilingPoints { get; init; }

        void IValidatable<Gacha>.Validate(IValidator<Gacha> validator)
        {
            validator.Validate(
                x => x.SingleCost >= 1 && x.TenCost >= 1 && x.CeilingPoints >= 1,
                "gacha: SingleCost・TenCost・CeilingPoints が 1 未満");
            validator.Validate(x => x.NormalTableId != x.GuaranteedTableId, "gacha: NormalTableId と GuaranteedTableId が同じ");
        }
    }

    [MemoryTable("gachaDetailTable"), MessagePackObject(true)]
    public class GachaDetailTable
    {
        [PrimaryKey]
        public int GachaTableId { get; init; }
    }

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

    [MemoryTable("duplicateConversion"), MessagePackObject(true)]
    public class DuplicateConversion : IValidatable<DuplicateConversion>
    {
        [PrimaryKey(keyOrder: 0)]
        public ItemKind ItemKind { get; init; }

        [PrimaryKey(keyOrder: 1)]
        public Rarity Rarity { get; init; }

        public int Amount { get; init; }

        void IValidatable<DuplicateConversion>.Validate(IValidator<DuplicateConversion> validator)
        {
            validator.Validate(x => x.ItemKind != ItemKind.Nothing, "duplicateConversion: ItemKind が Nothing");
            validator.Validate(x => x.Rarity != Rarity.Nothing, "duplicateConversion: Rarity が Nothing");
            validator.Validate(x => x.Amount >= 0, "duplicateConversion: Amount が負");
        }
    }
}
