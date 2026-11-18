using System.Collections.Generic;
using Uta.Shared.Domain.Lesson;
using Xunit;

namespace Uta.Shared.Domain.Tests.Lesson
{
    /// <summary>IL2CPP の一致の期待値が、今のルールで .NET が再生した値と同じであること（build-ci.md 5.2）。</summary>
    public sealed class LessonReplayExpectedTests
    {
        [Fact]
        public void Normal_MatchesScenario()
        {
            var state = LessonReplayScenario.RunNormal();
            var differences = LessonReplayExpected.Compare(LessonReplayExpected.Normal, state);

            Assert.True(differences.Count == 0, "LessonReplayExpected.Normal に写す:\n" + LessonReplayExpected.ToCSharpLines(state));
        }

        [Fact]
        public void Exam_MatchesScenario()
        {
            var state = LessonReplayScenario.RunExam();
            var differences = LessonReplayExpected.Compare(LessonReplayExpected.Exam, state);

            Assert.True(differences.Count == 0, "LessonReplayExpected.Exam に写す:\n" + LessonReplayExpected.ToCSharpLines(state));
        }

        [Fact]
        public void Compare_DifferentLine_ReportsIndex()
        {
            var state = LessonReplayScenario.RunNormal();
            var expected = new List<string>(LessonStateComparer.Describe(state));
            expected[0] = "x";

            var differences = LessonReplayExpected.Compare(expected, state);

            Assert.Single(differences);
            Assert.StartsWith("[0]", differences[0]);
        }

        [Fact]
        public void Compare_MissingLine_ReportsNone()
        {
            var state = LessonReplayScenario.RunNormal();
            var expected = new List<string>(LessonStateComparer.Describe(state));
            expected.RemoveAt(expected.Count - 1);

            var differences = LessonReplayExpected.Compare(expected, state);

            Assert.Single(differences);
            Assert.Contains("期待 (なし)", differences[0]);
        }
    }
}
