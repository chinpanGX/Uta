using TMPro;
using UnityEngine;
using Uta.Client.Presentation.Common;

namespace Uta.Client.Editor.Prefabs
{
    /// <summary>TMP の文字の設定。</summary>
    public sealed class TextSpec
    {
        public TextSpec(int fontSize, Color color, TextAlignmentOptions alignment, bool wrap, bool ellipsis)
        {
            FontSize = fontSize;
            Color = color;
            Alignment = alignment;
            Wrap = wrap;
            Ellipsis = ellipsis;
        }

        public int FontSize { get; }
        public Color Color { get; }
        public TextAlignmentOptions Alignment { get; }

        /// <summary>true → TextWrappingModes.Normal、false → NoWrap。</summary>
        public bool Wrap { get; }

        /// <summary>true → TextOverflowModes.Ellipsis、false → Overflow。</summary>
        public bool Ellipsis { get; }

        public static TextSpec Title
        {
            get { return new TextSpec(UiTheme.FontTitle, UiTheme.Text, TextAlignmentOptions.Center, false, true); }
        }

        public static TextSpec Body
        {
            get { return new TextSpec(UiTheme.FontBody, UiTheme.Text, TextAlignmentOptions.MidlineLeft, true, false); }
        }

        public static TextSpec BodyCenter
        {
            get { return new TextSpec(UiTheme.FontBody, UiTheme.Text, TextAlignmentOptions.Center, true, false); }
        }

        public static TextSpec Small
        {
            get { return new TextSpec(UiTheme.FontSmall, UiTheme.TextMuted, TextAlignmentOptions.MidlineLeft, true, false); }
        }

        public static TextSpec SingleLine
        {
            get { return new TextSpec(UiTheme.FontBody, UiTheme.Text, TextAlignmentOptions.MidlineLeft, false, true); }
        }
    }
}
