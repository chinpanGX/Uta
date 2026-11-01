using System;
using System.Collections.Generic;
using System.Linq;
using Uta.Shared.Domain.Lesson;
using Uta.Shared.Domain.Produce;
using Xunit;

namespace Uta.Shared.Domain.Tests.Produce
{
    public class ProduceCatalogTests
    {
        private static readonly ProduceCatalog Catalog = TestProduceMaster.Create();

        [Fact]
        public void Create_RealMaster_ScenarioShape()
        {
            var scenario1 = Catalog.GetScenario(1);
            Assert.Equal(3, scenario1.WeekCount);
            Assert.Equal(1000, scenario1.ParamCap);
            Assert.Equal(3, scenario1.RetryCount);
            Assert.Equal(3, scenario1.Weeks.Count);

            var scenario2 = Catalog.GetScenario(2);
            Assert.Equal(13, scenario2.Weeks.Count);

            var week2 = scenario2.GetWeek(2);
            Assert.Equal(WeekKind.Action, week2.Kind);
            Assert.Equal(new[] { ActionKind.Class, ActionKind.Supply }, week2.Actions.ToArray());
            Assert.Equal(3, week2.ClassOptions.Count);
            AssertOption(week2.ClassOptions[0], 8, 40, true);
            AssertOption(week2.ClassOptions[1], 4, 30, true);
            AssertOption(week2.ClassOptions[2], 0, 15, true);

            var week6 = scenario2.GetWeek(6);
            Assert.Equal(WeekKind.MidExam, week6.Kind);
            Assert.Equal(900, week6.ExamEarlyEndScore);
            Assert.Equal(900, week6.RivalBaseScore);

            var week13 = scenario2.GetWeek(13);
            Assert.Equal(WeekKind.FinalExam, week13.Kind);
            Assert.Equal(2250, week13.RivalBaseScore);

            var first = scenario1.GetWeek(1);
            Assert.Equal(5, first.GetTurns(Plan.Sense));
            Assert.Equal(6, first.GetTurns(Plan.Logic));
            Assert.Equal(25, first.ClearTarget);
            Assert.Equal(50, first.PerfectTarget);
        }

        [Fact]
        public void Create_RealMaster_IdolAndSupport()
        {
            var idol = Catalog.GetIdol(1);
            Assert.Equal(Plan.Sense, idol.Plan);
            Assert.Equal(30, idol.MaxHp);
            Assert.Equal(ParamKind.Singing, idol.FirstParam);
            Assert.Equal(ParamKind.Rhythm, idol.SecondParam);
            Assert.Equal(ParamKind.Expression, idol.ThirdParam);
            Assert.Equal(107, idol.UniqueCardId);
            Assert.Equal(new[] { 104, 101, 101 }, idol.RecommendCardIds.ToArray());
            Assert.Equal(30, idol.GetInitialParam(ParamKind.Singing));
            Assert.Equal(50, idol.GetBonusPerThousand(ParamKind.Singing));
            Assert.Equal(ParamKind.Rhythm, idol.GetExamParam(2));

            var support = Catalog.GetSupportCard(9);
            Assert.Equal(Rarity.SSR, support.Rarity);
            Assert.Equal(60, support.MaxLevelAtFullLimitBreak);
            Assert.Equal(2, support.Abilities.Count);
            var ability = support.Abilities[0];
            Assert.Equal(SupportAbilityKind.InitialParam, ability.Kind);
            Assert.Equal(ParamKind.Singing, ability.Param);
            Assert.Equal(15, ability.ValueAtLv1);
            Assert.Equal(65, ability.ValueAtMax);
        }

        [Fact]
        public void Create_RealMaster_Tables()
        {
            Assert.Equal(new[] { 1, 1, 2, 3, 3 }, Catalog.CommonDeckCardIds.ToArray());
            Assert.Equal(10, Catalog.GetOfferWeight(Rarity.SSR));
            Assert.Equal(30, Catalog.GetOfferWeight(Rarity.N));
            Assert.Equal(30, Catalog.GetMaxLevel(Rarity.R, 2));
            Assert.Equal(60, Catalog.GetMaxLevel(Rarity.SSR, 4));
            Assert.Equal(30, Catalog.GetExamRankParamAll(ExamKind.Final, 1));
            Assert.Equal(5, Catalog.GetExamRankParamAll(ExamKind.Mid, 3));
            Assert.Equal(0, Catalog.GetExamRankParamAll(ExamKind.Mid, 4));
            Assert.Equal(225, Catalog.ScoreBonusA);
            Assert.Equal(190, Catalog.GetScoreBonusB(1));
            Assert.Equal(150, Catalog.GetScoreBonusB(2));
            Assert.Equal(120, Catalog.GetScoreBonusB(3));
            Assert.Equal(7, Catalog.EvaluationScoreBands.Count);
            Assert.Equal(0, Catalog.EvaluationScoreBands[0].MinScore);
            Assert.Equal(13, Catalog.EvaluationRanks.Count);
            Assert.Equal(ProduceRank.F, Catalog.EvaluationRanks[0].Rank);
            Assert.Equal(ProduceRank.SSPlus, Catalog.EvaluationRanks[12].Rank);

            var pattern = Catalog.GetExamPattern(1);
            Assert.Equal(new[] { 4, 3, 2 }, pattern.GetTurnCounts(ExamKind.Mid, Plan.Sense));
            Assert.Equal(new[] { 5, 4, 3 }, pattern.GetTurnCounts(ExamKind.Final, Plan.Sense));
            Assert.Equal(new[] { 5, 3, 3 }, pattern.GetTurnCounts(ExamKind.Final, Plan.Logic));
            Assert.Equal(1500, pattern.FirstTurnSecondRatePerTenThousand);
        }

