using System;
using Pretzel.Data;
using UnityEngine;

namespace Pretzel.Samples.NoContainer
{
    /// <summary>
    /// The one class that touches the store for game progress. A file that cannot be read is kept, not replaced
    /// by an empty save, so a player never loses progress to a transient error.
    /// </summary>
    public sealed class ProgressService : IProgressService
    {
        public static readonly DataKey SaveKey = new("progress");

        private readonly IDataStore _store;
        private bool _dirty;

        public ProgressService(IDataStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            Current = new GameProgress();
        }

        public GameProgress Current { get; private set; }

        public bool IsSaveProtected { get; private set; }

        public event Action<GameProgress> Changed;

        public void Load()
        {
            LoadResult<GameProgress> result = _store.Load<GameProgress>(SaveKey);
            IsSaveProtected = false;
            _dirty = false;
            switch (result.Status)
            {
                case DataStatus.Ok:
                    Current = result.Value ?? new GameProgress();
                    break;
                case DataStatus.NotFound:
                    Current = new GameProgress();
                    break;
                default:
                    Current = new GameProgress();
                    IsSaveProtected = true;
                    Debug.LogError($"[Progress] Cannot read {SaveKey} ({result.ErrorCode}): {result.Error} The file is kept and progress will not be saved this session.");
                    break;
            }

            Changed?.Invoke(Current);
        }

        public bool RecordRound(int score)
        {
            bool isBest = score > Current.bestScore;
            Current.roundsPlayed++;
            if (isBest)
            {
                Current.bestScore = score;
            }

            _dirty = true;
            Changed?.Invoke(Current);
            return isBest;
        }

        public void Save()
        {
            if (!_dirty || IsSaveProtected)
            {
                return;
            }

            _store.Save(SaveKey, Current);
            _dirty = false;
        }
    }
}
