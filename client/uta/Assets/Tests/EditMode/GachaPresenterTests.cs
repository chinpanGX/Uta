using System;
using System.Collections;
using System.Linq;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using Uta.Client.Application.Gacha;
using Uta.Client.Infrastructure.Player;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Gacha;

namespace Uta.Client.Tests
{
    public sealed class GachaPresenterTests
    {
        private const string Lineup = "SSR 育成アイドル: アイドルA\nSSR サポートカード: サポートB";

        private sealed class FakeGachaView : IGachaView
        {
            private readonly CallLog log;

            public FakeGachaView(CallLog log)
            {
                this.log = log;
            }

            public event Action SingleClicked;
            public event Action TenClicked;
            public event Action RatesClicked;
            public event Action BackClicked;

            public void SetCurrency(string text)
            {
                log.Add("SetCurrency:" + text);
            }

            public void SetExchange(string text)
            {
                log.Add("SetExchange:" + text);
            }

            public void SetCosts(string singleLabel, string tenLabel)
            {
                log.Add("SetCosts:" + singleLabel + "," + tenLabel);
            }

            public void SetLineup(string text)
            {
                log.Add("SetLineup:" + text);
            }

            public void RaiseBack()
            {
                BackClicked?.Invoke();
            }
        }

        private CallLog log;
        private FakeGachaView view;
        private FakeGachaUseCase gachaUseCase;
        private FakeDialogOpener dialogs;
        private FakeScreenNavigator navigator;
        private PlayerRepository players;

        [SetUp]
        public void SetUp()
        {
            log = new CallLog();
            view = new FakeGachaView(log);
            gachaUseCase = new FakeGachaUseCase(log);
            dialogs = new FakeDialogOpener(log);
            navigator = new FakeScreenNavigator(log);
            players = new PlayerRepository();
            players.Upsert(GachaFixtures.Player(GachaFixtures.Currency));
        }

        private GachaPresenter CreatePresenter()
        {
            var dtoFactory = new GachaDtoFactory(players, GachaFixtures.CreateMaster());
            return new GachaPresenter(view, dtoFactory, gachaUseCase, dialogs, navigator);
        }

        private static GachaResult[] TwoResults()
        {
            return new[]
            {
                new GachaResult(1, Uta.MasterData.ItemKind.Idol, 1, true, 0, 0),
                new GachaResult(2, Uta.MasterData.ItemKind.SupportCard, 2, true, 0, 0),
            };
        }

        [UnityTest]
        public IEnumerator InitializeAsync_ShowsCurrencyCostsLineupAndExchange() => UniTask.ToCoroutine(async () =>
        {
            gachaUseCase.ExchangePointsResults.Enqueue(() => UniTask.FromResult<int?>(37));
            var presenter = CreatePresenter();

            await presenter.InitializeAsync();

            CollectionAssert.AreEqual(
                new[]
                {
                    "SetCurrency:ガチャ通貨 3,250",
                    "SetCosts:単発 250,10連 2,500",
                    "SetLineup:" + Lineup,
                    "SetExchange:交換pt 37 / 200",
                },
                log.Entries);
        });

        [UnityTest]
        public IEnumerator InitializeAsync_StatusFails_ShowsDash() => UniTask.ToCoroutine(async () =>
        {
            gachaUseCase.ExchangePointsResults.Enqueue(() => UniTask.FromResult<int?>(null));
            var presenter = CreatePresenter();

            await presenter.InitializeAsync();

            Assert.AreEqual("SetExchange:交換pt - / 200", log.Entries[log.Entries.Count - 1]);
        });

        [UnityTest]
        public IEnumerator DrawAsync_Confirmed_PushesResultModal() => UniTask.ToCoroutine(async () =>
        {
            gachaUseCase.DrawResults.Enqueue(() =>
            {
                players.Upsert(GachaFixtures.Player(750));
                return UniTask.FromResult(DrawResult.Success(TwoResults(), 750, 47));
            });
            var presenter = CreatePresenter();
            await presenter.InitializeAsync();
            log.Entries.Clear();

            await presenter.DrawAsync(10);

            CollectionAssert.AreEqual(
                new[]
                {
                    "Confirm:ガチャ通貨 2,500 を使って 10 回引きます。（所持 3,250）",
                    "Draw:10",
                    "SetCurrency:ガチャ通貨 750",
                    "SetCosts:単発 250,10連 2,500",
                    "SetLineup:" + Lineup,
                    "SetExchange:交換pt 47 / 200",
                    "PushModal:GachaResultPresenter",
                },
                log.Entries);
            var args = (GachaResultArgs)navigator.Args[0];
            Assert.AreEqual(2, args.Results.Count);
            Assert.IsNotNull(args.Retry);
        });

        [UnityTest]
        public IEnumerator DrawAsync_Declined_DoesNotDraw() => UniTask.ToCoroutine(async () =>
        {
            dialogs.ConfirmResults.Enqueue(DialogResult.Negative);
            var presenter = CreatePresenter();
            await presenter.InitializeAsync();
            log.Entries.Clear();

            await presenter.DrawAsync(10);

            Assert.IsFalse(log.Entries.Any(e => e.StartsWith("Draw:")));
            Assert.IsFalse(log.Entries.Any(e => e.StartsWith("PushModal:")));
        });

