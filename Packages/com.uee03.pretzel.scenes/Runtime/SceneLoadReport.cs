using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace Pretzel.Scenes
{
    /// <summary>Timings and outcome of one load. Raised with <see cref="SceneLoader.LoadFinished"/>; log or send it to analytics.</summary>
    public sealed class SceneLoadReport
    {
        public IReadOnlyList<string> SceneNames { get; }

        public LoadSceneMode Mode { get; }

        public bool FromPreload { get; }

        public double ShowSeconds { get; internal set; }

        /// <summary>Reading scene files up to 90 %, including the wait for the minimum duration.</summary>
        public double LoadSeconds { get; internal set; }

        public double ActivateSeconds { get; internal set; }

        public double SetupSeconds { get; internal set; }

        public double HideSeconds { get; internal set; }

        public double TotalSeconds { get; internal set; }

        public bool Succeeded { get; internal set; }

        public bool Cancelled { get; internal set; }

        /// <summary>Why the load failed, or null.</summary>
        public Exception Error { get; internal set; }

        /// <summary>True when the load failed and <see cref="SceneLoadOptions.FallbackScene"/> was loaded instead.</summary>
        public bool RecoveredToFallback { get; internal set; }

        internal SceneLoadReport(IReadOnlyList<string> sceneNames, LoadSceneMode mode, bool fromPreload)
        {
            SceneNames = sceneNames;
            Mode = mode;
            FromPreload = fromPreload;
        }

        public override string ToString()
        {
            string outcome = Succeeded ? "loaded" : Cancelled ? "cancelled" : "failed";
            return $"{string.Join(", ", SceneNames)} {outcome} in {TotalSeconds:0.00} s "
                   + $"(show {ShowSeconds:0.00}, load {LoadSeconds:0.00}, activate {ActivateSeconds:0.00}, setup {SetupSeconds:0.00}, hide {HideSeconds:0.00})";
        }
    }
}
