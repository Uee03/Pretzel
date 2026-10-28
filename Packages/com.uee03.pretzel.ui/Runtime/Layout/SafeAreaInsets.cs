using UnityEngine;

namespace Pretzel.UI
{
    /// <summary>Insets in panel units on each edge, for notches, rounded corners and system bars.</summary>
    // Value type: four floats with value equality, recomputed on every layout change.
    public readonly struct SafeAreaInsets
    {
        public float Left { get; }
        public float Top { get; }
        public float Right { get; }
        public float Bottom { get; }

        public SafeAreaInsets(float left, float top, float right, float bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        /// <summary>
        /// Converts a screen safe area (pixels, origin bottom left, as in <see cref="Screen.safeArea"/>) to insets of a panel
        /// that covers the whole screen. Panel scaling is taken from the ratio of panel size to screen size.
        /// </summary>
        public static SafeAreaInsets Compute(Rect safeArea, Vector2 screenSize, Vector2 panelSize)
        {
            if (screenSize.x <= 0f || screenSize.y <= 0f)
            {
                return default;
            }

            float scaleX = panelSize.x / screenSize.x;
            float scaleY = panelSize.y / screenSize.y;
            return new SafeAreaInsets(
                Mathf.Max(0f, safeArea.xMin) * scaleX,
                Mathf.Max(0f, screenSize.y - safeArea.yMax) * scaleY,
                Mathf.Max(0f, screenSize.x - safeArea.xMax) * scaleX,
                Mathf.Max(0f, safeArea.yMin) * scaleY);
        }

        public override string ToString() => $"(left {Left}, top {Top}, right {Right}, bottom {Bottom})";
    }
}
