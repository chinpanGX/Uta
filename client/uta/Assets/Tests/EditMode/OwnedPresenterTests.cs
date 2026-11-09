using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using Uta.Client.Application.Owned;
using Uta.Client.Infrastructure.Idol;
using Uta.Client.Infrastructure.SupportCard;
using Uta.Client.Presentation.Owned;
using ZLinq;

namespace Uta.Client.Tests
{
    public sealed class OwnedPresenterTests
    {
        private const string IdolRows = "アイドルA [SSR]/感性　かけら 3/True|アイドルB [R]/理論　かけら 0/True";
        private const string SupportRowsRarityDesc =
            "サポートA [SSR]/歌唱　共通　Lv 3　解放 0/True|サポートA [SSR]/歌唱　共通　Lv 1　解放 0/True|サポートB [SR]/律動　感性　Lv 5　解放 1/True|サポートC [R]/表現　理論　Lv 1　解放 0/True";
        private const string SupportRowsLevelDesc =
            "サポートB [SR]/律動　感性　Lv 5　解放 1/True|サポートA [SSR]/歌唱　共通　Lv 3　解放 0/True|サポートC [R]/表現　理論　Lv 1　解放 0/True|サポートA [SSR]/歌唱　共通　Lv 1　解放 0/True";
        private const string SupportRowsAcquiredDesc =
            "サポートA [SSR]/歌唱　共通　Lv 1　解放 0/True|サポートC [R]/表現　理論　Lv 1　解放 0/True|サポートB [SR]/律動　感性　Lv 5　解放 1/True|サポートA [SSR]/歌唱　共通　Lv 3　解放 0/True";
        private const string SupportRowsRarityAsc =
            "サポートC [R]/表現　理論　Lv 1　解放 0/True|サポートB [SR]/律動　感性　Lv 5　解放 1/True|サポートA [SSR]/歌唱　共通　Lv 3　解放 0/True|サポートA [SSR]/歌唱　共通　Lv 1　解放 0/True";

        private sealed class FakeOwnedView : IOwnedView
        {
            private readonly CallLog log;

            public FakeOwnedView(CallLog log)
            {
                this.log = log;
            }

            public event Action<OwnedTabKind> TabSelected;
            public event Action SortKeyClicked;
            public event Action SortOrderClicked;
            public event Action PlanFilterClicked;
            public event Action TypeFilterClicked;
            public event Action<int> RowClicked;
            public event Action BackClicked;

            public void SetTitle(string title)
            {
                log.Add("SetTitle:" + title);
            }

            public void SetTabs(OwnedTabKind selected, bool visible)
            {
                log.Add("SetTabs:" + selected + "," + visible);
            }

            public void SetControls(bool visible, string sortLabel, string orderLabel, string planLabel, string typeLabel, bool filtersVisible)
            {
                log.Add("SetControls:" + visible + "," + sortLabel + "," + orderLabel + "," + planLabel + "," + typeLabel + "," + filtersVisible);
            }

            public void SetRows(IReadOnlyList<OwnedRowViewDto> rows)
            {
                log.Add("SetRows:" + string.Join("|", rows.Select(r => r.Title + "/" + r.Detail + "/" + r.Enabled).ToArray()));
            }

            public void RaiseTabSelected(OwnedTabKind tab)
            {
                TabSelected?.Invoke(tab);
            }

            public void RaiseSortKeyClicked()
            {
                SortKeyClicked?.Invoke();
            }

            public void RaiseSortOrderClicked()
            {
                SortOrderClicked?.Invoke();
            }

            public void RaisePlanFilterClicked()
            {
                PlanFilterClicked?.Invoke();
            }

            public void RaiseTypeFilterClicked()
            {
                TypeFilterClicked?.Invoke();
            }

            public void RaiseRowClicked(int index)
            {
                RowClicked?.Invoke(index);
            }

            public void RaiseBackClicked()
            {
                BackClicked?.Invoke();
            }
        }

        private CallLog log;
        private FakeOwnedView view;
        private FakeScreenNavigator navigator;
        private IdolRepository idolRepository;
        private SupportCardRepository supportCardRepository;

        [SetUp]
        public void SetUp()
        {
            log = new CallLog();
            view = new FakeOwnedView(log);
            navigator = new FakeScreenNavigator(log);
            idolRepository = new IdolRepository();
            supportCardRepository = new SupportCardRepository();
            OwnedFixtures.SetOwned(idolRepository, supportCardRepository);
        }

