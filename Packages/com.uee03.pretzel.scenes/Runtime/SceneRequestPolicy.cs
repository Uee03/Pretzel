namespace Pretzel.Scenes
{
    /// <summary>What a <see cref="SceneLoader"/> does with a request that arrives while another load runs.</summary>
    public enum SceneRequestPolicy
    {
        /// <summary>Waits and runs after the current load, in call order.</summary>
        Queue = 0,

        /// <summary>Returns at once without loading. The returned scene is invalid and a preload returns null.</summary>
        Ignore = 1,

        /// <summary>Throws <see cref="System.InvalidOperationException"/>.</summary>
        Throw = 2
    }
}
