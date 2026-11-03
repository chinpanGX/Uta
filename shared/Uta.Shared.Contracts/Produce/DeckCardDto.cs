using MessagePack;

namespace Uta.Shared.Contracts.Produce
{
    [MessagePackObject]
    public sealed class DeckCardDto
    {
        [SerializationConstructor]
        public DeckCardDto(int instanceId, int cardId, bool enhanced)
        {
            InstanceId = instanceId;
            CardId = cardId;
            Enhanced = enhanced;
        }

        [Key(0)]
        public int InstanceId { get; }

        [Key(1)]
        public int CardId { get; }

        [Key(2)]
        public bool Enhanced { get; }
    }
}
