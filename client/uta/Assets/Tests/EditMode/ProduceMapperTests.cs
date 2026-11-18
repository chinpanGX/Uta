using NUnit.Framework;
using Uta.Client.Application.Produce;
using Uta.Client.Infrastructure.Produce;
using Uta.Shared.Contracts.Produce;
using Uta.Shared.Domain.Lesson;
using Uta.Shared.Domain.Produce;

namespace Uta.Client.Tests
{
    public sealed class ProduceMapperTests
    {
        [Test]
        public void ToStateResult_MapsAllFields()
        {
            var lesson = new LessonDto(LessonKind.MidExam, ParamKind.Singing, 6, 0, 0, new[] { new ExamTurnDto(ParamKind.Rhythm, 150) },
                2, 3, 1, 0, new[] { 11, 12 }, new int[0], new int[0], new int[0], new int[0], new ParamBonusDto[0], new PersistentDto[0],
                new int[0], new[] { 900, 800, 700 }, new[] { true, false });
            var examResult = new ExamResultDto(ExamKind.Mid, 2, 1234, 1, new[] { 1000, 900, 800 }, true);
            var result = new ProduceResultResponse(ProduceEnding.Completed, ExamResultKind.FinalPassed, 5000, ProduceRank.B, 4321, 2, 300, 1500, 40, 120, 30);
            var response = TestProduce.StateResponse(
                operationNumber: 7, phase: ProducePhase.Exam, scenarioId: 2, idolId: 3, week: 9, singing: 120, rhythm: 130, expression: 140,
                hp: 25, maxHp: 30, examRetryLeft: 1, deck: new[] { new DeckCardDto(1, 101, true) }, lesson: lesson, examResult: examResult, result: result);

            var state = ProduceMapper.ToStateResult(response);

            Assert.AreEqual(7, state.OperationNumber);
            Assert.AreEqual(ProducePhase.Exam, state.Phase);
            Assert.AreEqual(2, state.ScenarioId);
            Assert.AreEqual(3, state.IdolId);
            Assert.AreEqual(130, state.GetParam(ParamKind.Rhythm));
            Assert.AreEqual(1, state.Deck[0].InstanceId);
            Assert.AreEqual(101, state.Deck[0].CardId);
            Assert.IsTrue(state.Deck[0].Enhanced);

            Assert.IsTrue(state.Lesson.IsExam);
            CollectionAssert.AreEqual(new[] { 11, 12 }, state.Lesson.Hand);
            CollectionAssert.AreEqual(new[] { true, false }, state.Lesson.UsableHand);
            Assert.AreEqual(ParamKind.Rhythm, state.Lesson.ExamTurns[0].Param);
            Assert.AreEqual(150, state.Lesson.ExamTurns[0].ScoreBonusPercent);
            CollectionAssert.AreEqual(new[] { 900, 800, 700 }, state.Lesson.NpcScores);

            Assert.AreEqual(ExamKind.Mid, state.ExamResult.Kind);
            Assert.AreEqual(2, state.ExamResult.Attempt);
            Assert.AreEqual(1234, state.ExamResult.Score);
            Assert.AreEqual(1, state.ExamResult.Rank);
            Assert.IsTrue(state.ExamResult.Passed);

            Assert.AreEqual(ProduceEndKind.Completed, state.Result.Ending);
            Assert.AreEqual(ProduceRank.B, state.Result.Rank);
            Assert.AreEqual(1500, state.Result.Gold);
            Assert.AreEqual(30, state.Result.PlayerExp);
        }

        [Test]
        public void ToStateResult_PhaseNothing_ReturnsEmpty()
        {
            var state = ProduceMapper.ToStateResult(TestProduce.StateResponse(phase: ProducePhase.Nothing));

            Assert.AreEqual(ProducePhase.Nothing, state.Phase);
            Assert.IsEmpty(state.Deck);
            Assert.IsEmpty(state.WeekActions);
            Assert.IsEmpty(state.ClassOptions);
            Assert.IsEmpty(state.CardOffers);
            Assert.IsNull(state.Lesson);
            Assert.IsNull(state.ExamResult);
            Assert.IsNull(state.Result);
        }

        [Test]
        public void ToStateResult_NullArrays_BecomeEmpty()
        {
            var lesson = new LessonDto(LessonKind.Normal, ParamKind.Singing, 6, 0, 0, null, 1, 0, 0, 0, null, null, null, null, null, null, null, null, null, null);
            var response = new ProduceStateResponse(0, ProducePhase.Lesson, 1, 1, 1, 0, 0, 0, 0, 0, 0, null, null, null, null, lesson, null, null);

            var state = ProduceMapper.ToStateResult(response);

            Assert.IsEmpty(state.Deck);
            Assert.IsEmpty(state.CardOffers);
            Assert.IsEmpty(state.Lesson.Hand);
            Assert.IsEmpty(state.Lesson.UsableHand);
            Assert.IsEmpty(state.Lesson.ExamTurns);
            Assert.IsEmpty(state.Lesson.NpcScores);
        }

        [Test]
        public void ToEndResult_MapsEndKind()
        {
            var end = ProduceMapper.ToEndResult(TestProduce.ResultResponse(ProduceEnding.Abandoned));

            Assert.AreEqual(ProduceEndKind.Abandoned, end.Ending);
        }
    }
}
