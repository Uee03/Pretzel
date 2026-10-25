using System;

namespace Pretzel.Data
{
    /// <summary>Authenticated AES-256 encryption for local saves.</summary>
    public sealed class EncryptedDataStrategy : IDataFormatStrategy
    {
        private readonly EncryptionTransform _transform;

        public EncryptedDataStrategy(EncryptionKeys keys, EncryptionMode mode = EncryptionMode.Aes)
        {
            if (mode != EncryptionMode.Aes)
            {
                throw new NotSupportedException("Only AES encryption is supported.");
            }
            // A private copy, so later changes to the caller's keys cannot change what this store reads or writes.
            _transform = new EncryptionTransform(mode, keys?.Clone());
        }

        public string DefaultExtension => "pretzel";

        public byte[] Encode(byte[] data) => _transform.Encode(data);

        public byte[] Decode(byte[] data) => _transform.Decode(data);
    }
}
