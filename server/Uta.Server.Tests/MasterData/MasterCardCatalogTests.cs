using Uta.Server.Infrastructure;
using Uta.Shared.Domain.Lesson;

namespace Uta.Server.Tests.MasterData;

public class MasterCardCatalogTests
{
    private static readonly CardCatalog Catalog = MasterCardCatalog.Create(MasterDataLoader.Load());

    [Fact]
    public void Create_Mvp_Has36Cards()
    {
        Assert.Equal(36, Catalog.CardIds.Count);
        Assert.Equal(1, Catalog.CardIds[0]);
        Assert.Equal(215, Catalog.CardIds[^1]);
        Assert.Equal(Catalog.CardIds.OrderBy(x => x).ToList(), Catalog.CardIds.ToList());
    }

    [Fact]
    public void Get_F01_HasNormalAndEnhancedFaces()
    {
        var card = Catalog.Get(1);
        Assert.Equal(Plan.Free, card.Plan);
        Assert.Equal(CardType.Active, card.Type);
        Assert.Equal(Rarity.N, card.Rarity);
        Assert.Single(card.Base.Costs);
        Assert.Equal(CostKind.Consume, card.Base.Costs[0].Kind);
        Assert.Equal(4, card.Base.Costs[0].Value);
        Assert.Single(card.Base.Effects);
        Assert.Equal(EffectOp.GainParam, card.Base.Effects[0].Op);
        Assert.Equal(9, card.Base.Effects[0].Value);
        Assert.NotNull(card.Enhanced);
        Assert.Equal(CostKind.Consume, card.Enhanced.Costs[0].Kind);
        Assert.Equal(3, card.Enhanced.Costs[0].Value);
        Assert.Equal(14, card.Enhanced.Effects[0].Value);
    }

    [Fact]
    public void Get_S14_HasConditionedEffect()
    {
        var effects = Catalog.Get(114).Base.Effects;
        Assert.Equal(2, effects.Count);
        Assert.Single(effects[1].Conditions);
        Assert.Equal(ConditionSubject.Focus, effects[1].Conditions[0].Subject);
        Assert.Equal(Comparator.GreaterOrEqual, effects[1].Conditions[0].Comparator);
        Assert.Equal(3, effects[1].Conditions[0].Value);
    }

    [Fact]
    public void Get_S13_HasPersistentWithChild()
    {
        var persistent = Catalog.Get(113).Base.Effects.Single(x => x.Op == EffectOp.AddPersistent);
        Assert.Equal(PersistentTrigger.OnActiveCardUse, persistent.Trigger);
        Assert.Single(persistent.Children);
        Assert.Equal(EffectOp.AddStatus, persistent.Children[0].Op);
        Assert.Equal(StatusKind.Focus, persistent.Children[0].Status);
        Assert.Equal(1, persistent.Children[0].Value);
    }

    [Fact]
    public void Get_S04_HasUseCondition()
    {
        var conditions = Catalog.Get(104).Base.UseConditions;
        Assert.Single(conditions);
        Assert.Equal(ConditionSubject.GoodConditionTurns, conditions[0].Subject);
        Assert.Equal(Comparator.GreaterOrEqual, conditions[0].Comparator);
        Assert.Equal(2, conditions[0].Value);
    }
}
