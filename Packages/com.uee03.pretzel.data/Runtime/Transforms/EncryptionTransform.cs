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
    /// Layout: "PRTZ", version, mode, [RSA only: wrapped key length (2 bytes, big endian) and the wrapped key], IV (16), ciphertext, MAC (32).
    /// </para>
    /// </summary>
    public sealed class EncryptionTransform : IDataTransform
    {
        private static readonly byte[] Magic = { (byte)'P', (byte)'R', (byte)'T', (byte)'Z' };
        private const byte FormatVersion = 1;
        private const byte ModeAes = 1;
        private const byte ModeRsa = 2;
        private const int IvSize = 16;
        private const int MacSize = 32;
        private const int HeaderSize = 6;

        private readonly EncryptionMode _mode;
        private readonly EncryptionKeys _keys;

        /// <summary>
        /// Encoding needs the AES key (<see cref="EncryptionMode.Aes"/>) or an RSA public key (<see cref="EncryptionMode.Rsa"/>).
        /// Decoding follows the mode written in the file, and needs the AES key or the RSA private key.
        /// </summary>
        public EncryptionTransform(EncryptionMode mode, EncryptionKeys keys)
        {
            _mode = mode;
            _keys = keys ?? throw new ArgumentNullException(nameof(keys));

            if (mode == EncryptionMode.Aes && !keys.HasAesKey)
            {
                throw new InvalidOperationException("Encryption mode AES needs a 32 byte AES key.");
            }

            if (mode == EncryptionMode.Rsa && !keys.HasPublicKey && !keys.HasPrivateKey)
            {
                throw new InvalidOperationException("Encryption mode RSA needs an RSA public key.");
            }
        }

        public byte[] Encode(byte[] data)
        {
            byte[] master;
            byte[] wrapped = null;
            if (_mode == EncryptionMode.Aes)
            {
                master = _keys.AesKey;
            }
            else
            {
                master = EncryptionKeys.GenerateAesKey();
                wrapped = WrapKey(master);
            }

            DeriveKeys(master, out byte[] encryptionKey, out byte[] macKey);

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
            output.Write(Magic, 0, Magic.Length);
            output.WriteByte(FormatVersion);
            output.WriteByte(_mode == EncryptionMode.Aes ? ModeAes : ModeRsa);
            if (wrapped != null)
            {
                output.WriteByte((byte)(wrapped.Length >> 8));
                output.WriteByte((byte)wrapped.Length);
                output.Write(wrapped, 0, wrapped.Length);
            }

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

            for (int i = 0; i < Magic.Length; i++)
            {
                if (data[i] != Magic[i])
                {
                    throw new InvalidDataException("This is not a Pretzel encrypted file (missing the PRTZ header).");
                }
            }

            if (data[4] != FormatVersion)
            {
                throw new InvalidDataException($"Unsupported encryption format version {data[4]}.");
            }

            byte mode = data[5];
            int offset = HeaderSize;
            if (mode == ModeAes)
            {
                if (!_keys.HasAesKey)
                {
                    throw new InvalidOperationException("This file was encrypted with an AES key, and no AES key is available.");
                }

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

            if (mode != ModeRsa)
            {
                throw new InvalidDataException($"Unknown encryption mode {mode}.");
            }

            if (!_keys.HasPrivateKey)
            {
                throw new InvalidOperationException("This file was encrypted with an RSA public key. Decrypting needs the RSA private key.");
            }

            int length = (data[offset] << 8) | data[offset + 1];
            offset += 2;
            if (offset + length + IvSize + MacSize > data.Length)
            {
                throw new InvalidDataException("The wrapped key length is invalid.");
            }

            byte[] wrapped = new byte[length];
            Buffer.BlockCopy(data, offset, wrapped, 0, length);
            offset += length;
            return Decrypt(data, offset, UnwrapKey(wrapped));
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

        private byte[] WrapKey(byte[] master)
        {
            using RSA rsa = RSA.Create();
            rsa.FromXmlString(_keys.HasPublicKey ? _keys.RsaPublicKey : _keys.RsaPrivateKey);
            // Mono's RSA only supports OAEP with SHA-1. That is fine for wrapping a random key, because OAEP does not rely on SHA-1 collision resistance.
            return rsa.Encrypt(master, RSAEncryptionPadding.OaepSHA1);
        }

        private byte[] UnwrapKey(byte[] wrapped)
        {
            using RSA rsa = RSA.Create();
            rsa.FromXmlString(_keys.RsaPrivateKey);
            return rsa.Decrypt(wrapped, RSAEncryptionPadding.OaepSHA1);
        }

        private static void DeriveKeys(byte[] master, out byte[] encryptionKey, out byte[] macKey)
        {
            using HMACSHA256 hmac = new(master);
            encryptionKey = hmac.ComputeHash(Encoding.ASCII.GetBytes("pretzel-encrypt"));
            macKey = hmac.ComputeHash(Encoding.ASCII.GetBytes("pretzel-mac"));
        }
    }
}
