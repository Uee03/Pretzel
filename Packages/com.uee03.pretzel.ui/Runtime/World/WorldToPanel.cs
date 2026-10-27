using Pretzel.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.UI
{
    public static class WorldToPanel
    {
        /// <summary>
        /// Converts a world position to a position in a screen-space panel, for health bars, name tags and markers.
        /// </summary>
        /// <returns>False when the point is behind the camera.</returns>
        public static bool TryGetPanelPosition(IPanel panel, Camera camera, Vector3 worldPosition, out Vector2 panelPosition)
        {
            Guard.NotNull(panel, nameof(panel));
            Guard.NotDestroyed(camera, nameof(camera));
            Vector3 viewport = camera.WorldToViewportPoint(worldPosition);
            if (viewport.z < 0f)
            {
                panelPosition = default;
                return false;
            }

            panelPosition = RuntimePanelUtils.CameraTransformWorldToPanel(panel, worldPosition, camera);
            return true;
        }

        /// <summary>True when the world position is in front of the camera and inside its viewport, with an optional margin.</summary>
        public static bool IsOnScreen(Camera camera, Vector3 worldPosition, float viewportMargin = 0f)
        {
            Guard.NotDestroyed(camera, nameof(camera));
            Vector3 viewport = camera.WorldToViewportPoint(worldPosition);
            return viewport.z > 0f
                   && viewport.x >= -viewportMargin && viewport.x <= 1f + viewportMargin
                   && viewport.y >= -viewportMargin && viewport.y <= 1f + viewportMargin;
        }
    }
}
