#if UNITY_6000_2_OR_NEWER
using UnityEngine.UIElements;

namespace Pretzel.UI
{
    /// <summary>
    /// Helpers for world-space UI Toolkit panels (Unity 6.2 or newer). A world-space UIDocument is a regular GameObject: move,
    /// rotate and scale it like any other, and add <see cref="UIBillboard"/> to make it face the camera.
    /// </summary>
    public static class WorldSpaceUIExtensions
    {
        public static bool IsWorldSpace(this PanelSettings settings)
        {
            return settings && settings.renderMode == PanelRenderMode.WorldSpace;
        }

        public static bool IsWorldSpace(this UIDocument document)
        {
            return document && document.panelSettings.IsWorldSpace();
        }
    }
}
#endif
