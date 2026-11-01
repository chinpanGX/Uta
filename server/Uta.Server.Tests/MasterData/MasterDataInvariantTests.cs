using Uta.MasterData;
using Uta.Server.Infrastructure;

namespace Uta.Server.Tests.MasterData;

public class MasterDataInvariantTests
{
    private static readonly MemoryDatabase Db = MasterDataLoader.Load();

    [Fact]
    public void Cards_EveryCardHasNormalAndEnhancedFace()
    {
        foreach (var card in Db.CardTable.All)
        {
            foreach (var face in new[] { CardFaceKind.Normal, CardFaceKind.Enhanced })
            {
                Assert.True(
                    Db.CardFaceTable.All.Count(x => x.CardId == card.CardId && x.Face == face) == 1,
                    $"card {card.CardId} に {face} の cardFace が 1 行ない");
            }
        }
    }

    [Fact]
    public void CardFaces_EveryFaceHasCostAndCostsHaveFace()
    {
        foreach (var face in Db.CardFaceTable.All)
        {
            Assert.True(
                Db.CardCostTable.All.Any(x => x.CardId == face.CardId && x.Face == face.Face),
                $"cardFace ({face.CardId}, {face.Face}) に cardCost がない");
        }

        foreach (var cost in Db.CardCostTable.All)
        {
            Assert.True(
                Db.CardFaceTable.All.Any(x => x.CardId == cost.CardId && x.Face == cost.Face),
                $"cardCost ({cost.CardId}, {cost.Face}, {cost.Order}) に cardFace がない");
        }
    }

    [Fact]
    public void ScenarioWeeks_WeekNumbersAreContiguousAndStructureIsValid()
    {
        foreach (var scenario in Db.ScenarioTable.All)
        {
            var weeks = Db.ScenarioWeekTable.All.Where(x => x.ScenarioId == scenario.ScenarioId).OrderBy(x => x.Week).ToList();
            Assert.True(
                weeks.Select(x => x.Week).SequenceEqual(Enumerable.Range(1, scenario.WeekCount)),
                $"scenario {scenario.ScenarioId} の週番号が 1〜{scenario.WeekCount} で抜けも重複もない形でない");
            Assert.True(
                weeks[^1].Kind == WeekKind.FinalExam,
                $"scenario {scenario.ScenarioId} の最後の週が FinalExam でない");
            foreach (var week in weeks.Where(x => x.Kind == WeekKind.MidExam || x.Kind == WeekKind.FinalExam))
            {
                Assert.True(week.Week >= 2, $"scenarioWeek ({week.ScenarioId}, {week.Week}) の前に週がない");
                var previous = weeks.Single(x => x.Week == week.Week - 1);
                Assert.True(
                    previous.Kind == WeekKind.Finishing,
                    $"scenarioWeek ({week.ScenarioId}, {week.Week}) の前の週が Finishing でない");
            }
        }
    }

    [Fact]
    public void ScenarioWeeks_ActionWeekTargetsMatchLessonSlot()
    {
        foreach (var week in Db.ScenarioWeekTable.All.Where(x => x.Kind == WeekKind.Action))
        {
            var hasLesson = Db.WeekActionSlotTable.All.Any(
                x => x.ScenarioId == week.ScenarioId && x.Week == week.Week && x.Kind == ActionKind.Lesson);
            var key = $"scenarioWeek ({week.ScenarioId}, {week.Week})";
            if (hasLesson)
            {
                Assert.True(
                    week.TurnsSense >= 1 && week.TurnsLogic >= 1 && week.ClearTarget >= 1 && week.PerfectTarget >= 1,
                    $"{key} に Lesson の枠があるのに、ターン数か目標が 1 未満");
            }
            else
            {
                Assert.True(
                    week.TurnsSense == 0 && week.TurnsLogic == 0 && week.ClearTarget == 0 && week.PerfectTarget == 0,
                    $"{key} に Lesson の枠がないのに、ターン数か目標が 0 でない");
            }
        }
    }

    [Fact]
    public void WeekActionSlots_WeekKindAndLessonRulesHold()
    {
        foreach (var slot in Db.WeekActionSlotTable.All)
        {
            var week = Db.ScenarioWeekTable.All.Single(x => x.ScenarioId == slot.ScenarioId && x.Week == slot.Week);
            Assert.True(
                week.Kind == WeekKind.Action || week.Kind == WeekKind.Finishing,
                $"weekActionSlot ({slot.ScenarioId}, {slot.Week}, {slot.Order}) の週が Action でも Finishing でもない ({week.Kind})");
        }

        foreach (var week in Db.ScenarioWeekTable.All.Where(x => x.Kind == WeekKind.Finishing))
        {
            var slots = Db.WeekActionSlotTable.All.Where(x => x.ScenarioId == week.ScenarioId && x.Week == week.Week).ToList();
            Assert.True(
                slots.Count == 1 && slots[0].Kind == ActionKind.Lesson,
                $"scenarioWeek ({week.ScenarioId}, {week.Week}) は Finishing なのに、枠が Lesson の 1 行だけでない");
        }
    }

