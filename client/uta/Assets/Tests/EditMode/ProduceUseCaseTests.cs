using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using Uta.Client.Application.Produce;
using Uta.Client.Infrastructure.Produce;
using Uta.Shared.Contracts.Produce;
using Uta.Shared.Domain.Lesson;
using Uta.Shared.Domain.Produce;

namespace Uta.Client.Tests
{
    /// <summary>手順のテスト（ADR 0022）。偽の IProduceService・本物の ProduceSessionState・偽の IPlayerUseCase。</summary>
    public sealed class ProduceUseCaseTests
    {
        private CallLog log;
        private FakeProduceService service;
        private ProduceSessionState session;
        private FakePlayerUseCase player;
        private ProduceUseCase useCase;

        [SetUp]
        public void SetUp()
        {
            log = new CallLog();
            service = new FakeProduceService(log);
            session = new ProduceSessionState();
            player = new FakePlayerUseCase(log);
            useCase = new ProduceUseCase(service, session, player);
        }

        private static ProduceStateResponse State(int operationNumber = 0, ProducePhase phase = ProducePhase.WeekSelect)
        {
            return TestProduce.StateResponse(operationNumber: operationNumber, phase: phase);
        }

        private void EnqueueState(ProduceStateResponse response)
        {
            service.StateResults.Enqueue(FakeProduceService.Ok(response));
        }

        private void EnqueueFail(VolTech.Server.Abstractions.ErrorCode code)
        {
            service.StateResults.Enqueue(FakeProduceService.Fail<ProduceStateResponse>(code));
        }

        private void StartSession(int operationNumber = 0)
        {
            session.Set(TestProduce.StateResult(operationNumber: operationNumber));
        }

        // Start

        [UnityTest]
        public IEnumerator Start_Success_SetsSession() => UniTask.ToCoroutine(async () =>
        {
            EnqueueState(State(operationNumber: 0));

            var kind = await useCase.StartAsync(1, new[] { "u1", "u2" }, 2);

            Assert.AreEqual(ProduceStartResultKind.Succeeded, kind);
            Assert.AreEqual(new[] { "Start:1:u1,u2:2" }, log.Entries.ToArray());
            Assert.IsNotNull(session.Current);
            Assert.AreEqual(ProducePhase.WeekSelect, session.Current.Phase);
        });

        [UnityTest]
        public IEnumerator Start_InvalidFormation_ReturnsKind() => UniTask.ToCoroutine(async () =>
        {
            EnqueueFail(ProduceErrors.InvalidFormation);

            Assert.AreEqual(ProduceStartResultKind.InvalidFormation, await useCase.StartAsync(1, new[] { "u1" }, 1));
            Assert.IsNull(session.Current);
        });

        [UnityTest]
        public IEnumerator Start_InvalidArgument_ReturnsKind() => UniTask.ToCoroutine(async () =>
        {
            EnqueueFail(ProduceErrors.InvalidArgument);

            Assert.AreEqual(ProduceStartResultKind.InvalidArgument, await useCase.StartAsync(1, new[] { "u1" }, 1));
        });

        [UnityTest]
        public IEnumerator Start_AlreadyInProgress_RefreshesAndReturnsKind() => UniTask.ToCoroutine(async () =>
        {
            session.Set(TestProduce.StateResult(operationNumber: 1));
            EnqueueFail(ProduceErrors.AlreadyInProgress);

            var kind = await useCase.StartAsync(1, new[] { "u1" }, 1);

            Assert.AreEqual(ProduceStartResultKind.AlreadyInProgress, kind);
            Assert.IsNull(session.Current);
            Assert.AreEqual(new[] { "Start:1:u1:1", "Refresh" }, log.Entries.ToArray());
        });

        [UnityTest]
        public IEnumerator Start_OtherFailure_ReturnsFailed() => UniTask.ToCoroutine(async () =>
        {
            EnqueueFail(ProduceErrors.StaleOperation);

            Assert.AreEqual(ProduceStartResultKind.Failed, await useCase.StartAsync(1, new[] { "u1" }, 1));
            CollectionAssert.DoesNotContain(log.Entries, "Refresh");
        });

        // Resume

        [UnityTest]
        public IEnumerator Resume_Active_SetsSessionAndReturnsResumed() => UniTask.ToCoroutine(async () =>
        {
            EnqueueState(State(operationNumber: 3));

            var kind = await useCase.ResumeAsync();

            Assert.AreEqual(ProduceResumeResultKind.Resumed, kind);
            Assert.AreEqual(3, session.Current.OperationNumber);
            Assert.AreEqual(new[] { "GetState" }, log.Entries.ToArray());
        });

        [UnityTest]
        public IEnumerator Resume_PhaseNothing_ClearsAndRefreshes() => UniTask.ToCoroutine(async () =>
        {
            StartSession();
            EnqueueState(State(phase: ProducePhase.Nothing));

            var kind = await useCase.ResumeAsync();

            Assert.AreEqual(ProduceResumeResultKind.NotInProgress, kind);
            Assert.IsNull(session.Current);
            Assert.AreEqual(new[] { "GetState", "Refresh" }, log.Entries.ToArray());
        });

