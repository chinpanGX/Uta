using UnityEngine;

namespace Uta.Client.Presentation.Common
{
    /// <summary>RectTransform を Screen.safeArea に合わせる。画面の大きさ・Safe Area が変わったときだけ合わせ直す。</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private Rect lastSafeArea;
        private int lastWidth;
        private int lastHeight;
        private bool applied;

        private void OnEnable()
        {
            Apply();
        }

        private void LateUpdate()
        {
            if (!applied || lastSafeArea != Screen.safeArea || lastWidth != Screen.width || lastHeight != Screen.height)
            {
                Apply();
            }
        }

        private void Apply()
        {
            var width = Screen.width;
            var height = Screen.height;
            if (width <= 0 || height <= 0)
            {
                return;
            }

            var safeArea = Screen.safeArea;
            var rect = (RectTransform)transform;
            var size = new Vector2(width, height);
            rect.anchorMin = safeArea.position / size;
            rect.anchorMax = (safeArea.position + safeArea.size) / size;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            lastSafeArea = safeArea;
            lastWidth = width;
            lastHeight = height;
            applied = true;
        }
    }
}
