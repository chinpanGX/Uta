namespace Uta.Client.Domain.Player
{
    public sealed class IdolEntity
    {
        public IdolEntity(int idolId, int training, int awakening, int shards)
        {
            IdolId = idolId;
            Training = training;
            Awakening = awakening;
            Shards = shards;
        }

        public int IdolId { get; }
        public int Training { get; }
        public int Awakening { get; }
        public int Shards { get; }
    }
}
