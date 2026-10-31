namespace Pretzel.Multiplayer
{
    /// <summary>Keeps the record in memory. Useful for tests and for games that never want to persist it.</summary>
    public sealed class InMemoryReconnectStore : IReconnectStore
    {
        private ReconnectRecord _record;

        public ReconnectRecord Load()
        {
            return _record;
        }

        public void Save(ReconnectRecord record)
        {
            _record = record;
        }

        public void Clear()
        {
            _record = null;
        }
    }
}
