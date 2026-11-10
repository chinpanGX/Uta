using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using Uta.Client.Application.Owned;
using Uta.Client.Application.Support;
using Uta.Client.Domain.SupportCard;
using Uta.Client.Infrastructure.Idol;
using Uta.Client.Infrastructure.Player;
using Uta.Client.Infrastructure.SupportCard;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Owned;
using Uta.Shared.Domain.Produce;

namespace Uta.Client.Tests
{
    public sealed class SupportDetailPresenterTests
    {
        private static readonly string[] ReshowPrefixes = { "SetInfo:", "SetAbilities:", "SetResources:", "SetLevelButtons:", "SetLimitBreak:" };

        private sealed class FakeSupportDetailView : ISupportDetailView
        {
            private readonly CallLog log;

            public FakeSupportDetailView(CallLog log)
            {
                this.log = log;
            }

            public event Action Plus1Clicked;
            public event Action Plus5Clicked;
            public event Action MaxClicked;
            public event Action LimitBreakClicked;
            public event Action BackClicked;

            public void SetInfo(IReadOnlyList<string> lines)
            {
                log.Add("SetInfo:" + string.Join("|", lines));
            }

            public void SetAbilities(IReadOnlyList<string> lines)
            {
                log.Add("SetAbilities:" + string.Join("|", lines));
            }

            public void SetResources(string text)
            {
                log.Add("SetResources:" + text);
            }

            public void SetLevelButtons(string plus1, string plus5, string max, bool plus1Enabled, bool plus5Enabled, bool maxEnabled)
            {
                log.Add("SetLevelButtons:" + plus1 + "," + plus5 + "," + max + "," + plus1Enabled + "," + plus5Enabled + "," + maxEnabled);
            }

            public void SetLimitBreak(string label, bool enabled)
            {
                log.Add("SetLimitBreak:" + label + "," + enabled);
            }

            public bool HasSubscribers
            {
                get { return Plus1Clicked != null || Plus5Clicked != null || MaxClicked != null || LimitBreakClicked != null || BackClicked != null; }
            }

            public void RaisePlus1Clicked()
            {
                Plus1Clicked?.Invoke();
            }

            public void RaiseBackClicked()
            {
                BackClicked?.Invoke();
            }
        }

        private CallLog log;
        private FakeSupportDetailView view;
        private FakeSupportUseCase supportUseCase;
        private FakeDialogOpener dialogOpener;
        private FakeScreenNavigator navigator;
        private PlayerRepository playerRepository;
        private SupportCardRepository supportCardRepository;

        [SetUp]
        public void SetUp()
        {
            log = new CallLog();
            view = new FakeSupportDetailView(log);
            supportUseCase = new FakeSupportUseCase(log);
            dialogOpener = new FakeDialogOpener(log);
            navigator = new FakeScreenNavigator(log);
            playerRepository = new PlayerRepository();
            supportCardRepository = new SupportCardRepository();
            OwnedFixtures.SetPlayer(playerRepository);
            OwnedFixtures.SetOwned(new IdolRepository(), supportCardRepository);
        }

        private SupportDetailPresenter CreatePresenter(string uid = "01A")
        {
            var master = OwnedFixtures.CreateMaster();
            var factory = new SupportDetailDtoFactory(playerRepository, supportCardRepository, master, new SupportLevelPlanner(master),
                new OwnedListFactory(new IdolRepository(), supportCardRepository, master));
            return new SupportDetailPresenter(view, new SupportDetailArgs(uid), factory, supportUseCase, dialogOpener, navigator);
        }

        /// <summary>InitializeAsync のあと、記録を空にして返す。</summary>
        private async UniTask<SupportDetailPresenter> InitializedAsync(string uid = "01A")
        {
            var presenter = CreatePresenter(uid);
            await presenter.InitializeAsync();
            log.Entries.Clear();
            return presenter;
        }

        private bool Has(string prefix)
        {
            return log.Entries.Exists(e => e.StartsWith(prefix));
        }

