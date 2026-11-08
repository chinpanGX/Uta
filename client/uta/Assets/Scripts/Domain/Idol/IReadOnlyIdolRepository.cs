using System.Collections.Generic;

namespace Uta.Client.Domain.Idol
{
    public interface IReadOnlyIdolRepository
    {
        /// <summary>MasterId の昇順。0 件は空。</summary>
        IReadOnlyList<IdolEntity> FindAll();

        /// <summary>無ければ null。</summary>
        IdolEntity Find(string uniqueId);
    }
}
