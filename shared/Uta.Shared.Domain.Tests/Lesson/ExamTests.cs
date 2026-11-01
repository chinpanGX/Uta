using System;
using System.Collections.Generic;
using System.Linq;
using Uta.Shared.Domain.Lesson;
using Xunit;
using static Uta.Shared.Domain.Tests.Lesson.TestCards;

namespace Uta.Shared.Domain.Tests.Lesson
{
    public sealed class ExamTests
    {
        private const int Synthetic = 901;

        private static LessonSetup Exam(int[] percents, int earlyEnd = 0, LessonKind kind = LessonKind.MidExam)
        {
            var kinds = new[] { ParamKind.Singing, ParamKind.Rhythm, ParamKind.Expression };
            var turns = percents.Select((p, i) => new ExamTurn(kinds[i % 3], p)).ToArray();
            return new LessonSetup(kind, ParamKind.Nothing, percents.Length, 0, 0, new ExamSetup(turns, earlyEnd));
        }

        private static LessonSetup MainExam()
        {
            var turns = new[]
            {
                new ExamTurn(ParamKind.Singing, 150),
                new ExamTurn(ParamKind.Rhythm, 120),
                new ExamTurn(ParamKind.Expression, 100),
                new ExamTurn(ParamKind.Singing, 100),
            };
            return new LessonSetup(LessonKind.MidExam, ParamKind.Nothing, 4, 0, 0, new ExamSetup(turns, 0));
        }

        private static LessonEngine EngineWith(params CardDefinition[] extra)
        {
            return new LessonEngine(new TestCardCatalog(extra));
        }

        [Fact]
        public void UseCard_Exam_ConvertsParamToScore()
        {
            var state = TestStates.PlayerState(new[] { Phase1Cards.F01 }, hand: new[] { 1 }, drawPile: new int[0], hp: 30, usesLeft: 2, setup: MainExam(), turn: 1);

            Assert.Equal(LessonApplyResult.Accepted, EngineWith().Apply(state, LessonCommand.UseCard(1), TestStates.NewRandom()));

            Assert.Equal(14, state.ParamGained);
        }

        [Fact]
        public void Skip_ExamImpression_ConvertsWithTurnBonus()
        {
            var state = TestStates.PlayerState(
                new[] { Phase1Cards.F01 },
                hand: new int[0],
                drawPile: new int[0],
                hp: 20,
                setup: MainExam(),
                turn: 2,
                statuses: new Dictionary<StatusKind, int> { { StatusKind.Impression, 3 } });

            EngineWith().Apply(state, LessonCommand.Skip(), TestStates.NewRandom());

            Assert.Equal(4, state.ParamGained);
        }

        [Fact]
        public void UseCard_ExamMultiHit_ConvertsEachHit()
        {
            var card = Card(Synthetic, Plan.Free, CardType.Active, Rarity.N, false, false, Face(Consume(0), Param(5, hits: 2)), null);
            var turns = new[]
            {
                new ExamTurn(ParamKind.Singing, 130),
                new ExamTurn(ParamKind.Rhythm, 100),
                new ExamTurn(ParamKind.Expression, 100),
                new ExamTurn(ParamKind.Singing, 100),
            };
            var setup = new LessonSetup(LessonKind.MidExam, ParamKind.Nothing, 4, 0, 0, new ExamSetup(turns, 0));
            var state = TestStates.PlayerState(new[] { Synthetic }, hand: new[] { 1 }, drawPile: new int[0], hp: 30, usesLeft: 2, setup: setup, turn: 1);

            EngineWith(card).Apply(state, LessonCommand.UseCard(1), TestStates.NewRandom());

            Assert.Equal(14, state.ParamGained);
        }