        private void AssertReshown(int startIndex)
        {
            Assert.AreEqual(startIndex + ReshowPrefixes.Length, log.Entries.Count);
            for (var i = 0; i < ReshowPrefixes.Length; i++)
            {
                StringAssert.StartsWith(ReshowPrefixes[i], log.Entries[startIndex + i]);
            }
        }

        [UnityTest]
        public IEnumerator InitializeAsync_ShowsInfoAbilitiesAndButtons() => UniTask.ToCoroutine(async () =>
        {
            var presenter = CreatePresenter();
            await presenter.InitializeAsync();

            CollectionAssert.AreEqual(
                new[]
                {
                    "SetInfo:サポートA|SSR　歌唱　共通|Lv 3 / 40|上限解放 0 / 4",
                    "SetAbilities:1 初期歌唱 +" + ProduceFormulas.AbilityValue(10, 50, 3, 60),
                    "SetResources:強化pt 1,000　ゴールド 1,000",
                    "SetLevelButtons:+1 → Lv 4（強化pt 100 / G 50）,+5 → Lv 8（強化pt 500 / G 250）,最大 → Lv 13（強化pt 1,000 / G 500）,True,True,True",
                    "SetLimitBreak:上限解放（素材を選ぶ）,True",
                },
                log.Entries);
        });

        [UnityTest]
        public IEnumerator InitializeAsync_AtMaxLevel_ButtonsShowCap() => UniTask.ToCoroutine(async () =>
        {
            supportCardRepository.Upsert(new SupportCardEntity("01A", 10, 40, 0));
            var presenter = CreatePresenter();
            await presenter.InitializeAsync();

            Assert.IsTrue(log.Entries.Contains("SetLevelButtons:+1（上限）,+5（上限）,最大（上限）,False,False,False"));
        });

        [UnityTest]
        public IEnumerator InitializeAsync_MaxNotAffordable_ShowsShortage() => UniTask.ToCoroutine(async () =>
        {
            OwnedFixtures.SetPlayer(playerRepository, supportPoints: 0);
            var presenter = CreatePresenter();
            await presenter.InitializeAsync();

            Assert.IsTrue(log.Entries.Contains("SetLevelButtons:+1 → Lv 4（強化pt 100 / G 50）,+5 → Lv 8（強化pt 500 / G 250）,最大（足りません）,True,True,False"));
        });

        [UnityTest]
        public IEnumerator LevelUpAsync_Confirmed_ExecutesAndRefreshes() => UniTask.ToCoroutine(async () =>
        {
            var presenter = await InitializedAsync();
            supportUseCase.LevelUpResults.Enqueue(() =>
            {
                supportCardRepository.Upsert(new SupportCardEntity("01A", 10, 4, 0));
                return UniTask.FromResult(LevelUpResult.Success(4, 900, 950));
            });

            await presenter.LevelUpAsync(LevelUpStepKind.Plus1);

            Assert.AreEqual(7, log.Entries.Count);
            Assert.AreEqual("Confirm:Lv 3 → Lv 4 に上げます。\n強化pt 100・ゴールド 50 を使います。", log.Entries[0]);
            Assert.AreEqual("LevelUp:01A,4", log.Entries[1]);
            AssertReshown(2);
            CollectionAssert.AreEqual(new[] { "LevelUp:01A,4" }, supportUseCase.Calls);
        });

        [UnityTest]
        public IEnumerator LevelUpAsync_Declined_DoesNotExecute() => UniTask.ToCoroutine(async () =>
        {
            var presenter = await InitializedAsync();
            dialogOpener.ConfirmResults.Enqueue(DialogResult.Negative);

            await presenter.LevelUpAsync(LevelUpStepKind.Plus1);

            Assert.AreEqual(0, supportUseCase.Calls.Count);
            Assert.IsFalse(Has("SetInfo:"));
        });

