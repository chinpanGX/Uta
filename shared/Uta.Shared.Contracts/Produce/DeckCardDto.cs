using MessagePack;

namespace Uta.Shared.Contracts.Produce
{
    [MessagePackObject]
    public sealed class DeckCardDto
    {
        [SerializationConstructor]
        public DeckCardDto(int instanceId, int cardMasterId, bool enhanced)
        {
            InstanceId = instanceId;
            CardMasterId = cardMasterId;
            Enhanced = enhanced;
        }

        [Key(0)]
        public int InstanceId { get; }

        [Key(1)]
        public int CardMasterId { get; }

        [Key(2)]
        public bool Enhanced { get; }
    }
}
