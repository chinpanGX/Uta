using System.Collections.Generic;

namespace Uta.Client.Domain.Idol
{
    public interface IIdolRepository : IReadOnlyIdolRepository
    {
        /// <summary>UniqueId が同じものを置き換え、無ければ足す。null なら ArgumentNullException。</summary>
        void Upsert(IdolEntity idol);

        /// <summary>UniqueId が同じものを置き換え、無ければ足す（渡した順）。idols が null、または要素に null があれば ArgumentNullException（1 件も変えない）。</summary>
        void Upsert(IReadOnlyList<IdolEntity> idols);

        /// <summary>全部消す。</summary>
        void Clear();
    }
}