        [UnityTest]
        public IEnumerator LevelUpAsync_UsesRepositoryAtClickTime() => UniTask.ToCoroutine(async () =>
        {
            var presenter = await InitializedAsync();
            supportCardRepository.Upsert(new SupportCardEntity("01A", 10, 5, 0));
            supportUseCase.LevelUpResults.Enqueue(() => UniTask.FromResult(LevelUpResult.Success(6, 900, 950)));

            await presenter.LevelUpAsync(LevelUpStepKind.Plus1);

            Assert.AreEqual("Confirm:Lv 5 → Lv 6 に上げます。\n強化pt 100・ゴールド 50 を使います。", log.Entries[0]);
            CollectionAssert.AreEqual(new[] { "LevelUp:01A,6" }, supportUseCase.Calls);
        });

        [UnityTest]
        public IEnumerator LevelUpAsync_NotEnoughPoints_ShowsInfo() => UniTask.ToCoroutine(async () =>
        {
            var presenter = await InitializedAsync();
            supportUseCase.LevelUpResults.Enqueue(() => UniTask.FromResult(LevelUpResult.Rejected(SupportResultKind.NotEnoughPoints)));

            await presenter.LevelUpAsync(LevelUpStepKind.Plus1);

            Assert.AreEqual(8, log.Entries.Count);
            Assert.AreEqual("Confirm:Lv 3 → Lv 4 に上げます。\n強化pt 100・ゴールド 50 を使います。", log.Entries[0]);
            Assert.AreEqual("LevelUp:01A,4", log.Entries[1]);
            Assert.AreEqual("Info:強化ptが足りません。", log.Entries[2]);
            AssertReshown(3);
        });

        [UnityTest]
        public IEnumerator LimitBreakAsync_NoMaterial_ShowsInfo() => UniTask.ToCoroutine(async () =>
        {
            supportCardRepository.Remove("01D");
            var presenter = await InitializedAsync();

            await presenter.LimitBreakAsync();

            CollectionAssert.AreEqual(new[] { "Info:素材にできる同じカードがありません。" }, log.Entries);
            Assert.IsFalse(Has("PushPage:"));
        });

        [UnityTest]
        public IEnumerator LimitBreakAsync_SelectsMaterialThenConfirms() => UniTask.ToCoroutine(async () =>
        {
            var presenter = await InitializedAsync();
            navigator.PopResults.Enqueue(OwnedSelection.ForSupportCard("01D"));
            supportUseCase.LimitBreakResults.Enqueue(() => UniTask.FromResult(LimitBreakResult.Success(1, 45)));

            await presenter.LimitBreakAsync();

            Assert.AreEqual(9, log.Entries.Count);
            Assert.AreEqual("PushPage:OwnedPresenter", log.Entries[0]);
            Assert.AreEqual("WaitForPop", log.Entries[1]);
            Assert.AreEqual("Confirm:Lv 1 の「サポートA」を素材にして上限解放します（0 → 1）。\n素材は無くなります。強化に使った強化pt・ゴールドは戻りません。", log.Entries[2]);
            Assert.AreEqual("LimitBreak:01A,01D", log.Entries[3]);
            AssertReshown(4);
            CollectionAssert.AreEqual(new[] { "LimitBreak:01A,01D" }, supportUseCase.Calls);
            var ownedArgs = navigator.Args[0] as OwnedArgs;
            Assert.IsNotNull(ownedArgs);
            Assert.AreEqual(OwnedSelectionKind.LimitBreakMaterial, ownedArgs.Selection.Kind);
            Assert.AreEqual(10, ownedArgs.Selection.MaterialCardId);
            Assert.AreEqual("01A", ownedArgs.Selection.MaterialTargetUid);
        });

        [UnityTest]
        public IEnumerator LimitBreakAsync_SelectionCancelled_DoesNotExecute() => UniTask.ToCoroutine(async () =>
        {
            var presenter = await InitializedAsync();

            await presenter.LimitBreakAsync();

            Assert.IsFalse(Has("Confirm:"));
            Assert.AreEqual(0, supportUseCase.Calls.Count);
        });

