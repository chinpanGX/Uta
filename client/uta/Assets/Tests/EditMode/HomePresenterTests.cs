using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using Uta.Client.Application.Idol;
using Uta.Client.Application.Player;
using Uta.Client.Domain.Idol;
using Uta.Client.Domain.Player;
using Uta.Client.Infrastructure.Idol;
using Uta.Client.Infrastructure.Player;
using Uta.Client.Presentation.Home;
using Uta.MasterData;

namespace Uta.Client.Tests
{
    public sealed class HomePresenterTests
    {
        private const string DiscardMessage = "バージョンが変わったため、進行中のプロデュースは破棄されました。";

        private sealed class FakeHomeView : IHomeView
        {
            private readonly CallLog log;

            public FakeHomeView(CallLog log)
            {
                this.log = log;
            }

            public event Action ProduceClicked;
            public event Action GachaClicked;
            public event Action OwnedClicked;

            public void SetProfile(string text)
            {
                log.Add("SetProfile:" + text);
            }

            public void SetResume(bool visible, string primaryLabel)
            {
                log.Add("SetResume:" + visible + "," + primaryLabel);
            }

            public void RaiseProduceClicked()
            {
                ProduceClicked?.Invoke();
            }

            public void RaiseGachaClicked()
            {
                GachaClicked?.Invoke();
            }

            public void RaiseOwnedClicked()
            {
                OwnedClicked?.Invoke();
            }
        }

        private sealed class FakeCharacterStage : ICharacterStage
        {
            private readonly CallLog log;

            public FakeCharacterStage(CallLog log)
            {
                this.log = log;
            }

            public void ShowIdol(int idolId, string name)
            {
                log.Add("ShowIdol:" + idolId + "," + name);
            }

            public void Clear()
            {
                log.Add("ClearStage");
            }
        }

        private CallLog log;
        private FakeHomeView view;
        private FakeDialogOpener dialogs;
        private PlayerRepository players;
        private IdolRepository idols;
        private DiscardNoticeState notice;

        [SetUp]
        public void SetUp()
        {
            log = new CallLog();
            view = new FakeHomeView(log);
            dialogs = new FakeDialogOpener(log);
            players = new PlayerRepository();
            idols = new IdolRepository();
            notice = new DiscardNoticeState();
        }

        private HomePresenter CreatePresenter(MemoryDatabase db = null)
        {
            db = db ?? TestMasterData.Build(_ => { });
            var dtoFactory = new HomeDtoFactory(players, new FeaturedIdolPicker(idols, db), notice, db);
            return new HomePresenter(view, new FakeCharacterStage(log), dtoFactory, new FakePlayerUseCase(log, notice), dialogs, new FakeScreenNavigator(log));
        }

        private void SetPlayer(int level = 12, int gachaCurrency = 3250, bool hasActiveProduce = true, bool discardedProduce = false)
        {
            players.Upsert(new PlayerEntity("P1", level, 0, 0, 0, gachaCurrency, hasActiveProduce, discardedProduce));
        }

        private static MemoryDatabase IdolDb()
        {
            return TestMasterData.Build(b => b.Append(new[]
            {
                TestMasterData.Idol(1, "アイドル1", Rarity.SR),
                TestMasterData.Idol(2, "アイドル2", Rarity.SSR),
                TestMasterData.Idol(3, "アイドル3", Rarity.SSR),
                TestMasterData.Idol(4, "アイドル4", Rarity.R),
            }));
        }

        private void Own(params int[] ids)
        {
            idols.Clear();
            foreach (var id in ids)
            {
                idols.Upsert(new IdolEntity("U" + id, id, 0, 0, 0));
            }
        }

        [UnityTest]
        public IEnumerator InitializeAsync_ShowsProfileAndResume() => UniTask.ToCoroutine(async () =>
        {
            SetPlayer();
            var presenter = CreatePresenter();

            await presenter.InitializeAsync();

            CollectionAssert.AreEqual(
                new[] { "SetProfile:プレイヤーLv 12　ガチャ通貨 3,250", "SetResume:True,再開", "ClearStage" },
                log.Entries);
        });

        [UnityTest]
        public IEnumerator InitializeAsync_ShowsHighestRarityIdolOnStage() => UniTask.ToCoroutine(async () =>
        {
            SetPlayer();
            Own(4, 3, 1, 2);
            var presenter = CreatePresenter(IdolDb());

            await presenter.InitializeAsync();

            Assert.AreEqual("ShowIdol:2,アイドル2", log.Entries[log.Entries.Count - 1]);
        });

        [UnityTest]
        public IEnumerator InitializeAsync_SingleIdol_ShowsIt() => UniTask.ToCoroutine(async () =>
        {
            SetPlayer();
            Own(1);
            var presenter = CreatePresenter(IdolDb());

            await presenter.InitializeAsync();

            Assert.AreEqual("ShowIdol:1,アイドル1", log.Entries[log.Entries.Count - 1]);
        });

        [UnityTest]
        public IEnumerator InitializeAsync_NoPlayer_DoesNothing() => UniTask.ToCoroutine(async () =>
        {
            var presenter = CreatePresenter();

            await presenter.InitializeAsync();

            Assert.IsEmpty(log.Entries);
        });

