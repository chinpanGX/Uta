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
        private readonly ActiveSceneUi sceneUi;
        private BusyIndicator active;

        public ApiActivityBridge(ActiveSceneUi sceneUi)
        {
            this.sceneUi = sceneUi;
        }

        /// <summary>active = sceneUi.Busy（null を許す）。active が null でなければ active.Begin()。</summary>
        public void OnActivityStarted()
        {
            active = sceneUi.Busy;
            if (active != null)
            {
                active.Begin();
            }
        }

        /// <summary>開始のときと違うシーンになっていても、開始したほうの数を戻す。</summary>
        public void OnActivityEnded()
        {
            if (active != null)
            {
                active.End();
            }

            active = null;
        }
    }
}
