using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Uta.Client.Application.Idol;
using Uta.Client.Application.Owned;
using Uta.Client.Application.Produce;
using Uta.Client.Domain.Idol;
using Uta.Client.Domain.SupportCard;
using Uta.Client.Infrastructure.Idol;
using Uta.Client.Infrastructure.SupportCard;
using Uta.Client.Presentation.Formation;
using Uta.Client.Presentation.Owned;
using Uta.MasterData;
using ZLinq;

namespace Uta.Client.Tests
{
    public sealed class FormationPresenterTests
    {
        private sealed class FakeFormationView : IFormationView
        {
            private readonly CallLog log;

            public FakeFormationView(CallLog log)
            {
                this.log = log;
            }

            public event Action IdolClicked;
            public event Action<int> SlotClicked;
            public event Action<int> SlotClearClicked;
            public event Action ScenarioClicked;
            public event Action StartClicked;
            public event Action BackClicked;

            public FormationViewDto LastDto { get; private set; }

            public void Set(FormationViewDto formationViewDto)
            {
                LastDto = formationViewDto;
                log.Add("Set:" + formationViewDto.IdolLabel + "|" + string.Join("/", formationViewDto.Slots.Select(s => s.Label))
                    + "|" + formationViewDto.ScenarioLabel + "," + formationViewDto.CanChangeScenario + "," + formationViewDto.CanStart);
            }

            public void RaiseIdolClicked()
            {
                IdolClicked?.Invoke();
            }

            public void RaiseSlotClicked(int index)
            {
                SlotClicked?.Invoke(index);
            }

            public void RaiseSlotClearClicked(int index)
            {
                SlotClearClicked?.Invoke(index);
            }

            public void RaiseScenarioClicked()
            {
                ScenarioClicked?.Invoke();
            }

            public void RaiseStartClicked()
            {
                StartClicked?.Invoke();
            }

            public void RaiseBackClicked()
            {
                BackClicked?.Invoke();
            }
        }

        private const string RemovedInfo = "Info:プランが合わないサポートカードを外しました。";

        private CallLog log;
        private FakeFormationView view;
        private FakeDialogOpener dialogs;
        private FakeScreenNavigator navigator;
        private FakeSceneNavigator scenes;
        private FakeProduceUseCase produce;
        private IdolRepository idols;
        private SupportCardRepository supportCards;

        [SetUp]
        public void SetUp()
        {
            log = new CallLog();
            view = new FakeFormationView(log);
            dialogs = new FakeDialogOpener(log);
            navigator = new FakeScreenNavigator(log);
            scenes = new FakeSceneNavigator(log);
            produce = new FakeProduceUseCase(log);
            idols = new IdolRepository();
            supportCards = new SupportCardRepository();
        }

        // 育成アイドル 1 = SR・Sense、2 = SSR・Logic、3 = SSR・Sense。カード 10 = Sense、20 = Logic、30 = Free。シナリオ 1・2。
        private static MemoryDatabase CreateDb(int scenarioCount = 2)
        {
            return TestMasterData.Build(b =>
            {
                b.Append(new[]
                {
                    TestMasterData.Idol(1, "アイドル1", Rarity.SR),
                    new Idol { IdolId = 2, Name = "アイドル2", Rarity = Rarity.SSR, Plan = Plan.Logic, MaxHp = 30 },
                    TestMasterData.Idol(3, "アイドル3", Rarity.SSR),
                });
                b.Append(new[]
                {
                    new SupportCard { SupportCardId = 10, Name = "カード10", Rarity = Rarity.SR, Plan = Plan.Sense },
                    new SupportCard { SupportCardId = 20, Name = "カード20", Rarity = Rarity.SR, Plan = Plan.Logic },
                    new SupportCard { SupportCardId = 30, Name = "カード30", Rarity = Rarity.R, Plan = Plan.Free },
                });
                var scenarios = new Scenario[scenarioCount];
                for (var i = 0; i < scenarioCount; i++)
                {
                    scenarios[i] = new Scenario { ScenarioId = i + 1, Name = "シナリオ" + (i + 1) };
                }

                b.Append(scenarios);
            });
        }

        private FormationPresenter CreatePresenter(int scenarioCount = 2)
        {
            var db = CreateDb(scenarioCount);
            var scenarios = new ScenarioResolver(db);
            return new FormationPresenter(
                view,
                new FormationDtoFactory(supportCards, scenarios, db),
                new FormationNormalizer(supportCards, db),
                new FeaturedIdolPicker(idols, db),
                scenarios,
                produce,
                dialogs,
                navigator,
                scenes);
        }

        private void OwnIdols(params int[] ids)
        {
            foreach (var id in ids)
            {
                idols.Upsert(new IdolEntity("I" + id, id, 0, 0, 0));
            }
        }

        private void OwnCards()
        {
            supportCards.Upsert(new SupportCardEntity("U10", 10, 1, 0));
            supportCards.Upsert(new SupportCardEntity("U20", 20, 1, 0));
            supportCards.Upsert(new SupportCardEntity("U30", 30, 1, 0));
            supportCards.Upsert(new SupportCardEntity("u9", 30, 1, 0));
        }

