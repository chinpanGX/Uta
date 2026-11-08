using System.Collections.Generic;

namespace Uta.Client.Domain.Player
{
    public interface IReadOnlySupportCardRepository
    {
        /// <summary>入れた順（サインインの応答の順。Upsert で足したものは末尾）。</summary>
        IReadOnlyList<SupportCardEntity> FindAll();

        /// <summary>無ければ null。</summary>
        SupportCardEntity Find(string uid);
    }
}
