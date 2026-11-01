namespace Pretzel.Cameras
{
    /// <summary>How a playfield is fitted to a screen whose aspect ratio is not the playfield's.</summary>
    public enum FitMode
    {
        /// <summary>The whole playfield is visible. Extra space shows around it.</summary>
        Contain,

        /// <summary>The playfield fills the screen. Part of it is cropped.</summary>
        Cover,

        /// <summary>The playfield's width fits the screen exactly.</summary>
        Width,

        /// <summary>The playfield's height fits the screen exactly.</summary>
        Height
    }
}
