using System;
using System.Collections.Generic;
using System.Linq;
using Uta.Shared.Domain.Lesson;
using Uta.Shared.Domain.Produce;
using Uta.Shared.Domain.Rng;
using Xunit;

namespace Uta.Shared.Domain.Tests.Produce
{
    public class ProduceEngineExamTests
    {
        private static readonly ProduceCatalog DefaultCatalog = TestProduceMaster.Create();

        private static ProduceStartInput Input(int scenario = 1, int idol = 1)
        {
            return new ProduceStartInput(scenario, idol, 1, new SupportCardInput[0]);
        }

        // シナリオ 1 の週 3 の RivalBaseScore を差し替える。retryCount が負でなければシナリオ 1 の RetryCount、paramCap が正ならシナリオ 1 の ParamCap も差し替える
        private static ProduceCatalog ModifyScenario1(int rival, int retryCount = -1, int paramCap = 0)
        {
            var rows = TestProduceMaster.LoadRows();
            rows.ScenarioWeeks = rows.ScenarioWeeks
                .Select(w => w.ScenarioId == 1 && w.Week == 3
                    ? new ScenarioWeekRow(w.ScenarioId, w.Week, w.Kind, w.TurnsSense, w.TurnsLogic, w.ClearTarget, w.PerfectTarget, w.ExamEarlyEndScore, rival)
                    : w)
                .ToArray();
            rows.Scenarios = rows.Scenarios
                .Select(s => s.ScenarioId == 1
                    ? new ScenarioRow(s.ScenarioId, s.Name, s.Difficulty, s.WeekCount, paramCap > 0 ? paramCap : s.ParamCap, retryCount >= 0 ? retryCount : s.RetryCount)
                    : s)
                .ToArray();
            return TestProduceMaster.Create(rows);
        }

        private static ProduceCatalog ModifyScenario2MidRival(int rival)
        {
            var rows = TestProduceMaster.LoadRows();
            rows.ScenarioWeeks = rows.ScenarioWeeks
                .Select(w => w.ScenarioId == 2 && w.Week == 6
                    ? new ScenarioWeekRow(w.ScenarioId, w.Week, w.Kind, w.TurnsSense, w.TurnsLogic, w.ClearTarget, w.PerfectTarget, w.ExamEarlyEndScore, rival)
                    : w)
                .ToArray();
            return TestProduceMaster.Create(rows);
        }

        private static void Accept(ProduceEngine engine, ProduceState state, TestRandomStreamSource streams, ProduceCommand command)
        {
            Assert.Equal(ProduceApplyResult.Accepted, engine.Apply(state, command, streams));
        }

        private static void SkipWhile(ProduceEngine engine, ProduceState state, TestRandomStreamSource streams, ProducePhase phase)
        {
            while (state.Phase == phase)
            {
                Accept(engine, state, streams, ProduceCommand.Skip());
            }
        }

        // シナリオ 1・idol 1 で、スキップだけで Exam まで進める
        private static ProduceState SkipToExam(ProduceEngine engine, TestRandomStreamSource streams)
        {
            var state = engine.Start(Input(), streams);
            Accept(engine, state, streams, ProduceCommand.ChooseLesson(0, ParamKind.Singing));
            SkipWhile(engine, state, streams, ProducePhase.Lesson);
            Accept(engine, state, streams, ProduceCommand.ChooseLesson(0, ParamKind.Singing));
            SkipWhile(engine, state, streams, ProducePhase.Lesson);
            Assert.Equal(ProducePhase.Exam, state.Phase);
            return state;
        }

        // 試験をスキップだけで終える（Exam でなくなるまで）
        private static void SkipExam(ProduceEngine engine, ProduceState state, TestRandomStreamSource streams)
        {
            SkipWhile(engine, state, streams, ProducePhase.Exam);
        }

