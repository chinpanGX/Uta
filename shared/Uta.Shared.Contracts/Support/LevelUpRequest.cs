using MessagePack;

namespace Uta.Shared.Contracts.Support
{
    [MessagePackObject]
    public sealed class LevelUpRequest
    {
        [SerializationConstructor]
        public LevelUpRequest(string supportCardUniqueId, int targetLevel)
        {
            SupportCardUniqueId = supportCardUniqueId;
            TargetLevel = targetLevel;
        }

        [Key(0)]
        public string SupportCardUniqueId { get; }

        [Key(1)]
        public int TargetLevel { get; }
    }
}
