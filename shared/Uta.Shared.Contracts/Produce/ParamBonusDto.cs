using MessagePack;

namespace Uta.Shared.Contracts.Produce
{
    [MessagePackObject]
    public sealed class ParamBonusDto
    {
        [SerializationConstructor]
        public ParamBonusDto(int percent, int remainingTurns)
        {
            Percent = percent;
            RemainingTurns = remainingTurns;
        }

        [Key(0)]
        public int Percent { get; }

        [Key(1)]
        public int RemainingTurns { get; }
    }
}
