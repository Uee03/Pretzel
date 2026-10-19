namespace Pretzel.Data
{
    public enum EncryptionMode
    {
        /// <summary>One secret AES-256 key encrypts and decrypts. The game needs the key to read its own files.</summary>
        Aes,

        /// <summary>A random AES key per file, wrapped with an RSA public key. Saving needs only the public key, loading needs the private key.</summary>
        Rsa
    }
}
