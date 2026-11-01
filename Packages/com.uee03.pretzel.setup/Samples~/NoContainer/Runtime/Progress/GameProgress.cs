using System;

namespace Pretzel.Samples.NoContainer
{
    /// <summary>What is saved between sessions. Add fields freely; missing ones load as their defaults.</summary>
    [Serializable]
    public sealed class GameProgress
    {
        public int bestScore;
        public int roundsPlayed;
    }
}
