namespace Pretzel.Data
{
    /// <summary>Outcome of a load. A missing or corrupt file is a result, not an exception.</summary>
    public sealed class LoadResult<T>
    {
        public DataStatus Status { get; }

        /// <summary>The loaded value. Default when <see cref="Ok"/> is false.</summary>
        public T Value { get; }

        /// <summary>Why the load failed, or null.</summary>
        public string Error { get; }

        public DataError ErrorCode { get; }

        /// <summary>True when the main file was corrupt and the value came from the .bak file.</summary>
        public bool RecoveredFromBackup { get; }

        public bool Ok => Status == DataStatus.Ok;

        private LoadResult(DataStatus status, T value, string error, bool recovered, DataError errorCode = DataError.None)
        {
            Status = status;
            Value = value;
            Error = error;
            RecoveredFromBackup = recovered;
            ErrorCode = errorCode;
        }

        public static LoadResult<T> Success(T value, bool recoveredFromBackup = false)
        {
            return new LoadResult<T>(DataStatus.Ok, value, null, recoveredFromBackup);
        }

        public static LoadResult<T> NotFound() => new(DataStatus.NotFound, default, "No saved data was found.", false);

        public static LoadResult<T> Corrupt(string error, DataError errorCode = DataError.InvalidData) => new(DataStatus.Corrupt, default, error, false, errorCode);

        public bool TryGetValue(out T value)
        {
            value = Value;
            return Ok;
        }

        public T ValueOr(T fallback) => Ok ? Value : fallback;
    }
}
