using UnityEngine;

namespace Uta.Client.Presentation.Common
{
    /// <summary>色・寸法・フォントサイズの定数（値は仮。screen-design 7.4）。</summary>
    public static class UiTheme
    {
        public static readonly Vector2 ReferenceResolution = new Vector2(1080f, 1920f);

        public static readonly Color Background = new Color(0.10f, 0.11f, 0.15f, 1f);
        public static readonly Color Header = new Color(0.14f, 0.15f, 0.20f, 1f);
        public static readonly Color Panel = new Color(0.18f, 0.20f, 0.27f, 1f);
        public static readonly Color ButtonPrimary = new Color(0.25f, 0.45f, 0.85f, 1f);
        public static readonly Color ButtonSecondary = new Color(0.30f, 0.33f, 0.42f, 1f);
        public static readonly Color ButtonDanger = new Color(0.80f, 0.30f, 0.30f, 1f);
        public static readonly Color Text = new Color(1f, 1f, 1f, 1f);
        public static readonly Color TextMuted = new Color(0.70f, 0.72f, 0.78f, 1f);
        public static readonly Color Accent = new Color(1f, 0.78f, 0.25f, 1f);
        public static readonly Color Overlay = new Color(0f, 0f, 0f, 0.6f);
        public static readonly Color Transparent = new Color(0f, 0f, 0f, 0f);

        // Button の ColorTint（押した・使えないの見た目を、C# の処理なしで出す）
        public static readonly Color TintNormal = new Color(1f, 1f, 1f, 1f);
        public static readonly Color TintHighlighted = new Color(0.92f, 0.92f, 0.92f, 1f);
        public static readonly Color TintPressed = new Color(0.75f, 0.75f, 0.75f, 1f);
        public static readonly Color TintDisabled = new Color(0.45f, 0.45f, 0.45f, 1f);

        public const int FontTitle = 56;
        public const int FontBody = 40;
        public const int FontSmall = 32;

        public const float ButtonHeight = 130f;
        public const float HeaderHeight = 150f;
        public const float TabHeight = 110f;
        public const float RowHeight = 160f;
        public const float Spacing = 24f;
        public const int Padding = 32;
        public const float DialogWidth = 880f;
        public const float ModalWidth = 900f;
    }
}