        [UnityTest]
        public IEnumerator DrawAsync_NotEnoughCurrency_ShowsInfo() => UniTask.ToCoroutine(async () =>
        {
            gachaUseCase.DrawResults.Enqueue(() => UniTask.FromResult(DrawResult.Rejected(DrawResultKind.NotEnoughCurrency)));
            var presenter = CreatePresenter();
            await presenter.InitializeAsync();
            log.Entries.Clear();

            await presenter.DrawAsync(10);

            Assert.AreEqual("Info:ガチャ通貨が足りません。", log.Entries[log.Entries.Count - 1]);
            Assert.IsFalse(log.Entries.Any(e => e.StartsWith("PushModal:")));
        });

        [UnityTest]
        public IEnumerator DrawAsync_Failed_ShowsNothing() => UniTask.ToCoroutine(async () =>
        {
            gachaUseCase.DrawResults.Enqueue(() => UniTask.FromResult(DrawResult.Rejected(DrawResultKind.Failed)));
            var presenter = CreatePresenter();
            await presenter.InitializeAsync();
            log.Entries.Clear();

            await presenter.DrawAsync(10);

            CollectionAssert.AreEqual(
                new[] { "Confirm:ガチャ通貨 2,500 を使って 10 回引きます。（所持 3,250）", "Draw:10" },
                log.Entries);
        });

        [UnityTest]
        public IEnumerator DrawAsync_WhileRunning_Ignored() => UniTask.ToCoroutine(async () =>
        {
            var gate = new UniTaskCompletionSource<DrawResult>();
            gachaUseCase.DrawResults.Enqueue(() => gate.Task);
            var presenter = CreatePresenter();
            await presenter.InitializeAsync();
            log.Entries.Clear();

            var first = presenter.DrawAsync(1);
            await presenter.DrawAsync(1);
            gate.TrySetResult(DrawResult.Rejected(DrawResultKind.Failed));
            await first;

            Assert.AreEqual(1, log.Entries.Count(e => e == "Draw:1"));
        });

        [UnityTest]
        public IEnumerator DrawAsync_DisposedDuringDraw_TouchesNothing() => UniTask.ToCoroutine(async () =>
        {
            var gate = new UniTaskCompletionSource<DrawResult>();
            gachaUseCase.DrawResults.Enqueue(() => gate.Task);
            var presenter = CreatePresenter();
            await presenter.InitializeAsync();
            log.Entries.Clear();

            var first = presenter.DrawAsync(1);
            presenter.Dispose();
            gate.TrySetResult(DrawResult.Success(new[] { TwoResults()[0] }, 3000, 1));
            await first;

            Assert.AreEqual("Draw:1", log.Entries[log.Entries.Count - 1]);
        });

        [UnityTest]
        public IEnumerator ResultRetry_DrawsAgain() => UniTask.ToCoroutine(async () =>
        {
            gachaUseCase.DrawResults.Enqueue(() =>
            {
                players.Upsert(GachaFixtures.Player(750));
                return UniTask.FromResult(DrawResult.Success(TwoResults(), 750, 47));
            });
            gachaUseCase.DrawResults.Enqueue(() => UniTask.FromResult(DrawResult.Success(TwoResults(), 750, 48)));
            var presenter = CreatePresenter();
            await presenter.InitializeAsync();
            log.Entries.Clear();

            await presenter.DrawAsync(10);
            await ((GachaResultArgs)navigator.Args[0]).Retry();

            var confirms = log.Entries.Where(e => e.StartsWith("Confirm:")).ToArray();
            Assert.AreEqual(2, confirms.Length);
            Assert.AreEqual("Confirm:ガチャ通貨 2,500 を使って 10 回引きます。（所持 750）", confirms[1]);
            Assert.AreEqual(2, log.Entries.Count(e => e == "Draw:10"));
            Assert.AreEqual(2, log.Entries.Count(e => e == "PushModal:GachaResultPresenter"));
            Assert.IsFalse(log.Entries.Any(e => e.StartsWith("Info:")));
        });

        [UnityTest]
        public IEnumerator OpenRatesAsync_PushesRatesModal() => UniTask.ToCoroutine(async () =>
        {
            var presenter = CreatePresenter();
            await presenter.InitializeAsync();
            log.Entries.Clear();

            await presenter.OpenRatesAsync();

            CollectionAssert.AreEqual(new[] { "PushModal:GachaRatesPresenter" }, log.Entries);
            Assert.AreEqual(1, ((GachaRatesArgs)navigator.Args[0]).GachaId);
        });

        [UnityTest]
        public IEnumerator BackAsync_PopsPage() => UniTask.ToCoroutine(async () =>
        {
            var presenter = CreatePresenter();
            await presenter.InitializeAsync();
            log.Entries.Clear();

            await presenter.BackAsync();

            CollectionAssert.AreEqual(new[] { "PopPage:GachaPresenter" }, log.Entries);
        });

        [UnityTest]
        public IEnumerator Dispose_UnsubscribesEvents() => UniTask.ToCoroutine(async () =>
        {
            var presenter = CreatePresenter();
            await presenter.InitializeAsync();
            presenter.Dispose();
            log.Entries.Clear();

            view.RaiseBack();

            Assert.IsFalse(log.Entries.Any(e => e.StartsWith("PopPage:")));
        });
    }
}