        [UnityTest]
        public IEnumerator LimitBreakAsync_AtMax_DoesNothing() => UniTask.ToCoroutine(async () =>
        {
            supportCardRepository.Upsert(new SupportCardEntity("01A", 10, 3, 4));
            var presenter = CreatePresenter();
            await presenter.InitializeAsync();
            Assert.IsTrue(log.Entries.Contains("SetLimitBreak:上限解放（最大）,False"));
            log.Entries.Clear();

            await presenter.LimitBreakAsync();

            Assert.IsFalse(Has("PushPage:"));
            Assert.IsFalse(Has("Info:"));
        });

        [UnityTest]
        public IEnumerator Refresh_CardGone_DisablesAll() => UniTask.ToCoroutine(async () =>
        {
            var presenter = CreatePresenter("01Z");
            await presenter.InitializeAsync();

            CollectionAssert.AreEqual(
                new[]
                {
                    "SetInfo:このカードは所持していません。",
                    "SetAbilities:",
                    "SetResources:",
                    "SetLevelButtons:+1,+5,最大,False,False,False",
                    "SetLimitBreak:上限解放,False",
                },
                log.Entries);
        });

        [UnityTest]
        public IEnumerator LevelUpAsync_AfterDispose_DoesNothing() => UniTask.ToCoroutine(async () =>
        {
            var presenter = await InitializedAsync();
            presenter.Dispose();

            await presenter.LevelUpAsync(LevelUpStepKind.Plus1);

            Assert.IsFalse(Has("Confirm:"));
            Assert.AreEqual(0, supportUseCase.Calls.Count);
        });

        [UnityTest]
        public IEnumerator LevelUpAsync_WhileRunning_Ignored() => UniTask.ToCoroutine(async () =>
        {
            var gate = new UniTaskCompletionSource<LevelUpResult>();
            supportUseCase.LevelUpResults.Enqueue(() => gate.Task);
            var presenter = await InitializedAsync();

            var first = presenter.LevelUpAsync(LevelUpStepKind.Plus1);
            await presenter.LevelUpAsync(LevelUpStepKind.Plus1);
            gate.TrySetResult(LevelUpResult.Rejected(SupportResultKind.Failed));
            await first;

            CollectionAssert.AreEqual(new[] { "LevelUp:01A,4" }, supportUseCase.Calls);
        });

        [UnityTest]
        public IEnumerator LevelUpAsync_DisposedDuringCall_TouchesNothing() => UniTask.ToCoroutine(async () =>
        {
            var gate = new UniTaskCompletionSource<LevelUpResult>();
            supportUseCase.LevelUpResults.Enqueue(() => gate.Task);
            var presenter = await InitializedAsync();

            var first = presenter.LevelUpAsync(LevelUpStepKind.Plus1);
            presenter.Dispose();
            gate.TrySetResult(LevelUpResult.Success(4, 900, 950));
            await first;

            Assert.AreEqual(2, log.Entries.Count);
            Assert.AreEqual("LevelUp:01A,4", log.Entries[1]);
        });

        [UnityTest]
        public IEnumerator LevelUpAsync_Failed_ShowsNothing() => UniTask.ToCoroutine(async () =>
        {
            var presenter = await InitializedAsync();
            supportUseCase.LevelUpResults.Enqueue(() => UniTask.FromResult(LevelUpResult.Rejected(SupportResultKind.Failed)));

            await presenter.LevelUpAsync(LevelUpStepKind.Plus1);

            Assert.AreEqual(2, log.Entries.Count);
            StringAssert.StartsWith("Confirm:", log.Entries[0]);
            Assert.AreEqual("LevelUp:01A,4", log.Entries[1]);
        });

        [UnityTest]
        public IEnumerator LevelUpAsync_NotEnoughGold_ShowsInfo() => UniTask.ToCoroutine(async () =>
        {
            await AssertLevelUpInfoAsync(SupportResultKind.NotEnoughGold, "Info:ゴールドが足りません。");
        });

        [UnityTest]
        public IEnumerator LevelUpAsync_InvalidTarget_ShowsInfo() => UniTask.ToCoroutine(async () =>
        {
            await AssertLevelUpInfoAsync(SupportResultKind.InvalidTarget, "Info:レベルを上げられません。");
        });

