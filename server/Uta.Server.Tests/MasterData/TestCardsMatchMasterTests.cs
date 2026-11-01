using Uta.Server.Infrastructure;
using Uta.Shared.Domain.Lesson;
using Uta.Shared.Domain.Tests.Lesson;

namespace Uta.Server.Tests.MasterData;

/// <summary>
/// Shared.Domain.Tests のテストのカードと、マスタ（CSV）から組み立てたカードの一致を確かめる（lesson-phases.md 1章）。
/// Expected = マスタ、Actual = テストのカード。
/// UnlockLv は比べない（テストのカードは 1 固定で、マスタは 1・5・10 の仮の値。レッスンの計算には使わない）。
/// 落ちたら、lesson-card.md 14章の表でどちらが正しいかを決める。
/// </summary>
public class TestCardsMatchMasterTests
{
    private static readonly CardCatalog MasterCatalog = MasterCardCatalog.Create(MasterDataLoader.Load());

    private static readonly IReadOnlyList<CardDefinition> TestCards = Phase1Cards.All()
        .Concat(Phase2Cards.All())
        .Concat(Phase3Cards.All())
        .Concat(Phase4Cards.All())
        .ToList();

    public static TheoryData<int> AllCardIds => new(AllIds());

    private static List<int> AllIds()
    {
        return Phase1Cards.AllIds
            .Concat(Phase2Cards.AllIds)
            .Concat(Phase3Cards.AllIds)
            .Concat(Phase4Cards.AllIds)
            .OrderBy(x => x)
            .ToList();
    }

    [Fact]
    public void CardIds_MatchBetweenTestCardsAndMaster()
    {
        var testIds = AllIds();
        Assert.Equal(36, testIds.Count);
        Assert.Equal(testIds, MasterCatalog.CardIds.ToList());
    }

    [Theory]
    [MemberData(nameof(AllCardIds))]
    public void Card_TestCardMatchesMaster(int cardId)
    {
        var testCard = TestCards.Single(x => x.Id == cardId);
        Assert.Equal(
            CardDefinitionDescriber.Describe(MasterCatalog.Get(cardId)),
            CardDefinitionDescriber.Describe(testCard));
    }

    [Fact]
    public void Describe_DistinguishesValueAndMissingEnhancedFace()
    {
        var original = TestCards.Single(x => x.Id == Phase1Cards.F01);
        var effects = original.Base.Effects;
        var changedEffects = effects.Select((e, i) => i == 0 && e.Op == EffectOp.GainParam
            ? new EffectDef(e.Op, e.Status, e.Trigger, e.Value + 1, e.Arg1, e.Arg2, e.Arg3, e.RefCardId, e.Conditions, e.Children)
            : e).ToList();
        Assert.Equal(EffectOp.GainParam, effects[0].Op);

        var valueChanged = new CardDefinition(
            original.Id, original.Plan, original.Type, original.Rarity, original.UnlockLv,
            original.OnceInLesson, original.Unique, original.StartInHand,
            new CardFace(original.Base.Costs, original.Base.UseConditions, changedEffects),
            original.Enhanced);
        var noEnhanced = new CardDefinition(
            original.Id, original.Plan, original.Type, original.Rarity, original.UnlockLv,
            original.OnceInLesson, original.Unique, original.StartInHand,
            original.Base,
            null);

        var text = CardDefinitionDescriber.Describe(original);
        Assert.NotEqual(text, CardDefinitionDescriber.Describe(valueChanged));
        Assert.NotEqual(text, CardDefinitionDescriber.Describe(noEnhanced));
    }
}
