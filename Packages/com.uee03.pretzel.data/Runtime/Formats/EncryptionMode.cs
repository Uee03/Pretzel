namespace Pretzel.Data
{
    public enum EncryptionMode
    {
        /// <summary>One secret AES-256 key encrypts and decrypts. The game needs the key to read its own files.</summary>
        Aes = 0
    }
}