    [Fact]
    public void ClassOptions_MatchClassSlots()
    {
        foreach (var option in Db.ClassOptionTable.All)
        {
            Assert.True(
                Db.WeekActionSlotTable.All.Any(
                    x => x.ScenarioId == option.ScenarioId && x.Week == option.Week && x.Kind == ActionKind.Class),
                $"classOption ({option.ScenarioId}, {option.Week}, {option.Order}) の週に Class の weekActionSlot がない");
        }

        foreach (var slot in Db.WeekActionSlotTable.All.Where(x => x.Kind == ActionKind.Class))
        {
            Assert.True(
                Db.ClassOptionTable.All.Any(x => x.ScenarioId == slot.ScenarioId && x.Week == slot.Week),
                $"weekActionSlot ({slot.ScenarioId}, {slot.Week}, {slot.Order}) は Class なのに classOption が 1 行もない");
        }
    }

    [Fact]
    public void ExamRankRewards_HaveRanks1To3ForEachExamKind_AndScoreBonusIsSingleRow()
    {
        foreach (var kind in new[] { ExamKind.Mid, ExamKind.Final })
        {
            foreach (var rank in new[] { 1, 2, 3 })
            {
                Assert.True(
                    Db.ExamRankRewardTable.All.Count(x => x.ExamKind == kind && x.Rank == rank) == 1,
                    $"examRankReward ({kind}, {rank}) が 1 行ない");
            }
        }

        Assert.True(Db.ScoreBonusSettingTable.Count == 1, $"scoreBonusSetting が 1 行でない ({Db.ScoreBonusSettingTable.Count} 行)");
    }

    [Fact]
    public void EvaluationScoreBands_AreContiguousAndIncreasing()
    {
        var bands = Db.EvaluationScoreBandTable.All.OrderBy(x => x.Order).ToList();
        Assert.True(
            bands.Select(x => x.Order).SequenceEqual(Enumerable.Range(1, bands.Count)),
            "evaluationScoreBand の Order が 1 から連続していない");
        Assert.True(bands[0].MinScore == 0, $"evaluationScoreBand の先頭 (Order {bands[0].Order}) の MinScore が 0 でない");
        for (var i = 1; i < bands.Count; i++)
        {
            Assert.True(
                bands[i].MinScore > bands[i - 1].MinScore,
                $"evaluationScoreBand Order {bands[i].Order} の MinScore が前の行以下");
        }
    }

    [Fact]
    public void EvaluationRanks_HaveAllRanksAndIncreasingMinValue()
    {
        var ranks = Enum.GetValues<ProduceRank>().Where(x => x != ProduceRank.Nothing).OrderBy(x => (int)x).ToList();
        Assert.Equal(13, ranks.Count);
        foreach (var rank in ranks)
        {
            Assert.True(Db.EvaluationRankTable.All.Count(x => x.Rank == rank) == 1, $"evaluationRank {rank} が 1 行ない");
            Assert.True(
                Db.EvaluationRankRewardTable.All.Count(x => x.Rank == rank) == 1,
                $"evaluationRankReward {rank} が 1 行ない");
        }

        Assert.True(
            Db.EvaluationRankTable.All.Single(x => x.Rank == ProduceRank.F).MinValue == 0,
            "evaluationRank F の MinValue が 0 でない");
        for (var i = 1; i < ranks.Count; i++)
        {
            var current = Db.EvaluationRankTable.All.Single(x => x.Rank == ranks[i]);
            var previous = Db.EvaluationRankTable.All.Single(x => x.Rank == ranks[i - 1]);
            Assert.True(current.MinValue > previous.MinValue, $"evaluationRank {current.Rank} の MinValue が前のランク以下");
        }
    }

    [Fact]
    public void OfferRarityWeights_SumIsPositive_AndPlayerLevelExpIsContiguous()
    {
        Assert.True(
            Db.OfferRarityWeightTable.All.Sum(x => x.Weight) >= 1,
            "offerRarityWeight の Weight の合計が 1 未満");
        var levels = Db.PlayerLevelExpTable.All.Select(x => x.Level).OrderBy(x => x).ToList();
        Assert.True(
            levels.SequenceEqual(Enumerable.Range(1, levels.Count)),
            "playerLevelExp の Level が 1 から連続していない");
    }

    [Fact]
    public void Idols_UniqueCardPlanMatchesIdolPlanOrFree()
    {
        foreach (var idol in Db.IdolTable.All)
        {
            var card = Db.CardTable.FindByCardId(idol.UniqueCardId);
            Assert.True(
                card.Plan == idol.Plan || card.Plan == Plan.Free,
                $"idol {idol.IdolId} の UniqueCardId {idol.UniqueCardId} の Plan ({card.Plan}) が idol の Plan ({idol.Plan}) と合わない");
        }
    }

