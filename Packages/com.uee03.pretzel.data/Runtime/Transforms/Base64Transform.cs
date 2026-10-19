using System;
using System.Text;

namespace Pretzel.Data
{
    /// <summary>Writes the bytes as Base64 text, so a binary file can be pasted, put in a JSON field or stored in PlayerPrefs. Adds about 33 percent.</summary>
    public sealed class Base64Transform : IDataTransform
    {
        public byte[] Encode(byte[] data)
        {
            return Encoding.ASCII.GetBytes(Convert.ToBase64String(data));
        }

        public byte[] Decode(byte[] data)
        {
            return FromBase64Text(Encoding.ASCII.GetString(data));
        }

        /// <summary>Accepts line breaks, spaces, URL-safe characters and missing padding.</summary>
        public static byte[] FromBase64Text(string text)
        {
            StringBuilder clean = new(text.Length);
            foreach (char c in text)
            {
                if (char.IsWhiteSpace(c) || c == '\uFEFF')
                {
                    continue;
                }

                clean.Append(c == '-' ? '+' : c == '_' ? '/' : c);
            }

            while (clean.Length % 4 != 0)
            {
                clean.Append('=');
            }

            return Convert.FromBase64String(clean.ToString());
        }
    }
}