        private int SetCount()
        {
            return log.Entries.Count(e => e.StartsWith("Set:"));
        }

        private async UniTask PutAsync(FormationPresenter presenter, int slot, string uid)
        {
            navigator.PopResults.Enqueue(OwnedSelection.ForSupportCard(uid));
            await presenter.SelectSlotAsync(slot);
        }

        [UnityTest]
        public IEnumerator Initialize_DefaultsToHighestRarityIdol() => UniTask.ToCoroutine(async () =>
        {
            OwnIdols(1, 2, 3);
            var presenter = CreatePresenter();

            await presenter.InitializeAsync();

            Assert.AreEqual(1, SetCount());
            StringAssert.StartsWith("育成アイドル: アイドル2 ", view.LastDto.IdolLabel);
            Assert.IsTrue(view.LastDto.CanStart);
        });

        [UnityTest]
        public IEnumerator Initialize_NoIdols_StartDisabled() => UniTask.ToCoroutine(async () =>
        {
            var presenter = CreatePresenter();

            await presenter.InitializeAsync();

            Assert.AreEqual("育成アイドル: 未選択", view.LastDto.IdolLabel);
            Assert.IsFalse(view.LastDto.CanStart);
        });

        [UnityTest]
        public IEnumerator Initialize_ShowsEmptySlotsAndScenario() => UniTask.ToCoroutine(async () =>
        {
            OwnIdols(1);
            var presenter = CreatePresenter();

            await presenter.InitializeAsync();

            Assert.AreEqual(5, view.LastDto.Slots.Count);
            Assert.IsTrue(view.LastDto.Slots.All(s => !s.Filled));
            Assert.AreEqual("シナリオ: シナリオ1", view.LastDto.ScenarioLabel);
            Assert.IsTrue(view.LastDto.CanChangeScenario);
        });

        [UnityTest]
        public IEnumerator SelectIdol_Selected_ChangesIdolAndClearsIncompatibleSupport() => UniTask.ToCoroutine(async () =>
        {
            OwnIdols(1, 3);
            OwnCards();
            var presenter = CreatePresenter();
            await presenter.InitializeAsync();   // 育成アイドル 3（Sense）
            await PutAsync(presenter, 0, "U10");
            await PutAsync(presenter, 1, "U30");
            log.Entries.Clear();
            navigator.PopResults.Enqueue(OwnedSelection.ForIdol(2));

            await presenter.SelectIdolAsync();

            CollectionAssert.AreEqual(new[] { "PushPage:OwnedPresenter", "WaitForPop" }, log.Entries.Take(2).ToArray());
            var args = (OwnedArgs)navigator.Args[navigator.Args.Count - 1];
            Assert.AreEqual(OwnedSelectionKind.Idol, args.Selection.Kind);
            StringAssert.StartsWith("育成アイドル: アイドル2 ", view.LastDto.IdolLabel);
            Assert.IsFalse(view.LastDto.Slots[0].Filled);
            Assert.IsTrue(view.LastDto.Slots[1].Filled);
            Assert.AreEqual(RemovedInfo, log.Entries.Last());
        });

        [UnityTest]
        public IEnumerator SelectIdol_Cancelled_KeepsState() => UniTask.ToCoroutine(async () =>
        {
            OwnIdols(1, 2);
            var presenter = CreatePresenter();
            await presenter.InitializeAsync();
            var before = SetCount();
            navigator.PopResults.Enqueue(OwnedSelection.Cancelled);

            await presenter.SelectIdolAsync();
            await presenter.SelectIdolAsync();   // PopResults が空（null）でも同じ

            Assert.AreEqual(before, SetCount());
        });

        [UnityTest]
        public IEnumerator SelectIdol_Disposed_DoesNothing() => UniTask.ToCoroutine(async () =>
        {
            OwnIdols(1, 3);
            var presenter = CreatePresenter();
            await presenter.InitializeAsync();
            var before = SetCount();
            navigator.PopResults.Enqueue(OwnedSelection.ForIdol(2));
            presenter.Dispose();

            await presenter.SelectIdolAsync();

            Assert.AreEqual(before, SetCount());
        });

        [UnityTest]
        public IEnumerator SelectSlot_NoIdol_ShowsInfo() => UniTask.ToCoroutine(async () =>
        {
            var presenter = CreatePresenter();
            await presenter.InitializeAsync();
            log.Entries.Clear();

            await presenter.SelectSlotAsync(0);

            CollectionAssert.AreEqual(new[] { "Info:先に育成アイドルを選んでください。" }, log.Entries);
        });

        [UnityTest]
        public IEnumerator SelectSlot_BuildsRequestWithExclusions() => UniTask.ToCoroutine(async () =>
        {
            OwnIdols(1, 2, 3);   // 育成アイドル 2（Logic）
            OwnCards();
            var presenter = CreatePresenter();
            await presenter.InitializeAsync();
            await PutAsync(presenter, 1, "U20");
            await PutAsync(presenter, 2, "U30");

            await PutAsync(presenter, 0, "u9");

            var condition = ((OwnedArgs)navigator.Args[navigator.Args.Count - 1]).Selection;
            Assert.AreEqual(OwnedSelectionKind.SupportCard, condition.Kind);
            Assert.AreEqual(2, condition.IdolId);
            CollectionAssert.AreEqual(new[] { "U20", "U30" }, condition.ExcludedUids.ToArray());
            Assert.IsTrue(view.LastDto.Slots[0].Filled);
        });

