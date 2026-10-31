using System.Threading;
using VContainer.Unity;
#if VCONTAINER_UNITASK_INTEGRATION
using Awaitable = Cysharp.Threading.Tasks.UniTask;
#else
using UnityEngine;
#endif

namespace Pretzel.Samples.VContainerExample
{
    /// <summary>
    /// Asynchronous startup. VContainer cancels the token when the scope is destroyed. With UniTask installed the
    /// interface returns UniTask instead of Awaitable.
    /// </summary>
    public sealed class AsyncStartup : IAsyncStartable
    {
        private const string Topic = "04 Entry points";

        private readonly ISampleLog _log;

        public AsyncStartup(ISampleLog log) => _log = log;

        public async Awaitable StartAsync(CancellationToken cancellation)
        {
            _log.Write(Topic, "IAsyncStartable.StartAsync: loading for one second...");
#if VCONTAINER_UNITASK_INTEGRATION
            await Cysharp.Threading.Tasks.UniTask.Delay(1000, cancellationToken: cancellation);
#else
            await Awaitable.WaitForSecondsAsync(1f, cancellation);
#endif
            _log.Write(Topic, "IAsyncStartable.StartAsync: loaded");
        }
    }
}
