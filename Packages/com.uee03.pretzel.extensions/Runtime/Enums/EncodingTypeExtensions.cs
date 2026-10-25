using System.Text;

namespace Pretzel.Extensions
{
    public static class EncodingTypeExtensions
    {
        /// <summary>
        /// Convert an <see cref="EncodingType"/> to the matching <see cref="Encoding"/>. Unknown values fall back to UTF8.
        /// </summary>
        public static Encoding ToEncoding(this EncodingType encodingType)
        {
            return encodingType switch
            {
                EncodingType.UTF8 => Encoding.UTF8,
                EncodingType.ASCII => Encoding.ASCII,
                EncodingType.Unicode => Encoding.Unicode,
                EncodingType.UTF32 => Encoding.UTF32,
                EncodingType.BigEndianUnicode => Encoding.BigEndianUnicode,
                _ => Encoding.UTF8
            };
        }
    }
}
