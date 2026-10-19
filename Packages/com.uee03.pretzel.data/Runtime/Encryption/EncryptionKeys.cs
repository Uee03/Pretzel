using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Pretzel.Data
{
    /// <summary>
    /// The secrets behind <see cref="DataFormat.Encrypted"/>. The AES key is a secret. The RSA public key can be public,
    /// the RSA private key must never ship with a game.
    /// </summary>
    public sealed class EncryptionKeys
    {
        public const int AesKeySize = 32;
        public const int SaltSize = 16;
        public const int DefaultPbkdf2Iterations = 600000;
        public const int MinPbkdf2Iterations = 100000;
        public const int RecommendedRsaKeySize = 4096;

        public static readonly int[] RsaKeySizes = { 2048, 3072, 4096 };

        /// <summary>Set this at startup to supply keys from code, for example keys fetched from a server with a token. It wins over every other source.</summary>
        public static Func<EncryptionKeys> Provider { get; set; }

        /// <summary>32 random bytes.</summary>
        public byte[] AesKey { get; set; }

        /// <summary>RSA key in XML form. Safe to publish.</summary>
        public string RsaPublicKey { get; set; }

        /// <summary>RSA key in XML form. Keep it secret.</summary>
        public string RsaPrivateKey { get; set; }

        public bool HasAesKey => AesKey != null && AesKey.Length == AesKeySize;

        public bool HasPublicKey => !string.IsNullOrWhiteSpace(RsaPublicKey);

        public bool HasPrivateKey => !string.IsNullOrWhiteSpace(RsaPrivateKey);

        public bool IsEmpty => !HasAesKey && !HasPublicKey && !HasPrivateKey;

        public static string AesVariable(string prefix) => prefix + "_AES_KEY";

        public static string PublicVariable(string prefix) => prefix + "_RSA_PUBLIC";

        public static string PrivateVariable(string prefix) => prefix + "_RSA_PRIVATE";

        public static byte[] GenerateAesKey()
        {
            byte[] key = new byte[AesKeySize];
            using RandomNumberGenerator random = RandomNumberGenerator.Create();
            random.GetBytes(key);
            return key;
        }

        /// <summary>16 random bytes. A salt is not secret, store it next to the data.</summary>
        public static byte[] GenerateSalt()
        {
            byte[] salt = new byte[SaltSize];
            using RandomNumberGenerator random = RandomNumberGenerator.Create();
            random.GetBytes(salt);
            return salt;
        }

        /// <summary>
        /// Derives an AES key from a passphrase with PBKDF2-HMAC-SHA256. Use it when the secret comes from a person, for example a player password.
        /// Keep the salt and iteration count with the data. Prefer <see cref="GenerateAesKey"/> when no person has to remember the secret.
        /// </summary>
        public static byte[] DeriveAesKey(string passphrase, byte[] salt, int iterations = DefaultPbkdf2Iterations)
        {
            if (string.IsNullOrEmpty(passphrase))
            {
                throw new ArgumentException("The passphrase cannot be empty.", nameof(passphrase));
            }

            if (salt == null || salt.Length < SaltSize)
            {
                throw new ArgumentException($"The salt must be at least {SaltSize} bytes.", nameof(salt));
            }

            if (iterations < MinPbkdf2Iterations)
            {
                throw new ArgumentOutOfRangeException(nameof(iterations), iterations, $"Use at least {MinPbkdf2Iterations} iterations.");
            }

            using Rfc2898DeriveBytes derive = new(passphrase, salt, iterations, HashAlgorithmName.SHA256);
            return derive.GetBytes(AesKeySize);
        }

        /// <summary>Generates an RSA pair. 4096 bits can take many seconds, so call it off the main thread.</summary>
        public static EncryptionKeys GenerateRsa(int keySize)
        {
            if (Array.IndexOf(RsaKeySizes, keySize) < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(keySize), keySize, "Use 2048, 3072 or 4096.");
            }

            using RSA rsa = RSA.Create(keySize);
            return new EncryptionKeys
            {
                RsaPublicKey = rsa.ToXmlString(false),
                RsaPrivateKey = rsa.ToXmlString(true)
            };
        }

        /// <summary>Reads the three variables named from <paramref name="prefix"/>. Returns null when none is set.</summary>
        public static EncryptionKeys FromEnvironment(string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix))
            {
                return null;
            }

            EncryptionKeys keys = new();
            string aes = Environment.GetEnvironmentVariable(AesVariable(prefix));
            if (!string.IsNullOrWhiteSpace(aes))
            {
                keys.AesKey = DecodeAesKey(aes);
            }

            keys.RsaPublicKey = DecodeXml(Environment.GetEnvironmentVariable(PublicVariable(prefix)));
            keys.RsaPrivateKey = DecodeXml(Environment.GetEnvironmentVariable(PrivateVariable(prefix)));
            return keys.IsEmpty ? null : keys;
        }

        /// <summary>
        /// Finds keys in this order, taking each key from the first source that has it:
        /// <see cref="Provider"/>, environment variables, and in the Editor the per-project key file.
        /// </summary>
        public static EncryptionKeys Resolve(string environmentPrefix)
        {
            EncryptionKeys result = new();
            result.FillMissing(Provider?.Invoke());
            result.FillMissing(FromEnvironment(environmentPrefix));
            if (Application.isEditor)
            {
                result.FillMissing(EncryptionKeyFile.Load());
            }

            return result.IsEmpty ? null : result;
        }

        public static string EncodeAesKey(byte[] key) => Convert.ToBase64String(key);

        public static byte[] DecodeAesKey(string value)
        {
            try
            {
                return Convert.FromBase64String(value.Trim());
            }
            catch (FormatException)
            {
                return null;
            }
        }

        /// <summary>Keys travel as Base64 of the XML, so they survive shells, JSON and environment variables.</summary>
        public static string EncodeXml(string xml) => string.IsNullOrWhiteSpace(xml) ? null : Convert.ToBase64String(Encoding.UTF8.GetBytes(xml));

        /// <summary>Accepts the Base64 form, or raw XML.</summary>
        public static string DecodeXml(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            value = value.Trim();
            if (value[0] == '<')
            {
                return value;
            }

            try
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(value));
            }
            catch (FormatException)
            {
                return null;
            }
        }

        private void FillMissing(EncryptionKeys other)
        {
            if (other == null)
            {
                return;
            }

            if (!HasAesKey && other.HasAesKey)
            {
                AesKey = other.AesKey;
            }

            if (!HasPublicKey && other.HasPublicKey)
            {
                RsaPublicKey = other.RsaPublicKey;
            }

            if (!HasPrivateKey && other.HasPrivateKey)
            {
                RsaPrivateKey = other.RsaPrivateKey;
            }
        }
    }
}