        [Fact]
        public void Exam_FinalWeek_StartsAutomaticallyAfterFinishing()
        {
            var engine = new ProduceEngine(DefaultCatalog);
            var streams = new TestRandomStreamSource(1);
            var state = SkipToExam(engine, streams);

            Assert.Equal(ProducePhase.Exam, state.Phase);
            Assert.Equal(3, state.Week);
            Assert.Equal(0, state.Attempt);
            Assert.Equal(LessonKind.FinalExam, state.Lesson.Setup.Kind);
            Assert.Equal(12, state.Lesson.Setup.Exam.Turns.Count);
            Assert.Equal(0, state.Lesson.Setup.Exam.EarlyEndScore);
            Assert.NotNull(state.ExamBackup);
            Assert.Equal(new[] { 30, 20, 10 }, state.ExamBackup.Parameters.ToArray());
            Assert.Equal(30, state.ExamBackup.Hp);
            Assert.Equal(9, state.ExamBackup.Deck.Count);
            Assert.Equal(10, state.ExamBackup.NextInstanceId);
            Assert.Contains(streams.Requests, k => k.Equals(new RandomStreamKey(RandomStreamKind.Lesson, 3, 0)));
        }

        [Fact]
        public void Exam_ScoreBonusUsesParamsAtStart()
        {
            var engine = new ProduceEngine(DefaultCatalog);
            var streams = new TestRandomStreamSource(1);
            var state = SkipToExam(engine, streams);
            var idol = DefaultCatalog.GetIdol(1);
            var values = new[] { state.GetParam(ParamKind.Singing), state.GetParam(ParamKind.Rhythm), state.GetParam(ParamKind.Expression) };

            var turns = state.Lesson.Setup.Exam.Turns;
            for (var i = 0; i < turns.Count; i++)
            {
                var rank = 1;
                while (rank <= 3 && idol.GetExamParam(rank) != turns[i].Param)
                {
                    rank++;
                }

                Assert.True(rank <= 3);
                var expected = ProduceFormulas.ScoreBonusPercent(DefaultCatalog, values[ProduceParams.ToIndex(idol.GetExamParam(rank))], rank);
                Assert.Equal(expected, turns[i].ScoreBonusPercent);
            }
        }

        [Fact]
        public void Exam_SkipOnly_RivalHigh_GoesToExamResult()
        {
            var engine = new ProduceEngine(DefaultCatalog);
            var streams = new TestRandomStreamSource(1);
            var state = SkipToExam(engine, streams);
            SkipExam(engine, state, streams);

            Assert.Equal(ProducePhase.ExamResult, state.Phase);
            Assert.Null(state.Lesson);
            Assert.Equal(ExamKind.Final, state.LastExam.Kind);
            Assert.Equal(0, state.LastExam.Attempt);
            Assert.Equal(0, state.LastExam.Score);
            Assert.Equal(6, state.LastExam.Rank);
            Assert.False(state.LastExam.Passed);
            Assert.Equal(new[] { 1800, 1462, 1125, 787, 450 }, state.LastExam.NpcScores.ToArray());
            Assert.Equal(3, state.ExamRetryLeft);
            Assert.NotNull(state.ExamBackup);
            Assert.Null(state.Result);
        }

        [Fact]
        public void Exam_Retry_RestoresAndChangesStream()
        {
            var engine = new ProduceEngine(DefaultCatalog);
            var streams = new TestRandomStreamSource(1);
            var state = SkipToExam(engine, streams);
            SkipExam(engine, state, streams);
            var oldLesson = state.Lesson;

            Accept(engine, state, streams, ProduceCommand.ExamRetry());

            Assert.Equal(ProducePhase.Exam, state.Phase);
            Assert.Equal(1, state.Attempt);
            Assert.Equal(2, state.ExamRetryLeft);
            Assert.NotNull(state.Lesson);
            Assert.NotSame(oldLesson, state.Lesson);
            Assert.Equal(12, state.Lesson.Setup.Exam.Turns.Count);
            Assert.Equal(state.ExamBackup.Parameters[0], state.GetParam(ParamKind.Singing));
            Assert.Equal(state.ExamBackup.Parameters[1], state.GetParam(ParamKind.Rhythm));
            Assert.Equal(state.ExamBackup.Parameters[2], state.GetParam(ParamKind.Expression));
            Assert.Equal(state.ExamBackup.Hp, state.Hp);
            Assert.Contains(streams.Requests, k => k.Equals(new RandomStreamKey(RandomStreamKind.Lesson, 3, 1)));
            Assert.Equal(0, state.Operations.Last().Attempt);
            Assert.Equal(OperationKind.ExamRetry, state.Operations.Last().Command.Kind);
        }

