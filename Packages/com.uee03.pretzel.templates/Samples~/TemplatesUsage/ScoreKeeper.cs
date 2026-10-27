using System;
using Pretzel.Core.Singletons;

namespace Pretzel.Samples.Templates
{
    /// <summary>Made from Assets > Create > Pretzel > C# Singleton. Keeps the score for the current scene.</summary>
    public class ScoreKeeper : Singleton<ScoreKeeper>
    {
        public event Action<int> ScoreChanged;

        public int Score { get; private set; }

        protected override void Awake()
        {
            // Destroys a duplicate before anything else runs.
            base.Awake();
        }

        public void Add(int points)
        {
            if (points <= 0)
            {
                return;
            }

            Score += points;
            ScoreChanged?.Invoke(Score);
        }
    }
}
