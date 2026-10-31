using System;
using System.Collections.Generic;
using Pretzel.Core;

namespace Pretzel.Cloud.InMemory
{
    /// <summary>
    /// Failure injection shared by the in-memory fakes: take the service offline or queue specific errors for the next calls.
    /// </summary>
    public sealed class CloudFaults
    {
        private readonly Queue<CloudError> _queued = new();

        /// <summary>When true every call fails with <see cref="CloudErrorKind.Offline"/> before doing anything.</summary>
        public bool Offline { get; set; }

        /// <summary>How many calls reached the fake, failed or not.</summary>
        public int CallCount { get; private set; }

        /// <summary>Makes the next <paramref name="count"/> calls fail with <paramref name="error"/>.</summary>
        public void Enqueue(CloudError error, int count = 1)
        {
            Guard.NotNull(error, nameof(error));
            for (int i = 0; i < count; i++)
            {
                _queued.Enqueue(error);
            }
        }

        public void Enqueue(CloudErrorKind kind, int count = 1) => Enqueue(new CloudError(kind, "Injected " + kind + " fault."), count);

        public void Clear()
        {
            _queued.Clear();
            Offline = false;
        }

        /// <summary>Counts a call and returns the error it must fail with, or null.</summary>
        public CloudError Next()
        {
            CallCount++;
            if (Offline)
            {
                return new CloudError(CloudErrorKind.Offline, "The fake service is offline.");
            }

            return _queued.Count > 0 ? _queued.Dequeue() : null;
        }
    }
}
