using System.Collections.Generic;

namespace Uta.Client.Presentation.Formation
{
    /// <summary>編成のページの表示の値。</summary>
    public sealed class FormationViewDto
    {
        public FormationViewDto(string idolLabel, IReadOnlyList<FormationSlotRowViewDto> slots, string scenarioLabel, bool canChangeScenario, bool canStart)
        {
            IdolLabel = idolLabel;
            Slots = slots;
            ScenarioLabel = scenarioLabel;
            CanChangeScenario = canChangeScenario;
            CanStart = canStart;
        }

        public string IdolLabel { get; }
        public IReadOnlyList<FormationSlotRowViewDto> Slots { get; }
        public string ScenarioLabel { get; }
        public bool CanChangeScenario { get; }
        public bool CanStart { get; }
    }
}
