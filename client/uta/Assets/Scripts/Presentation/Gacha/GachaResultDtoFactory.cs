using System;
using System.Collections.Generic;
using System.Globalization;
using Uta.Client.Application.Gacha;
using Uta.MasterData;

namespace Uta.Client.Presentation.Gacha
{
    /// <summary>結果のモーダルの行を作る。状態を持たず、リポジトリに書かず、通信・遷移・View に触れない。</summary>
    public sealed class GachaResultDtoFactory
    {
        private readonly MemoryDatabase memoryDatabase;

        public GachaResultDtoFactory(MemoryDatabase memoryDatabase)
        {
            this.memoryDatabase = memoryDatabase;
        }

        /// <summary>results が null なら ArgumentNullException。</summary>
        public IReadOnlyList<GachaLineRowViewDto> Create(IReadOnlyList<GachaResult> results)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            var lines = new List<GachaLineRowViewDto>();
            foreach (var gachaResult in results)
            {
                lines.Add(new GachaLineRowViewDto(CreateText(gachaResult), false));
            }

            return lines;
        }

        private string CreateText(GachaResult gachaResult)
        {
            Rarity rarity;
            string name;
            string note;
            if (gachaResult.Kind == ItemKind.Idol)
            {
                var idol = memoryDatabase.IdolTable.FindByIdolId(gachaResult.ItemId);
                rarity = idol.Rarity;
                name = idol.Name;
                note = gachaResult.IsNew ? "NEW" : "かけら +" + N0(gachaResult.Shards);
            }
            else if (gachaResult.Kind == ItemKind.SupportCard)
            {
                var supportCard = memoryDatabase.SupportCardTable.FindBySupportCardId(gachaResult.ItemId);
                rarity = supportCard.Rarity;
                name = supportCard.Name;
                note = gachaResult.IsNew ? "所持に追加" : "サポート強化pt +" + N0(gachaResult.SupportPoints);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(gachaResult));
            }

            return gachaResult.Slot + ". " + rarity + " " + GachaTexts.Kind(gachaResult.Kind) + " " + name + "　" + note;
        }

        private static string N0(int value)
        {
            return value.ToString("N0", CultureInfo.InvariantCulture);
        }
    }
}
