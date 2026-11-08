using System.Collections.Generic;

namespace Uta.Client.Domain.Player
{
    public interface IReadOnlyIdolRepository
    {
        /// <summary>IdolId の昇順。0 件は空。</summary>
        IReadOnlyList<IdolEntity> FindAll();

        /// <summary>無ければ null。</summary>
        IdolEntity Find(int idolId);
    }
}
