using System;
using System.Collections.Generic;
using Pretzel.Core;

namespace Pretzel.Data
{
    /// <summary>Builds a <see cref="DataStore"/> for one of the <see cref="DataFormat"/> choices.</summary>
    public static class DataStoreFactory
    {
        /// <param name="keys">Required for <see cref="DataFormat.Encrypted"/>.</param>
        /// <param name="extension">Without the dot. Defaults to <see cref="DataFormats.DefaultExtension"/>.</param>
        /// <param name="base64">Writes Compressed, Binary and Encrypted files as Base64 text instead of raw bytes.</param>
        /// <param name="prettyPrint">Only applies to <see cref="DataFormat.PlainText"/>.</param>
        public static DataStore Create(
            IDataRoot root,
            DataFormat format,
            EncryptionKeys keys = null,
            EncryptionMode mode = EncryptionMode.Aes,
            ISerializer serializer = null,
            string extension = null,
            bool base64 = false,
            bool prettyPrint = false,
            bool keepBackup = true)
        {
            Guard.NotNull(root, nameof(root));

            DataStoreOptions options = new()
            {
                PrettyPrint = format == DataFormat.PlainText && prettyPrint,
                KeepBackup = keepBackup
            };
            options.Transforms.AddRange(CreateTransforms(format, keys, mode, base64));

            string fileExtension = string.IsNullOrWhiteSpace(extension) ? DataFormats.DefaultExtension(format) : extension;
            return new DataStore(root, serializer, fileExtension, options);
        }

        /// <summary>The byte transforms a format uses, in the order they run on save.</summary>
        public static List<IDataTransform> CreateTransforms(DataFormat format, EncryptionKeys keys, EncryptionMode mode, bool base64)
        {
            List<IDataTransform> transforms = new();
            switch (format)
            {
                case DataFormat.Compressed:
                    transforms.Add(new GZipTransform());
                    break;
                case DataFormat.Binary:
                    transforms.Add(new BinaryTransform());
                    break;
                case DataFormat.Encrypted:
                    if (keys == null)
                    {
                        throw new InvalidOperationException("The Encrypted format needs EncryptionKeys. See EncryptionKeys.Resolve.");
                    }

                    transforms.Add(new EncryptionTransform(mode, keys));
                    break;
            }

            if (base64 && format != DataFormat.PlainText)
            {
                transforms.Add(new Base64Transform());
            }

            return transforms;
        }
    }
}
