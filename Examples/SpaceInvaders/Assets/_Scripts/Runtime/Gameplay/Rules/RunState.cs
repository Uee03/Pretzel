using System;

namespace SpaceInvaders.Gameplay
{
    /// <summary>Lives and wave of the run in progress. Plain C#, scoped to a level, so a new level always starts clean.</summary>
    public sealed class RunState
    {
        public const int StartingLives = 3;

        public int Lives { get; private set; } = StartingLives;

        public int Wave { get; private set; }

        public bool IsOutOfLives => Lives <= 0;

        public event Action<int> LivesChanged;

        public event Action<int> WaveChanged;

        public void Reset()
        {
            Lives = StartingLives;
            Wave = 0;
            LivesChanged?.Invoke(Lives);
            WaveChanged?.Invoke(Wave);
        }

        /// <returns>The lives left.</returns>
        public int LoseLife()
        {
            if (Lives > 0)
            {
                Lives--;
                LivesChanged?.Invoke(Lives);
            }

            return Lives;
        }

        public void LoseAllLives()
        {
            if (Lives > 0)
            {
                Lives = 0;
                LivesChanged?.Invoke(Lives);
            }
        }

        public void SetLives(int lives)
        {
            Lives = Math.Max(0, lives);
            LivesChanged?.Invoke(Lives);
        }

        public int NextWave()
        {
            SetWave(Wave + 1);
            return Wave;
        }

        public void SetWave(int wave)
        {
            Wave = Math.Max(1, wave);
            WaveChanged?.Invoke(Wave);
        }
    }
}