        [Fact]
        public void Exam_NoRetry_Fail_FinishesWithEvaluation()
        {
            var engine = new ProduceEngine(DefaultCatalog);
            var streams = new TestRandomStreamSource(1);
            var state = SkipToExam(engine, streams);
            SkipExam(engine, state, streams);

            Accept(engine, state, streams, ProduceCommand.ExamNoRetry());

            Assert.Equal(ProducePhase.Finished, state.Phase);
            Assert.Equal(ExamOutcome.FinalFailed, state.Result.Outcome);
            Assert.Equal(6, state.Result.FinalRank);
            Assert.Equal(0, state.Result.FinalScore);
            Assert.Equal(138, state.Result.EvaluationValue);
            Assert.Equal(ProduceRank.F, state.Result.Rank);
            Assert.Null(state.ExamBackup);
            Assert.Null(state.Lesson);
        }

        [Fact]
        public void Exam_RetriesExhausted_FinishesWithoutResultPhase()
        {
            var engine = new ProduceEngine(DefaultCatalog);
            var streams = new TestRandomStreamSource(1);
            var state = SkipToExam(engine, streams);
            for (var i = 0; i < 3; i++)
            {
                SkipExam(engine, state, streams);
                Assert.Equal(ProducePhase.ExamResult, state.Phase);
                Accept(engine, state, streams, ProduceCommand.ExamRetry());
            }

            SkipExam(engine, state, streams);

            Assert.Equal(0, state.ExamRetryLeft);
            Assert.Equal(ProducePhase.Finished, state.Phase);
            Assert.Equal(ExamOutcome.FinalFailed, state.Result.Outcome);
            Assert.Equal(3, state.LastExam.Attempt);
            Assert.Equal(0, state.Attempt);
        }

        [Fact]
        public void Exam_Pass_StillOffersRetry_ThenNoRetryFinishes()
        {
            var catalog = ModifyScenario1(1);
            var engine = new ProduceEngine(catalog);
            var streams = new TestRandomStreamSource(1);
            var state = SkipToExam(engine, streams);
            SkipExam(engine, state, streams);

            Assert.Equal(ProducePhase.ExamResult, state.Phase);
            Assert.Equal(1, state.LastExam.Rank);
            Assert.True(state.LastExam.Passed);

            Accept(engine, state, streams, ProduceCommand.ExamNoRetry());

            Assert.Equal(ProducePhase.Finished, state.Phase);
            Assert.Equal(ExamOutcome.FinalPassed, state.Result.Outcome);
            Assert.Equal(60, state.GetParam(ParamKind.Singing));
            Assert.Equal(50, state.GetParam(ParamKind.Rhythm));
            Assert.Equal(40, state.GetParam(ParamKind.Expression));
            Assert.Equal(1, state.Result.FinalRank);
            Assert.Equal(2045, state.Result.EvaluationValue);
            Assert.Equal(ProduceRank.D, state.Result.Rank);
        }

        [Fact]
        public void Exam_Pass_NoRetryLeft_FinishesImmediately()
        {
            var catalog = ModifyScenario1(1, retryCount: 0);
            var engine = new ProduceEngine(catalog);
            var streams = new TestRandomStreamSource(1);
            var state = SkipToExam(engine, streams);
            SkipExam(engine, state, streams);

            Assert.Equal(ProducePhase.Finished, state.Phase);
            Assert.Equal(ExamOutcome.FinalPassed, state.Result.Outcome);
        }

