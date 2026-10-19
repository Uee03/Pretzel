using System;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Pretzel.Data
{
    /// <summary>Optional game-readable keys bundled as a JSON TextAsset. Never contains an RSA private key.</summary>
    public static class GameEncryptionKeyConfig
    {
        public const string ResourcePath = "Pretzel/GameEncryptionKeys";

        /// <summary>Returns null only when no bundled config exists. Invalid configs throw.</summary>
        public static EncryptionKeys Load()
        {
            TextAsset config = Resources.Load<TextAsset>(ResourcePath);
            return config ? FromJson(config.text) : null;
        }

        /// <summary>Exports only the selected mode's AES key or RSA public key.</summary>
        public static string ToJson(EncryptionKeys keys, EncryptionMode mode)
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
                    break;
                case EncryptionMode.Rsa:
                    if (!keys.HasPublicKey)
                    {
                        throw new InvalidOperationException("Generate an RSA public key on the Encryption tab first.");
                    }

                    config["rsaPublicKey"] = EncryptionKeys.EncodeXml(PublicKeyOnly(keys.RsaPublicKey));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported encryption mode.");
            }

            return config.ToString(Formatting.Indented);
        }

        public static EncryptionKeys FromJson(string json)
        {
            JObject config = JObject.Parse(json);
            if (config["rsaPrivateKey"] != null)
            {
                throw new FormatException("A game key config must not contain an RSA private key.");
            }

            EncryptionKeys keys = new();
            string aes = ReadString(config, "aesKey");
            if (aes != null)
            {
                keys.AesKey = Convert.FromBase64String(aes);
                if (!keys.HasAesKey)
                {
                    throw new FormatException("The game key config AES key must contain exactly 32 bytes.");
                }
            }

            string publicKey = ReadString(config, "rsaPublicKey");
            if (publicKey != null)
            {
                string xml = EncryptionKeys.DecodeXml(publicKey);
                if (string.IsNullOrWhiteSpace(xml))
                {
                    throw new FormatException("The game key config RSA public key must be XML or Base64-encoded XML.");
                }

                keys.RsaPublicKey = PublicKeyOnly(xml);
            }

            if (keys.IsEmpty)
            {
                throw new FormatException("The game key config must contain an AES key or an RSA public key.");
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

        private static string PublicKeyOnly(string xml)
        {
            using RSA rsa = RSA.Create();
            rsa.FromXmlString(xml);
            return rsa.ToXmlString(false);
        }
    }
}
