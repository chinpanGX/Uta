using MessagePack;

namespace Uta.Shared.Contracts.Common
{
    [MessagePackObject]
    public sealed class AppCommonDto
    {
        [SerializationConstructor]
        public AppCommonDto(PlayerDiffDto playerDiff)
        {
            PlayerDiff = playerDiff;
        }

        /// <summary>null なら変化なし。</summary>
        [Key(0)]
        public PlayerDiffDto PlayerDiff { get; }
    }
}