        private OwnedPresenter CreatePresenter(OwnedSelectionCondition condition = null)
        {
            var ownedListFactory = new OwnedListFactory(idolRepository, supportCardRepository, OwnedFixtures.CreateMaster());
            var ownedDtoFactory = new OwnedDtoFactory(ownedListFactory);
            return new OwnedPresenter(view, new OwnedArgs(condition), ownedDtoFactory, navigator);
        }

        private async UniTask<OwnedPresenter> InitializedAsync(OwnedSelectionCondition condition = null)
        {
            var presenter = CreatePresenter(condition);
            await presenter.InitializeAsync();
            return presenter;
        }

        private List<string> Entries(string prefix)
        {
            return log.Entries.FindAll(e => e.StartsWith(prefix));
        }

        private bool Has(string prefix)
        {
            return log.Entries.Exists(e => e.StartsWith(prefix));
        }

        [UnityTest]
        public IEnumerator InitializeAsync_Browse_ShowsIdolsWithoutControls() => UniTask.ToCoroutine(async () =>
        {
            await InitializedAsync();

            CollectionAssert.AreEqual(
                new[]
                {
                    "SetTitle:所持一覧",
                    "SetTabs:Idols,True",
                    "SetControls:False,並び: レア度,降順,プラン: 全て,タイプ: 全て,True",
                    "SetRows:" + IdolRows,
                },
                log.Entries);
        });

        [UnityTest]
        public IEnumerator SelectTab_Support_ShowsControlsAndSortedRows() => UniTask.ToCoroutine(async () =>
        {
            var presenter = await InitializedAsync();
            log.Entries.Clear();

            presenter.SelectTab(OwnedTabKind.SupportCards);

            CollectionAssert.AreEqual(
                new[]
                {
                    "SetTitle:所持一覧",
                    "SetTabs:SupportCards,True",
                    "SetControls:True,並び: レア度,降順,プラン: 全て,タイプ: 全て,True",
                    "SetRows:" + SupportRowsRarityDesc,
                },
                log.Entries);
        });

        [UnityTest]
        public IEnumerator CycleSortKey_CyclesThreeKeys() => UniTask.ToCoroutine(async () =>
        {
            var presenter = await InitializedAsync();
            presenter.SelectTab(OwnedTabKind.SupportCards);
            log.Entries.Clear();

            presenter.CycleSortKey();
            presenter.CycleSortKey();
            presenter.CycleSortKey();

            var controls = Entries("SetControls:");
            var rows = Entries("SetRows:");
            Assert.AreEqual("SetControls:True,並び: Lv,降順,プラン: 全て,タイプ: 全て,True", controls[0]);
            Assert.AreEqual("SetControls:True,並び: 入手順,降順,プラン: 全て,タイプ: 全て,True", controls[1]);
            Assert.AreEqual("SetControls:True,並び: レア度,降順,プラン: 全て,タイプ: 全て,True", controls[2]);
            Assert.AreEqual("SetRows:" + SupportRowsLevelDesc, rows[0]);
            Assert.AreEqual("SetRows:" + SupportRowsAcquiredDesc, rows[1]);
        });

        [UnityTest]
        public IEnumerator ToggleSortOrder_Toggles() => UniTask.ToCoroutine(async () =>
        {
            var presenter = await InitializedAsync();
            presenter.SelectTab(OwnedTabKind.SupportCards);
            log.Entries.Clear();

            presenter.ToggleSortOrder();

            Assert.AreEqual("SetControls:True,並び: レア度,昇順,プラン: 全て,タイプ: 全て,True", Entries("SetControls:")[0]);
            Assert.AreEqual("SetRows:" + SupportRowsRarityAsc, Entries("SetRows:")[0]);
        });

        [UnityTest]
        public IEnumerator CyclePlanFilter_Cycles() => UniTask.ToCoroutine(async () =>
        {
            var presenter = await InitializedAsync();
            presenter.SelectTab(OwnedTabKind.SupportCards);
            log.Entries.Clear();

            presenter.CyclePlanFilter();
            presenter.CyclePlanFilter();
            presenter.CyclePlanFilter();
            presenter.CyclePlanFilter();

            var controls = Entries("SetControls:");
            Assert.AreEqual("SetControls:True,並び: レア度,降順,プラン: 共通,タイプ: 全て,True", controls[0]);
            Assert.AreEqual("SetControls:True,並び: レア度,降順,プラン: 感性,タイプ: 全て,True", controls[1]);
            Assert.AreEqual("SetControls:True,並び: レア度,降順,プラン: 理論,タイプ: 全て,True", controls[2]);
            Assert.AreEqual("SetControls:True,並び: レア度,降順,プラン: 全て,タイプ: 全て,True", controls[3]);
            Assert.AreEqual("SetRows:サポートA [SSR]/歌唱　共通　Lv 3　解放 0/True|サポートA [SSR]/歌唱　共通　Lv 1　解放 0/True", Entries("SetRows:")[0]);
        });

