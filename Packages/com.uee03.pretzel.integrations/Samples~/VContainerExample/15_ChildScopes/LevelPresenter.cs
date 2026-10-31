using System;
using VContainer.Unity;

namespace Pretzel.Samples.VContainerExample
{
    /// <summary>An entry point of the child scope: it starts with the level and is disposed with it.</summary>
    public sealed class LevelPresenter : IStartable, IDisposable
    {
        private const string Topic = "15 Child scopes";

        private readonly LevelState _level;
        private readonly ISampleLog _log;
        private readonly IScoreWriter _scoreWriter;
        private readonly IScoreReader _scoreReader;

        // LevelState comes from the child scope; the log and the score come from the parent.
        public LevelPresenter(LevelState level, ISampleLog log, IScoreWriter scoreWriter, IScoreReader scoreReader)
        {
            _level = level;
            _log = log;
            _scoreWriter = scoreWriter;
            _scoreReader = scoreReader;
        }

        public void Start()
        {
            _scoreWriter.Add(10);
            _log.Write(Topic, $"Level {_level.Number} started; shared score is {_scoreReader.Score}");
        }

        public void Dispose() => _log.Write(Topic, $"Level {_level.Number} disposed with its scope");
    }
}
