using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Pretzel.Data
{
    /// <summary>Optional game-readable AES key bundled as a JSON TextAsset.</summary>
    public static class GameEncryptionKeyConfig
    {
        public const string ResourcePath = "Pretzel/GameEncryptionKeys";

        /// <summary>Returns null only when no bundled config exists. Invalid configs throw.</summary>
        public static EncryptionKeys Load()
        {
            TextAsset config = Resources.Load<TextAsset>(ResourcePath);
            return config ? FromJson(config.text) : null;
        }

        /// <summary>Exports the AES key, its fingerprint and optional previous keys.</summary>
        public static string ToJson(EncryptionKeys keys, EncryptionMode mode = EncryptionMode.Aes)
        {
            if (keys == null)
            {
                throw new ArgumentNullException(nameof(keys));
            }

            JObject config = new();
            switch (mode)
            {
                case EncryptionMode.Aes:
                    if (!keys.HasAesKey)
                    {
                        throw new InvalidOperationException("Generate a 32 byte AES key on the Encryption tab first.");
                    }

                    config["aesKey"] = EncryptionKeys.EncodeAesKey(keys.AesKey);
                    config["keyId"] = EncryptionKeys.Fingerprint(keys.AesKey);
                    if (keys.PreviousAesKeys.Count > 0)
                    {
                        JArray previous = new();
                        foreach (byte[] old in keys.PreviousAesKeys)
                        {
                            previous.Add(EncryptionKeys.EncodeAesKey(old));
                        }

                        config["previousAesKeys"] = previous;
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported encryption mode.");
            }

            return config.ToString(Formatting.Indented);
        }

        public static EncryptionKeys FromJson(string json)
        {
            JObject config = JObject.Parse(json);
            EncryptionKeys keys = new();
            string aes = ReadString(config, "aesKey");
            if (aes != null)
            {
                keys.AesKey = Convert.FromBase64String(aes);
                if (!keys.HasAesKey)
                {
                    throw new FormatException("The game key config AES key must contain exactly 32 bytes.");
                }

                // keyId is only a fingerprint. A mismatch means the file was edited or half-copied.
                string keyId = ReadString(config, "keyId");
                if (keyId != null && !string.Equals(keyId, EncryptionKeys.Fingerprint(keys.AesKey), StringComparison.OrdinalIgnoreCase))
                {
                    throw new FormatException("The game key config keyId does not match its AES key. Export the key again.");
                }

                if (config["previousAesKeys"] is JArray previous)
                {
                    foreach (JToken item in previous)
                    {
                        if (item.Type != JTokenType.String)
                        {
                            throw new FormatException("The game key config previousAesKeys must be a list of Base64 strings.");
                        }

                        byte[] old = Convert.FromBase64String((string)item);
                        if (old.Length != EncryptionKeys.AesKeySize)
                        {
                            throw new FormatException("Every previous AES key in the game key config must contain exactly 32 bytes.");
                        }

                        keys.AddPreviousAesKey(old);
                    }
                }
                else if (config["previousAesKeys"] != null)
                {
                    throw new FormatException("The game key config previousAesKeys must be a list.");
                }
            }

            if (keys.IsEmpty)
            {
                throw new FormatException("The game key config must contain a Base64-encoded 32 byte AES key.");
            }

            return keys;
        }

        private static string ReadString(JObject config, string name)
        {
            JToken value = config[name];
            if (value == null)
            {
                return null;
            }

            if (value.Type != JTokenType.String || string.IsNullOrWhiteSpace((string)value))
            {
                throw new FormatException($"The game key config '{name}' must be a non-empty string.");
            }

            return (string)value;
        }

    }
}
