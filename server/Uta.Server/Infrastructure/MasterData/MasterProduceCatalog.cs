using Uta.Shared.Domain.Lesson;
using Uta.Shared.Domain.Produce;

namespace Uta.Server.Infrastructure;

/// <summary>
/// マスタの行を Shared.Domain の行の型に詰めて ProduceCatalog を作る。
/// 検証と組み立ては ProduceCatalog.Create が行う（repository-layout.md 4章）。
/// </summary>
public static class MasterProduceCatalog
{
    public static ProduceCatalog Create(Uta.MasterData.MemoryDatabase db, ICardCatalog cards)
    {
        var rows = new ProduceMasterRows
        {
            Scenarios = db.ScenarioTable.All
                .Select(x => new ScenarioRow(x.ScenarioId, x.Name, (Difficulty)(int)x.Difficulty, x.WeekCount, x.ParamCap, x.RetryCount))
                .ToList(),
            ScenarioWeeks = db.ScenarioWeekTable.All
                .Select(x => new ScenarioWeekRow(
                    x.ScenarioId, x.Week, (WeekKind)(int)x.Kind, x.TurnsSense, x.TurnsLogic, x.ClearTarget, x.PerfectTarget,
                    x.ExamEarlyEndScore, x.RivalBaseScore))
                .ToList(),
            WeekActionSlots = db.WeekActionSlotTable.All
                .Select(x => new WeekActionSlotRow(x.ScenarioId, x.Week, x.Order, (ActionKind)(int)x.Kind))
                .ToList(),
            ClassOptions = db.ClassOptionTable.All
                .Select(x => new ClassOptionRow(x.ScenarioId, x.Week, x.Order, x.HpCost, x.ParamGain, x.GivesCard))
                .ToList(),
            ExamPatterns = db.ExamPatternTable.All
                .Select(x => new ExamPatternRow(
                    x.ExamPatternId, x.FirstPercent, x.SecondPercent, x.ThirdPercent, x.MidFirstTurns, x.MidSecondTurns, x.MidThirdTurns,
                    x.FinalSenseFirstTurns, x.FinalSenseSecondTurns, x.FinalSenseThirdTurns, x.FinalLogicFirstTurns, x.FinalLogicSecondTurns,
                    x.FinalLogicThirdTurns, x.FirstTurnSecondRatePerTenThousand))
                .ToList(),
            ExamRankRewards = db.ExamRankRewardTable.All
                .Select(x => new ExamRankRewardRow((ExamKind)(int)x.ExamKind, x.Rank, x.ParamAll))
                .ToList(),
            ScoreBonusSettings = db.ScoreBonusSettingTable.All
                .Select(x => new ScoreBonusSettingRow(x.Id, x.APerThousand, x.BFirst, x.BSecond, x.BThird))
                .ToList(),
            EvaluationScoreBands = db.EvaluationScoreBandTable.All
                .Select(x => new EvaluationScoreBandRow(x.Order, x.MinScore, x.ScalePerThousand, x.Fixed))
                .ToList(),
            EvaluationRanks = db.EvaluationRankTable.All
                .Select(x => new EvaluationRankRow((ProduceRank)(int)x.Rank, x.MinValue))
                .ToList(),
            OfferRarityWeights = db.OfferRarityWeightTable.All
                .Select(x => new OfferRarityWeightRow((Rarity)(int)x.Rarity, x.Weight))
                .ToList(),
            Idols = db.IdolTable.All
                .Select(x => new IdolRow(
                    x.IdolId, x.Name, (Rarity)(int)x.Rarity, (Plan)(int)x.Plan, (RecommendKind)(int)x.Recommend, x.MaxHp,
                    x.InitialSinging, x.InitialRhythm, x.InitialExpression, x.BonusSingingPerThousand, x.BonusRhythmPerThousand,
                    x.BonusExpressionPerThousand, x.ExamPatternId, (ParamKind)(int)x.FirstParam, (ParamKind)(int)x.SecondParam,
                    (ParamKind)(int)x.ThirdParam, x.UniqueCardId))
                .ToList(),
            IdolRecommendDecks = db.IdolRecommendDeckTable.All
                .Select(x => new IdolRecommendDeckRow((RecommendKind)(int)x.Recommend, x.Order, x.CardId))
                .ToList(),
            SupportCards = db.SupportCardTable.All
                .Select(x => new SupportCardRow(x.SupportCardId, x.Name, (Rarity)(int)x.Rarity, (SupportType)(int)x.Type, (Plan)(int)x.Plan))
                .ToList(),
            SupportAbilities = db.SupportAbilityTable.All
                .Select(x => new SupportAbilityRow(
                    x.SupportCardId, x.Order, (SupportAbilityKind)(int)x.Kind, (ParamKind)(int)x.Param, x.ValueAtLv1, x.ValueAtMax))
                .ToList(),
            SupportLevelCaps = db.SupportLevelCapTable.All
                .Select(x => new SupportLevelCapRow((Rarity)(int)x.Rarity, x.LimitBreak, x.MaxLevel))
                .ToList(),
            CommonDeckCards = db.CommonDeckCardTable.All
                .Select(x => new CommonDeckCardRow(x.Order, x.CardId))
                .ToList(),
        };
        return ProduceCatalog.Create(rows, cards);
    }
}
