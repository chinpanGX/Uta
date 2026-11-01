using MessagePack;

namespace Uta.Shared.Contracts.Support
{
    [MessagePackObject]
    public sealed class LevelUpResponse
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
    }
}
