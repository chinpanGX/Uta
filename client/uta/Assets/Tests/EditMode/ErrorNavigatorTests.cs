using System;
using System.Collections;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using Uta.Client.Application.Player;
using Uta.Client.Bootstrap;
using VContainer;

namespace Uta.Client.Tests
{
    public sealed class ErrorNavigatorTests
    {
        private IObjectResolver container;

        [TearDown]
        public void TearDown()
        {
            if (container is IDisposable disposable)
            {
                disposable.Dispose();
            }

            container = null;
        }

        private ErrorNavigator Create(FakePlayerUseCase fake, CallLog log)
        {
            var builder = new ContainerBuilder();
            if (fake != null)
            {
                builder.RegisterInstance<IPlayerUseCase>(fake);
            }

            container = builder.Build();
            return new ErrorNavigator(new FakeSceneNavigator(log), container);
        }

        [UnityTest]
        public IEnumerator GoToHomeAsync_ResolvesRefreshLazily_RefreshesThenGoesHome() => UniTask.ToCoroutine(async () =>
        {
            var log = new CallLog();
            var fake = new FakePlayerUseCase(log);
            var navigator = Create(fake, log);

            await navigator.GoToHomeAsync();

            CollectionAssert.AreEqual(new[] { "Refresh", "GoToHome" }, log.Entries);
            Assert.AreEqual(1, fake.RefreshCount);
        });

        [UnityTest]
        public IEnumerator GoToHomeAsync_WhileRefreshing_SkipsRefresh() => UniTask.ToCoroutine(async () =>
        {
            var log = new CallLog();
            var fake = new FakePlayerUseCase(log);
            var gate = new UniTaskCompletionSource();
            fake.RefreshResults.Enqueue(() => gate.Task);
            var navigator = Create(fake, log);

            var first = navigator.GoToHomeAsync();
            CollectionAssert.AreEqual(new[] { "Refresh" }, log.Entries);
            Assert.AreEqual(1, fake.RefreshCount);

            await navigator.GoToHomeAsync();
            CollectionAssert.AreEqual(new[] { "Refresh", "GoToHome" }, log.Entries);
            Assert.AreEqual(1, fake.RefreshCount);

            gate.TrySetResult();
            await first;
            CollectionAssert.AreEqual(new[] { "Refresh", "GoToHome", "GoToHome" }, log.Entries);

            await navigator.GoToHomeAsync();
            Assert.AreEqual(2, fake.RefreshCount);
        });

        [UnityTest]
        public IEnumerator GoToHomeAsync_RefreshFails_StillGoesHome() => UniTask.ToCoroutine(async () =>
        {
            var log = new CallLog();
            var fake = new FakePlayerUseCase(log);
            fake.RefreshResults.Enqueue(() => throw new InvalidOperationException("sign-in failed: x"));
            var navigator = Create(fake, log);
            LogAssert.Expect(UnityEngine.LogType.Warning, new Regex("refresh on go-to-home failed"));

            await navigator.GoToHomeAsync();

            CollectionAssert.AreEqual(new[] { "Refresh", "GoToHome" }, log.Entries);
        });

        [UnityTest]
        public IEnumerator GoToHomeAsync_ResolveFails_StillGoesHome() => UniTask.ToCoroutine(async () =>
        {
            var log = new CallLog();
            var navigator = Create(null, log);
            LogAssert.Expect(UnityEngine.LogType.Warning, new Regex("refresh on go-to-home failed"));

            await navigator.GoToHomeAsync();

            CollectionAssert.AreEqual(new[] { "GoToHome" }, log.Entries);
        });
    }
}
