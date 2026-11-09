using System;

namespace Uta.Client.Presentation.Gacha
{
    public interface IGachaView
    {
        void SetCurrency(string text);
        void SetExchange(string text);
        void SetCosts(string singleLabel, string tenLabel);
        void SetLineup(string text);
        event Action SingleClicked;
        event Action TenClicked;
        event Action RatesClicked;
        event Action BackClicked;
    }
}
