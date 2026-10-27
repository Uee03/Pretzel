using System;
using System.Threading;

namespace Pretzel.Core
{
    /// <summary>Runs an action once when disposed. Disposing again does nothing.</summary>
    public sealed class DisposableAction : IDisposable
    {
        public static readonly IDisposable Empty = new DisposableAction(null);

        private Action _onDispose;

        public bool IsDisposed => _onDispose == null;

        public DisposableAction(Action onDispose)
        {
            _onDispose = onDispose;
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref _onDispose, null)?.Invoke();
        }
    }
}
