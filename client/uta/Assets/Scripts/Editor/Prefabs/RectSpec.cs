using UnityEngine;

namespace Uta.Client.Editor.Prefabs
{
    /// <summary>RectTransform の置き方。offsetMin/offsetMax で持つ（pos/size と同じ意味）。</summary>
    public readonly struct RectSpec
    {
        public RectSpec(Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 offsetMin, Vector2 offsetMax)
        {
            AnchorMin = anchorMin;
            AnchorMax = anchorMax;
            Pivot = pivot;
            OffsetMin = offsetMin;
            OffsetMax = offsetMax;
        }

        public Vector2 AnchorMin { get; }
        public Vector2 AnchorMax { get; }
        public Vector2 Pivot { get; }
        public Vector2 OffsetMin { get; }
        public Vector2 OffsetMax { get; }

        /// <summary>全面。</summary>
        public static RectSpec Stretch()
        {
            return new RectSpec(Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        }

        /// <summary>全面から内側へ。</summary>
        public static RectSpec Stretch(float left, float right, float top, float bottom)
        {
            return new RectSpec(Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(left, bottom), new Vector2(-right, -top));
        }

        /// <summary>上帯。</summary>
        public static RectSpec Top(float height)
        {
            return new RectSpec(new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), new Vector2(0f, -height), Vector2.zero);
        }

        /// <summary>下帯。</summary>
        public static RectSpec Bottom(float height)
        {
            return new RectSpec(Vector2.zero, new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, height));
        }

        /// <summary>中央 width x height。</summary>
        public static RectSpec Center(float width, float height)
        {
            var center = new Vector2(0.5f, 0.5f);
            return new RectSpec(center, center, center, new Vector2(-width / 2f, -height / 2f), new Vector2(width / 2f, height / 2f));
        }
    }
}
