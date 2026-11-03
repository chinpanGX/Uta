using Uta.Server.Domain.Produce;
using Uta.Shared.Domain.Rng;

namespace Uta.Server.Tests.Domain;

public class SessionRandomStreamsTests
{
    [Fact]
    public void SessionRandomStreams_ExportThenRestore_ContinuesSequence()
    {
        var weekKey = new RandomStreamKey(RandomStreamKind.Week, 1, 0);
        var lessonKey = new RandomStreamKey(RandomStreamKind.Lesson, 1, 0);

        var first = new SessionRandomStreams(7, []);
        for (var i = 0; i < 5; i++)
        {
            first.Get(weekKey).NextUInt64();
            first.Get(lessonKey).NextUInt64();
        }

        var restored = new SessionRandomStreams(7, first.Export());

        var reference = new SessionRandomStreams(7, []);
        var referenceWeek = new List<ulong>();
        var referenceLesson = new List<ulong>();
        for (var i = 0; i < 10; i++)
        {
            referenceWeek.Add(reference.Get(weekKey).NextUInt64());
            referenceLesson.Add(reference.Get(lessonKey).NextUInt64());
        }

        for (var i = 0; i < 5; i++)
        {
            Assert.Equal(referenceWeek[5 + i], restored.Get(weekKey).NextUInt64());
            Assert.Equal(referenceLesson[5 + i], restored.Get(lessonKey).NextUInt64());
        }

        Assert.All(restored.Export(), x => Assert.Equal(10, x.DrawCount));
    }

    [Fact]
    public void SessionRandomStreams_SameKey_SameObject()
    {
        var streams = new SessionRandomStreams(7, []);
        var key = new RandomStreamKey(RandomStreamKind.Offer, 2, 0);

        Assert.Same(streams.Get(key), streams.Get(key));
    }

    [Fact]
    public void SessionRandomStreams_Export_OrderedByKey()
    {
        var streams = new SessionRandomStreams(7, []);
        streams.Get(new RandomStreamKey(RandomStreamKind.Lesson, 3, 0));
        streams.Get(new RandomStreamKey(RandomStreamKind.Week, 1, 0));
        streams.Get(new RandomStreamKey(RandomStreamKind.Scenario, 0, 0));

        var ids = streams.Export().Select(x => x.Key.ToUInt64()).ToList();

        Assert.Equal(3, ids.Count);
        Assert.Equal(ids.Order().ToList(), ids);
    }

    [Fact]
    public void SessionRandomStreams_Export_IncludesLoadedStreamsNotRequested()
    {
        var source = new SessionRandomStreams(7, []);
        var key = new RandomStreamKey(RandomStreamKind.Week, 1, 0);
        source.Get(key).NextUInt64();

        var restored = new SessionRandomStreams(7, source.Export());
        var exported = restored.Export();

        var state = Assert.Single(exported);
        Assert.Equal(key, state.Key);
        Assert.Equal(1, state.DrawCount);
    }
}
