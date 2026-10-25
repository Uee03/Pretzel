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
        /// <param name="base64">Writes Binary and Encrypted files as Base64 text instead of raw bytes.</param>
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
            bool keepBackup = true,
            Action<DataStoreOptions> configure = null)
        {
            Guard.NotNull(root, nameof(root));

            DataStoreOptions options = new()
            {
                PrettyPrint = format == DataFormat.PlainText && prettyPrint,
                KeepBackup = keepBackup
            };
            options.Transforms.AddRange(CreateTransforms(format, keys, mode, base64));
            configure?.Invoke(options);

            string fileExtension = string.IsNullOrWhiteSpace(extension) ? DataFormats.DefaultExtension(format) : extension;
            return new DataStore(root, serializer, fileExtension, options);
        }

        /// <summary>The byte transforms a format uses, in the order they run on save.</summary>
        public static List<IDataTransform> CreateTransforms(DataFormat format, EncryptionKeys keys, EncryptionMode mode, bool base64)
        {
            List<IDataTransform> transforms = new() { CreateStrategy(format, keys, mode) };

            if (base64 && format != DataFormat.PlainText)
            {
                transforms.Add(new Base64Transform());
            }

            return transforms;
        }

        /// <summary>Creates an interchangeable encoding strategy.</summary>
        public static IDataFormatStrategy CreateStrategy(DataFormat format, EncryptionKeys keys = null, EncryptionMode mode = EncryptionMode.Aes)
        {
            return format switch
            {
                DataFormat.PlainText => new PlainTextStrategy(),
                DataFormat.Binary => new BinaryDataStrategy(),
                DataFormat.Encrypted => new EncryptedDataStrategy(keys
                    ?? throw new InvalidOperationException("Encrypted data needs EncryptionKeys. See EncryptionKeys.Resolve."), mode),
                _ => throw new NotSupportedException($"Data format {format} is not supported.")
            };
        }

        /// <summary>Inject a built-in or project-owned strategy. Changing strategy does not convert existing files.</summary>
        public static DataStore CreateWithStrategy(
            IDataRoot root,
            IDataFormatStrategy strategy,
            ISerializer serializer = null,
            string extension = null,
            bool base64 = false,
            bool prettyPrint = false,
            bool keepBackup = true,
            Action<DataStoreOptions> configure = null)
        {
            Guard.NotNull(strategy, nameof(strategy));
            DataStoreOptions options = new() { PrettyPrint = prettyPrint, KeepBackup = keepBackup };
            options.Transforms.Add(strategy);
            if (base64)
            {
                options.Transforms.Add(new Base64Transform());
            }

            configure?.Invoke(options);
            return new DataStore(root, serializer, extension ?? strategy.DefaultExtension, options);
        }
    }
}
