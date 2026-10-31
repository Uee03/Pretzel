using System.Collections.Generic;

namespace Pretzel.Samples.VContainerExample
{
    /// <summary>One registration of Repository&lt;&gt; serves every element type that is asked for.</summary>
    public sealed class Repository<T> : IRepository<T>
    {
        private readonly List<T> _items = new();

        public IReadOnlyList<T> All => _items;

        public void Add(T item) => _items.Add(item);
    }
}
