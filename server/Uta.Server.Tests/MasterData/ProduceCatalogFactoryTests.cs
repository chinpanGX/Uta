using Uta.Server.Infrastructure;
using Uta.Shared.Domain.Produce;
using Uta.Shared.Domain.Tests.Produce;

namespace Uta.Server.Tests.MasterData;

public class ProduceCatalogFactoryTests
{
    private static readonly ProduceCatalog Catalog = ProduceCatalogFactory.Create(MasterDataLoader.Load(), CardCatalogFactory.Create(MasterDataLoader.Load()));

    [Fact]
    public void Create_FromMaster_Succeeds()
    {
        Assert.Equal(3, Catalog.GetScenario(1).WeekCount);
        Assert.Equal(13, Catalog.GetScenario(2).WeekCount);
        Assert.NotNull(Catalog.GetIdol(1));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 2)]
    [InlineData(2, 1)]
    [InlineData(2, 2)]
    [InlineData(2, 3)]
    [InlineData(2, 4)]
    public void BotPlay_FromMaster_ReachesFinished(int scenarioId, int idolId)
    {
        var engine = new ProduceEngine(Catalog);
        var streams = new TestRandomStreamSource(1);
        var state = engine.Start(new ProduceStartInput(scenarioId, idolId, 1, new SupportCardInput[0]), streams);

        ProduceBot.Run(engine, Catalog, state, streams);

        Assert.Equal(ProducePhase.Finished, state.Phase);
    }
}
