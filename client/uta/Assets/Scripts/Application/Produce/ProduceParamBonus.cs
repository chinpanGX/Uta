namespace Uta.Client.Application.Produce
{
    public sealed class ProduceParamBonus
    {
        public ProduceParamBonus(int percent, int remainingTurns)
        {
            Percent = percent;
            RemainingTurns = remainingTurns;
        }

        public int Percent { get; }
        public int RemainingTurns { get; }
    }
}