        [UnityTest]
        public IEnumerator DidPopEnter_RepicksStage() => UniTask.ToCoroutine(async () =>
        {
            SetPlayer();
            var presenter = CreatePresenter(IdolDb());
            await presenter.InitializeAsync();
            Assert.AreEqual("ClearStage", log.Entries[log.Entries.Count - 1]);

            Own(3);
            presenter.DidPopEnter();

            Assert.AreEqual("ShowIdol:3,アイドル3", log.Entries[log.Entries.Count - 1]);
        });

        [UnityTest]
        public IEnumerator DidPopEnter_RefreshesFromRepository() => UniTask.ToCoroutine(async () =>
        {
            SetPlayer();
            var presenter = CreatePresenter();
            await presenter.InitializeAsync();
            log.Entries.Clear();

            SetPlayer(level: 13);
            presenter.DidPopEnter();

            Assert.AreEqual("SetProfile:プレイヤーLv 13　ガチャ通貨 3,250", log.Entries[0]);
        });

        [UnityTest]
        public IEnumerator ProduceClicked_ShowsNotReady() => UniTask.ToCoroutine(async () =>
        {
            SetPlayer();
            var presenter = CreatePresenter();
            await presenter.InitializeAsync();
            log.Entries.Clear();

            view.RaiseProduceClicked();

            CollectionAssert.AreEqual(new[] { "Info:準備中です。" }, log.Entries);
        });

        [UnityTest]
        public IEnumerator GachaClicked_PushesGachaPage() => UniTask.ToCoroutine(async () =>
        {
            SetPlayer();
            var presenter = CreatePresenter();
            await presenter.InitializeAsync();
            log.Entries.Clear();

            view.RaiseGachaClicked();

            CollectionAssert.AreEqual(new[] { "PushPage:GachaPresenter" }, log.Entries);
        });

        [UnityTest]
        public IEnumerator OwnedClicked_ShowsNotReady() => UniTask.ToCoroutine(async () =>
        {
            SetPlayer();
            var presenter = CreatePresenter();
            await presenter.InitializeAsync();
            log.Entries.Clear();

            view.RaiseOwnedClicked();

            CollectionAssert.AreEqual(new[] { "Info:準備中です。" }, log.Entries);
        });

        [UnityTest]
        public IEnumerator Dispose_UnsubscribesEvents() => UniTask.ToCoroutine(async () =>
        {
            SetPlayer();
            var presenter = CreatePresenter();
            await presenter.InitializeAsync();
            presenter.Dispose();
            log.Entries.Clear();

            view.RaiseProduceClicked();
            view.RaiseGachaClicked();
            view.RaiseOwnedClicked();

            Assert.IsEmpty(log.Entries);
        });

        [UnityTest]
        public IEnumerator OpenProduceAsync_WhileRunning_Ignored() => UniTask.ToCoroutine(async () =>
        {
            SetPlayer();
            var presenter = CreatePresenter();
            await presenter.InitializeAsync();
            log.Entries.Clear();
            var gate = new UniTaskCompletionSource();
            dialogs.InfoGate = gate;

            var first = presenter.OpenProduceAsync();
            await presenter.OpenProduceAsync();
            Assert.AreEqual(1, log.Entries.FindAll(e => e.StartsWith("Info:")).Count);

            gate.TrySetResult();
            await first;
            dialogs.InfoGate = null;
            await presenter.OpenProduceAsync();

            Assert.AreEqual(2, log.Entries.FindAll(e => e.StartsWith("Info:")).Count);
        });

        [UnityTest]
        public IEnumerator OpenGachaAsync_PushesGachaPage() => UniTask.ToCoroutine(async () =>
        {
            SetPlayer();
            var presenter = CreatePresenter();
            await presenter.InitializeAsync();
            log.Entries.Clear();

            await presenter.OpenGachaAsync();

            CollectionAssert.AreEqual(new[] { "PushPage:GachaPresenter" }, log.Entries);
        });

        [UnityTest]
        public IEnumerator ShowDiscardNotice_Discarded_ShowsOnce() => UniTask.ToCoroutine(async () =>
        {
            SetPlayer(discardedProduce: true);
            var presenter = CreatePresenter();

            await presenter.ShowDiscardNoticeIfNeededAsync();
            await presenter.ShowDiscardNoticeIfNeededAsync();

            CollectionAssert.AreEqual(new[] { "MarkDiscardNoticeShown", "Info:" + DiscardMessage }, log.Entries);
            Assert.IsTrue(notice.Shown);
        });

        [UnityTest]
        public IEnumerator ShowDiscardNotice_NotDiscarded_ShowsNothing() => UniTask.ToCoroutine(async () =>
        {
            SetPlayer(discardedProduce: false);
            var presenter = CreatePresenter();

            await presenter.ShowDiscardNoticeIfNeededAsync();

            Assert.IsEmpty(log.Entries);
        });

        [UnityTest]
        public IEnumerator ShowDiscardNotice_NoPlayer_ShowsNothing() => UniTask.ToCoroutine(async () =>
        {
            var presenter = CreatePresenter();

            await presenter.ShowDiscardNoticeIfNeededAsync();

            Assert.IsEmpty(log.Entries);
        });
    }
}
