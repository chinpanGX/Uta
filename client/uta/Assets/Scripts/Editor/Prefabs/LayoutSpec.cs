using UnityEngine;

namespace Uta.Client.Editor.Prefabs
{
    /// <summary>レイアウトグループの設定。</summary>
    public readonly struct LayoutSpec
    {
        public LayoutSpec(float spacing, int left, int right, int top, int bottom, TextAnchor alignment,
            bool controlWidth, bool controlHeight, bool expandWidth, bool expandHeight)
        {
            Spacing = spacing;
            Left = left;
            Right = right;
            Top = top;
            Bottom = bottom;
            Alignment = alignment;
            ControlWidth = controlWidth;
            ControlHeight = controlHeight;
            ExpandWidth = expandWidth;
            ExpandHeight = expandHeight;
        }

        public float Spacing { get; }
        public int Left { get; }
        public int Right { get; }
        public int Top { get; }
        public int Bottom { get; }
        public TextAnchor Alignment { get; }
        public bool ControlWidth { get; }
        public bool ControlHeight { get; }
        public bool ExpandWidth { get; }
        public bool ExpandHeight { get; }

        /// <summary>spacing 24、padding 32、UpperCenter、制御 true/true、伸ばす true/false。</summary>
        public static LayoutSpec Column
        {
            get { return new LayoutSpec(24f, 32, 32, 32, 32, TextAnchor.UpperCenter, true, true, true, false); }
        }

        /// <summary>spacing 24、padding 0、MiddleCenter、制御 true/true、伸ばす true/true。</summary>
        public static LayoutSpec Row
        {
            get { return new LayoutSpec(24f, 0, 0, 0, 0, TextAnchor.MiddleCenter, true, true, true, true); }
        }
    }
}
