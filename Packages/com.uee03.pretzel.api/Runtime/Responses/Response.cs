using System.Collections.Generic;

namespace Pretzel.Api
{
    /// <summary>Result of one request. A failed request is a response with <see cref="Ok"/> false, not an exception.</summary>
    public sealed class Response<T>
    {
        /// <summary>True when the request completed with a success status code and the data could be read.</summary>
        public bool Ok { get; }

        public long StatusCode { get; }

        /// <summary>The parsed body. Default when <see cref="Ok"/> is false.</summary>
        public T Data { get; }

        public string Error { get; }

        /// <summary>The server's response body when it sent one with an error status, for text requests only.</summary>
        public string ErrorBody { get; }

        public bool TimedOut { get; }

        public IReadOnlyDictionary<string, string> Headers { get; }

        internal Response(bool ok, long statusCode, T data, string error, string errorBody, bool timedOut, IReadOnlyDictionary<string, string> headers)
        {
            Ok = ok;
            StatusCode = statusCode;
            Data = data;
            Error = error;
            ErrorBody = errorBody;
            TimedOut = timedOut;
            Headers = headers ?? _emptyHeaders;
        }

        private static readonly IReadOnlyDictionary<string, string> _emptyHeaders = new Dictionary<string, string>();

        internal static Response<T> Failure(string error) => new(false, 0, default, error, null, false, null);

        public override string ToString() => Ok ? $"{StatusCode} OK" : $"{StatusCode} {Error}";
    }
}
