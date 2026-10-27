using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Pretzel.Data
{
    /// <summary>
    /// AES-256-CBC with an HMAC-SHA256 over the whole file (encrypt-then-MAC). Every file gets a random IV, stored in the file,
    /// so the IV is not a secret and there is nothing to configure.
    /// <para>
    /// Layout: "PRTZ", version 1, AES mode 1, IV (16), ciphertext, MAC (32).
    /// </para>
    /// </summary>
    public sealed class EncryptionTransform : IDataTransform
    {
        private static readonly byte[] _magic = { (byte)'P', (byte)'R', (byte)'T', (byte)'Z' };
        private const byte FormatVersion = 1;
        private const byte ModeAes = 1;
        private const int IvSize = 16;
        private const int MacSize = 32;
        private const int HeaderSize = 6;

        private readonly EncryptionKeys _keys;

        public EncryptionTransform(EncryptionKeys keys) : this(EncryptionMode.Aes, keys)
        {
        }

        /// <summary>Only AES is supported.</summary>
        public EncryptionTransform(EncryptionMode mode, EncryptionKeys keys)
        {
            if (mode != EncryptionMode.Aes)
            {
                throw new NotSupportedException("Only AES encryption is supported.");
            }
            _keys = keys ?? throw new ArgumentNullException(nameof(keys));

            if (!keys.HasAesKey)
            {
                throw new InvalidOperationException("Encryption mode AES needs a 32 byte AES key.");
            }
        }

        public byte[] Encode(byte[] data)
        {
            RequireKey();
            DeriveKeys(_keys.AesKey, out byte[] encryptionKey, out byte[] macKey);

            byte[] iv = new byte[IvSize];
            using (RandomNumberGenerator random = RandomNumberGenerator.Create())
            {
                random.GetBytes(iv);
            }

            byte[] cipher;
            using (Aes aes = Aes.Create())
            {
                aes.Key = encryptionKey;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                using ICryptoTransform encryptor = aes.CreateEncryptor();
                cipher = encryptor.TransformFinalBlock(data, 0, data.Length);
            }

            using MemoryStream output = new();
            output.Write(_magic, 0, _magic.Length);
            output.WriteByte(FormatVersion);
            output.WriteByte(ModeAes);

            output.Write(iv, 0, iv.Length);
            output.Write(cipher, 0, cipher.Length);

            byte[] body = output.ToArray();
            using HMACSHA256 hmac = new(macKey);
            byte[] mac = hmac.ComputeHash(body);

            byte[] result = new byte[body.Length + MacSize];
            Buffer.BlockCopy(body, 0, result, 0, body.Length);
            Buffer.BlockCopy(mac, 0, result, body.Length, MacSize);
            return result;
        }

        public byte[] Decode(byte[] data)
        {
            if (data.Length < HeaderSize + IvSize + 16 + MacSize)
            {
                throw new InvalidDataException("The data is too short to be a Pretzel encrypted file.");
            }

            for (int i = 0; i < _magic.Length; i++)
            {
                if (data[i] != _magic[i])
                {
                    throw new InvalidDataException("This is not a Pretzel encrypted file (missing the PRTZ header).");
                }
            }

            if (data[4] != FormatVersion)
            {
                throw new NotSupportedException($"Unsupported encryption format version {data[4]}.");
            }

            byte mode = data[5];
            int offset = HeaderSize;
            if (mode == ModeAes)
            {
                RequireKey();

                // The authentication check makes trying earlier keys safe: only the key that wrote the file passes it.
                // The first failure is reported, because the current key is the one the caller expects to work.
                CryptographicException failure = null;
                try
                {
                    return Decrypt(data, offset, _keys.AesKey);
                }
                catch (CryptographicException ex)
                {
                    failure = ex;
                }

                foreach (byte[] previous in _keys.PreviousAesKeys)
                {
                    if (previous == null || previous.Length != EncryptionKeys.AesKeySize)
                    {
                        continue;
                    }

                    try
                    {
                        return Decrypt(data, offset, previous);
                    }
                    catch (CryptographicException)
                    {
                        // Not this key either.
                    }
                }

                throw failure;
            }

            throw new NotSupportedException($"Unsupported encryption mode {mode}.");
        }

        private void RequireKey()
        {
            if (!_keys.HasAesKey)
            {
                throw new DataReadException(DataError.MissingKey, "The configured 32 byte AES key is no longer available.");
            }
        }

        private static byte[] Decrypt(byte[] data, int offset, byte[] master)
        {
            DeriveKeys(master, out byte[] encryptionKey, out byte[] macKey);

            int bodyLength = data.Length - MacSize;
            byte[] body = new byte[bodyLength];
            Buffer.BlockCopy(data, 0, body, 0, bodyLength);
            using (HMACSHA256 hmac = new(macKey))
            {
                byte[] expected = hmac.ComputeHash(body);
                if (!CryptographicOperations.FixedTimeEquals(expected, new ReadOnlySpan<byte>(data, bodyLength, MacSize)))
                {
                    throw new CryptographicException("Wrong key, or the file was modified.");
                }
            }

            byte[] iv = new byte[IvSize];
            Buffer.BlockCopy(data, offset, iv, 0, IvSize);
            offset += IvSize;
            int cipherLength = bodyLength - offset;
            if (cipherLength <= 0 || cipherLength % 16 != 0)
            {
                throw new InvalidDataException("The encrypted data has an invalid length.");
            }

            using Aes aes = Aes.Create();
            aes.Key = encryptionKey;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            using ICryptoTransform decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(data, offset, cipherLength);
        }

        private static void DeriveKeys(byte[] master, out byte[] encryptionKey, out byte[] macKey)
        {
            using HMACSHA256 hmac = new(master);
            encryptionKey = hmac.ComputeHash(Encoding.ASCII.GetBytes("pretzel-encrypt"));
            macKey = hmac.ComputeHash(Encoding.ASCII.GetBytes("pretzel-mac"));
        }
    }
}
