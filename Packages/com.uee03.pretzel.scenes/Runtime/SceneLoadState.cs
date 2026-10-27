namespace Pretzel.Scenes
{
    /// <summary>Where a <see cref="SceneLoader"/> is in a load. Watch it to block input, duck audio or show debug info.</summary>
    public enum SceneLoadState
    {
        Idle = 0,
        ShowingTransition = 1,
        Loading = 2,

        /// <summary>A preloaded scene is held at 90 % and waits for <see cref="PreloadedScene.ActivateAsync"/>.</summary>
        Preloaded = 3,
        Activating = 4,
        SettingUp = 5,
        HidingTransition = 6
    }
}