        [Fact]
        public void Exam_Pass_RankRewardCappedByParamCap()
        {
            var catalog = ModifyScenario1(1, paramCap: 40);
            var engine = new ProduceEngine(catalog);
            var streams = new TestRandomStreamSource(1);
            var state = SkipToExam(engine, streams);
            SkipExam(engine, state, streams);

            Accept(engine, state, streams, ProduceCommand.ExamNoRetry());

            Assert.Equal(40, state.GetParam(ParamKind.Singing));
            Assert.Equal(40, state.GetParam(ParamKind.Rhythm));
            Assert.Equal(40, state.GetParam(ParamKind.Expression));
            Assert.Equal(120 * 2300 / 1000 + 1700, state.Result.EvaluationValue);
            Assert.Equal(ProduceRank.E, state.Result.Rank);
        }

        private static ProduceState ToMidExam(ProduceEngine engine, ProduceCatalog catalog, TestRandomStreamSource streams)
        {
            var state = engine.Start(Input(2, 1), streams);
            ProduceBot.RunUntil(engine, catalog, state, streams, s => s.Phase == ProducePhase.Exam && s.Week == 6, playCards: false, retryExam: false);
            Assert.Equal(ProducePhase.Exam, state.Phase);
            Assert.Equal(6, state.Week);
            return state;
        }

        [Fact]
        public void Exam_Mid_Pass_GoesToNextWeekWithReward()
        {
            var catalog = ModifyScenario2MidRival(1);
            var engine = new ProduceEngine(catalog);
            var streams = new TestRandomStreamSource(1);
            var state = ToMidExam(engine, catalog, streams);

            Assert.Equal(LessonKind.MidExam, state.Lesson.Setup.Kind);
            Assert.Equal(9, state.Lesson.Setup.Exam.Turns.Count);
            Assert.Equal(900, state.Lesson.Setup.Exam.EarlyEndScore);
            var before = state.ExamBackup.Parameters.ToArray();

            for (var i = 0; i < 9; i++)
            {
                Accept(engine, state, streams, ProduceCommand.Skip());
            }

            Assert.Equal(ProducePhase.ExamResult, state.Phase);
            Assert.True(state.LastExam.Passed);
            Assert.Equal(1, state.LastExam.Rank);

            Accept(engine, state, streams, ProduceCommand.ExamNoRetry());

            Assert.Equal(ProducePhase.WeekSelect, state.Phase);
            Assert.Equal(7, state.Week);
            Assert.Equal(Math.Min(1000, before[0] + 20), state.GetParam(ParamKind.Singing));
            Assert.Equal(Math.Min(1000, before[1] + 20), state.GetParam(ParamKind.Rhythm));
            Assert.Equal(Math.Min(1000, before[2] + 20), state.GetParam(ParamKind.Expression));
            Assert.Same(state.LastExam, state.MidExam);
            Assert.Null(state.ExamBackup);
        }

        [Fact]
        public void Exam_Mid_Fail_FinishesWithMidFailed()
        {
            var catalog = ModifyScenario2MidRival(100000);
            var engine = new ProduceEngine(catalog);
            var streams = new TestRandomStreamSource(1);
            var state = ToMidExam(engine, catalog, streams);

            SkipExam(engine, state, streams);
            Assert.Equal(ProducePhase.ExamResult, state.Phase);
            Accept(engine, state, streams, ProduceCommand.ExamNoRetry());

            Assert.Equal(ProducePhase.Finished, state.Phase);
            Assert.Equal(ExamOutcome.MidFailed, state.Result.Outcome);
            Assert.Equal(0, state.Result.FinalRank);
            Assert.Equal(0, state.Result.FinalScore);
            var sum = state.GetParam(ParamKind.Singing) + state.GetParam(ParamKind.Rhythm) + state.GetParam(ParamKind.Expression);
            Assert.Equal(sum * 2300 / 1000, state.Result.EvaluationValue);
            Assert.Equal(ProduceRank.F, state.Result.Rank);
        }