        [UnityTest]
        public IEnumerator CycleTypeFilter_Cycles() => UniTask.ToCoroutine(async () =>
        {
            var presenter = await InitializedAsync();
            presenter.SelectTab(OwnedTabKind.SupportCards);
            log.Entries.Clear();

            presenter.CycleTypeFilter();
            presenter.CycleTypeFilter();
            presenter.CycleTypeFilter();
            presenter.CycleTypeFilter();
            presenter.CycleTypeFilter();

            var controls = Entries("SetControls:");
            Assert.AreEqual("SetControls:True,並び: レア度,降順,プラン: 全て,タイプ: 歌唱,True", controls[0]);
            Assert.AreEqual("SetControls:True,並び: レア度,降順,プラン: 全て,タイプ: 律動,True", controls[1]);
            Assert.AreEqual("SetControls:True,並び: レア度,降順,プラン: 全て,タイプ: 表現,True", controls[2]);
            Assert.AreEqual("SetControls:True,並び: レア度,降順,プラン: 全て,タイプ: アシスト,True", controls[3]);
            Assert.AreEqual("SetControls:True,並び: レア度,降順,プラン: 全て,タイプ: 全て,True", controls[4]);
            Assert.AreEqual("SetRows:", Entries("SetRows:")[3]);
        });

        [UnityTest]
        public IEnumerator SelectTab_KeepsSupportFilters() => UniTask.ToCoroutine(async () =>
        {
            var presenter = await InitializedAsync();
            presenter.SelectTab(OwnedTabKind.SupportCards);
            presenter.CycleTypeFilter();

            presenter.SelectTab(OwnedTabKind.Idols);
            presenter.SelectTab(OwnedTabKind.SupportCards);

            var controls = Entries("SetControls:");
            Assert.AreEqual("SetControls:True,並び: レア度,降順,プラン: 全て,タイプ: 歌唱,True", controls[controls.Count - 1]);
        });

        [UnityTest]
        public IEnumerator SelectTab_InSelectionMode_Ignored() => UniTask.ToCoroutine(async () =>
        {
            var presenter = await InitializedAsync(OwnedSelectionCondition.ForIdol());
            log.Entries.Clear();

            presenter.SelectTab(OwnedTabKind.SupportCards);

            Assert.IsEmpty(log.Entries);
        });

        [UnityTest]
        public IEnumerator SelectRowAsync_BrowseSupport_PushesDetail() => UniTask.ToCoroutine(async () =>
        {
            var presenter = await InitializedAsync();
            presenter.SelectTab(OwnedTabKind.SupportCards);
            log.Entries.Clear();

            await presenter.SelectRowAsync(1);

            CollectionAssert.AreEqual(new[] { "PushPage:SupportDetailPresenter" }, log.Entries);
            var args = (SupportDetailArgs)navigator.Args[0];
            Assert.AreEqual("01D", args.SupportCardUniqueId);
        });

        [UnityTest]
        public IEnumerator SelectRowAsync_BrowseIdol_DoesNothing() => UniTask.ToCoroutine(async () =>
        {
            var presenter = await InitializedAsync();

            await presenter.SelectRowAsync(0);

            Assert.IsFalse(Has("PushPage:"));
            Assert.IsFalse(Has("PopPage:"));
        });

        [UnityTest]
        public IEnumerator SelectRowAsync_Selection_SetsResultAndPops() => UniTask.ToCoroutine(async () =>
        {
            var presenter = await InitializedAsync(OwnedSelectionCondition.ForIdol());
            log.Entries.Clear();

            await presenter.SelectRowAsync(1);

            CollectionAssert.AreEqual(new[] { "PopPage:OwnedPresenter" }, log.Entries);
            var selection = (OwnedSelection)await presenter.CompleteAsync();
            Assert.IsFalse(selection.IsCancelled);
            Assert.AreEqual(2, selection.IdolId);
        });

