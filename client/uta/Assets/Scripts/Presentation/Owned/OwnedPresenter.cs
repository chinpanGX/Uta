using System;
using Cysharp.Threading.Tasks;
using UnityScreenNavigator;
using Uta.Client.Application.Owned;
using Uta.Shared.Domain.Lesson;
using Uta.Shared.Domain.Produce;
using VContainer;

namespace Uta.Client.Presentation.Owned
{
    [AssetAddress(nameof(OwnedPage))]
    public sealed class OwnedPresenter : IScreenWithArgs<OwnedArgs>
    {
        private readonly IOwnedView ownedView;
        private readonly OwnedDtoFactory ownedDtoFactory;
        private readonly IScreenNavigator screenNavigator;
        private readonly OwnedSelectionCondition condition;
        private readonly bool filtersVisible;
        private OwnedSelection result = OwnedSelection.Cancelled;
        private OwnedViewDto current;
        private bool running;
        private bool closed;
        private bool disposed;
        private OwnedTabKind tab;
        private OwnedSortKind sort;
        private bool descending;
        private Plan? planFilter;
        private SupportType? typeFilter;

        [Inject]
        public OwnedPresenter(OwnedPage ownedPage, OwnedArgs ownedArgs, OwnedDtoFactory ownedDtoFactory, IScreenNavigator screenNavigator)
            : this((IOwnedView)ownedPage, ownedArgs, ownedDtoFactory, screenNavigator)
        {
        }

        /// <summary>ownedArgs が null なら ArgumentNullException。</summary>
        public OwnedPresenter(IOwnedView ownedView, OwnedArgs ownedArgs, OwnedDtoFactory ownedDtoFactory, IScreenNavigator screenNavigator)
        {
            if (ownedArgs == null)
            {
                throw new ArgumentNullException(nameof(ownedArgs));
            }

            this.ownedView = ownedView;
            this.ownedDtoFactory = ownedDtoFactory;
            this.screenNavigator = screenNavigator;
            condition = ownedArgs.Selection;
            filtersVisible = condition == null || condition.Kind == OwnedSelectionKind.SupportCard;

            tab = OwnedTabKind.Idols;
            sort = OwnedSortKind.Rarity;
            descending = true;
            if (condition != null)
            {
                switch (condition.Kind)
                {
                    case OwnedSelectionKind.SupportCard:
                        tab = OwnedTabKind.SupportCards;
                        break;
                    case OwnedSelectionKind.LimitBreakMaterial:
                        tab = OwnedTabKind.SupportCards;
                        sort = OwnedSortKind.Level;
                        descending = false;
                        break;
                }
            }
        }

        public UniTask InitializeAsync()
        {
            ownedView.TabSelected += OnTabSelected;
            ownedView.SortKeyClicked += OnSortKeyClicked;
            ownedView.SortOrderClicked += OnSortOrderClicked;
            ownedView.PlanFilterClicked += OnPlanFilterClicked;
            ownedView.TypeFilterClicked += OnTypeFilterClicked;
            ownedView.RowClicked += OnRowClicked;
            ownedView.BackClicked += OnBackClicked;
            Refresh();
            return UniTask.CompletedTask;
        }

        /// <summary>詳細から戻ったとき。</summary>
        public void DidPopEnter()
        {
            if (disposed)
            {
                return;
            }

            Refresh();
        }

        public void Dispose()
        {
            disposed = true;
            ownedView.TabSelected -= OnTabSelected;
            ownedView.SortKeyClicked -= OnSortKeyClicked;
            ownedView.SortOrderClicked -= OnSortOrderClicked;
            ownedView.PlanFilterClicked -= OnPlanFilterClicked;
            ownedView.TypeFilterClicked -= OnTypeFilterClicked;
            ownedView.RowClicked -= OnRowClicked;
            ownedView.BackClicked -= OnBackClicked;
        }

        /// <summary>基盤が Pop の完了時に呼び、WaitForPopAsync の結果になる。</summary>
        public UniTask<object> CompleteAsync()
        {
            return UniTask.FromResult<object>(result);
        }