        [UnityTest]
        public IEnumerator LevelUpAsync_NotOwned_ShowsInfo() => UniTask.ToCoroutine(async () =>
        {
            await AssertLevelUpInfoAsync(SupportResultKind.NotOwned, "Info:このカードは所持していません。");
        });

        private async UniTask AssertLevelUpInfoAsync(SupportResultKind kind, string info)
        {
            var presenter = await InitializedAsync();
            supportUseCase.LevelUpResults.Enqueue(() => UniTask.FromResult(LevelUpResult.Rejected(kind)));

            await presenter.LevelUpAsync(LevelUpStepKind.Plus1);

            Assert.AreEqual("LevelUp:01A,4", log.Entries[1]);
            Assert.AreEqual(info, log.Entries[2]);
        }

        [UnityTest]
        public IEnumerator LimitBreakAsync_MaxLimitBreak_ShowsInfo() => UniTask.ToCoroutine(async () =>
        {
            await AssertLimitBreakInfoAsync(SupportResultKind.MaxLimitBreak, "Info:これ以上は解放できません。");
        });

        [UnityTest]
        public IEnumerator LimitBreakAsync_InvalidMaterial_ShowsInfo() => UniTask.ToCoroutine(async () =>
        {
            await AssertLimitBreakInfoAsync(SupportResultKind.InvalidMaterial, "Info:素材にできません。");
        });

        [UnityTest]
        public IEnumerator LimitBreakAsync_NotOwned_ShowsInfo() => UniTask.ToCoroutine(async () =>
        {
            await AssertLimitBreakInfoAsync(SupportResultKind.NotOwned, "Info:このカードは所持していません。");
        });

        private async UniTask AssertLimitBreakInfoAsync(SupportResultKind kind, string info)
        {
            var presenter = await InitializedAsync();
            navigator.PopResults.Enqueue(OwnedSelection.ForSupportCard("01D"));
            supportUseCase.LimitBreakResults.Enqueue(() => UniTask.FromResult(LimitBreakResult.Rejected(kind)));

            await presenter.LimitBreakAsync();

            Assert.AreEqual("LimitBreak:01A,01D", log.Entries[3]);
            Assert.AreEqual(info, log.Entries[4]);
        }

        [UnityTest]
        public IEnumerator LimitBreakAsync_DisposedDuringSelection_TouchesNothing() => UniTask.ToCoroutine(async () =>
        {
            var gate = new UniTaskCompletionSource<object>();
            navigator.PopGates.Enqueue(gate);
            var presenter = await InitializedAsync();

            var first = presenter.LimitBreakAsync();
            presenter.Dispose();
            gate.TrySetResult(OwnedSelection.ForSupportCard("01D"));
            await first;

            Assert.AreEqual("WaitForPop", log.Entries[log.Entries.Count - 1]);
            Assert.AreEqual(0, supportUseCase.Calls.Count);
        });

        [UnityTest]
        public IEnumerator BackAsync_PopsPageOnce() => UniTask.ToCoroutine(async () =>
        {
            var presenter = await InitializedAsync();

            await presenter.BackAsync();
            await presenter.BackAsync();

            CollectionAssert.AreEqual(new[] { "PopPage:SupportDetailPresenter" }, log.Entries);
        });

        [UnityTest]
        public IEnumerator Plus1Clicked_RunsLevelUp() => UniTask.ToCoroutine(async () =>
        {
            await InitializedAsync();
            supportUseCase.LevelUpResults.Enqueue(() => UniTask.FromResult(LevelUpResult.Rejected(SupportResultKind.Failed)));

            view.RaisePlus1Clicked();

            Assert.IsTrue(Has("Confirm:"));
        });

        [UnityTest]
        public IEnumerator Dispose_UnsubscribesEvents() => UniTask.ToCoroutine(async () =>
        {
            var presenter = await InitializedAsync();
            Assert.IsTrue(view.HasSubscribers);

            presenter.Dispose();

            Assert.IsFalse(view.HasSubscribers);
        });
    }
}
