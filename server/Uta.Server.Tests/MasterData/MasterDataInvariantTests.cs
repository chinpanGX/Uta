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
}
