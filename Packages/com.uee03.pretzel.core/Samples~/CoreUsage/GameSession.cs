using Pretzel.Core.Singletons;
using UnityEngine;

namespace Pretzel.Samples.Core
{
    /// <summary>
    /// Persistent singleton: survives scene loads. The first instance wins and later duplicates are destroyed.
    /// </summary>
    public class GameSession : PersistentSingleton<GameSession>
    {
        public int Score { get; private set; }

        public void AddScore(int amount)
        {
            Score += amount;
        }
    }
}
