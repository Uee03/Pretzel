using UnityEngine;

namespace Pretzel.Cameras
{
    /// <summary>The arithmetic of fitting a playfield to a screen, as pure functions.</summary>
    public static class PlayfieldFit
    {
        /// <summary>
        /// Half-height, in world units, for an orthographic camera to show a playfield of the given width and height on
        /// a screen of the given aspect (width over height).
        /// </summary>
        public static float OrthographicSize(Vector2 playfield, float aspect, FitMode mode)
        {
            return HalfHeight(playfield, aspect, mode);
        }

        /// <summary>
        /// Vertical field of view in degrees for a perspective camera <paramref name="distance"/> in front of the
        /// playfield's plane to show it as <paramref name="mode"/> says.
        /// </summary>
        public static float VerticalFieldOfView(Vector2 playfield, float distance, float aspect, FitMode mode)
        {
            float safeDistance = Mathf.Max(0.0001f, distance);
            return 2f * Mathf.Atan(HalfHeight(playfield, aspect, mode) / safeDistance) * Mathf.Rad2Deg;
        }

        private static float HalfHeight(Vector2 playfield, float aspect, FitMode mode)
        {
            float safeAspect = Mathf.Max(0.0001f, aspect);
            float byHeight = Mathf.Max(0f, playfield.y) * 0.5f;
            float byWidth = Mathf.Max(0f, playfield.x) * 0.5f / safeAspect;
            switch (mode)
            {
                case FitMode.Cover:
                    return Mathf.Min(byHeight, byWidth);
                case FitMode.Width:
                    return byWidth;
                case FitMode.Height:
                    return byHeight;
                default:
                    return Mathf.Max(byHeight, byWidth);
            }
        }
    }
}
