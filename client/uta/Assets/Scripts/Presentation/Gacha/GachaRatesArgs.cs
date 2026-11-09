namespace Uta.Client.Presentation.Gacha
{
    public sealed class GachaRatesArgs
    {
        public GachaRatesArgs(int gachaId)
        {
            GachaId = gachaId;
        }

        public int GachaId { get; }
    }
}
