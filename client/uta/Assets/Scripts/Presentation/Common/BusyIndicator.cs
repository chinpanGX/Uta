using UnityEngine;
using UnityEngine.UI;

namespace Uta.Client.Presentation.Common
{
    /// <summary>通信中のブロッカー（BusyBlocker のルート）。Begin/End/IsBusy は foundation が足す。</summary>
    public sealed class BusyIndicator : MonoBehaviour
    {
        [SerializeField] private Image blocker;
    }
}
