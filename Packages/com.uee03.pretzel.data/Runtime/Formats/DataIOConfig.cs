using System;
using UnityEngine;

namespace Pretzel.Data
{
    /// <summary>
    /// How <see cref="DataIO"/> saves. Created and edited by the Pretzel/Data Format window, stored at Assets/Resources/Pretzel/DataIOConfig.asset.
    /// It holds no secrets, so it is safe to commit and to ship.
    /// </summary>
    public sealed class DataIOConfig : ScriptableObject
    {
        public const string ResourcePath = "Pretzel/DataIOConfig";

        public DataFormat format = DataFormat.PlainText;
        public SerializerChoice serializer = SerializerChoice.Newtonsoft;
        public string fileExtension = "json";
        public string saveFolder = "SaveData";
        public bool prettyPrint = true;
        public bool keepBackup = true;

        [Tooltip("Writes Compressed, Binary and Encrypted files as Base64 text.")]
        public bool storeAsBase64;

        public EncryptionMode encryptionMode = EncryptionMode.Aes;
        public int rsaKeySize = EncryptionKeys.RecommendedRsaKeySize;

        [Tooltip("Names the environment variables that hold the keys: <prefix>_AES_KEY, <prefix>_RSA_PUBLIC and <prefix>_RSA_PRIVATE.")]
        public string environmentPrefix = "PRETZEL_GAME";

        [Tooltip("Where encrypted formats look for keys. Choose the one your build ships: Provider (embedded setup or token) or BundledJson. Automatic is for development.")]
        public KeySource keySource = KeySource.Automatic;

        /// <summary>The asset under Resources, or null when none exists.</summary>
        public static DataIOConfig Load() => Resources.Load<DataIOConfig>(ResourcePath);

        /// <param name="root">Defaults to persistent data under <see cref="saveFolder"/>.</param>
        /// <param name="keys">Defaults to <see cref="EncryptionKeys.Resolve(string, KeySource, out string)"/> with <see cref="keySource"/>.</param>
        public DataStore CreateStore(IDataRoot root = null, EncryptionKeys keys = null)
        {
            root ??= DataRoots.PersistentData(string.IsNullOrWhiteSpace(saveFolder) ? null : saveFolder.Trim('/', '\\'));

            if (format == DataFormat.Encrypted)
            {
                keys ??= EncryptionKeys.Resolve(environmentPrefix, keySource, out _);
                if (keys == null)
                {
                    throw new InvalidOperationException(
                        $"DataIO is set to Encrypted, but no keys were found (key source: {keySource}). Export a game key config or the embedded setup on the Keys tab, " +
                        $"set the environment variables {EncryptionKeys.AesVariable(environmentPrefix)} " +
                        $"(or {EncryptionKeys.PublicVariable(environmentPrefix)} / {EncryptionKeys.PrivateVariable(environmentPrefix)}), " +
                        "assign EncryptionKeys.Provider before the first save, " +
                        "or create the store yourself with DataStoreFactory.");
                }
            }

            ISerializer chosen = serializer == SerializerChoice.JsonUtility ? new JsonUtilitySerializer() : new NewtonsoftSerializer();
            return DataStoreFactory.Create(root, format, keys, encryptionMode, chosen, fileExtension, storeAsBase64, prettyPrint, keepBackup);
        }
    }
}
