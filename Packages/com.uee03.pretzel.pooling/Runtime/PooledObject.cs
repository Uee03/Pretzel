using System;
using UnityEngine;

namespace Pretzel.Pooling
{
    /// <summary>
    /// Added to every instance a pool creates, so the object can return itself. Outside a pool, <see cref="Release"/> destroys it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PooledObject : MonoBehaviour
    {
        private Action _release;
        private int _lease;

        /// <summary>True while the object is taken from a pool and not yet released.</summary>
        public bool IsInUse { get; private set; }

        internal void Bind(Action release) => _release = release;

        internal void MarkTaken()
        {
            _lease++;
            IsInUse = true;
        }

        internal void MarkReturned()
        {
            _lease++;
            IsInUse = false;
        }

        public void Release()
        {
            if (_release == null)
            {
                Destroy(gameObject);
                return;
            }

            if (IsInUse)
            {
                _release();
            }
        }

        /// <summary>Releases after a delay, unless the object is released or taken again first.</summary>
        public void ReleaseAfter(float seconds) => _ = ReleaseAfterAsync(seconds, _lease);

        private async Awaitable ReleaseAfterAsync(float seconds, int lease)
        {
            try
            {
                await Awaitable.WaitForSecondsAsync(seconds, destroyCancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (lease == _lease)
            {
                Release();
            }
        }
    }
}