    [Fact]
    public void RecommendDecks_Have3RowsPerRecommend_AndCommonDeckHas5Rows()
    {
        foreach (var kind in new[] { RecommendKind.GoodCondition, RecommendKind.Focus, RecommendKind.Impression, RecommendKind.Motivation })
        {
            var orders = Db.IdolRecommendDeckTable.All.Where(x => x.Recommend == kind).Select(x => x.Order).OrderBy(x => x).ToList();
            Assert.True(
                orders.SequenceEqual(new[] { 1, 2, 3 }),
                $"idolRecommendDeck {kind} の Order が 1〜3 の 3 行でない");
        }

        Assert.True(Db.CommonDeckCardTable.Count == 5, $"commonDeckCard が 5 行でない ({Db.CommonDeckCardTable.Count} 行)");
    }

    [Fact]
    public void SupportLevelCost_IsContiguousAndEndsBeforeMaxLevel()
    {
        foreach (var rarity in new[] { Rarity.R, Rarity.SR, Rarity.SSR })
        {
            var levels = Db.SupportLevelCostTable.All.Where(x => x.Rarity == rarity).Select(x => x.Level).OrderBy(x => x).ToList();
            Assert.True(
                levels.SequenceEqual(Enumerable.Range(1, levels.Count)),
                $"supportLevelCost {rarity} の Level が 1 から連続していない");
            var max = Db.SupportLevelCapTable.FindByRarityAndLimitBreak((rarity, 4)).MaxLevel;
            Assert.True(
                levels[^1] + 1 == max,
                $"supportLevelCost {rarity} の最後の Level + 1 ({levels[^1] + 1}) が supportLevelCap の LimitBreak 4 の MaxLevel ({max}) と違う");
        }
    }

    [Fact]
    public void SupportLevelCap_Has5RowsPerRarityAndIncreases()
    {
        foreach (var rarity in new[] { Rarity.R, Rarity.SR, Rarity.SSR })
        {
            var rows = Db.SupportLevelCapTable.All.Where(x => x.Rarity == rarity).OrderBy(x => x.LimitBreak).ToList();
            Assert.True(
                rows.Select(x => x.LimitBreak).SequenceEqual(new[] { 0, 1, 2, 3, 4 }),
                $"supportLevelCap {rarity} の LimitBreak が 0〜4 の 5 行でない");
            for (var i = 1; i < rows.Count; i++)
            {
                Assert.True(
                    rows[i].MaxLevel > rows[i - 1].MaxLevel,
                    $"supportLevelCap ({rarity}, {rows[i].LimitBreak}) の MaxLevel が前の行以下");
            }
        }
    }

    [Fact]
    public void InitialSupportCards_AreDistinct_AndInitialSettingIsSingleRow()
    {
        var ids = Db.InitialSupportCardTable.All.Select(x => x.SupportCardId).ToList();
        Assert.True(ids.Distinct().Count() == ids.Count, "initialSupportCard の SupportCardId に重複がある");
        Assert.True(Db.InitialSettingTable.Count == 1, $"initialSetting が 1 行でない ({Db.InitialSettingTable.Count} 行)");
    }

    [Fact]
    public void GachaDetailTables_HaveEntriesAndWeightSumFitsInt()
    {
        foreach (var table in Db.GachaDetailTableTable.All)
        {
            var entries = Db.GachaTableEntryTable.All.Where(x => x.GachaTableId == table.GachaTableId).ToList();
            Assert.True(entries.Count >= 1, $"gachaDetailTable {table.GachaTableId} に gachaTableEntry がない");
            var sum = entries.Sum(x => (long)x.Weight);
            Assert.True(
                sum <= int.MaxValue,
                $"gachaDetailTable {table.GachaTableId} の重みの合計 {sum} が int.MaxValue を超える");
        }
    }

    [Fact]
    public void Gacha_GuaranteedTableHasNoRarityR()
    {
        foreach (var gacha in Db.GachaTable.All)
        {
            foreach (var entry in Db.GachaTableEntryTable.All.Where(x => x.GachaTableId == gacha.GuaranteedTableId))
            {
                var rarity = entry.ItemKind == ItemKind.Idol
                    ? Db.IdolTable.FindByIdolId(entry.IdolId).Rarity
                    : Db.SupportCardTable.FindBySupportCardId(entry.SupportCardId).Rarity;
                Assert.True(
                    rarity != Rarity.R,
                    $"gacha {gacha.GachaId} の GuaranteedTableId {gacha.GuaranteedTableId} の gachaTableEntry ({entry.GachaTableId}, {entry.Order}) が R のカード");
            }
        }
    }

    [Fact]
    public void DuplicateConversion_HasSixRows()
    {
        foreach (var kind in new[] { ItemKind.Idol, ItemKind.SupportCard })
        {
            foreach (var rarity in new[] { Rarity.R, Rarity.SR, Rarity.SSR })
            {
                Assert.True(
                    Db.DuplicateConversionTable.All.Count(x => x.ItemKind == kind && x.Rarity == rarity) == 1,
                    $"duplicateConversion ({kind}, {rarity}) の行が 1 行ない");
            }
        }

        Assert.True(Db.DuplicateConversionTable.Count == 6, $"duplicateConversion が 6 行でない ({Db.DuplicateConversionTable.Count} 行)");
    }
}
