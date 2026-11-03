using MessagePack;

namespace Uta.Shared.Contracts.Produce
{
    [MessagePackObject]
    public sealed class StartProduceRequest
    {
        [SerializationConstructor]
        public StartProduceRequest(int idolId, string[] supportCardUids, int rentalCandidateId, int scenarioId)
        {
            IdolId = idolId;
            SupportCardUids = supportCardUids;
            RentalCandidateId = rentalCandidateId;
            ScenarioId = scenarioId;
        }

        [Key(0)]
        public int IdolId { get; }

        [Key(1)]
        public string[] SupportCardUids { get; }

        [Key(2)]
        public int RentalCandidateId { get; }

        [Key(3)]
        public int ScenarioId { get; }
    }
}
