using UnityEngine;
using UnityEngine.UI;

namespace Uta.Client.Presentation.Common
{
    /// <summary>通信中のブロッカー（BusyBlocker のルート）。画面は直接呼ばない（ApiActivityBridge が呼ぶ）。</summary>
    public sealed class BusyIndicator : MonoBehaviour
    {
        [SerializeField] private Image blocker;

        private int count;

        public bool IsBusy => count > 0;

        /// <summary>数を 1 増やし、0 → 1 のとき blocker が null でなければ blocker.enabled = true。</summary>
        public void Begin()
        {
            count++;
            if (count == 1 && blocker != null)
            {
                blocker.enabled = true;
            }
        }

        /// <summary>数が 0 なら何もしない。そうでなければ 1 減らし、1 → 0 のとき blocker が null でなければ blocker.enabled = false。</summary>
        public void End()
        {
            if (count == 0)
            {
                return;
            }

            count--;
            if (count == 0 && blocker != null)
            {
                blocker.enabled = false;
            }
        }
    }
}
