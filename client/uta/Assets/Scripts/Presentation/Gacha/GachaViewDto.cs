namespace Uta.Client.Presentation.Gacha
{
    public sealed class GachaViewDto
    {
        public GachaViewDto(int gachaId, string currencyText, string exchangeText, string singleLabel, string tenLabel, string lineupText)
        {
            GachaId = gachaId;
            CurrencyText = currencyText;
            ExchangeText = exchangeText;
            SingleLabel = singleLabel;
            TenLabel = tenLabel;
            LineupText = lineupText;
        }

        public int GachaId { get; }
        public string CurrencyText { get; }
        public string ExchangeText { get; }
        public string SingleLabel { get; }
        public string TenLabel { get; }
        public string LineupText { get; }
    }
}
