using System.Linq;
using System.Text;
using Uta.Shared.Domain.Lesson;
using Uta.Shared.Domain.Rng;
using Xunit;

namespace Uta.Shared.Domain.Tests.Lesson
{
    /// <summary>全フェーズのカード 36 枚のデッキで、同じ操作から同じ状態になること（produce.md 10.5・10.6）。</summary>
    public sealed class LessonReplayTests
    {
        private static readonly TestCardCatalog Catalog = new TestCardCatalog();
        private static readonly LessonEngine Engine = new LessonEngine(Catalog);

        private static LessonSetup NormalSetup()
        {
            return new LessonSetup(LessonKind.Normal, ParamKind.Rhythm, 7, 1000, 2000);
        }

        private static LessonSetup ExamLessonSetup()
        {
            var percents = new[] { 100, 120, 150, 200, 100, 150, 200 };
            var kinds = new[] { ParamKind.Singing, ParamKind.Rhythm, ParamKind.Expression };
            var turns = percents.Select((p, i) => new ExamTurn(kinds[i % 3], p)).ToArray();
            return new LessonSetup(LessonKind.MidExam, ParamKind.Nothing, 7, 0, 0, new ExamSetup(turns, 300));
        }

        private static int[] AllCardIds()
        {
            return Phase1Cards.AllIds
                .Concat(Phase2Cards.AllIds)
                .Concat(Phase3Cards.AllIds)
                .Concat(Phase4Cards.AllIds)
                .ToArray();
        }

        [Fact]
        public void Replay_AllIds_Are36Cards()
        {
            Assert.Equal(36, AllCardIds().Length);
            Assert.Equal(36, AllCardIds().Distinct().Count());
        }

        [Fact]
        public void Replay_SameSeedAndCommands_SameResult()
        {
            var first = RunLesson(NormalSetup(), 30, 1, 0, null).Log;
            var second = RunLesson(NormalSetup(), 30, 1, 0, null).Log;

            Assert.Equal(first, second);
        }

        [Fact]
        public void Replay_DifferentAttempt_DifferentResult()
        {
            Assert.NotEqual(RunLesson(NormalSetup(), 30, 1, 0, null).Log, RunLesson(NormalSetup(), 30, 1, 1, null).Log);
        }

        [Fact]
        public void Replay_RestoredMidway_SameResultAsContinuousRun()
        {
            // 3ターン目で保存・復元しても、通しで進めたときと同じ結果になる
            var restored = RunLesson(NormalSetup(), 30, 3, 0, 3);

            Assert.True(restored.Restored);
            Assert.Equal(RunLesson(NormalSetup(), 30, 3, 0, null).Log, restored.Log);
        }

        [Fact]
        public void Replay_Exam_SameSeedAndCommands_SameResult()
        {
            var first = RunLesson(ExamLessonSetup(), 40, 2, 0, null).Log;
            var second = RunLesson(ExamLessonSetup(), 40, 2, 0, null).Log;

            Assert.Equal(first, second);
        }

        [Fact]
        public void Replay_Exam_RestoredMidway_SameResult()
        {
            var restored = RunLesson(ExamLessonSetup(), 40, 2, 0, 3);

            Assert.True(restored.Restored);
            Assert.Equal(RunLesson(ExamLessonSetup(), 40, 2, 0, null).Log, restored.Log);
        }

        [Fact]
        public void Replay_FinalDiff_Empty()
        {
            var first = RunLesson(NormalSetup(), 30, 5, 0, null).Final;
            var second = RunLesson(NormalSetup(), 30, 5, 0, null).Final;

            Assert.Empty(LessonStateComparer.Diff(first, second));
        }

        /// <summary>
        /// 36 枚を 1 枚ずつ入れたデッキで、使えるカードがあれば手札の先頭から使い、なければスキップする。
        /// restoreAtTurn を渡すと、そのターンの入力待ちで状態と乱数を作り直してから続ける。
        /// </summary>
        private static (string Log, LessonState Final, bool Restored) RunLesson(LessonSetup setup, int hp, ulong seed, int attempt, int? restoreAtTurn)
        {
            var state = new LessonState(setup, TestStates.Deck(AllCardIds()), hp, hp);
            var random = RandomStreams.Create(seed, new RandomStreamKey(RandomStreamKind.Lesson, 3, attempt));
            var log = new StringBuilder();
            var restored = false;

            Engine.Start(state, random);
            while (!state.IsFinished)
            {
                if (restoreAtTurn.HasValue && state.Turn == restoreAtTurn.Value)
                {
                    state = TestStates.Copy(state);
                    random = new Xoshiro256StarStar(random.S0, random.S1, random.S2, random.S3, random.DrawCount);
                    restoreAtTurn = null;
                    restored = true;
                }

                var usable = state.Zones.Hand.FirstOrDefault(id => Engine.CanUse(state, id) == UseCheckResult.Ok);
                var command = usable != 0 ? LessonCommand.UseCard(usable) : LessonCommand.Skip();
                Assert.Equal(LessonApplyResult.Accepted, Engine.Apply(state, command, random));
                log.Append(string.Join("\n", LessonStateComparer.Describe(state))).Append('\n');
            }

            return (log.ToString(), state, restored);
        }
    }
}
