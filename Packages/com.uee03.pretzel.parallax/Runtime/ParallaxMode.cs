namespace Pretzel.Parallax
{
    /// <summary>How a layer decides how much it follows the camera.</summary>
    public enum ParallaxMode
    {
        /// <summary>Use the layer's own factor.</summary>
        Manual,

        /// <summary>
        /// Derive the factor from the layer's distance from the camera, so an orthographic camera shows the same
        /// parallax a perspective camera would. A perspective camera already does this, so such a layer is left still.
        /// </summary>
        Depth
    }
}