        [UnityTest]
        public IEnumerator SelectRowAsync_Twice_PopsOnce() => UniTask.ToCoroutine(async () =>
        {
            var presenter = await InitializedAsync(OwnedSelectionCondition.ForIdol());

            await presenter.SelectRowAsync(0);
            await presenter.SelectRowAsync(1);

            Assert.AreEqual(1, Entries("PopPage:").Count);
        });

        [UnityTest]
        public IEnumerator SelectRowThenBack_PopsOnce() => UniTask.ToCoroutine(async () =>
        {
            var presenter = await InitializedAsync(OwnedSelectionCondition.ForIdol());

            await presenter.SelectRowAsync(1);
            await presenter.BackAsync();

            Assert.AreEqual(1, Entries("PopPage:").Count);
            var selection = (OwnedSelection)await presenter.CompleteAsync();
            Assert.IsFalse(selection.IsCancelled);
            Assert.AreEqual(2, selection.IdolId);
        });

        [UnityTest]
        public IEnumerator SelectRowAsync_DisabledRow_DoesNothing() => UniTask.ToCoroutine(async () =>
        {
            var presenter = await InitializedAsync(OwnedSelectionCondition.ForIdol(new[] { 2 }));

            await presenter.SelectRowAsync(1);

            Assert.IsFalse(Has("PopPage:"));
        });

        [UnityTest]
        public IEnumerator SelectRowAsync_OutOfRange_DoesNothing() => UniTask.ToCoroutine(async () =>
        {
            var presenter = await InitializedAsync(OwnedSelectionCondition.ForIdol());

            await presenter.SelectRowAsync(5);
            await presenter.SelectRowAsync(-1);

            Assert.IsFalse(Has("PopPage:"));
            Assert.IsFalse(Has("PushPage:"));
        });

        [UnityTest]
        public IEnumerator BackAsync_Selection_CompletesCancelled() => UniTask.ToCoroutine(async () =>
        {
            var presenter = await InitializedAsync(OwnedSelectionCondition.ForIdol());

            await presenter.BackAsync();

            Assert.IsTrue(Has("PopPage:OwnedPresenter"));
            var selection = (OwnedSelection)await presenter.CompleteAsync();
            Assert.IsTrue(selection.IsCancelled);
        });

        [UnityTest]
        public IEnumerator InitializeAsync_IdolSelection_HidesTabsAndControls() => UniTask.ToCoroutine(async () =>
        {
            await InitializedAsync(OwnedSelectionCondition.ForIdol(new[] { 2 }));

            CollectionAssert.AreEqual(
                new[]
                {
                    "SetTitle:育成アイドルを選ぶ",
                    "SetTabs:Idols,False",
                    "SetControls:False,並び: レア度,降順,プラン: 全て,タイプ: 全て,False",
                    "SetRows:アイドルA [SSR]/感性　かけら 3/True|アイドルB [R]/理論　かけら 0　（編成済み）/False",
                },
                log.Entries);
        });

        [UnityTest]
        public IEnumerator InitializeAsync_Material_HidesFiltersAndSortsByLevelAscending() => UniTask.ToCoroutine(async () =>
        {
            await InitializedAsync(OwnedSelectionCondition.ForLimitBreakMaterial(10, "01A"));

            CollectionAssert.AreEqual(
                new[]
                {
                    "SetTitle:素材にするカードを選ぶ",
                    "SetTabs:SupportCards,False",
                    "SetControls:True,並び: Lv,昇順,プラン: 全て,タイプ: 全て,False",
                    "SetRows:サポートA [SSR]/歌唱　共通　Lv 1　解放 0/True",
                },
                log.Entries);
        });

        [UnityTest]
        public IEnumerator Dispose_UnsubscribesEvents() => UniTask.ToCoroutine(async () =>
        {
            var presenter = await InitializedAsync();
            presenter.Dispose();
            log.Entries.Clear();

            view.RaiseBackClicked();

            Assert.IsFalse(Has("PopPage:"));
        });

        [UnityTest]
        public IEnumerator SelectRowAsync_AfterDispose_DoesNothing() => UniTask.ToCoroutine(async () =>
        {
            var presenter = await InitializedAsync();
            presenter.Dispose();
            log.Entries.Clear();

            await presenter.SelectRowAsync(0);

            Assert.IsFalse(Has("PushPage:"));
            Assert.IsFalse(Has("PopPage:"));
        });
    }
}