        [Fact]
        public void EarlyEnd_ReachedAtTurnEnd_FinishesWithEarlyEnd()
        {
            var engine = EngineWith();
            var state = new LessonState(Exam(new[] { 200, 200, 200, 200 }, 30), TestStates.Deck(Enumerable.Repeat(Phase1Cards.F01, 9)), 30, 30);
            var random = TestStates.NewRandom();
            engine.Start(state, random);

            engine.Apply(state, LessonCommand.UseCard(state.Zones.Hand[0]), random);
            Assert.False(state.IsFinished);
            Assert.Equal(2, state.Turn);
            Assert.Equal(18, state.ParamGained);

            engine.Apply(state, LessonCommand.UseCard(state.Zones.Hand[0]), random);

            Assert.True(state.IsFinished);
            var result = LessonEngine.GetResult(state);
            Assert.Equal(36, result.Gained);
            Assert.Equal(2, result.LastTurn);
            Assert.Equal(LessonEndReason.EarlyEnd, result.Reason);
            Assert.Equal(22, result.Hp);
        }

        [Fact]
        public void EarlyEnd_OnLastTurn_ReasonIsEarlyEnd()
        {
            var engine = EngineWith();
            var state = new LessonState(Exam(new[] { 200, 200 }, 30), TestStates.Deck(Enumerable.Repeat(Phase1Cards.F01, 9)), 30, 30);
            var random = TestStates.NewRandom();
            engine.Start(state, random);

            engine.Apply(state, LessonCommand.UseCard(state.Zones.Hand[0]), random);
            engine.Apply(state, LessonCommand.UseCard(state.Zones.Hand[0]), random);

            Assert.True(state.IsFinished);
            Assert.Equal(2, state.Turn);
            Assert.Equal(LessonEndReason.EarlyEnd, LessonEngine.GetResult(state).Reason);
        }

        [Fact]
        public void EarlyEnd_ReachedMidTurn_WaitsForRemainingUses()
        {
            var card = Card(Synthetic, Plan.Free, CardType.Active, Rarity.N, false, false, Face(Consume(0), Param(40), Uses(1)), null);
            var state = TestStates.PlayerState(
                new[] { Synthetic, Phase1Cards.F01 },
                hand: new[] { 1, 2 },
                drawPile: new int[0],
                hp: 30,
                usesLeft: 1,
                setup: Exam(new[] { 100, 100, 100, 100 }, 30),
                turn: 1);
            var engine = EngineWith(card);
            var random = TestStates.NewRandom();

            engine.Apply(state, LessonCommand.UseCard(1), random);

            Assert.Equal(40, state.ParamGained);
            Assert.False(state.IsFinished);
            Assert.Equal(LessonPhase.Player, state.Phase);
            Assert.Equal(1, state.UsesLeft);

            engine.Apply(state, LessonCommand.UseCard(2), random);

            Assert.Equal(49, state.ParamGained);
            Assert.True(state.IsFinished);
            Assert.Equal(LessonEndReason.EarlyEnd, LessonEngine.GetResult(state).Reason);
        }

        [Fact]
        public void GetResult_TurnsOver()
        {
            var engine = EngineWith();
            var state = new LessonState(Exam(new[] { 100, 100 }), TestStates.Deck(Enumerable.Repeat(Phase1Cards.F01, 5)), 20, 30);
            var random = TestStates.NewRandom();
            engine.Start(state, random);

            engine.Apply(state, LessonCommand.Skip(), random);
            engine.Apply(state, LessonCommand.Skip(), random);

            var result = LessonEngine.GetResult(state);
            Assert.Equal(0, result.Gained);
            Assert.Equal(2, result.LastTurn);
            Assert.Equal(LessonEndReason.TurnsOver, result.Reason);
            Assert.Equal(24, result.Hp);
        }

        [Fact]
        public void GetResult_Perfect()
        {
            var state = TestStates.PlayerState(new[] { Phase1Cards.F01 }, hand: new[] { 1 }, drawPile: new int[0], hp: 30, paramGained: 75);

            EngineWith().Apply(state, LessonCommand.UseCard(1), TestStates.NewRandom());

            var result = LessonEngine.GetResult(state);
            Assert.Equal(80, result.Gained);
            Assert.Equal(1, result.LastTurn);
            Assert.Equal(LessonEndReason.Perfect, result.Reason);
        }

