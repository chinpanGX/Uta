using System.Collections.Generic;

namespace Uta.Client.Presentation.Owned
{
    public sealed class SupportDetailViewDto
    {
        public SupportDetailViewDto(bool exists, int supportCardId, IReadOnlyList<string> infoLines, IReadOnlyList<string> abilityLines, string resourcesText,
            IReadOnlyList<SupportLevelStepViewDto> levelSteps, string limitBreakLabel, bool limitBreakEnabled, bool hasMaterial)
        {
            Exists = exists;
            SupportCardId = supportCardId;
            InfoLines = infoLines;
            AbilityLines = abilityLines;
            ResourcesText = resourcesText;
            LevelSteps = levelSteps;
            LimitBreakLabel = limitBreakLabel;
            LimitBreakEnabled = limitBreakEnabled;
            HasMaterial = hasMaterial;
        }

        public bool Exists { get; }
        public int SupportCardId { get; }
        public IReadOnlyList<string> InfoLines { get; }
        public IReadOnlyList<string> AbilityLines { get; }
        public string ResourcesText { get; }

        /// <summary>3 つで、添字は (int)LevelUpStepKind（Plus1・Plus5・Max）。</summary>
        public IReadOnlyList<SupportLevelStepViewDto> LevelSteps { get; }

        public string LimitBreakLabel { get; }
        public bool LimitBreakEnabled { get; }
        public bool HasMaterial { get; }
    }
}
