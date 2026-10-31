using Pretzel.Core;

namespace Pretzel.Cloud
{
    /// <summary>Outcome of a cloud operation without a value. A failure is a result, not an exception; cancellation still throws.</summary>
    public sealed class CloudResult
    {
        private static readonly CloudResult _success = new(null);

        public bool IsSuccess => Error == null;

        /// <summary>Why the operation failed, or null on success.</summary>
        public CloudError Error { get; }

        private CloudResult(CloudError error)
        {
            Error = error;
        }

        public static CloudResult Success() => _success;

        public static CloudResult Failure(CloudError error) => new(Guard.NotNull(error, nameof(error)));

        public static CloudResult Failure(CloudErrorKind kind, string message) => new(new CloudError(kind, message));

        public override string ToString() => IsSuccess ? "Success" : Error.ToString();
    }
}
