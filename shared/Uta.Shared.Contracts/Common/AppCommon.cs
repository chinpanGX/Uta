using MessagePack;

namespace Uta.Shared.Contracts.Common
{
    [MessagePackObject]
    public sealed class AppCommon
    {
        [SerializationConstructor]
        public AppCommon(PlayerDiff playerDiff)
        {
            PlayerDiff = playerDiff;
        }

        /// <summary>null なら変化なし。</summary>
        [Key(0)]
        public PlayerDiff PlayerDiff { get; }
    }
}
