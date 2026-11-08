using System.Collections.Generic;

namespace Uta.Client.Domain.Player
{
    public interface ISupportCardRepository : IReadOnlySupportCardRepository
    {
        /// <summary>SupportCardUid が同じものを置き換え、無ければ末尾に足す。</summary>
        void Upsert(SupportCardEntity card);

        /// <summary>無ければ何もしない。</summary>
        void Remove(string uid);

        void ReplaceAll(IReadOnlyList<SupportCardEntity> cards);
    }
}
