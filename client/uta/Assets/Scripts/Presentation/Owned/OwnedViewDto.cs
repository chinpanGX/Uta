using System.Collections.Generic;

namespace Uta.Client.Presentation.Owned
{
    public sealed class OwnedViewDto
    {
        public OwnedViewDto(string title, bool tabsVisible, bool controlsVisible, string sortLabel, string orderLabel, string planLabel, string typeLabel,
            bool filtersVisible, IReadOnlyList<OwnedRowViewDto> rows)
        {
            Title = title;
            TabsVisible = tabsVisible;
            ControlsVisible = controlsVisible;
            SortLabel = sortLabel;
            OrderLabel = orderLabel;
            PlanLabel = planLabel;
            TypeLabel = typeLabel;
            FiltersVisible = filtersVisible;
            Rows = rows;
        }

        public string Title { get; }
        public bool TabsVisible { get; }
        public bool ControlsVisible { get; }
        public string SortLabel { get; }
        public string OrderLabel { get; }
        public string PlanLabel { get; }
        public string TypeLabel { get; }
        public bool FiltersVisible { get; }
        public IReadOnlyList<OwnedRowViewDto> Rows { get; }
    }
}
