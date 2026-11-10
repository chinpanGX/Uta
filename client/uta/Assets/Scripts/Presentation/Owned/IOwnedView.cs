using System;
using System.Collections.Generic;

namespace Uta.Client.Presentation.Owned
{
    public interface IOwnedView
    {
        void SetTitle(string title);
        void SetTabs(OwnedTabKind selected, bool visible);
        void SetControls(bool visible, string sortLabel, string orderLabel, string planLabel, string typeLabel, bool filtersVisible);
        void SetRows(IReadOnlyList<OwnedRowViewDto> rows);

        event Action<OwnedTabKind> TabSelected;
        event Action SortKeyClicked;
        event Action SortOrderClicked;
        event Action PlanFilterClicked;
        event Action TypeFilterClicked;

        /// <summary>引数は SetRows に渡した行の添字。</summary>
        event Action<int> RowClicked;
        event Action BackClicked;
    }
}
