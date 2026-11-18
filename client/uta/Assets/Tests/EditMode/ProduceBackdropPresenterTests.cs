using System.Collections;
using System.Linq;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using Uta.Client.Application.Produce;
using Uta.Client.Presentation.Produce;
using ZLinq;

namespace Uta.Client.Tests
{
    public sealed class ProduceBackdropPresenterTests
    {
        private sealed class FakeBackdropView : IProduceBackdropView
        {
            private readonly CallLog log;

            public FakeBackdropView(CallLog log)
            {
                this.log = log;
            }

            public void SetHeader(string text)
            {
                log.Add("SetHeader:" + text);
            }
        }

        private CallLog log;
        private ProduceSessionState session;
        private ProduceBackdropPresenter presenter;

        [SetUp]
        public void SetUp()
        {
            log = new CallLog();
            session = new ProduceSessionState();
            presenter = new ProduceBackdropPresenter(new FakeBackdropView(log), session, new ProduceBackdropDtoFactory());
        }

        [UnityTest]
        public IEnumerator Initialize_SetsHeader() => UniTask.ToCoroutine(async () =>
        {
            var p = TestProduce.StateResult(week: 3, singing: 100, rhythm: 110, expression: 120, hp: 25, maxHp: 30);
            session.Set(p);

            await presenter.InitializeAsync();

            CollectionAssert.AreEqual(new[] { "SetHeader:" + new ProduceBackdropDtoFactory().Create(p).Header }, log.Entries);
        });

        [UnityTest]
        public IEnumerator DidPopEnter_SetsHeaderFromLatest() => UniTask.ToCoroutine(async () =>
        {
            session.Set(TestProduce.StateResult(week: 3));
            await presenter.InitializeAsync();

            var latest = TestProduce.StateResult(week: 4);
            session.Set(latest);
            presenter.DidPopEnter();

            Assert.AreEqual("SetHeader:" + new ProduceBackdropDtoFactory().Create(latest).Header, log.Entries.Last());
            StringAssert.StartsWith("SetHeader:週 4", log.Entries.Last());
        });

        [UnityTest]
        public IEnumerator Initialize_NoSession_DoesNotSetHeader() => UniTask.ToCoroutine(async () =>
        {
            await presenter.InitializeAsync();

            Assert.IsEmpty(log.Entries);
        });
    }
}
