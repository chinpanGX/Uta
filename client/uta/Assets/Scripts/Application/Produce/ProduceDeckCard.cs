namespace Uta.Client.Application.Produce
{
    public sealed class ProduceDeckCard
    {
        public ProduceDeckCard(int instanceId, int cardId, bool enhanced)
        {
            InstanceId = instanceId;
            CardId = cardId;
            Enhanced = enhanced;
        }

        public int InstanceId { get; }
        public int CardId { get; }
        public bool Enhanced { get; }
    }
}
