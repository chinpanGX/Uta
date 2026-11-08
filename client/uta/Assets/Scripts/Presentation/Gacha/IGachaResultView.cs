using System;
using System.Collections.Generic;

namespace Uta.Client.Presentation.Gacha
{
    public interface IGachaResultView
    {
        void SetLines(IReadOnlyList<GachaLineRowViewDto> lines);
        event Action CloseClicked;
        event Action RetryClicked;
    }
}
