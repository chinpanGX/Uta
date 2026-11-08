using MessagePack;

namespace Uta.Shared.Contracts.Player
{
    [MessagePackObject]
    public sealed class OwnedIdolDto
    {
        [SerializationConstructor]
        public OwnedIdolDto(int masterId, int training, int awakening, int shards, string uniqueId)
        {
            MasterId = masterId;
            Training = training;
            Awakening = awakening;
            Shards = shards;
            UniqueId = uniqueId;
        }

        [Key(0)]
        public int MasterId { get; }

        [Key(1)]
        public int Training { get; }

        [Key(2)]
        public int Awakening { get; }

        [Key(3)]
        public int Shards { get; }

        [Key(4)]
        public string UniqueId { get; }
    }
}
