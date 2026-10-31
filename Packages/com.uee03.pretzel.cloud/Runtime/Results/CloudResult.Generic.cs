using Pretzel.Core;

namespace Pretzel.Cloud
{
    /// <summary>Outcome of a cloud operation with a value. A failure is a result, not an exception; cancellation still throws.</summary>
    public sealed class CloudResult<T>
    {
        public bool IsSuccess => Error == null;

        /// <summary>The value. Default when <see cref="IsSuccess"/> is false.</summary>
        public T Value { get; }

        /// <summary>Why the operation failed, or null on success.</summary>
        public CloudError Error { get; }

        private CloudResult(T value, CloudError error)
        {
            Value = value;
            Error = error;
        }

        public static CloudResult<T> Success(T value) => new(value, null);

        public static CloudResult<T> Failure(CloudError error) => new(default, Guard.NotNull(error, nameof(error)));

        public static CloudResult<T> Failure(CloudErrorKind kind, string message) => new(default, new CloudError(kind, message));

        public bool TryGetValue(out T value)
        {
            value = Value;
            return IsSuccess;
        }

        public T ValueOr(T fallback) => IsSuccess ? Value : fallback;

        /// <summary>Drops the value, keeping success or the error.</summary>
        public CloudResult ToResult() => IsSuccess ? CloudResult.Success() : CloudResult.Failure(Error);

        public override string ToString() => IsSuccess ? $"Success({Value})" : Error.ToString();
    }
}
