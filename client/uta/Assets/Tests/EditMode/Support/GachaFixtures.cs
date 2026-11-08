using System.Collections.Generic;
using System.Linq;
using Uta.Client.Domain.Player;
using Uta.Shared.Contracts.Gacha;

namespace Uta.Client.Tests
{
    /// <summary>ガチャのテストのダミー値。マスタの型は Uta.MasterData.、Contracts の ItemKind と区別するため名前空間付きで書く。</summary>
    public static class GachaFixtures
    {
        public const int GachaId = 1;
        public const int NormalTableId = 10;
        public const int GuaranteedTableId = 11;
        public const int SingleCost = 250;
        public const int TenCost = 2500;
        public const int CeilingPoints = 200;
        public const int Currency = 3250;

        public static Uta.MasterData.SupportCard SupportCard(int supportCardId, string name, Uta.MasterData.Rarity rarity)
        {
            return new Uta.MasterData.SupportCard
            {
                SupportCardId = supportCardId,
                Name = name,
                Rarity = rarity,
                Type = Uta.MasterData.SupportType.Singing,
                Plan = Uta.MasterData.Plan.Free,
            };
        }

        public static Uta.MasterData.GachaTableEntry IdolEntry(int tableId, int order, int idolId, int weight)
        {
            return new Uta.MasterData.GachaTableEntry
            {
                GachaTableId = tableId,
                Order = order,
                ItemKind = Uta.MasterData.ItemKind.Idol,
                IdolId = idolId,
                Weight = weight,
            };
        }

        public static Uta.MasterData.GachaTableEntry SupportCardEntry(int tableId, int order, int supportCardId, int weight)
        {
            return new Uta.MasterData.GachaTableEntry
            {
                GachaTableId = tableId,
                Order = order,
                ItemKind = Uta.MasterData.ItemKind.SupportCard,
                SupportCardId = supportCardId,
                Weight = weight,
            };
        }

        public static Uta.MasterData.MemoryDatabase Build(
            IEnumerable<Uta.MasterData.Idol> idols,
            IEnumerable<Uta.MasterData.SupportCard> supportCards,
            IEnumerable<Uta.MasterData.GachaTableEntry> entries)
        {
            return TestMasterData.Build(b =>
            {
                b.Append(idols);
                b.Append(supportCards);
                b.Append(entries);
                b.Append(new[]
                {
                    new Uta.MasterData.Gacha
                    {
                        GachaId = GachaId,
                        SingleCost = SingleCost,
                        TenCost = TenCost,
                        NormalTableId = NormalTableId,
                        GuaranteedTableId = GuaranteedTableId,
                        CeilingPoints = CeilingPoints,
                    },
                });
            });
        }

        public static List<Uta.MasterData.Idol> Idols()
        {
            return new List<Uta.MasterData.Idol>
            {
                TestMasterData.Idol(1, "アイドルA", Uta.MasterData.Rarity.SSR),
                TestMasterData.Idol(3, "アイドルC", Uta.MasterData.Rarity.R),
            };
        }

        public static List<Uta.MasterData.SupportCard> SupportCards()
        {
            return new List<Uta.MasterData.SupportCard>
            {
                SupportCard(2, "サポートB", Uta.MasterData.Rarity.SSR),
                SupportCard(4, "サポートD", Uta.MasterData.Rarity.SR),
            };
        }

        /// <summary>通常枠（表 10）。</summary>
        public static List<Uta.MasterData.GachaTableEntry> NormalEntries()
        {
            return new List<Uta.MasterData.GachaTableEntry>
            {
                IdolEntry(10, 1, 1, 1),
                SupportCardEntry(10, 2, 2, 1),
                IdolEntry(10, 3, 3, 8),
            };
        }

        /// <summary>確定枠（表 11）。</summary>
        public static List<Uta.MasterData.GachaTableEntry> GuaranteedEntries()
        {
            return new List<Uta.MasterData.GachaTableEntry>
            {
                SupportCardEntry(11, 1, 4, 3),
                IdolEntry(11, 2, 1, 1),
            };
        }

        /// <summary>共通の前提のマスタ。</summary>
        public static Uta.MasterData.MemoryDatabase CreateMaster()
        {
            return Build(Idols(), SupportCards(), NormalEntries().Concat(GuaranteedEntries()));
        }

        public static PlayerEntity Player(int gachaCurrency)
        {
            return new PlayerEntity("P1", 1, 0, 0, 0, gachaCurrency, false, false);
        }

        public static DrawResultDto IdolResult(int slot, int idolId, GachaOutcome outcome, int shards)
        {
            return new DrawResultDto(slot, ItemKind.Idol, idolId, outcome, shards, 0, "");
        }

        public static DrawResultDto SupportCardResult(int slot, int supportCardId, GachaOutcome outcome, int supportPoints)
        {
            return new DrawResultDto(slot, ItemKind.SupportCard, supportCardId, outcome, 0, supportPoints, "uid" + slot);
        }
    }
}