        [Fact]
        public void GetScenario_Unknown_Throws()
        {
            Assert.Throws<KeyNotFoundException>(() => Catalog.GetScenario(99999));
            Assert.Throws<KeyNotFoundException>(() => Catalog.GetIdol(99999));
            Assert.Throws<KeyNotFoundException>(() => Catalog.GetSupportCard(99999));
            Assert.Throws<KeyNotFoundException>(() => Catalog.GetExamPattern(99999));
        }

        [Fact]
        public void Create_DuplicateScenario_Throws()
        {
            var rows = TestProduceMaster.LoadRows();
            rows.Scenarios = rows.Scenarios.Concat(new[] { rows.Scenarios.First(x => x.ScenarioId == 1) }).ToList();

            AssertThrowsContaining(rows, "Scenario 1", "重複");
        }

        [Fact]
        public void Create_LastWeekNotFinalExam_Throws()
        {
            var rows = TestProduceMaster.LoadRows();
            ChangeScenario1LastWeek(rows);

            AssertThrowsContaining(rows, "FinalExam");
        }

        [Fact]
        public void Create_OutingSlot_Throws()
        {
            var rows = TestProduceMaster.LoadRows();
            AddOutingSlot(rows);

            AssertThrowsContaining(rows, "未対応");
        }

        [Fact]
        public void Create_IdolExamOrderNotPermutation_Throws()
        {
            var rows = TestProduceMaster.LoadRows();
            rows.Idols = rows.Idols.Select(x => x.IdolId != 1 ? x : WithIdol(x, secondParam: ParamKind.Singing)).ToList();

            AssertThrowsContaining(rows, "Idol 1", "審査の順");
        }

        [Fact]
        public void Create_MissingOfferWeight_Throws()
        {
            var rows = TestProduceMaster.LoadRows();
            rows.OfferRarityWeights = rows.OfferRarityWeights.Where(x => x.Rarity != Rarity.SSR).ToList();

            AssertThrowsContaining(rows, "OfferRarityWeight");
        }

        [Fact]
        public void Create_ExamPatternFirstTurnsTooSmall_Throws()
        {
            var rows = TestProduceMaster.LoadRows();
            rows.ExamPatterns = rows.ExamPatterns.Select(x => x.ExamPatternId != 1 ? x : new ExamPatternRow(
                x.ExamPatternId, x.FirstPercent, x.SecondPercent, x.ThirdPercent, 1, x.MidSecondTurns, x.MidThirdTurns,
                x.FinalSenseFirstTurns, x.FinalSenseSecondTurns, x.FinalSenseThirdTurns,
                x.FinalLogicFirstTurns, x.FinalLogicSecondTurns, x.FinalLogicThirdTurns, x.FirstTurnSecondRatePerTenThousand)).ToList();

            AssertThrowsContaining(rows, "ExamPattern 1");
        }

        [Fact]
        public void Create_UnknownUniqueCard_Throws()
        {
            var rows = TestProduceMaster.LoadRows();
            rows.Idols = rows.Idols.Select(x => x.IdolId != 1 ? x : WithIdol(x, uniqueCardId: 99999)).ToList();

            AssertThrowsContaining(rows, "Idol 1", "UniqueCardId");
        }

        [Fact]
        public void Create_MultipleProblems_ReportsAll()
        {
            var rows = TestProduceMaster.LoadRows();
            ChangeScenario1LastWeek(rows);
            AddOutingSlot(rows);

            AssertThrowsContaining(rows, "FinalExam", "未対応");
        }

        [Fact]
        public void Create_NullArguments_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => ProduceCatalog.Create(null, new Lesson.TestCardCatalog()));
            Assert.Throws<ArgumentNullException>(() => ProduceCatalog.Create(TestProduceMaster.LoadRows(), null));
        }

        private static void AssertOption(ClassOptionDefinition option, int hpCost, int paramGain, bool givesCard)
        {
            Assert.Equal(hpCost, option.HpCost);
            Assert.Equal(paramGain, option.ParamGain);
            Assert.Equal(givesCard, option.GivesCard);
        }

        private static void AssertThrowsContaining(ProduceMasterRows rows, params string[] words)
        {
            var exception = Assert.Throws<InvalidOperationException>(() => TestProduceMaster.Create(rows));
            foreach (var word in words)
            {
                Assert.Contains(word, exception.Message);
            }
        }

        private static void ChangeScenario1LastWeek(ProduceMasterRows rows)
        {
            rows.ScenarioWeeks = rows.ScenarioWeeks.Select(x => x.ScenarioId == 1 && x.Week == 3
                ? new ScenarioWeekRow(x.ScenarioId, x.Week, WeekKind.Action, x.TurnsSense, x.TurnsLogic, x.ClearTarget, x.PerfectTarget, x.ExamEarlyEndScore, x.RivalBaseScore)
                : x).ToList();
        }

        private static void AddOutingSlot(ProduceMasterRows rows)
        {
            rows.WeekActionSlots = rows.WeekActionSlots.Concat(new[] { new WeekActionSlotRow(2, 2, 3, ActionKind.Outing) }).ToList();
        }

        private static IdolRow WithIdol(IdolRow x, ParamKind? secondParam = null, int? uniqueCardId = null)
        {
            return new IdolRow(
                x.IdolId, x.Name, x.Rarity, x.Plan, x.Recommend, x.MaxHp,
                x.InitialSinging, x.InitialRhythm, x.InitialExpression,
                x.BonusSingingPerThousand, x.BonusRhythmPerThousand, x.BonusExpressionPerThousand,
                x.ExamPatternId, x.FirstParam, secondParam ?? x.SecondParam, x.ThirdParam, uniqueCardId ?? x.UniqueCardId);
        }
    }
}
