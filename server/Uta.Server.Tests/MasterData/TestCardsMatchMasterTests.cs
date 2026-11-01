using Uta.Server.Infrastructure;
using Uta.Shared.Domain.Lesson;
using Uta.Shared.Domain.Tests.Lesson;

namespace Uta.Server.Tests.MasterData;

/// <summary>
/// Shared.Domain.Tests のテストのカードと、マスタ（CSV）から組み立てたカードの一致を確かめる（lesson-phases.md 1章）。
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
}
