using System;
using UnityEngine;
using VContainer.Unity;

namespace Pretzel.Samples.VContainerExample
{
    /// <summary>The container calls Dispose on every IDisposable it created when the scope is destroyed.</summary>
    public sealed class DisposableResource : IStartable, IDisposable
    {
        private readonly ISampleLog _log;

        public DisposableResource(ISampleLog log) => _log = log;

        public void Start() => _log.Write("14 Callbacks", "Resource acquired; exit Play mode to see it released");

        public void Dispose() => Debug.Log("[14 Callbacks] IDisposable.Dispose: resource released");
    }
}
