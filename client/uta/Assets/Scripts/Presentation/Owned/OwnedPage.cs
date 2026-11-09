using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Common.Parts;

namespace Uta.Client.Presentation.Owned
{
    /// <summary>所持一覧のページ（Prefab OwnedPage のルート）。</summary>
    public sealed class OwnedPage : PageBase, IOwnedView
    {
        [SerializeField] private HeaderView header;
        [SerializeField] private TabBarView tabBar;
        [SerializeField] private RectTransform controls;
        [SerializeField] private RectTransform filterRow;
        [SerializeField] private ButtonView sortButton;
        [SerializeField] private ButtonView orderButton;
        [SerializeField] private ButtonView planButton;
        [SerializeField] private ButtonView typeButton;
        [SerializeField] private TMP_Text emptyText;
        [SerializeField] private RectTransform listContent;
        [SerializeField] private ListRowView rowPrefab;

        public event Action<OwnedTabKind> TabSelected;
        public event Action SortKeyClicked;
        public event Action SortOrderClicked;
        public event Action PlanFilterClicked;
        public event Action TypeFilterClicked;
        public event Action<int> RowClicked;
        public event Action BackClicked;

        private void Awake()
        {
            header.BackButton.Button.onClick.AddListener(() => BackClicked?.Invoke());
            tabBar.TabClicked += index => TabSelected?.Invoke(index == 0 ? OwnedTabKind.Idols : OwnedTabKind.SupportCards);
            sortButton.Button.onClick.AddListener(() => SortKeyClicked?.Invoke());
            orderButton.Button.onClick.AddListener(() => SortOrderClicked?.Invoke());
            planButton.Button.onClick.AddListener(() => PlanFilterClicked?.Invoke());
            typeButton.Button.onClick.AddListener(() => TypeFilterClicked?.Invoke());
        }

        public void SetTitle(string title)
        {
            header.SetTitle(title);
        }

        public void SetTabs(OwnedTabKind selected, bool visible)
        {
            tabBar.gameObject.SetActive(visible);
            tabBar.SetSelected(selected == OwnedTabKind.Idols ? 0 : 1);
        }

        public void SetControls(bool visible, string sortLabel, string orderLabel, string planLabel, string typeLabel, bool filtersVisible)
        {
            controls.gameObject.SetActive(visible);
            filterRow.gameObject.SetActive(filtersVisible);
            sortButton.SetLabel(sortLabel);
            orderButton.SetLabel(orderLabel);
            planButton.SetLabel(planLabel);
            typeButton.SetLabel(typeLabel);
        }

        public void SetRows(IReadOnlyList<OwnedRowViewDto> rows)
        {
            UiObjects.ClearChildren(listContent);
            emptyText.gameObject.SetActive(rows.Count == 0);
            for (var i = 0; i < rows.Count; i++)
            {
                var row = UiObjects.AddRow(rowPrefab, listContent, "Row" + i);
                row.SetTexts(rows[i].Title, rows[i].Detail, "");
                row.SetInteractable(rows[i].Enabled);
                var index = i;
                row.Button.onClick.AddListener(() => RowClicked?.Invoke(index));
            }
        }
    }
}