        [UnityTest]
        public IEnumerator ClearSlot_ClearsSlot() => UniTask.ToCoroutine(async () =>
        {
            OwnIdols(2);
            OwnCards();
            var presenter = CreatePresenter();
            await presenter.InitializeAsync();
            await PutAsync(presenter, 0, "U20");
            Assert.IsTrue(view.LastDto.Slots[0].Filled);

            presenter.ClearSlot(0);

            Assert.IsFalse(view.LastDto.Slots[0].Filled);
        });

        [UnityTest]
        public IEnumerator NextScenario_Cycles() => UniTask.ToCoroutine(async () =>
        {
            OwnIdols(2);
            var presenter = CreatePresenter();
            await presenter.InitializeAsync();

            presenter.NextScenario();
            Assert.AreEqual("シナリオ: シナリオ2", view.LastDto.ScenarioLabel);
            presenter.NextScenario();
            Assert.AreEqual("シナリオ: シナリオ1", view.LastDto.ScenarioLabel);
        });

        [UnityTest]
        public IEnumerator Start_Success_GoesToProduce() => UniTask.ToCoroutine(async () =>
        {
            OwnIdols(2);
            OwnCards();
            var presenter = CreatePresenter();
            await presenter.InitializeAsync();
            await PutAsync(presenter, 0, "U20");
            await PutAsync(presenter, 2, "U30");
            produce.StartResults.Enqueue(() => UniTask.FromResult(ProduceStartResultKind.Succeeded));
            log.Entries.Clear();

            await presenter.StartAsync();

            CollectionAssert.AreEqual(new[] { "Start:2:U20,U30:1", "GoToProduce" }, log.Entries);
        });

        [UnityTest]
        public IEnumerator Start_InvalidFormation_ShowsInfo() => UniTask.ToCoroutine(async () =>
        {
            OwnIdols(2);
            var presenter = CreatePresenter();
            await presenter.InitializeAsync();
            produce.StartResults.Enqueue(() => UniTask.FromResult(ProduceStartResultKind.InvalidFormation));
            log.Entries.Clear();

            await presenter.StartAsync();

            CollectionAssert.AreEqual(new[] { "Start:2::1", "Info:編成が正しくありません。" }, log.Entries);
        });

        [UnityTest]
        public IEnumerator Start_AlreadyInProgress_ShowsInfoAndPops() => UniTask.ToCoroutine(async () =>
        {
            OwnIdols(2);
            var presenter = CreatePresenter();
            await presenter.InitializeAsync();
            produce.StartResults.Enqueue(() => UniTask.FromResult(ProduceStartResultKind.AlreadyInProgress));
            log.Entries.Clear();

            await presenter.StartAsync();

            CollectionAssert.AreEqual(
                new[] { "Start:2::1", "Info:進行中のプロデュースがあります。ホームから再開してください。", "PopPage:FormationPresenter" },
                log.Entries);
        });

        [UnityTest]
        public IEnumerator Start_WhileRunning_Ignored() => UniTask.ToCoroutine(async () =>
        {
            OwnIdols(2);
            var presenter = CreatePresenter();
            await presenter.InitializeAsync();
            produce.StartResults.Enqueue(() => UniTask.FromResult(ProduceStartResultKind.InvalidFormation));
            dialogs.InfoGate = new UniTaskCompletionSource();

            var first = presenter.StartAsync();
            await presenter.StartAsync();
            dialogs.InfoGate.TrySetResult();
            await first;

            Assert.AreEqual(1, log.Entries.Count(e => e.StartsWith("Start:")));
        });

        [UnityTest]
        public IEnumerator Start_Exception_ShowsError() => UniTask.ToCoroutine(async () =>
        {
            OwnIdols(2);
            var presenter = CreatePresenter();
            await presenter.InitializeAsync();
            log.Entries.Clear();
            LogAssert.Expect(LogType.Exception, new Regex("FakeProduceUseCase: no start result"));

            await presenter.StartAsync();

            Assert.AreEqual("Info:エラーが発生しました: FakeProduceUseCase: no start result", log.Entries.Last());
        });

        [UnityTest]
        public IEnumerator Start_NoIdol_DoesNothing() => UniTask.ToCoroutine(async () =>
        {
            var presenter = CreatePresenter();
            await presenter.InitializeAsync();
            log.Entries.Clear();

            await presenter.StartAsync();

            Assert.IsEmpty(log.Entries);
        });

        [UnityTest]
        public IEnumerator Back_PopsSelf() => UniTask.ToCoroutine(async () =>
        {
            var presenter = CreatePresenter();
            await presenter.InitializeAsync();
            log.Entries.Clear();

            await presenter.BackAsync();

            CollectionAssert.AreEqual(new[] { "PopPage:FormationPresenter" }, log.Entries);
        });
    }
}
