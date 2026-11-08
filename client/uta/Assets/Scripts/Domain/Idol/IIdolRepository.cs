using System.Collections.Generic;

namespace Uta.Client.Domain.Idol
{
    public interface IIdolRepository : IReadOnlyIdolRepository
    {
        /// <summary>UniqueId が同じものを置き換え、無ければ足す。null なら ArgumentNullException。</summary>
        void Upsert(IdolEntity idol);

        /// <summary>全部入れ替える。null なら ArgumentNullException。</summary>
        void ReplaceAll(IReadOnlyList<IdolEntity> idols);
    }
}
