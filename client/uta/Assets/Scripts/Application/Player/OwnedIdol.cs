namespace Uta.Client.Application.Player
{
    public sealed class OwnedIdol
    {
        public OwnedIdol(int idolId, int training, int awakening, int shards)
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
