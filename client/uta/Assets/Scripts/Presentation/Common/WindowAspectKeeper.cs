using UnityEngine;

namespace Uta.Client.Presentation.Common
{
    /// <summary>Windows のプレイヤーのウィンドウを 9:16 に保つ。Windows のプレイヤー以外では何もしない。</summary>
    public sealed class WindowAspectKeeper : MonoBehaviour
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private bool waiting;

        private void Update()
        {
            if (waiting)
            {
                // SetResolution の反映待ち。呼んだ次の 1 フレームは判定を休む。
                waiting = false;
                return;
            }
            if (Screen.fullScreen)
            {
                return;
            }

            var width = Screen.width;
            var expectedHeight = Mathf.RoundToInt(width * 16f / 9f);
            if (Mathf.Abs(Screen.height - expectedHeight) > 1)
            {
                Screen.SetResolution(width, expectedHeight, FullScreenMode.Windowed);
                waiting = true;
            }
        }
#endif
    }
}
