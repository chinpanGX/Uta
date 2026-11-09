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
    public sealed class GachaRatesPresenterTests
    {
        private sealed class FakeGachaRatesView : IGachaRatesView
        {
            private readonly CallLog log;

            public FakeGachaRatesView(CallLog log)
            {
                this.log = log;
            }

            public event Action CloseClicked;

            public IReadOnlyList<GachaLineRowViewDto> LastLines { get; private set; }

            public void SetLines(IReadOnlyList<GachaLineRowViewDto> lines)
            {
                log.Add("SetLines:" + lines.Count);
                LastLines = lines;
            }

            public void RaiseClose()
            {
                CloseClicked?.Invoke();
            }
        }

        private CallLog log;
        private FakeGachaRatesView view;
        private FakeScreenNavigator navigator;

        [SetUp]
        public void SetUp()
        {
            log = new CallLog();
            view = new FakeGachaRatesView(log);
            navigator = new FakeScreenNavigator(log);
        }

        private GachaRatesPresenter CreatePresenter()
        {
            var master = GachaFixtures.CreateMaster();
            var dtoFactory = new GachaRatesDtoFactory(master, new GachaRateCalculator(master));
            return new GachaRatesPresenter(view, new GachaRatesArgs(GachaFixtures.GachaId), dtoFactory, navigator);
        }

        [UnityTest]
        public IEnumerator InitializeAsync_SetsLines() => UniTask.ToCoroutine(async () =>
        {
            var presenter = CreatePresenter();

            await presenter.InitializeAsync();

            CollectionAssert.AreEqual(new[] { "SetLines:14" }, log.Entries);
            Assert.IsTrue(view.LastLines[0].IsHeading);
            Assert.AreEqual("通常枠（単発・10連の1〜9枠目）", view.LastLines[0].Text);
        });

        [UnityTest]
        public IEnumerator CloseAsync_PopsModal() => UniTask.ToCoroutine(async () =>
        {
            var presenter = CreatePresenter();
            await presenter.InitializeAsync();
            log.Entries.Clear();

            await presenter.CloseAsync();

            CollectionAssert.AreEqual(new[] { "PopModal:GachaRatesPresenter" }, log.Entries);
        });

        [UnityTest]
        public IEnumerator Dispose_UnsubscribesEvents() => UniTask.ToCoroutine(async () =>
        {
            var presenter = CreatePresenter();
            await presenter.InitializeAsync();
            log.Entries.Clear();

            presenter.Dispose();
            view.RaiseClose();

            CollectionAssert.IsEmpty(log.Entries);
        });
    }
}
