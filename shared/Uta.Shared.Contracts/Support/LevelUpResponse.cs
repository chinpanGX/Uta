using MessagePack;
using Uta.Shared.Contracts.Common;
using VolTech.Server.Abstractions;

namespace Uta.Shared.Contracts.Support
{
    [MessagePackObject]
    public sealed class LevelUpResponse : IApiResponse<AppCommonDto>
    {
        [SerializationConstructor]
        public LevelUpResponse(int level, int supportPoints, int gold)
        {
            Level = level;
            SupportPoints = supportPoints;
            Gold = gold;
        }

        [Key(0)]
        public int Level { get; }

        /// <summary>残り。</summary>
        [Key(1)]
        public int SupportPoints { get; }

        /// <summary>残り。</summary>
        [Key(2)]
        public int Gold { get; }

        /// <summary>共通レスポンス項目。基盤が入れる。</summary>
        [Key(3)]
        public AppCommonDto Common { get; set; }
    }
}
