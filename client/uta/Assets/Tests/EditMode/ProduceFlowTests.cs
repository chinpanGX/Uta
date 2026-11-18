using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using Uta.Client.Application.Produce;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Produce;
using Uta.Shared.Domain.Produce;

namespace Uta.Client.Tests
{
    public sealed class ProduceFlowTests
    {
        private const string NotReady = "Info:この局面の画面は準備中です: WeekSelect";

        private CallLog log;
        private ProduceSessionState session;

        [SetUp]
        public void SetUp()
        {
            log = new CallLog();
            session = new ProduceSessionState();
        }

        private ProduceFlow CreateFlow(int pages, int modals)
        {
            return new ProduceFlow(session, new FakeScreenNavigator(log), new PageStack(() => pages, () => modals), new FakeDialogOpener(log), new FakeSceneNavigator(log));
        }

        [UnityTest]
        public IEnumerator ShowCurrent_UnsupportedPhase_PopsToBackdropThenShowsInfoAndGoesHome() => UniTask.ToCoroutine(async () =>
        {
            session.Set(TestProduce.StateResult(ProducePhase.WeekSelect));
            var flow = CreateFlow(2, 0);

            await flow.ShowCurrentAsync();

            CollectionAssert.AreEqual(new[] { "PopPage:1", NotReady, "GoToHome" }, log.Entries);
        });

        [UnityTest]
        public IEnumerator ShowCurrent_ModalOpen_PopsModalFirst() => UniTask.ToCoroutine(async () =>
        {
            session.Set(TestProduce.StateResult(ProducePhase.WeekSelect));
            var flow = CreateFlow(1, 1);

            await flow.ShowCurrentAsync();

            CollectionAssert.AreEqual(new[] { "PopModal:1", NotReady, "GoToHome" }, log.Entries);
        });

        [UnityTest]
        public IEnumerator ShowCurrent_NoSession_Throws() => UniTask.ToCoroutine(async () =>
        {
            var flow = CreateFlow(1, 0);
            InvalidOperationException caught = null;

            try
            {
                await flow.ShowCurrentAsync();
            }
            catch (InvalidOperationException e)
            {
                caught = e;
            }

            Assert.IsNotNull(caught);
        });

        [UnityTest]
        public IEnumerator ExitToHome_GoesHomeWithoutPop() => UniTask.ToCoroutine(async () =>
        {
            var flow = CreateFlow(3, 1);

            await flow.ExitToHomeAsync();

            CollectionAssert.AreEqual(new[] { "GoToHome" }, log.Entries);
        });

        [UnityTest]
        public IEnumerator HandleStep_SessionEnded_ShowsInfoThenGoesHome() => UniTask.ToCoroutine(async () =>
        {
            var flow = CreateFlow(1, 0);

            await flow.HandleStepAsync(ProduceStepResultKind.SessionEnded);

            CollectionAssert.AreEqual(new[] { "Info:プロデュースは終了しています。", "GoToHome" }, log.Entries);
        });

        [UnityTest]
        public IEnumerator HandleStep_Rejected_ShowsInfo() => UniTask.ToCoroutine(async () =>
        {
            var flow = CreateFlow(1, 0);

            await flow.HandleStepAsync(ProduceStepResultKind.Rejected);

            CollectionAssert.AreEqual(new[] { "Info:その操作はできません。" }, log.Entries);
        });

        [UnityTest]
        public IEnumerator HandleStep_Failed_DoesNothing() => UniTask.ToCoroutine(async () =>
        {
            var flow = CreateFlow(1, 0);

            await flow.HandleStepAsync(ProduceStepResultKind.Failed);

            Assert.IsEmpty(log.Entries);
        });

        [UnityTest]
        public IEnumerator HandleStep_Succeeded_ShowCurrent() => UniTask.ToCoroutine(async () =>
        {
            session.Set(TestProduce.StateResult(ProducePhase.WeekSelect));
            var flow = CreateFlow(2, 0);

            await flow.HandleStepAsync(ProduceStepResultKind.Succeeded);

            CollectionAssert.AreEqual(new[] { "PopPage:1", NotReady, "GoToHome" }, log.Entries);
        });

        [UnityTest]
        public IEnumerator HandleStep_Recovered_ShowCurrent() => UniTask.ToCoroutine(async () =>
        {
            session.Set(TestProduce.StateResult(ProducePhase.WeekSelect));
            var flow = CreateFlow(2, 0);

            await flow.HandleStepAsync(ProduceStepResultKind.Recovered);

            CollectionAssert.AreEqual(new[] { "PopPage:1", NotReady, "GoToHome" }, log.Entries);
        });
    }
}
