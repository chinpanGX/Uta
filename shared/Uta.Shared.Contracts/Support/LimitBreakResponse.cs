using MessagePack;
using Uta.Shared.Contracts.Common;
using VolTech.Server.Abstractions;

namespace Uta.Shared.Contracts.Support
{
    [MessagePackObject]
    public sealed class LimitBreakResponse : IApiResponse<AppCommon>
    {
        [SerializationConstructor]
        public LimitBreakResponse(int limitBreak, int maxLevel)
        {
            LimitBreak = limitBreak;
            MaxLevel = maxLevel;
        }

        /// <summary>解放後の回数。</summary>
        [Key(0)]
        public int LimitBreak { get; }

        /// <summary>解放後の上限レベル。</summary>
        [Key(1)]
        public int MaxLevel { get; }

        /// <summary>共通レスポンス項目。基盤が入れる。</summary>
        [Key(2)]
        public AppCommon Common { get; set; }
    }
}
