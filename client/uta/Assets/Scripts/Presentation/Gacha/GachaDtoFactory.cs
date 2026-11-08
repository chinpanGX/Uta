using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Uta.Client.Domain.Player;
using Uta.MasterData;

namespace Uta.Client.Presentation.Gacha
{
    /// <summary>ガチャのページの表示の値と文字を作る。状態を持たず、リポジトリに書かず、通信・遷移・View に触れない。</summary>
    public sealed class GachaDtoFactory
    {
        private readonly IReadOnlyPlayerRepository playerRepository;
        private readonly MemoryDatabase memoryDatabase;

        public GachaDtoFactory(IReadOnlyPlayerRepository playerRepository, MemoryDatabase memoryDatabase)
        {
            this.playerRepository = playerRepository;
            this.memoryDatabase = memoryDatabase;
        }

        /// <summary>ガチャはマスタの GachaTable の GachaId が最小の 1 本（MVP はガチャ 1 本）。通貨はリポジトリの値（無ければ 0）。</summary>
        public GachaViewDto Create(int? exchangePoints)
        {
            var master = FindGacha();
            var currencyText = "ガチャ通貨 " + N0(CurrentCurrency());
            var exchangeText = "交換pt " + (exchangePoints.HasValue ? N0(exchangePoints.Value) : "-") + " / " + N0(master.CeilingPoints);
            var singleLabel = "単発 " + N0(master.SingleCost);
            var tenLabel = "10連 " + N0(master.TenCost);
            return new GachaViewDto(master.GachaId, currencyText, exchangeText, singleLabel, tenLabel, CreateLineup(master));
        }

        public string CreateDrawConfirmText(int count)
        {
            var master = FindGacha();
            var cost = count == 1 ? master.SingleCost : master.TenCost;
            return "ガチャ通貨 " + N0(cost) + " を使って " + count + " 回引きます。（所持 " + N0(CurrentCurrency()) + "）";
        }

        private Uta.MasterData.Gacha FindGacha()
        {
            return memoryDatabase.GachaTable.All.OrderBy(x => x.GachaId).First();
        }

        private int CurrentCurrency()
        {
            var player = playerRepository.Find();
            return player == null ? 0 : player.GachaCurrency;
        }

        private string CreateLineup(Uta.MasterData.Gacha master)
        {
            var ssr = new List<(ItemKind Kind, int ItemId, string Name)>();
            var entries = memoryDatabase.GachaTableEntryTable.All
                .Where(e => e.GachaTableId == master.NormalTableId)
                .OrderBy(e => e.Order);
            foreach (var entry in entries)
            {
                if (entry.ItemKind == ItemKind.Idol)
                {
                    var idol = memoryDatabase.IdolTable.FindByIdolId(entry.IdolId);
                    if (idol.Rarity == Rarity.SSR)
                    {
                        ssr.Add((entry.ItemKind, entry.IdolId, idol.Name));
                    }
                }
                else if (entry.ItemKind == ItemKind.SupportCard)
                {
                    var supportCard = memoryDatabase.SupportCardTable.FindBySupportCardId(entry.SupportCardId);
                    if (supportCard.Rarity == Rarity.SSR)
                    {
                        ssr.Add((entry.ItemKind, entry.SupportCardId, supportCard.Name));
                    }
                }
                else
                {
                    GachaTexts.Kind(entry.ItemKind);   // 想定外の種別は ArgumentOutOfRangeException
                }
            }

            var builder = new StringBuilder();
            var lines = ssr
                .OrderBy(x => (int)x.Kind)
                .ThenBy(x => x.ItemId)
                .GroupBy(x => (x.Kind, x.ItemId))
                .Select(g => g.First());
            foreach (var line in lines)
            {
                if (builder.Length > 0)
                {
                    builder.Append('\n');
                }

                builder.Append("SSR ").Append(GachaTexts.Kind(line.Kind)).Append(": ").Append(line.Name);
            }

            return builder.ToString();
        }

        private static string N0(int value)
        {
            return value.ToString("N0", CultureInfo.InvariantCulture);
        }
    }
}
