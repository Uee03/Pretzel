using UnityEngine;

namespace Pretzel.Extensions
{
    public static class LayerMaskExtensions
    {
        /// <summary>
        /// Check if a layer index (0 to 31) is part of the layer mask.
        /// </summary>
        public static bool InLayerMask(this LayerMask layerMask, int layer)
        {
            return (uint)layer < 32u && (layerMask.value & (1 << layer)) != 0;
        }
    }
}
