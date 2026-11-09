using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using Uta.Client.Application.Gacha;
using Uta.Client.Presentation.Gacha;

namespace Uta.Client.Tests
{
    public sealed class GachaResultPresenterTests
    {
        private sealed class FakeGachaResultView : IGachaResultView
        {
            private readonly CallLog log;

            public FakeGachaResultView(CallLog log)
            {
                this.log = log;
            }

            public event Action CloseClicked;
            public event Action RetryClicked;

            public void SetLines(IReadOnlyList<GachaLineRowViewDto> lines)
            {
                log.Add("SetLines:" + lines.Count);
            }

            public void RaiseClose()
            {
                CloseClicked?.Invoke();
            }

            public void RaiseRetry()
            {
                RetryClicked?.Invoke();
            }
        }

        private CallLog log;
        private FakeGachaResultView view;
        private FakeScreenNavigator navigator;

        [SetUp]
        public void SetUp()
        {
            log = new CallLog();
            view = new FakeGachaResultView(log);
            navigator = new FakeScreenNavigator(log);
        }

        private GachaResultPresenter CreatePresenter(Func<UniTask> retry)
        {
            var results = new[]
            {
                new GachaResult(1, Uta.MasterData.ItemKind.Idol, 1, true, 0, 0),
                new GachaResult(2, Uta.MasterData.ItemKind.SupportCard, 2, true, 0, 0),
            };
            var dtoFactory = new GachaResultDtoFactory(GachaFixtures.CreateMaster());
            return new GachaResultPresenter(view, new GachaResultArgs(results, retry), dtoFactory, navigator);
        }

        [UnityTest]
        public IEnumerator InitializeAsync_SetsLines() => UniTask.ToCoroutine(async () =>
        {
            var presenter = CreatePresenter(null);

            await presenter.InitializeAsync();

            CollectionAssert.AreEqual(new[] { "SetLines:2" }, log.Entries);
        });

        [UnityTest]
        public IEnumerator RetryAsync_PopsModalThenCallsRetry() => UniTask.ToCoroutine(async () =>
        {
            var presenter = CreatePresenter(() =>
            {
                log.Add("Retry");
                return UniTask.CompletedTask;
            });
            await presenter.InitializeAsync();
            log.Entries.Clear();

            await presenter.RetryAsync();

            CollectionAssert.AreEqual(new[] { "PopModal:GachaResultPresenter", "Retry" }, log.Entries);
        });

        [UnityTest]
        public IEnumerator RetryAsync_NullRetry_OnlyPops() => UniTask.ToCoroutine(async () =>
        {
            var presenter = CreatePresenter(null);
            await presenter.InitializeAsync();
            log.Entries.Clear();

            await presenter.RetryAsync();

            CollectionAssert.AreEqual(new[] { "PopModal:GachaResultPresenter" }, log.Entries);
        });

        [UnityTest]
        public IEnumerator CloseAsync_PopsModal() => UniTask.ToCoroutine(async () =>
        {
            var presenter = CreatePresenter(() =>
            {
                log.Add("Retry");
                return UniTask.CompletedTask;
            });
            await presenter.InitializeAsync();
            log.Entries.Clear();

            await presenter.CloseAsync();

            CollectionAssert.AreEqual(new[] { "PopModal:GachaResultPresenter" }, log.Entries);
        });

        [UnityTest]
        public IEnumerator Dispose_UnsubscribesEvents() => UniTask.ToCoroutine(async () =>
        {
            var presenter = CreatePresenter(null);
            await presenter.InitializeAsync();
            log.Entries.Clear();

            presenter.Dispose();
            view.RaiseClose();
            view.RaiseRetry();

            CollectionAssert.IsEmpty(log.Entries);
        });
    }
}