        [UnityTest]
        public IEnumerator Resume_Failure_ReturnsFailed() => UniTask.ToCoroutine(async () =>
        {
            EnqueueFail(ProduceErrors.StaleOperation);

            Assert.AreEqual(ProduceResumeResultKind.Failed, await useCase.ResumeAsync());
            Assert.IsNull(session.Current);
        });

        // Abandon

        [UnityTest]
        public IEnumerator Abandon_Success_ClearsAndRefreshes() => UniTask.ToCoroutine(async () =>
        {
            StartSession();
            service.AbandonResults.Enqueue(FakeProduceService.Ok(TestProduce.ResultResponse()));

            Assert.IsTrue(await useCase.AbandonAsync());

            Assert.IsNull(session.Current);
            Assert.AreEqual(new[] { "Abandon", "Refresh" }, log.Entries.ToArray());
        });

        [UnityTest]
        public IEnumerator Abandon_NotInProgress_ClearsAndReturnsTrue() => UniTask.ToCoroutine(async () =>
        {
            StartSession();
            service.AbandonResults.Enqueue(FakeProduceService.Fail<ProduceResultResponse>(ProduceErrors.NotInProgress));

            Assert.IsTrue(await useCase.AbandonAsync());
            Assert.IsNull(session.Current);
        });

        [UnityTest]
        public IEnumerator Abandon_SessionDiscarded_ClearsAndReturnsTrue() => UniTask.ToCoroutine(async () =>
        {
            StartSession();
            service.AbandonResults.Enqueue(FakeProduceService.Fail<ProduceResultResponse>(ProduceErrors.SessionDiscarded));

            Assert.IsTrue(await useCase.AbandonAsync());
            Assert.IsNull(session.Current);
        });

        [UnityTest]
        public IEnumerator Abandon_OtherFailure_ReturnsFalse() => UniTask.ToCoroutine(async () =>
        {
            StartSession();
            service.AbandonResults.Enqueue(FakeProduceService.Fail<ProduceResultResponse>(ProduceErrors.StaleOperation));

            Assert.IsFalse(await useCase.AbandonAsync());
            Assert.IsNotNull(session.Current);
        });

        [UnityTest]
        public IEnumerator Abandon_RefreshFails_StillTrue() => UniTask.ToCoroutine(async () =>
        {
            StartSession();
            service.AbandonResults.Enqueue(FakeProduceService.Ok(TestProduce.ResultResponse()));
            player.RefreshResults.Enqueue(() => throw new InvalidOperationException("sign-in failed: x"));

            Assert.IsTrue(await useCase.AbandonAsync());
            Assert.IsNull(session.Current);
        });

        // 操作

        [UnityTest]
        public IEnumerator ChooseWeekAction_Success_SetsSessionAndPassesOperationNumber() => UniTask.ToCoroutine(async () =>
        {
            StartSession(operationNumber: 4);
            EnqueueState(State(operationNumber: 5));

            var kind = await useCase.ChooseWeekActionAsync(1, -1, ParamKind.Rhythm);

            Assert.AreEqual(ProduceStepResultKind.Succeeded, kind);
            Assert.AreEqual(new[] { "ChooseWeekAction:4,1,-1,Rhythm" }, log.Entries.ToArray());
            Assert.AreEqual(5, session.Current.OperationNumber);
            Assert.AreEqual(4, session.Previous.OperationNumber);
        });

        [UnityTest]
        public IEnumerator AllOperations_MapToService() => UniTask.ToCoroutine(async () =>
        {
            StartSession();
            for (var i = 0; i < 7; i++)
            {
                EnqueueState(State());
            }

            await useCase.ChooseWeekActionAsync(0, 1, ParamKind.Singing);
            await useCase.PickCardOfferAsync(2);
            await useCase.DeclineCardOfferAsync();
            await useCase.DecideExamRetryAsync(true);
            await useCase.UseCardAsync(7);
            await useCase.UseDrinkAsync(1);
            await useCase.SkipLessonTurnAsync();

            Assert.AreEqual(
                new[]
                {
                    "ChooseWeekAction:0,0,1,Singing",
                    "ResolveCardOffer:0,Pick,2",
                    "ResolveCardOffer:0,Decline,0",
                    "DecideExamRetry:0,True",
                    "ExecuteLessonAction:0,UseCard,7,0",
                    "ExecuteLessonAction:0,UseDrink,0,1",
                    "ExecuteLessonAction:0,Skip,0,0",
                },
                log.Entries.ToArray());
        });

        [UnityTest]
        public IEnumerator Operation_Finished_RefreshesPlayer() => UniTask.ToCoroutine(async () =>
        {
            StartSession();
            EnqueueState(TestProduce.StateResponse(phase: ProducePhase.Finished, result: TestProduce.ResultResponse(ProduceEnding.Completed)));

            var kind = await useCase.SkipLessonTurnAsync();

            Assert.AreEqual(ProduceStepResultKind.Succeeded, kind);
            Assert.AreEqual(new[] { "ExecuteLessonAction:0,Skip,0,0", "Refresh" }, log.Entries.ToArray());
            Assert.AreEqual(ProduceEndKind.Completed, session.Current.Result.Ending);
        });

