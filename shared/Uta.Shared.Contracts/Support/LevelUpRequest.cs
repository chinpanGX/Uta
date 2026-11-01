using MessagePack;

namespace Uta.Shared.Contracts.Support
{
    [MessagePackObject]
    public sealed class LevelUpRequest
    {
        [SerializationConstructor]
        public LevelUpRequest(string supportCardUid, int targetLevel)
        {
            SupportCardUid = supportCardUid;
            TargetLevel = targetLevel;
        }

        [Key(0)]
        public string SupportCardUid { get; }

        [Key(1)]
        public int TargetLevel { get; }
    }
}
