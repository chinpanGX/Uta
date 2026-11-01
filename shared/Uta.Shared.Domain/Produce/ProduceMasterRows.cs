using System.Collections.Generic;

namespace Uta.Shared.Domain.Produce
{
    /// <summary>
    /// 育成のマスタの行をまとめた入れ物。サーバー・クライアントがマスタの表を行の型に詰め替えて
    /// <see cref="ProduceCatalog.Create"/> に渡す（repository-layout.md 4章）。
    /// </summary>
    public sealed class ProduceMasterRows
    {
        public IReadOnlyList<ScenarioRow> Scenarios { get; set; } = new ScenarioRow[0];
        public IReadOnlyList<ScenarioWeekRow> ScenarioWeeks { get; set; } = new ScenarioWeekRow[0];
        public IReadOnlyList<WeekActionSlotRow> WeekActionSlots { get; set; } = new WeekActionSlotRow[0];
        public IReadOnlyList<ClassOptionRow> ClassOptions { get; set; } = new ClassOptionRow[0];
        public IReadOnlyList<ExamPatternRow> ExamPatterns { get; set; } = new ExamPatternRow[0];
        public IReadOnlyList<ExamRankRewardRow> ExamRankRewards { get; set; } = new ExamRankRewardRow[0];
        public IReadOnlyList<ScoreBonusSettingRow> ScoreBonusSettings { get; set; } = new ScoreBonusSettingRow[0];
        public IReadOnlyList<EvaluationScoreBandRow> EvaluationScoreBands { get; set; } = new EvaluationScoreBandRow[0];
        public IReadOnlyList<EvaluationRankRow> EvaluationRanks { get; set; } = new EvaluationRankRow[0];
        public IReadOnlyList<OfferRarityWeightRow> OfferRarityWeights { get; set; } = new OfferRarityWeightRow[0];
        public IReadOnlyList<IdolRow> Idols { get; set; } = new IdolRow[0];
        public IReadOnlyList<IdolRecommendDeckRow> IdolRecommendDecks { get; set; } = new IdolRecommendDeckRow[0];
        public IReadOnlyList<SupportCardRow> SupportCards { get; set; } = new SupportCardRow[0];
        public IReadOnlyList<SupportAbilityRow> SupportAbilities { get; set; } = new SupportAbilityRow[0];
        public IReadOnlyList<SupportLevelCapRow> SupportLevelCaps { get; set; } = new SupportLevelCapRow[0];
        public IReadOnlyList<CommonDeckCardRow> CommonDeckCards { get; set; } = new CommonDeckCardRow[0];
    }
}
