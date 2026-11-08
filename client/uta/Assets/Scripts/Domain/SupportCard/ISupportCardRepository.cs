using System.Collections.Generic;

namespace Uta.Client.Domain.SupportCard
{
    public interface ISupportCardRepository : IReadOnlySupportCardRepository
    {
        /// <summary>UniqueId が同じものを置き換え、無ければ末尾に足す。null なら ArgumentNullException。</summary>
        void Upsert(SupportCardEntity card);

        /// <summary>無ければ何もしない。</summary>
        void Remove(string uniqueId);

        /// <summary>UniqueId が同じものを置き換え、無ければ末尾に足す（渡した順）。cards が null、または要素に null があれば ArgumentNullException（1 件も変えない）。</summary>
        void Upsert(IReadOnlyList<SupportCardEntity> cards);

        /// <summary>全部消す。</summary>
        void Clear();
    }
}
