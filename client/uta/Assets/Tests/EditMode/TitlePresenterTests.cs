using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using Uta.Client.Presentation.Title;

namespace Uta.Client.Tests
{
    public sealed class TitlePresenterTests
    {
        private sealed class FakeTitleView : ITitleView
        {
            private readonly CallLog log;

            public FakeTitleView(CallLog log)
            {
                this.log = log;
            }

            public event Action StartClicked;

            public void SetStatus(string text)
            {
                log.Add("SetStatus:" + text);
            }

            public void SetStartInteractable(bool interactable)
            {
                log.Add("SetStartInteractable:" + interactable);
            }

            public void RaiseStartClicked()
            {
                StartClicked?.Invoke();
            }
        }

        private CallLog log;
        private FakeTitleView view;
        private FakeDeviceAuthUseCase deviceAuth;
        private FakePlayerUseCase player;
        private TitlePresenter presenter;

        [SetUp]
        public void SetUp()
        {
            log = new CallLog();
            view = new FakeTitleView(log);
            deviceAuth = new FakeDeviceAuthUseCase();
            player = new FakePlayerUseCase();
            presenter = new TitlePresenter(view, deviceAuth, player, new FakeSceneNavigator(log));
        }

        [UnityTest]
        public IEnumerator InitializeAsync_ShowsTapToStart() => UniTask.ToCoroutine(async () =>
        {
            await presenter.InitializeAsync();

            CollectionAssert.AreEqual(new[] { "SetStatus:タップしてスタート", "SetStartInteractable:True" }, log.Entries);
        });

        [UnityTest]
        public IEnumerator StartAsync_Success_GoesHome() => UniTask.ToCoroutine(async () =>
        {
            deviceAuth.SignInResults.Enqueue(() => UniTask.FromResult(true));
            await presenter.InitializeAsync();
            log.Entries.Clear();

            await presenter.StartAsync();

            CollectionAssert.AreEqual(new[] { "SetStartInteractable:False", "SetStatus:接続中…", "GoToHome" }, log.Entries);
        });

        [UnityTest]
        public IEnumerator StartAsync_SignInFails_ShowsRetry() => UniTask.ToCoroutine(async () =>
        {
            deviceAuth.SignInResults.Enqueue(() => throw new InvalidOperationException("x"));
            await presenter.InitializeAsync();
            log.Entries.Clear();

            await presenter.StartAsync();

            CollectionAssert.AreEqual(
                new[]
                {
                    "SetStartInteractable:False",
                    "SetStatus:接続中…",
                    "SetStatus:接続に失敗しました: x\nタップしてもう一度",
                    "SetStartInteractable:True",
                },
                log.Entries);
        });

        [UnityTest]
        public IEnumerator StartAsync_WhileRunning_Ignored() => UniTask.ToCoroutine(async () =>
        {
            var source = new UniTaskCompletionSource<bool>();
            deviceAuth.SignInResults.Enqueue(() => source.Task);
            await presenter.InitializeAsync();
            log.Entries.Clear();

            var first = presenter.StartAsync();
            await presenter.StartAsync();
            source.TrySetResult(false);
            await first;

            Assert.AreEqual(1, log.Entries.FindAll(e => e == "SetStatus:接続中…").Count);
            Assert.AreEqual(1, log.Entries.FindAll(e => e == "GoToHome").Count);
        });

        [UnityTest]
        public IEnumerator StartAsync_DisposedDuringSignIn_TouchesNothing() => UniTask.ToCoroutine(async () =>
        {
            var source = new UniTaskCompletionSource<bool>();
            deviceAuth.SignInResults.Enqueue(() => source.Task);
            await presenter.InitializeAsync();
            log.Entries.Clear();

            var first = presenter.StartAsync();
            presenter.Dispose();
            var countAtDispose = log.Entries.Count;
            source.TrySetException(new InvalidOperationException("x"));
            await first;

            Assert.AreEqual(countAtDispose, log.Entries.Count);
            CollectionAssert.DoesNotContain(log.Entries, "GoToHome");
            Assert.IsFalse(log.Entries.Exists(e => e.StartsWith("SetStatus:接続に失敗しました")));
        });

        [UnityTest]
        public IEnumerator StartClicked_AfterDispose_DoesNothing() => UniTask.ToCoroutine(async () =>
        {
            await presenter.InitializeAsync();
            presenter.Dispose();
            log.Entries.Clear();

            view.RaiseStartClicked();

            CollectionAssert.DoesNotContain(log.Entries, "SetStatus:接続中…");
        });
    }
}
