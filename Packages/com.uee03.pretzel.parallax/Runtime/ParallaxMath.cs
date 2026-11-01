using UnityEngine;

namespace Pretzel.Parallax
{
    /// <summary>
    /// The arithmetic of parallax. A layer's position is a function of the camera's offset from where it started, so
    /// there is no per-frame state to drift and a camera cut needs no special handling.
    /// </summary>
    public static class ParallaxMath
    {
        /// <summary>
        /// The factor that makes a layer <paramref name="layerDistance"/> in front of the camera look, under an
        /// orthographic camera, as it would under perspective. 0 at the focal distance, towards 1 far away (it follows
        /// the camera), negative in front of the focal distance (it moves faster than the world).
        /// </summary>
        public static float FactorFromDepth(float layerDistance, float focalDistance)
        {
            if (layerDistance <= Mathf.Epsilon)
            {
                return 0f;
            }

            return 1f - focalDistance / layerDistance;
        }

        /// <summary>Layer position for a camera that moved by <paramref name="cameraDelta"/>. The z of the origin is kept.</summary>
        public static Vector3 Position(Vector3 layerOrigin, Vector3 cameraDelta, Vector2 factor)
        {
            return new Vector3(layerOrigin.x + cameraDelta.x * factor.x, layerOrigin.y + cameraDelta.y * factor.y, layerOrigin.z);
        }

        /// <summary>
        /// Shifts a repeating layer by whole tiles so it stays within half a tile of the camera. With a tiled
        /// sprite at least three tiles wide plus the view, this keeps the screen covered forever.
        /// </summary>
        public static float Wrap(float cameraCoordinate, float layerCoordinate, float tileSize)
        {
            if (tileSize <= Mathf.Epsilon)
            {
                return layerCoordinate;
            }

            float tiles = Mathf.Round((cameraCoordinate - layerCoordinate) / tileSize);
            return layerCoordinate + tiles * tileSize;
        }

        /// <summary>Rounds to a multiple of <paramref name="unit"/>. A unit of 0 or less leaves the value alone.</summary>
        public static float Snap(float value, float unit)
        {
            if (unit <= Mathf.Epsilon)
            {
                return value;
            }

            return Mathf.Round(value / unit) * unit;
        }
    }
}
