using System.Linq;
using Uta.Shared.Domain.Lesson;
using Uta.Shared.Domain.Produce;
using Xunit;

namespace Uta.Shared.Domain.Tests.Produce
{
    public class ProduceStateComparerTests
    {
        private static readonly ProduceCatalog Catalog = TestProduceMaster.Create();

        private static ProduceState Start()
        {
            var engine = new ProduceEngine(Catalog);
            return engine.Start(new ProduceStartInput(1, 1, 1, new SupportCardInput[0]), new TestRandomStreamSource(1));
        }

        [Fact]
        public void Describe_InitialState_ContainsExpectedLines()
        {
            var lines = ProduceStateComparer.Describe(Start());

            Assert.Contains("scenario=1", lines);
            Assert.Contains("idol=1", lines);
            Assert.Contains("phase=1", lines);
            Assert.Contains("week=1", lines);
            Assert.Contains("deck=1:1:0,2:1:0,3:2:0,4:3:0,5:3:0,6:107:0,7:101:0,8:101:0,9:104:0", lines);
            Assert.Contains("lesson=none", lines);
            Assert.Contains("operations=0", lines);
            Assert.Contains("result=none", lines);
        }

        [Fact]
        public void Diff_SameState_Empty()
        {
            Assert.Empty(ProduceStateComparer.Diff(Start(), Start()));
        }

        [Fact]
        public void Diff_DifferentPhase_ReportsPhaseLine()
        {
            var engine = new ProduceEngine(Catalog);
            var expected = Start();
            var actual = Start();
            engine.Apply(actual, ProduceCommand.ChooseLesson(0, ParamKind.Singing), new TestRandomStreamSource(1));

            var diff = ProduceStateComparer.Diff(expected, actual);

            Assert.NotEmpty(diff);
            Assert.Contains("期待 phase=1 / 実際 phase=3", diff);
        }

        [Fact]
        public void Describe_InLesson_IncludesPrefixedLessonLines()
        {
            var engine = new ProduceEngine(Catalog);
            var state = Start();
            engine.Apply(state, ProduceCommand.ChooseLesson(0, ParamKind.Singing), new TestRandomStreamSource(1));

            var lines = ProduceStateComparer.Describe(state);

            Assert.Contains(lines, l => l.StartsWith("lesson.turn=", System.StringComparison.Ordinal));
        }
    }
}
