using System.Collections.Generic;

namespace Pretzel.Samples.VContainerExample
{
    public interface IRepository<T>
    {
        IReadOnlyList<T> All { get; }

        void Add(T item);
    }
}
