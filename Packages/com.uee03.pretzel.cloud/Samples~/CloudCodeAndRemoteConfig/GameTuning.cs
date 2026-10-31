using System;

namespace Pretzel.Samples.Cloud.CodeAndConfig
{
    /// <summary>A Remote Config JSON value, read with <c>GetJson</c>. The defaults apply when the key is missing or the JSON is malformed.</summary>
    [Serializable]
    public sealed class GameTuning
    {
        public float enemySpeed = 1f;
        public int maxEnemies = 10;
    }
}
