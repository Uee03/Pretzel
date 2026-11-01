using System;
using System.IO;
using System.Threading;
using Pretzel.Data;
using UnityEngine;

namespace SpaceInvaders
{
    /// <summary>
    /// The session state machine and the saved progress. Pause stops game time with Time.timeScale; every other state
    /// restores it.
    /// </summary>
    public sealed class GameService : IGameService, IDisposable
    {
        public const string SaveFolder = "Saves";
        public const string ProgressName = "progress";

        private static readonly DataKey _progressKey = new(ProgressName, null, SaveFolder);

        private readonly ISaveService _save;
        private GameProgress _progress = new();

        public GameService(ISaveService save) => _save = save;

        public event Action<GameState> StateChanged;

        public event Action<int> ScoreChanged;

        public event Action ProgressLoaded;

        public GameState State { get; private set; } = GameState.Menu;

        public int Score { get; private set; }

        public int HighScore => _progress.highScore;

        public int GamesPlayed => _progress.gamesPlayed;

        public bool IsLoaded { get; private set; }

        public async Awaitable LoadProgressAsync(CancellationToken cancellationToken = default)
        {
            _progress = await _save.LoadOrCreateAsync<GameProgress>(_progressKey, cancellationToken);
            IsLoaded = true;
            ProgressLoaded?.Invoke();
        }

        public void StartGame()
        {
            if (State != GameState.Menu && State != GameState.GameOver)
            {
                return;
            }

            SetScore(0);
            SetState(GameState.Playing);
        }

        public void AddScore(int points)
        {
            if (points < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(points), "Score can only increase.");
            }

            if (State == GameState.Playing && points > 0)
            {
                SetScore(Score + points);
            }
        }

        public void Pause()
        {
            if (State == GameState.Playing)
            {
                SetState(GameState.Paused);
            }
        }

        public void Resume()
        {
            if (State == GameState.Paused)
            {
                SetState(GameState.Playing);
            }
        }

        public void EndGame()
        {
            if (State != GameState.Playing && State != GameState.Paused)
            {
                return;
            }

            _progress.gamesPlayed++;
            _progress.highScore = Mathf.Max(_progress.highScore, Score);
            SaveProgress();
            SetState(GameState.GameOver);
        }

        public void ReturnToMenu()
        {
            SetScore(0);
            SetState(GameState.Menu);
        }

        public void Dispose() => Time.timeScale = 1f;

        private void SaveProgress()
        {
            // Saving before a successful load would overwrite progress that could not be read.
            if (!IsLoaded)
            {
                Debug.LogWarning("[GameService] Progress was not saved because it was never loaded. Call LoadProgressAsync at startup.");
                return;
            }

            try
            {
                _save.Save(_progressKey, _progress);
            }
            catch (IOException ex)
            {
                Debug.LogError("[GameService] Progress could not be saved: " + ex.Message);
            }
        }

        private void SetScore(int score)
        {
            if (Score != score)
            {
                Score = score;
                ScoreChanged?.Invoke(score);
            }
        }

        private void SetState(GameState state)
        {
            if (State == state)
            {
                return;
            }

            State = state;
            Time.timeScale = state == GameState.Paused ? 0f : 1f;
            StateChanged?.Invoke(state);
        }
    }
}
