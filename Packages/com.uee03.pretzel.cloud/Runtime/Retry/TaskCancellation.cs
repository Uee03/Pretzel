using System;
using System.Threading;
using System.Threading.Tasks;

namespace Pretzel.Cloud
{
    /// <summary>
    /// Lets a caller stop waiting for an SDK call that takes no <see cref="CancellationToken"/>.
    /// The request itself still runs to completion on the service; only the wait is cancelled.
    /// </summary>
    public static class TaskCancellation
    {
        public static async Task<T> WithCancellationAsync<T>(this Task<T> task, CancellationToken cancellationToken)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            await WaitOrCancelAsync(task, cancellationToken);
            return await task;
        }

        public static async Task WithCancellationAsync(this Task task, CancellationToken cancellationToken)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            await WaitOrCancelAsync(task, cancellationToken);
            await task;
        }

        private static async Task WaitOrCancelAsync(Task task, CancellationToken cancellationToken)
        {
            if (task.IsCompleted || !cancellationToken.CanBeCanceled)
            {
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();
            TaskCompletionSource<bool> cancelled = new(TaskCreationOptions.RunContinuationsAsynchronously);
            using (cancellationToken.Register(() => cancelled.TrySetResult(true)))
            {
                Task winner = await Task.WhenAny(task, cancelled.Task);
                if (!ReferenceEquals(winner, task))
                {
                    // The abandoned call may still fail later; read its exception so it is never reported as unobserved.
                    _ = task.ContinueWith(abandoned => abandoned.Exception, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                    throw new OperationCanceledException(cancellationToken);
                }
            }
        }
    }
}
