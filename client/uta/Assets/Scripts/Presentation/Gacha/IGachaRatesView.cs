using System;
using System.Collections.Generic;

namespace Uta.Client.Presentation.Gacha
{
    public interface IGachaRatesView
    {
        void SetLines(IReadOnlyList<GachaLineRowViewDto> lines);
        event Action CloseClicked;
    }
}
