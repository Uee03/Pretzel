using System;

namespace Pretzel.Cloud
{
    /// <summary>Recognizes the exceptions serializers throw for malformed or incompatible text, whichever serializer is plugged in.</summary>
    public static class CloudSerialization
    {
        /// <summary>
        /// True for argument, format, invalid-operation, cast and not-supported exceptions and for any Newtonsoft.Json exception.
        /// Use it to filter a catch around <c>ISerializer</c> calls on outside data; it is false for cancellation and for programming errors such as <see cref="NullReferenceException"/>.
        /// </summary>
        public static bool IsFailure(Exception exception)
        {
            if (exception == null)
            {
                return false;
            }

            if (exception is ArgumentException or FormatException or InvalidOperationException or InvalidCastException or NotSupportedException or System.IO.InvalidDataException)
            {
                return true;
            }

            string family = exception.GetType().Namespace;
            return family != null && family.StartsWith("Newtonsoft.Json", StringComparison.Ordinal);
        }
    }
}
