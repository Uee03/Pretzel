using VContainer.Unity;

namespace Pretzel.Samples.VContainerExample
{
    public sealed class RepositoryDemo : IStartable
    {
        private readonly ISampleLog _log;
        private readonly IRepository<PlayerRecord> _players;
        private readonly IRepository<ItemRecord> _items;

        public RepositoryDemo(ISampleLog log, IRepository<PlayerRecord> players, IRepository<ItemRecord> items)
        {
            _log = log;
            _players = players;
            _items = items;
        }

        public void Start()
        {
            _players.Add(new PlayerRecord("Ada"));
            _items.Add(new ItemRecord("Potion"));
            _items.Add(new ItemRecord("Key"));
            _log.Write("11 Open generics", $"{_players.All.Count} player and {_items.All.Count} items, one registration");
        }
    }
}