        [UnityTest]
        public IEnumerator Operation_Stale_RecoversWithGetState() => UniTask.ToCoroutine(async () =>
        {
            StartSession();
            EnqueueFail(ProduceErrors.StaleOperation);
            EnqueueState(State(operationNumber: 9, phase: ProducePhase.Lesson));

            var kind = await useCase.SkipLessonTurnAsync();

            Assert.AreEqual(ProduceStepResultKind.Recovered, kind);
            Assert.AreEqual(new[] { "ExecuteLessonAction:0,Skip,0,0", "GetState" }, log.Entries.ToArray());
            Assert.AreEqual(9, session.Current.OperationNumber);
        });

        [UnityTest]
        public IEnumerator Operation_UnexpectedPhase_RecoversWithGetState() => UniTask.ToCoroutine(async () =>
        {
            StartSession();
            EnqueueFail(ProduceErrors.UnexpectedPhase);
            EnqueueState(State(operationNumber: 9, phase: ProducePhase.Lesson));

            var kind = await useCase.SkipLessonTurnAsync();

            Assert.AreEqual(ProduceStepResultKind.Recovered, kind);
            Assert.AreEqual(9, session.Current.OperationNumber);
        });

        [UnityTest]
        public IEnumerator Operation_StaleThenGetStateNothing_SessionEnded() => UniTask.ToCoroutine(async () =>
        {
            StartSession();
            EnqueueFail(ProduceErrors.StaleOperation);
            EnqueueState(State(phase: ProducePhase.Nothing));

            var kind = await useCase.SkipLessonTurnAsync();

            Assert.AreEqual(ProduceStepResultKind.SessionEnded, kind);
            Assert.IsNull(session.Current);
            Assert.AreEqual("Refresh", log.Entries[log.Entries.Count - 1]);
        });

        [UnityTest]
        public IEnumerator Operation_StaleThenGetStateFails_Failed() => UniTask.ToCoroutine(async () =>
        {
            StartSession();
            EnqueueFail(ProduceErrors.StaleOperation);
            EnqueueFail(ProduceErrors.StaleOperation);

            Assert.AreEqual(ProduceStepResultKind.Failed, await useCase.SkipLessonTurnAsync());
            Assert.IsNotNull(session.Current);
        });

        [UnityTest]
        public IEnumerator Operation_SessionDiscarded_ClearsAndSessionEnded() => UniTask.ToCoroutine(async () =>
        {
            StartSession();
            EnqueueFail(ProduceErrors.SessionDiscarded);

            Assert.AreEqual(ProduceStepResultKind.SessionEnded, await useCase.SkipLessonTurnAsync());
            Assert.IsNull(session.Current);
            Assert.AreEqual("Refresh", log.Entries[log.Entries.Count - 1]);
        });

        [UnityTest]
        public IEnumerator Operation_NotInProgress_ClearsAndSessionEnded() => UniTask.ToCoroutine(async () =>
        {
            StartSession();
            EnqueueFail(ProduceErrors.NotInProgress);

            Assert.AreEqual(ProduceStepResultKind.SessionEnded, await useCase.SkipLessonTurnAsync());
            Assert.IsNull(session.Current);
        });

        [UnityTest]
        public IEnumerator Operation_InvalidArgument_Rejected() => UniTask.ToCoroutine(async () =>
        {
            StartSession();
            EnqueueFail(ProduceErrors.InvalidArgument);

            Assert.AreEqual(ProduceStepResultKind.Rejected, await useCase.SkipLessonTurnAsync());
            Assert.IsNotNull(session.Current);
        });

        [UnityTest]
        public IEnumerator Operation_OtherFailure_Failed() => UniTask.ToCoroutine(async () =>
        {
            StartSession();
            EnqueueFail(ProduceErrors.InvalidFormation);

            Assert.AreEqual(ProduceStepResultKind.Failed, await useCase.SkipLessonTurnAsync());
        });

        [UnityTest]
        public IEnumerator Operation_NoSession_Throws() => UniTask.ToCoroutine(async () =>
        {
            try
            {
                await useCase.SkipLessonTurnAsync();
                Assert.Fail("例外が投げられなかった");
            }
            catch (InvalidOperationException)
            {
            }

            Assert.IsEmpty(log.Entries);
        });

        // 状態の型への書き込み

        [Test]
        public void MarkExamResultShown_SetsFlag()
        {
            StartSession();

            useCase.MarkExamResultShown();

            Assert.IsTrue(session.ExamResultShown);
        }

        [Test]
        public void ClearSession_ClearsSession()
        {
            StartSession();

            useCase.ClearSession();

            Assert.IsNull(session.Current);
            Assert.IsEmpty(log.Entries);
        }
    }
}
