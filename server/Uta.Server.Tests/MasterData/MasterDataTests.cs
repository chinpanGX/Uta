using System.Text;
using MasterMemory.Addons;
using Uta.MasterData;
using Uta.Server.Infrastructure;

namespace Uta.Server.Tests.MasterData;

public class MasterDataTests
{
    private static readonly MemoryDatabase Db = MasterDataLoader.Load();

    public static TheoryData<Type, Type> EnumPairs => new()
    {
        { typeof(Uta.MasterData.Plan), typeof(Uta.Shared.Domain.Lesson.Plan) },
        { typeof(Uta.MasterData.CardType), typeof(Uta.Shared.Domain.Lesson.CardType) },
        { typeof(Uta.MasterData.Rarity), typeof(Uta.Shared.Domain.Lesson.Rarity) },
        { typeof(Uta.MasterData.ParamKind), typeof(Uta.Shared.Domain.Lesson.ParamKind) },
        { typeof(Uta.MasterData.EffectOp), typeof(Uta.Shared.Domain.Lesson.EffectOp) },
        { typeof(Uta.MasterData.PersistentTrigger), typeof(Uta.Shared.Domain.Lesson.PersistentTrigger) },
        { typeof(Uta.MasterData.StatusKind), typeof(Uta.Shared.Domain.Lesson.StatusKind) },
        { typeof(Uta.MasterData.CostKind), typeof(Uta.Shared.Domain.Lesson.CostKind) },
        { typeof(Uta.MasterData.ConditionSubject), typeof(Uta.Shared.Domain.Lesson.ConditionSubject) },
        { typeof(Uta.MasterData.Comparator), typeof(Uta.Shared.Domain.Lesson.Comparator) },
    };

    [Fact]
    public void Load_Embedded_ReturnsDatabase()
    {
        Assert.NotNull(MasterDataLoader.Load());
    }

