using System;

namespace Pretzel.Scenes
{
    /// <summary>Thrown when a load failed and the loader recovered to <see cref="SceneLoadOptions.FallbackScene"/>.</summary>
    public sealed class SceneLoadException : Exception
    {
        public string FallbackScene { get; }

        public SceneLoadException(string message, string fallbackScene, Exception inner) : base(message, inner)
        {
            FallbackScene = fallbackScene;
        }
    }
}
