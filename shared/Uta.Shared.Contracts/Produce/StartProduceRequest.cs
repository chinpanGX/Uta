using MessagePack;

namespace Uta.Shared.Contracts.Produce
{
    [MessagePackObject]
    public sealed class StartProduceRequest
    {
        [SerializationConstructor]
        public StartProduceRequest(int idolMasterId, string[] supportCardUniqueIds, int rentalCandidateMasterId, int scenarioMasterId)
        {
            IdolMasterId = idolMasterId;
            SupportCardUniqueIds = supportCardUniqueIds;
            RentalCandidateMasterId = rentalCandidateMasterId;
            ScenarioMasterId = scenarioMasterId;
        }

        [Key(0)]
        public int IdolMasterId { get; }

        [Key(1)]
        public string[] SupportCardUniqueIds { get; }

        [Key(2)]
        public int RentalCandidateMasterId { get; }

        [Key(3)]
        public int ScenarioMasterId { get; }
    }
}
