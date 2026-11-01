using System;

namespace SpaceInvaders
{
    /// <summary>Progress kept between sessions. Field names are the save file's keys; rename them with a migration.</summary>
    [Serializable]
    public sealed class GameProgress
    {
        public int highScore;
        public int gamesPlayed;
    }
}
