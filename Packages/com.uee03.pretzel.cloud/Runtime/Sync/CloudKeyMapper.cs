using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Pretzel.Data;

namespace Pretzel.Cloud
{
    /// <summary>
    /// Maps a <see cref="DataKey"/> to a Cloud Save key and back. Cloud Save keys allow letters, digits, '-' and '_' up to 255 characters,
    /// so every other character is written as '_' plus two hex digits per UTF-8 byte. The layout is <c>pz-{slot|n}-{folder}-{name}</c>.
    /// </summary>
    public static class CloudKeyMapper
    {
        public const string Prefix = "pz-";
        public const int MaxKeyLength = 255;

        private const string NoSlot = "n";

        /// <summary>Returns false when the key would be longer than <see cref="MaxKeyLength"/>.</summary>
        public static bool TryToCloudKey(DataKey key, out string cloudKey)
        {
            cloudKey = null;
            if (!key.IsValid)
            {
                return false;
            }

            string slot = key.Slot.HasValue ? key.Slot.Value.ToString(CultureInfo.InvariantCulture) : NoSlot;
            string candidate = Prefix + slot + "-" + Encode(key.Folder ?? string.Empty) + "-" + Encode(key.Name);
            if (candidate.Length > MaxKeyLength)
            {
                return false;
            }

            cloudKey = candidate;
            return true;
        }

        /// <exception cref="ArgumentException">The key is invalid or its cloud key would be too long.</exception>
        public static string ToCloudKey(DataKey key)
        {
            if (!TryToCloudKey(key, out string cloudKey))
            {
                throw new ArgumentException($"The key '{key}' cannot be stored in Cloud Save: it is invalid or longer than {MaxKeyLength} characters once encoded.", nameof(key));
            }

            return cloudKey;
        }

        /// <summary>Reads a key written by <see cref="ToCloudKey"/>. Returns false for any other Cloud Save key.</summary>
        public static bool TryParse(string cloudKey, out DataKey key)
        {
            key = default;
            if (string.IsNullOrEmpty(cloudKey) || !cloudKey.StartsWith(Prefix, StringComparison.Ordinal))
            {
                return false;
            }

            string[] parts = cloudKey.Substring(Prefix.Length).Split('-');
            if (parts.Length != 3 || parts[2].Length == 0)
            {
                return false;
            }

            int? slot = null;
            if (!string.Equals(parts[0], NoSlot, StringComparison.Ordinal))
            {
                if (!int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out int parsedSlot))
                {
                    return false;
                }

                slot = parsedSlot;
            }

            if (!TryDecode(parts[1], out string folder) || !TryDecode(parts[2], out string name))
            {
                return false;
            }

            try
            {
                key = new DataKey(name, slot, folder.Length == 0 ? null : folder);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private static string Encode(string text)
        {
            StringBuilder builder = new();
            foreach (byte value in Encoding.UTF8.GetBytes(text))
            {
                bool plain = (value >= 'A' && value <= 'Z') || (value >= 'a' && value <= 'z') || (value >= '0' && value <= '9');
                if (plain)
                {
                    builder.Append((char)value);
                }
                else
                {
                    builder.Append('_').Append(value.ToString("X2", CultureInfo.InvariantCulture));
                }
            }

            return builder.ToString();
        }

        private static bool TryDecode(string text, out string decoded)
        {
            decoded = null;
            List<byte> bytes = new(text.Length);
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '_')
                {
                    if (i + 2 >= text.Length)
                    {
                        return false;
                    }

                    if (!byte.TryParse(text.Substring(i + 1, 2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out byte value))
                    {
                        return false;
                    }

                    bytes.Add(value);
                    i += 2;
                }
                else if (c < 128)
                {
                    bytes.Add((byte)c);
                }
                else
                {
                    return false;
                }
            }

            decoded = Encoding.UTF8.GetString(bytes.ToArray());
            return true;
        }
    }
}
