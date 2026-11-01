using System.Linq;
using Uta.Shared.Domain.Lesson;
using Xunit;

namespace Uta.Shared.Domain.Tests.Lesson
{
    public sealed class LessonStateComparerTests
    {
        [Fact]
        public void Diff_SameState_Empty()
        {
            var state = TestStates.PlayerState(new[] { Phase1Cards.F01 }, hand: new[] { 1 }, drawPile: new int[0], hp: 30);

            Assert.Empty(LessonStateComparer.Diff(state, TestStates.Copy(state)));
        }

        [Fact]
        public void Diff_DifferentHp_ReportsHpOnly()
        {
            var a = TestStates.PlayerState(new[] { Phase1Cards.F01 }, hand: new[] { 1 }, drawPile: new int[0], hp: 30);
            var b = TestStates.PlayerState(new[] { Phase1Cards.F01 }, hand: new[] { 1 }, drawPile: new int[0], hp: 29);

            var diff = LessonStateComparer.Diff(a, b);

            Assert.Single(diff);
            Assert.Contains("hp=", diff[0]);
        }

        [Fact]
        public void Diff_ExtraPersistent_Reports()
        {
            var persistent = new PersistentEffect(
                PersistentTrigger.OnMentalCardUse, new[] { TestCards.Status(StatusKind.Impression, 1) }, -1, -1, 0, false);
            var a = TestStates.PlayerState(new[] { Phase1Cards.F01 }, hand: new[] { 1 }, drawPile: new int[0], hp: 30, persistents: new[] { persistent });
            var b = TestStates.PlayerState(new[] { Phase1Cards.F01 }, hand: new[] { 1 }, drawPile: new int[0], hp: 30);

            Assert.NotEmpty(LessonStateComparer.Diff(a, b));
        }

        [Fact]
        public void Describe_Fresh_ContainsInitialValues()
        {
            var state = TestStates.PlayerState(new[] { Phase1Cards.F01 }, hand: new[] { 1 }, drawPile: new int[0], hp: 30, shield: 3);

            var lines = LessonStateComparer.Describe(state);

            Assert.Contains("hp=30", lines);
            Assert.Contains("shield=3", lines);
            Assert.Contains("turn=1", lines);
            Assert.Contains("status.5=0/0", lines);
            Assert.DoesNotContain(lines, l => l.StartsWith("persistent.", System.StringComparison.Ordinal));
        }
    }
}
