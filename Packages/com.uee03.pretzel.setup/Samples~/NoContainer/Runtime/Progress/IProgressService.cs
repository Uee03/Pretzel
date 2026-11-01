using System;

namespace Pretzel.Samples.NoContainer
{
    public interface IProgressService
    {
        GameProgress Current { get; }

        /// <summary>True when the saved file could not be read. It is kept as it is and never overwritten.</summary>
        bool IsSaveProtected { get; }

        event Action<GameProgress> Changed;

        void Load();

        /// <summary>Counts a finished round and keeps the best score. Returns true when the score is a new best.</summary>
        bool RecordRound(int score);

        void Save();
    }
}
