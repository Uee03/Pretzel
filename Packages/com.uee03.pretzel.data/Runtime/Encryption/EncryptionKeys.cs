using System;
using System.Collections.Generic;
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

        /// <summary>
        /// Earlier AES keys, kept so saves written before a key change still open. They are only used to read:
        /// new files always use <see cref="AesKey"/>, so old files move to the new key as they are saved again.
        /// </summary>
        public List<byte[]> PreviousAesKeys { get; } = new();

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
        /// <see cref="Provider"/>, environment variables, in the Editor the per-project key file,
        /// then the optional bundled <see cref="GameEncryptionKeyConfig"/>. Use <see cref="Resolve(string, KeySource, out string)"/> to pin one source.
        /// </summary>
        public static EncryptionKeys Resolve(string environmentPrefix) => Resolve(environmentPrefix, KeySource.Automatic, out _);

        /// <summary>
        /// Finds keys from <paramref name="source"/>. <paramref name="origin"/> names the sources that supplied them, for diagnostics.
        /// It never contains a key.
        /// </summary>
        public static EncryptionKeys Resolve(string environmentPrefix, KeySource source, out string origin)
        {
            EncryptionKeys result = new();
            List<string> used = new();

            void Take(EncryptionKeys keys, string name)
            {
                if (result.FillMissing(keys))
                {
                    used.Add(name);
                }
            }

            if (source is KeySource.Automatic or KeySource.Provider)
            {
                Take(Provider?.Invoke(), "Provider (embedded setup or your code)");
            }

            if (source is KeySource.Automatic or KeySource.Environment)
            {
                Take(FromEnvironment(environmentPrefix), "environment variables");
            }

            if (source == KeySource.Automatic && Application.isEditor)
            {
                Take(EncryptionKeyFile.Load(), "Editor key file");
            }

            if (source is KeySource.Automatic or KeySource.BundledJson)
            {
                Take(GameEncryptionKeyConfig.Load(), "bundled JSON config");
            }

            origin = used.Count == 0 ? "none" : string.Join(" + ", used);
            return result.IsEmpty ? null : result;
        }

        /// <summary>A short fingerprint, so a key can be recognised in logs and the Editor without showing it.</summary>
        public static string Fingerprint(byte[] data)
        {
            using SHA256 sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(data);
            StringBuilder text = new();
            for (int i = 0; i < 6; i++)
            {
                text.Append(hash[i].ToString("X2"));
                if (i < 5)
                {
                    text.Append(':');
                }
            }

            return text.ToString();
        }

        public EncryptionKeys Clone()
        {
            EncryptionKeys copy = new() { AesKey = AesKey, RsaPublicKey = RsaPublicKey, RsaPrivateKey = RsaPrivateKey };
            copy.PreviousAesKeys.AddRange(PreviousAesKeys);
            return copy;
        }

        /// <summary>
        /// The keys a game client should hold for <paramref name="mode"/>: the AES key and its previous keys, or only the RSA public key.
        /// The RSA private key is never included.
        /// </summary>
        public EncryptionKeys ForClient(EncryptionMode mode)
        {
            EncryptionKeys copy = new();
            if (mode == EncryptionMode.Aes)
            {
                copy.AesKey = AesKey;
                copy.PreviousAesKeys.AddRange(PreviousAesKeys);
            }
            else
            {
                copy.RsaPublicKey = RsaPublicKey;
            }

            return copy;
        }

        /// <summary>True when this holds what a client needs to write in <paramref name="mode"/>.</summary>
        public bool CanWrite(EncryptionMode mode) => mode == EncryptionMode.Aes ? HasAesKey : HasPublicKey;

        /// <summary>Adds an earlier key that old saves may still use. Ignores duplicates, the current key and wrong sizes.</summary>
        public void AddPreviousAesKey(byte[] key)
        {
            if (key == null || key.Length != AesKeySize || SameKey(key, AesKey))
            {
                return;
            }

            foreach (byte[] existing in PreviousAesKeys)
            {
                if (SameKey(existing, key))
                {
                    return;
                }
            }

            PreviousAesKeys.Add(key);
        }

        /// <summary>One line for logs and the Editor. Fingerprints only, never key material.</summary>
        public string Describe(EncryptionMode mode, string origin)
        {
            if (mode == EncryptionMode.Aes)
            {
                string text = HasAesKey ? $"AES key {Fingerprint(AesKey)} from {origin}" : $"no AES key (looked in: {origin})";
                return PreviousAesKeys.Count > 0 ? $"{text}, {PreviousAesKeys.Count} previous key(s) for older saves" : text;
            }

            string rsa = HasPublicKey ? $"RSA public key {Fingerprint(Encoding.UTF8.GetBytes(RsaPublicKey))} from {origin} (writes only)" : $"no RSA public key (looked in: {origin})";
            return HasPrivateKey ? rsa + ". WARNING: a private key is loaded here and must never ship" : rsa;
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

        private bool FillMissing(EncryptionKeys other)
        {
            if (other == null)
            {
                return false;
            }

            bool used = false;
            if (!HasAesKey && other.HasAesKey)
            {
                AesKey = other.AesKey;
                used = true;
            }

            if (!HasPublicKey && other.HasPublicKey)
            {
                RsaPublicKey = other.RsaPublicKey;
                used = true;
            }

            if (!HasPrivateKey && other.HasPrivateKey)
            {
                RsaPrivateKey = other.RsaPrivateKey;
                used = true;
            }

            int before = PreviousAesKeys.Count;
            foreach (byte[] previous in other.PreviousAesKeys)
            {
                AddPreviousAesKey(previous);
            }

            return used || PreviousAesKeys.Count != before;
        }

        private static bool SameKey(byte[] a, byte[] b)
        {
            return a != null && b != null && a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
        }
    }
}
