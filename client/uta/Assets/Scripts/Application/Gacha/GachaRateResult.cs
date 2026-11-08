using System.Collections.Generic;

namespace Uta.Client.Application.Gacha
{
    /// <summary>排出率の計算結果。</summary>
    public sealed class GachaRateResult
    {
        public GachaRateResult(IReadOnlyList<GachaRateGroup> groups, IReadOnlyList<GachaItemRate> items)
        {
            Groups = groups;
            Items = items;
        }

        /// <summary>レア別・種別の合計。</summary>
        public IReadOnlyList<GachaRateGroup> Groups { get; }

        /// <summary>カードごと。</summary>
        public IReadOnlyList<GachaItemRate> Items { get; }
    }
}
