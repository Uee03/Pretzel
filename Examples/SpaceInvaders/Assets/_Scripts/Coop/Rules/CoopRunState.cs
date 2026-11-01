using System;
using System.Collections.Generic;

namespace SpaceInvaders.Coop
{
    /// <summary>
    /// The score and lives of every player in a co-op match, each with their own. The game is over when every player is out of lives.
    /// Plain C# with no Unity or network types, so the rules are tested without a scene.
    /// </summary>
    public sealed class CoopRunState
    {
        public const int StartingLives = 3;

        /// <summary>The co-op HUD and the lobby are laid out for two pilots; a third ship can exist (two on one machine plus a remote one) but does not play.</summary>
        public const int MaxPlayers = 2;

        private readonly List<CoopPlayerRun> _players = new();

        public IReadOnlyList<CoopPlayerRun> Players => _players;

        public bool HasPlayers => _players.Count > 0;

        public bool AllEliminated
        {
            get
            {
                if (_players.Count == 0)
                {
                    return false;
                }

                foreach (CoopPlayerRun player in _players)
                {
                    if (!player.IsEliminated)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public int TotalScore
        {
            get
            {
                int total = 0;
                foreach (CoopPlayerRun player in _players)
                {
                    total += player.Score;
                }

                return total;
            }
        }

        /// <summary>The slot with the highest score, or -1 when nobody has played or two players are tied.</summary>
        public int LeadingSlot
        {
            get
            {
                int best = -1;
                int bestScore = int.MinValue;
                bool tied = false;
                foreach (CoopPlayerRun player in _players)
                {
                    if (player.Score > bestScore)
                    {
                        best = player.Slot;
                        bestScore = player.Score;
                        tied = false;
                    }
                    else if (player.Score == bestScore)
                    {
                        tied = true;
                    }
                }

                return tied ? -1 : best;
            }
        }

        public event Action Changed;

        /// <summary>Adds a player with full lives, or returns the one already there so a rejoining player keeps their score.</summary>
        /// <returns>The player, or null when the slot is invalid or the match already has <see cref="MaxPlayers"/> players.</returns>
        public CoopPlayerRun AddPlayer(int slot)
        {
            CoopPlayerRun existing = Find(slot);
            if (existing != null)
            {
                return existing;
            }

            if (slot < 0 || slot >= MaxPlayers)
            {
                return null;
            }

            CoopPlayerRun player = new(slot);
            _players.Add(player);
            Changed?.Invoke();
            return player;
        }

        public bool RemovePlayer(int slot)
        {
            CoopPlayerRun player = Find(slot);
            if (player == null)
            {
                return false;
            }

            _players.Remove(player);
            Changed?.Invoke();
            return true;
        }

        public CoopPlayerRun Find(int slot)
        {
            foreach (CoopPlayerRun player in _players)
            {
                if (player.Slot == slot)
                {
                    return player;
                }
            }

            return null;
        }

        /// <returns>The lives left, or -1 when the slot is unknown.</returns>
        public int LoseLife(int slot)
        {
            CoopPlayerRun player = Find(slot);
            if (player == null)
            {
                return -1;
            }

            if (player.Lives > 0)
            {
                player.Lives--;
                Changed?.Invoke();
            }

            return player.Lives;
        }

        public void AddScore(int slot, int points)
        {
            CoopPlayerRun player = Find(slot);
            if (player == null || points <= 0 || player.IsEliminated)
            {
                return;
            }

            player.Score += points;
            Changed?.Invoke();
        }

        /// <summary>Takes every remaining life, for example when the invaders land.</summary>
        public void EliminateAll()
        {
            foreach (CoopPlayerRun player in _players)
            {
                player.Lives = 0;
            }

            Changed?.Invoke();
        }

        /// <summary>Starts a new game for the same players: full lives and no score.</summary>
        public void Reset()
        {
            foreach (CoopPlayerRun player in _players)
            {
                player.Lives = StartingLives;
                player.Score = 0;
            }

            Changed?.Invoke();
        }
    }
}
