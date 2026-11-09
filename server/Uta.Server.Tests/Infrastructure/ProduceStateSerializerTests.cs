using Uta.Server.Domain.Produce;
using Uta.Server.Infrastructure.Produce;
using Uta.Shared.Domain.Produce;
using Uta.Shared.Domain.Tests.Produce;

namespace Uta.Server.Tests.Infrastructure;

public class ProduceStateSerializerTests
{
    private const int ScenarioId = 2;

    public static TheoryData<int, ulong> Cases()
    {
        var data = new TheoryData<int, ulong>();
        for (var idol = 1; idol <= 4; idol++)
        {
            for (ulong seed = 1; seed <= 3; seed++)
            {
                data.Add(idol, seed);
            }
        }

        return data;
    }

    private static ProduceSessionData Pack(ulong seed, SessionRandomStreams streams, ProduceState state, ProduceCatalog catalog) =>
        new(seed, streams.Export(), ProduceSnapshot.From(state, catalog.Cards));

    [Theory]
    [MemberData(nameof(Cases))]
    public void Serializer_RoundTrip_SameState(int idol, ulong seed)
    {
        var catalog = TestProduceMaster.Create();
        var engine = new ProduceEngine(catalog);
        var input = new ProduceStartInput(ScenarioId, idol, 1, new SupportCardInput[0]);
        var streams = new SessionRandomStreams(seed, []);
        var state = engine.Start(input, streams);

        var count = 0;
        var checkedCount = 0;
        while (state.Phase != ProducePhase.Finished)
        {
            var command = ProduceBot.Next(engine, catalog, state);
            Assert.Equal(ProduceApplyResult.Accepted, engine.Apply(state, command, streams));
            count++;
            if (count % 10 == 0)
            {
                AssertRoundTrip(seed, streams, state, catalog);
                checkedCount++;
            }
        }

        AssertRoundTrip(seed, streams, state, catalog);
        Assert.True(checkedCount > 0);
    }

    private static void AssertRoundTrip(ulong seed, SessionRandomStreams streams, ProduceState state, ProduceCatalog catalog)
    {
        var data = Pack(seed, streams, state, catalog);
        var restored = ProduceStateSerializer.Deserialize(ProduceStateSerializer.Serialize(data));

        Assert.Equal(seed, restored.MasterSeed);
        Assert.Equal(data.Streams, restored.Streams);
        Assert.Empty(ProduceStateComparer.Diff(state, restored.Snapshot.ToState(catalog)));
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Serializer_ResumeWithStreams_SameFinalState(int idol, ulong seed)
    {
        var catalog = TestProduceMaster.Create();
        var engine = new ProduceEngine(catalog);
        var input = new ProduceStartInput(ScenarioId, idol, 1, new SupportCardInput[0]);

        // A: 途中を保存せず最後まで
        var streamsA = new SessionRandomStreams(seed, []);
        var stateA = engine.Start(input, streamsA);
        while (stateA.Phase != ProducePhase.Finished)
        {
            Assert.Equal(ProduceApplyResult.Accepted, engine.Apply(stateA, ProduceBot.Next(engine, catalog, stateA), streamsA));
        }

        // B: 10 回の操作ごとに保存と復元をして続ける
        var streamsB = new SessionRandomStreams(seed, []);
        var stateB = engine.Start(input, streamsB);
        var count = 0;
        while (stateB.Phase != ProducePhase.Finished)
        {
            Assert.Equal(ProduceApplyResult.Accepted, engine.Apply(stateB, ProduceBot.Next(engine, catalog, stateB), streamsB));
            count++;
            if (count % 10 == 0)
            {
                var data = ProduceStateSerializer.Deserialize(ProduceStateSerializer.Serialize(Pack(seed, streamsB, stateB, catalog)));
                streamsB = new SessionRandomStreams(data.MasterSeed, data.Streams);
                stateB = data.Snapshot.ToState(catalog);
            }
        }

        Assert.Empty(ProduceStateComparer.Diff(stateA, stateB));
        Assert.Equal(stateA.Operations.Count, stateB.Operations.Count);
    }

    [Fact]
    public void Serializer_NullableParts_RoundTrip()
    {
        var catalog = TestProduceMaster.Create();
        var engine = new ProduceEngine(catalog);
        var streams = new SessionRandomStreams(1, []);
        var state = engine.Start(new ProduceStartInput(ScenarioId, 1, 1, new SupportCardInput[0]), streams);

        var restored = ProduceStateSerializer.Deserialize(ProduceStateSerializer.Serialize(Pack(1, streams, state, catalog)));

        Assert.Null(restored.Snapshot.Lesson);
        Assert.Null(restored.Snapshot.ExamBackup);
        Assert.Null(restored.Snapshot.LastExam);
        Assert.Null(restored.Snapshot.MidExam);
        Assert.Null(restored.Snapshot.Result);
    }
}
