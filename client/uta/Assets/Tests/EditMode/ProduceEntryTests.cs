using System;
using System.Collections;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using Uta.Client.Application.Produce;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Produce;
using Uta.Shared.Domain.Produce;

namespace Uta.Client.Tests
{
    public sealed class ProduceEntryTests
    {
        private CallLog log;
        private ProduceSessionState session;

        [SetUp]
        public void SetUp()
        {
            log = new CallLog();
            session = new ProduceSessionState();
        }

        private ProduceEntry CreateEntry()
        {
            var navigator = new FakeScreenNavigator(log);
            var dialogs = new FakeDialogOpener(log);
            var scenes = new FakeSceneNavigator(log);
            var flow = new ProduceFlow(session, navigator, new PageStack(() => 1, () => 0), dialogs, scenes);
            return new ProduceEntry(navigator, session, flow, dialogs, scenes);
        }

        [UnityTest]
        public IEnumerator Start_Active_PushesBackdropThenShowsCurrent() => UniTask.ToCoroutine(async () =>
        {
            session.Set(TestProduce.StateResult(ProducePhase.WeekSelect));
            var entry = CreateEntry();

            await entry.StartAsync();

            CollectionAssert.AreEqual(
                new[] { "PushPage:ProduceBackdropPresenter", "Info:この局面の画面は準備中です: WeekSelect", "GoToHome" },
                log.Entries);
        });

        private sealed class ThrowingProduceFlow : ProduceFlow
        {
            public ThrowingProduceFlow(ProduceSessionState session, FakeScreenNavigator navigator, FakeDialogOpener dialogs, FakeSceneNavigator scenes)
                : base(session, navigator, new PageStack(() => 1, () => 0), dialogs, scenes)
            {
            }

            public override UniTask ShowCurrentAsync()
            {
                throw new InvalidOperationException("boom");
            }
        }

        [UnityTest]
        public IEnumerator Start_Exception_ShowsErrorAndGoesHome() => UniTask.ToCoroutine(async () =>
        {
            LogAssert.Expect(UnityEngine.LogType.Exception, new Regex("boom"));
            session.Set(TestProduce.StateResult(ProducePhase.WeekSelect));
            var navigator = new FakeScreenNavigator(log);
            var dialogs = new FakeDialogOpener(log);
            var scenes = new FakeSceneNavigator(log);
            var flow = new ThrowingProduceFlow(session, navigator, dialogs, scenes);
            var entry = new ProduceEntry(navigator, session, flow, dialogs, scenes);

            await entry.StartAsync();

            CollectionAssert.AreEqual(
                new[] { "PushPage:ProduceBackdropPresenter", "Info:エラーが発生しました: boom", "GoToHome" },
                log.Entries);
        });

        [UnityTest]
        public IEnumerator Start_NoSession_GoesHome() => UniTask.ToCoroutine(async () =>
        {
            LogAssert.Expect(UnityEngine.LogType.Warning, "[Uta] produce: no active session");
            var entry = CreateEntry();

            await entry.StartAsync();

            CollectionAssert.AreEqual(new[] { "GoToHome" }, log.Entries);
        });

        [UnityTest]
        public IEnumerator Start_Finished_GoesHome() => UniTask.ToCoroutine(async () =>
        {
            LogAssert.Expect(UnityEngine.LogType.Warning, "[Uta] produce: no active session");
            session.Set(TestProduce.StateResult(ProducePhase.Finished));
            var entry = CreateEntry();

            await entry.StartAsync();

            CollectionAssert.AreEqual(new[] { "GoToHome" }, log.Entries);
        });
    }
}