        [Fact]
        public void GetResult_NotFinished_Throws()
        {
            var engine = EngineWith();
            var state = new LessonState(TestStates.NormalLesson, TestStates.Deck(Enumerable.Repeat(Phase1Cards.F01, 9)), 30, 30);
            engine.Start(state, TestStates.NewRandom());

            Assert.Throws<InvalidOperationException>(() => LessonEngine.GetResult(state));
        }

        [Fact]
        public void Start_Exam_DoesNotThrow()
        {
            var state = new LessonState(MainExam(), TestStates.Deck(Enumerable.Repeat(Phase1Cards.F01, 9)), 30, 30);

            EngineWith().Start(state, TestStates.NewRandom());

            Assert.Equal(LessonPhase.Player, state.Phase);
            Assert.Equal(1, state.Turn);
        }

        [Fact]
        public void LessonSetup_ExamKindWithoutExam_Throws()
        {
            Assert.Throws<ArgumentException>(() => new LessonSetup(LessonKind.MidExam, ParamKind.Nothing, 4, 0, 0));
        }

        [Fact]
        public void LessonSetup_NormalWithExam_Throws()
        {
            var exam = new ExamSetup(new[] { new ExamTurn(ParamKind.Singing, 100) }, 0);

            Assert.Throws<ArgumentException>(() => new LessonSetup(LessonKind.Normal, ParamKind.Singing, 1, 40, 80, exam));
        }

        [Fact]
        public void LessonSetup_ExamMaxTurnsMismatch_Throws()
        {
            var exam = new ExamSetup(new[] { new ExamTurn(ParamKind.Singing, 100) }, 0);

            Assert.Throws<ArgumentOutOfRangeException>(() => new LessonSetup(LessonKind.FinalExam, ParamKind.Nothing, 2, 0, 0, exam));
        }

        [Fact]
        public void LessonSetup_ExamWithPerfectTarget_Throws()
        {
            var exam = new ExamSetup(new[] { new ExamTurn(ParamKind.Singing, 100) }, 0);

            Assert.Throws<ArgumentOutOfRangeException>(() => new LessonSetup(LessonKind.MidExam, ParamKind.Nothing, 1, 0, 5, exam));
        }

        [Fact]
        public void LessonSetup_ExamWithNothingTargetParam_DoesNotThrow()
        {
            var exam = new ExamSetup(new[] { new ExamTurn(ParamKind.Singing, 100) }, 0);

            var setup = new LessonSetup(LessonKind.MidExam, ParamKind.Nothing, 1, 0, 0, exam);

            Assert.True(setup.IsExam);
        }

        [Fact]
        public void LessonSetup_NormalWithNothingTargetParam_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new LessonSetup(LessonKind.Normal, ParamKind.Nothing, 6, 40, 80));
        }

        [Fact]
        public void ExamSetup_NullTurns_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new ExamSetup(null, 0));
        }

        [Fact]
        public void ExamSetup_EmptyTurns_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ExamSetup(new ExamTurn[0], 0));
        }

        [Fact]
        public void ExamSetup_NegativeEarlyEnd_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ExamSetup(new[] { new ExamTurn(ParamKind.Singing, 100) }, -1));
        }

        [Fact]
        public void ExamTurn_NothingParam_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ExamTurn(ParamKind.Nothing, 100));
        }

        [Fact]
        public void ExamTurn_NegativeBonus_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ExamTurn(ParamKind.Singing, -1));
        }

        [Fact]
        public void ExamSetup_DefaultExamTurn_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ExamSetup(new[] { default(ExamTurn) }, 0));
        }

        [Fact]
        public void Comparer_ExamSetup_DescribesExamLine()
        {
            var exam = new LessonState(MainExam(), TestStates.Deck(new[] { Phase1Cards.F01 }), 30, 30);
            var normal = new LessonState(TestStates.NormalLesson, TestStates.Deck(new[] { Phase1Cards.F01 }), 30, 30);

            Assert.Contains("exam=0;1:150,2:120,3:100,1:100", LessonStateComparer.Describe(exam));
            Assert.Contains("exam=none", LessonStateComparer.Describe(normal));
        }
    }
}