        public async UniTask SelectRowAsync(int index)
        {
            if (running || closed || disposed)
            {
                return;
            }

            running = true;
            try
            {
                if (current == null || index < 0 || index >= current.Rows.Count)
                {
                    return;
                }

                var row = current.Rows[index];
                if (condition != null)
                {
                    if (!row.Enabled)
                    {
                        return;
                    }

                    result = row.SupportCardUid == null ? OwnedSelection.ForIdol(row.IdolId) : OwnedSelection.ForSupportCard(row.SupportCardUid);
                    closed = true;
                    await screenNavigator.PopPageAsync(this, playAnimation: false);
                }
                else if (row.SupportCardUid != null)
                {
                    await screenNavigator.PushPageAsync<SupportDetailPresenter, SupportDetailArgs>(new SupportDetailArgs(row.SupportCardUid), playAnimation: false);
                }
            }
            finally
            {
                running = false;
            }
        }

        public async UniTask BackAsync()
        {
            if (running || closed || disposed)
            {
                return;
            }

            running = true;
            try
            {
                closed = true;
                await screenNavigator.PopPageAsync(this, playAnimation: false);
            }
            finally
            {
                running = false;
            }
        }

        public void SelectTab(OwnedTabKind selected)
        {
            if (disposed || current == null || condition != null || selected == tab)
            {
                return;
            }

            tab = selected;
            Refresh();
        }

        public void CycleSortKey()
        {
            if (disposed || current == null || tab != OwnedTabKind.SupportCards)
            {
                return;
            }

            switch (sort)
            {
                case OwnedSortKind.Rarity: sort = OwnedSortKind.Level; break;
                case OwnedSortKind.Level: sort = OwnedSortKind.Acquired; break;
                default: sort = OwnedSortKind.Rarity; break;
            }

            Refresh();
        }

        public void ToggleSortOrder()
        {
            if (disposed || current == null || tab != OwnedTabKind.SupportCards)
            {
                return;
            }

            descending = !descending;
            Refresh();
        }

        public void CyclePlanFilter()
        {
            if (disposed || current == null || tab != OwnedTabKind.SupportCards || !filtersVisible)
            {
                return;
            }

            if (!planFilter.HasValue)
            {
                planFilter = Plan.Free;
            }
            else if (planFilter.Value == Plan.Free)
            {
                planFilter = Plan.Sense;
            }
            else if (planFilter.Value == Plan.Sense)
            {
                planFilter = Plan.Logic;
            }
            else
            {
                planFilter = null;
            }

            Refresh();
        }

        public void CycleTypeFilter()
        {
            if (disposed || current == null || tab != OwnedTabKind.SupportCards || !filtersVisible)
            {
                return;
            }

            if (!typeFilter.HasValue)
            {
                typeFilter = SupportType.Singing;
            }
            else if (typeFilter.Value == SupportType.Singing)
            {
                typeFilter = SupportType.Rhythm;
            }
            else if (typeFilter.Value == SupportType.Rhythm)
            {
                typeFilter = SupportType.Expression;
            }
            else if (typeFilter.Value == SupportType.Expression)
            {
                typeFilter = SupportType.Assist;
            }
            else
            {
                typeFilter = null;
            }

            Refresh();
        }

        private void Refresh()
        {
            current = ownedDtoFactory.Create(condition, tab, sort, descending, planFilter, typeFilter);
            ownedView.SetTitle(current.Title);
            ownedView.SetTabs(tab, current.TabsVisible);
            ownedView.SetControls(current.ControlsVisible, current.SortLabel, current.OrderLabel, current.PlanLabel, current.TypeLabel, current.FiltersVisible);
            ownedView.SetRows(current.Rows);
        }

        private void OnTabSelected(OwnedTabKind selected)
        {
            SelectTab(selected);
        }

        private void OnSortKeyClicked()
        {
            CycleSortKey();
        }

        private void OnSortOrderClicked()
        {
            ToggleSortOrder();
        }

        private void OnPlanFilterClicked()
        {
            CyclePlanFilter();
        }

        private void OnTypeFilterClicked()
        {
            CycleTypeFilter();
        }

        private void OnRowClicked(int index)
        {
            SelectRowAsync(index).Forget();
        }

        private void OnBackClicked()
        {
            BackAsync().Forget();
        }
    }
}
