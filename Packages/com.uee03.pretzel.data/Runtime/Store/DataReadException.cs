using System;

namespace Pretzel.Data
{
    internal sealed class DataReadException : InvalidOperationException
    {
        public DataError Reason { get; }

        public DataReadException(DataError reason, string message) : base(message)
        {
            Reason = reason;
        }
    }
}