        [Fact]
        public void Exam_WrongPhaseCommands_Rejected()
        {
            var engine = new ProduceEngine(DefaultCatalog);
            var streams = new TestRandomStreamSource(1);
            var state = SkipToExam(engine, streams);

            AssertRejected(engine, state, streams, ProduceCommand.ExamRetry(), ProduceApplyResult.UnexpectedPhase);

            SkipExam(engine, state, streams);
            Assert.Equal(ProducePhase.ExamResult, state.Phase);
            AssertRejected(engine, state, streams, ProduceCommand.UseCard(1), ProduceApplyResult.UnexpectedPhase);
            AssertRejected(engine, state, streams, ProduceCommand.Skip(), ProduceApplyResult.UnexpectedPhase);
            AssertRejected(engine, state, streams, ProduceCommand.OfferPick(0), ProduceApplyResult.UnexpectedPhase);
            AssertRejected(engine, state, streams, ProduceCommand.ChooseOther(0), ProduceApplyResult.UnexpectedPhase);

            Accept(engine, state, streams, ProduceCommand.ExamNoRetry());
            Assert.Equal(ProducePhase.Finished, state.Phase);
            AssertRejected(engine, state, streams, ProduceCommand.Skip(), ProduceApplyResult.UnexpectedPhase);
        }

        private static void AssertRejected(ProduceEngine engine, ProduceState state, TestRandomStreamSource streams, ProduceCommand command, ProduceApplyResult expected)
        {
            var before = ProduceStateComparer.Describe(state);
            Assert.Equal(expected, engine.Apply(state, command, streams));
            Assert.Equal(before, ProduceStateComparer.Describe(state));
        }

        [Fact]
        public void Exam_HpFromExamResult()
        {
            var engine = new ProduceEngine(DefaultCatalog);
            var streams = new TestRandomStreamSource(1);
            var state = SkipToExam(engine, streams);
            var lesson = state.Lesson;

            SkipExam(engine, state, streams);

            Assert.Equal(LessonEngine.GetResult(lesson).Hp, state.Hp);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        public void Exam_PrecededByFinishing_DistributesAndRecovers(int seed)
        {
            var engine = new ProduceEngine(DefaultCatalog);
            var streams = new TestRandomStreamSource((ulong)seed);
            var idol = DefaultCatalog.GetIdol(1);
            var state = engine.Start(Input(), streams);
            Accept(engine, state, streams, ProduceCommand.ChooseLesson(0, ParamKind.Singing));
            SkipWhile(engine, state, streams, ProducePhase.Lesson);
            Assert.Equal(2, state.Week);

            var before = new[] { state.GetParam(ParamKind.Singing), state.GetParam(ParamKind.Rhythm), state.GetParam(ParamKind.Expression) };
            Accept(engine, state, streams, ProduceCommand.ChooseLesson(0, ParamKind.Singing));
            var lesson = state.Lesson;
            while (state.Phase == ProducePhase.Lesson)
            {
                Accept(engine, state, streams, ProduceBot.Next(engine, DefaultCatalog, state, true));
            }

            var result = LessonEngine.GetResult(lesson);
            var alloc = ProduceFormulas.DistributeGain(true, ParamKind.Singing, result.Gained, 75, idol.FirstParam);
            for (var i = 0; i < 3; i++)
            {
                var param = ProduceParams.FromIndex(i);
                var expected = Math.Min(1000, before[i] + alloc[i] + ProduceFormulas.BonusAmount(alloc[i], state.GetBonusPerThousand(param)));
                Assert.Equal(expected, state.ExamBackup.Parameters[i]);
            }

            var hp = result.Hp;
            if (result.Reason == LessonEndReason.Perfect)
            {
                hp = ProduceFormulas.Heal(hp, ProduceFormulas.PerfectRecover(lesson.Setup.MaxTurns, result.LastTurn), state.MaxHp);
            }

            hp = ProduceFormulas.Heal(hp, ProduceFormulas.RestAmount(state.MaxHp), state.MaxHp);
            Assert.Equal(hp, state.ExamBackup.Hp);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        public void Exam_BotPlay_Scenario1_Finishes(int seed)
        {
            var engine = new ProduceEngine(DefaultCatalog);
            var streams = new TestRandomStreamSource((ulong)seed);
            var state = engine.Start(Input(), streams);

            ProduceBot.Run(engine, DefaultCatalog, state, streams, playCards: true, retryExam: false);

            Assert.Equal(ProducePhase.Finished, state.Phase);
            Assert.NotNull(state.Result);
        }
    }
}
