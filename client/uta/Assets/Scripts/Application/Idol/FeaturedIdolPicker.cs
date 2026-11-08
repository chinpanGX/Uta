using Uta.Client.Domain.Idol;
using Uta.MasterData;

namespace Uta.Client.Application.Idol
{
    /// <summary>ホームのステージに出す育成アイドルを選ぶ。</summary>
    public sealed class FeaturedIdolPicker
    {
        private readonly IReadOnlyIdolRepository idolRepository;
        private readonly MemoryDatabase memoryDatabase;

        public FeaturedIdolPicker(IReadOnlyIdolRepository idolRepository, MemoryDatabase memoryDatabase)
        {
            this.idolRepository = idolRepository;
            this.memoryDatabase = memoryDatabase;
        }

        /// <summary>
        /// 所持の育成アイドルのうち、マスタの Rarity が最も大きいもの、同じなら IdolId が小さいものの IdolId。
        /// 所持が 0 なら 0。マスタに無い IdolId は捕まえない（所持の IdolId はサーバーと同じマスタにある）。
        /// </summary>
        public int Pick()
        {
            var bestId = 0;
            var bestRarity = int.MinValue;
            foreach (var entity in idolRepository.FindAll())
            {
                var rarity = (int)memoryDatabase.IdolTable.FindByIdolId(entity.MasterId).Rarity;
                if (rarity > bestRarity || (rarity == bestRarity && entity.MasterId < bestId))
                {
                    bestRarity = rarity;
                    bestId = entity.MasterId;
                }
            }

            return bestId;
        }
    }
}
