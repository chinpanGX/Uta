using MessagePack;

namespace Uta.Shared.Contracts.Player
{
    [MessagePackObject]
    public sealed class OwnedIdolDto
    {
        [SerializationConstructor]
        public OwnedIdolDto(int idolId, int training, int awakening, int shards)
        {
            IdolId = idolId;
            Training = training;
            Awakening = awakening;
            Shards = shards;
        }

        [Key(0)]
        public int IdolId { get; }

        [Key(1)]
        public int Training { get; }

        [Key(2)]
        public int Awakening { get; }

        [Key(3)]
        public int Shards { get; }
    }
}
