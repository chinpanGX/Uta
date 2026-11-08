using UnityEngine;
using Uta.Client.Presentation.Common;
using VolTech.Client;

namespace Uta.Client.Bootstrap
{
    /// <summary>
    /// ApiCore の進行中の通知を、いまのシーンの BusyIndicator に伝える。
    /// 通信中にシーンが変わったとき、新しいシーンにはブロッカーが付かない（MVP では受け入れる）。
    /// </summary>
    public sealed class ApiActivityBridge : IApiActivityHandler
    {
        private readonly ActiveSceneUi activeSceneUi;
        private BusyIndicator busyIndicator;

        public ApiActivityBridge(ActiveSceneUi activeSceneUi)
        {
            this.activeSceneUi = activeSceneUi;
        }

        /// <summary>busyIndicator = activeSceneUi.Busy（null を許す）。busyIndicator が null でなければ busyIndicator.Begin()。</summary>
        public void OnActivityStarted()
        {
            busyIndicator = activeSceneUi.Busy;
            if (busyIndicator != null)
            {
                busyIndicator.Begin();
            }
        }

        /// <summary>開始のときと違うシーンになっていても、開始したほうの数を戻す。</summary>
        public void OnActivityEnded()
        {
            if (busyIndicator != null)
            {
                busyIndicator.End();
            }

            busyIndicator = null;
        }
    }
}