    [Fact]
    public void Load_NotMasterData_ThrowsInvalidOperation()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => MasterDataLoader.Load(Encoding.UTF8.GetBytes("not master data")));
        Assert.IsType<MasterDataFormatException>(exception.InnerException);
    }

    [Fact]
    public void Cards_HaveExpectedCounts()
    {
        Assert.Equal(36, Db.CardTable.Count);
        Assert.Equal(72, Db.CardFaceTable.Count);
        Assert.Equal(72, Db.CardCostTable.Count);
        Assert.Equal(129, Db.EffectTable.Count);
        Assert.Equal(14, Db.ConditionTable.Count);
    }

    [Theory]
    [InlineData(EffectOp.GainParam, 26, 329)]
    [InlineData(EffectOp.GainParamByImpression, 6, 840)]
    [InlineData(EffectOp.GainParamByShield, 8, 890)]
    [InlineData(EffectOp.GainParamByMotivation, 2, 270)]
    [InlineData(EffectOp.AddShield, 24, 98)]
    [InlineData(EffectOp.AddStatus, 53, 148)]
    [InlineData(EffectOp.AddCardUses, 2, 2)]
    [InlineData(EffectOp.DrawCards, 2, 4)]
    [InlineData(EffectOp.ScaleShield, 2, 100)]
    [InlineData(EffectOp.AddPersistent, 4, 0)]
    public void Effects_CountAndValueSumByOp(EffectOp op, int count, int valueSum)
    {
        var rows = Db.EffectTable.All.Where(x => x.Op == op).ToList();
        Assert.Equal(count, rows.Count);
        Assert.Equal(valueSum, rows.Sum(x => x.Value));
    }

    [Fact]
    public void CardCosts_SumByKind()
    {
        Assert.Equal(176, Db.CardCostTable.All.Where(x => x.Kind == CostKind.Consume).Sum(x => x.Value));
        Assert.Equal(30, Db.CardCostTable.All.Where(x => x.Kind == CostKind.HpDirect).Sum(x => x.Value));
    }

    [Fact]
    public void Cards_SpotCheck()
    {
        // F01
        var f01Normal = Db.CardFaceTable.All.Single(x => x.CardId == 1 && x.Face == CardFaceKind.Normal);
        Assert.Equal(111, f01Normal.EffectGroupId);
        var f01NormalCost = Db.CardCostTable.All.Single(x => x.CardId == 1 && x.Face == CardFaceKind.Normal);
        Assert.Equal(CostKind.Consume, f01NormalCost.Kind);
        Assert.Equal(4, f01NormalCost.Value);
        var f01NormalEffect = Db.EffectTable.All.Single(x => x.EffectGroupId == 111);
        Assert.Equal(EffectOp.GainParam, f01NormalEffect.Op);
        Assert.Equal(9, f01NormalEffect.Value);

        var f01Enhanced = Db.CardFaceTable.All.Single(x => x.CardId == 1 && x.Face == CardFaceKind.Enhanced);
        var f01EnhancedCost = Db.CardCostTable.All.Single(x => x.CardId == 1 && x.Face == CardFaceKind.Enhanced);
        Assert.Equal(CostKind.Consume, f01EnhancedCost.Kind);
        Assert.Equal(3, f01EnhancedCost.Value);
        var f01EnhancedEffect = Db.EffectTable.All.Single(x => x.EffectGroupId == f01Enhanced.EffectGroupId);
        Assert.Equal(EffectOp.GainParam, f01EnhancedEffect.Op);
        Assert.Equal(14, f01EnhancedEffect.Value);

        // S04
        var s04Normal = Db.CardFaceTable.All.Single(x => x.CardId == 104 && x.Face == CardFaceKind.Normal);
        Assert.Equal(10411, s04Normal.UseConditionGroupId);
        var s04Condition = Db.ConditionTable.All.Single(x => x.ConditionGroupId == 10411);
        Assert.Equal(ConditionSubject.GoodConditionTurns, s04Condition.Subject);
        Assert.Equal(Comparator.GreaterOrEqual, s04Condition.Comparator);
        Assert.Equal(2, s04Condition.Value);
        var s04Effect = Db.EffectTable.All.Single(x => x.EffectGroupId == 10411);
        Assert.Equal(EffectOp.GainParam, s04Effect.Op);
        Assert.Equal(10, s04Effect.Value);
        Assert.Equal(200, s04Effect.Arg3);

        // S13
        var s13 = Db.EffectTable.All.Where(x => x.EffectGroupId == 11311).OrderBy(x => x.Order).ToList();
        Assert.Equal(2, s13.Count);
        Assert.Equal(EffectOp.AddStatus, s13[0].Op);
        Assert.Equal(StatusKind.Focus, s13[0].Status);
        Assert.Equal(2, s13[0].Value);
        Assert.Equal(EffectOp.AddPersistent, s13[1].Op);
        Assert.Equal(PersistentTrigger.OnActiveCardUse, s13[1].Trigger);
        Assert.Equal(11312, s13[1].RefEffectGroupId);
        var s13Child = Db.EffectTable.All.Single(x => x.EffectGroupId == 11312);
        Assert.Equal(EffectOp.AddStatus, s13Child.Op);
        Assert.Equal(StatusKind.Focus, s13Child.Status);
        Assert.Equal(1, s13Child.Value);

        // S14 強化面
        var s14 = Db.EffectTable.All.Single(x => x.EffectGroupId == 11421 && x.Order == 2);
        Assert.Equal(EffectOp.GainParam, s14.Op);
        Assert.Equal(34, s14.Value);
        Assert.Equal(150, s14.Arg2);
        Assert.Equal(11422, s14.RefConditionGroupId);
        var s14Condition = Db.ConditionTable.All.Single(x => x.ConditionGroupId == 11422);
        Assert.Equal(ConditionSubject.Focus, s14Condition.Subject);
        Assert.Equal(Comparator.GreaterOrEqual, s14Condition.Comparator);
        Assert.Equal(3, s14Condition.Value);
    }

    [Theory]
    [MemberData(nameof(EnumPairs))]
    public void EnumMirrors_MatchDomain(Type masterEnum, Type domainEnum)
    {
        Assert.Equal(Enum.GetNames(domainEnum), Enum.GetNames(masterEnum));
        foreach (var name in Enum.GetNames(domainEnum))
        {
            Assert.Equal(
                Convert.ToInt64(Enum.Parse(domainEnum, name)),
                Convert.ToInt64(Enum.Parse(masterEnum, name)));
        }
    }

    [Fact]
    public void Produce_HaveExpectedCounts()
    {
        Assert.Equal(2, Db.ScenarioTable.Count);
        Assert.Equal(16, Db.ScenarioWeekTable.Count);
        Assert.Equal(17, Db.WeekActionSlotTable.Count);
        Assert.Equal(12, Db.ClassOptionTable.Count);
        Assert.Equal(4, Db.ExamPatternTable.Count);
        Assert.Equal(6, Db.ExamRankRewardTable.Count);
        Assert.Equal(1, Db.ScoreBonusSettingTable.Count);
        Assert.Equal(7, Db.EvaluationScoreBandTable.Count);
        Assert.Equal(13, Db.EvaluationRankTable.Count);
        Assert.Equal(13, Db.EvaluationRankRewardTable.Count);
        Assert.Equal(4, Db.OfferRarityWeightTable.Count);
        Assert.Equal(99, Db.PlayerLevelExpTable.Count);
    }

    [Fact]
    public void Scenario2_Week6_IsMidExamWithRival900()
    {
        var week = Db.ScenarioWeekTable.All.Single(x => x.ScenarioId == 2 && x.Week == 6);
        Assert.Equal(WeekKind.MidExam, week.Kind);
        Assert.Equal(900, week.ExamEarlyEndScore);
        Assert.Equal(900, week.RivalBaseScore);
    }

    [Fact]
    public void PlayerLevelExp_EveryRowIs40TimesLevel()
    {
        foreach (var row in Db.PlayerLevelExpTable.All)
        {
            Assert.True(row.RequiredExp == 40 * row.Level, $"playerLevelExp Level {row.Level} の RequiredExp が {row.RequiredExp}");
        }
    }

    [Fact]
    public void IdolSupport_HaveExpectedCounts()
    {
        Assert.Equal(4, Db.IdolTable.Count);
        Assert.Equal(12, Db.IdolRecommendDeckTable.Count);
        Assert.Equal(5, Db.CommonDeckCardTable.Count);
        Assert.Equal(10, Db.SupportCardTable.Count);
        Assert.Equal(16, Db.SupportAbilityTable.Count);
        Assert.Equal(147, Db.SupportLevelCostTable.Count);
        Assert.Equal(15, Db.SupportLevelCapTable.Count);
        Assert.Equal(5, Db.InitialSupportCardTable.Count);
        Assert.Equal(1, Db.InitialSettingTable.Count);
    }

    [Fact]
    public void Idols_UniqueCardsAre107Or207()
    {
        Assert.Equal(107, Db.IdolTable.FindByIdolId(1).UniqueCardId);
        Assert.Equal(207, Db.IdolTable.FindByIdolId(2).UniqueCardId);
        Assert.Equal(107, Db.IdolTable.FindByIdolId(3).UniqueCardId);
        Assert.Equal(207, Db.IdolTable.FindByIdolId(4).UniqueCardId);
    }

    [Fact]
    public void SupportLevelCost_EveryRowIsBaseTimesLevel()
    {
        foreach (var row in Db.SupportLevelCostTable.All)
        {
            var baseCost = row.Rarity switch
            {
                Rarity.R => 39,
                Rarity.SR => 65,
                Rarity.SSR => 117,
                _ => throw new InvalidOperationException($"supportLevelCost ({row.Rarity}, {row.Level}) の Rarity が想定外"),
            };
            Assert.True(
                row.SupportPoints == baseCost * row.Level,
                $"supportLevelCost ({row.Rarity}, {row.Level}) の SupportPoints が {row.SupportPoints}");
            Assert.True(
                row.Gold == row.SupportPoints,
                $"supportLevelCost ({row.Rarity}, {row.Level}) の Gold が SupportPoints と違う");
        }
    }

    [Fact]
    public void InitialSetting_Is2500_10000_10000()
    {
        var row = Db.InitialSettingTable.FindById(1);
        Assert.Equal(2500, row.GachaCurrency);
        Assert.Equal(10000, row.Gold);
        Assert.Equal(10000, row.SupportPoints);
    }

    [Fact]
    public void Gacha_HaveExpectedCounts()
    {
        Assert.Equal(1, Db.GachaTable.Count);
        Assert.Equal(2, Db.GachaDetailTableTable.Count);
        Assert.Equal(21, Db.GachaTableEntryTable.Count);
        Assert.Equal(2, Db.GachaCeilingItemTable.Count);
        Assert.Equal(6, Db.DuplicateConversionTable.Count);
    }

    [Fact]
    public void GachaTables_WeightSums()
    {
        Assert.Equal(10000L, Db.GachaTableEntryTable.All.Where(x => x.GachaTableId == 1).Sum(x => (long)x.Weight));
        Assert.Equal(2200L, Db.GachaTableEntryTable.All.Where(x => x.GachaTableId == 2).Sum(x => (long)x.Weight));
    }

    [Fact]
    public void GachaTables_RarityShares()
    {
        long Share(ItemKind kind, Rarity rarity)
        {
            return Db.GachaTableEntryTable.All
                .Where(x => x.GachaTableId == 1 && x.ItemKind == kind)
                .Where(x => (kind == ItemKind.Idol
                    ? Db.IdolTable.FindByIdolId(x.IdolId).Rarity
                    : Db.SupportCardTable.FindBySupportCardId(x.SupportCardId).Rarity) == rarity)
                .Sum(x => (long)x.Weight);
        }

        Assert.Equal(200L, Share(ItemKind.Idol, Rarity.SSR));
        Assert.Equal(300L, Share(ItemKind.SupportCard, Rarity.SSR));
        Assert.Equal(680L, Share(ItemKind.Idol, Rarity.SR));
        Assert.Equal(1020L, Share(ItemKind.SupportCard, Rarity.SR));
        Assert.Equal(3120L, Share(ItemKind.Idol, Rarity.R));
        Assert.Equal(4680L, Share(ItemKind.SupportCard, Rarity.R));
        Assert.Equal(1700L, Share(ItemKind.Idol, Rarity.SR) + Share(ItemKind.SupportCard, Rarity.SR));
        Assert.Equal(7800L, Share(ItemKind.Idol, Rarity.R) + Share(ItemKind.SupportCard, Rarity.R));
    }

    [Fact]
    public void Gacha_CeilingItems()
    {
        var first = Db.GachaCeilingItemTable.FindByGachaIdAndOrder((1, 1));
        Assert.Equal(ItemKind.Idol, first.ItemKind);
        Assert.Equal(4, first.IdolId);
        var second = Db.GachaCeilingItemTable.FindByGachaIdAndOrder((1, 2));
        Assert.Equal(ItemKind.SupportCard, second.ItemKind);
        Assert.Equal(9, second.SupportCardId);
    }
}
