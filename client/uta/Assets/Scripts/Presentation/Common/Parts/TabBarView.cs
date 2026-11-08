using System;
using UnityEngine;

namespace Uta.Client.Presentation.Common.Parts
{
    /// <summary>TabBar 部品の View。tabs は部品の Prefab では空で、画面の生成で入れる。</summary>
    public sealed class TabBarView : MonoBehaviour
    {
        [SerializeField] private TabView[] tabs;

        public event Action<int> TabClicked;

        public int Count => tabs.Length;

        public TabView GetTab(int index)
        {
            return tabs[index];
        }

        /// <summary>index が範囲外なら ArgumentOutOfRangeException。</summary>
        public void SetSelected(int index)
        {
            if (index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            for (var i = 0; i < tabs.Length; i++)
            {
                tabs[i].SetSelected(i == index);
            }
        }

        private void Awake()
        {
            for (var i = 0; i < tabs.Length; i++)
            {
                var index = i;
                tabs[i].Button.onClick.AddListener(() => TabClicked?.Invoke(index));
            }
        }
    }
}
