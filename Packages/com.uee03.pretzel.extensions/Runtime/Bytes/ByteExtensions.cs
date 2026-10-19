using System;

namespace Pretzel.Extensions
{
    public static class ByteExtensions
    {
        /// <summary>
        /// Convert a byte array to a base 64 string.
        /// </summary>
        public static string ToBase64(this byte[] data)
        {
            return Convert.ToBase64String(data);
        }

        /// <summary>
        /// Convert a byte array to a string.
        /// </summary>
        /// <param name="data">The bytes to convert</param>
        /// <param name="encodingType">The encoding used to read the bytes, default is UTF8</param>
        public static string FromBytes(this byte[] data, EncodingType encodingType = EncodingType.UTF8)
        {
            return encodingType.ToEncoding().GetString(data);
        }
    }
}
