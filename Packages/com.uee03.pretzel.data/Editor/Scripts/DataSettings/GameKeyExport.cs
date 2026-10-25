using System.IO;
using System.Text;
using UnityEditor;

namespace Pretzel.Data.Editor
{
    /// <summary>Writes the game-readable key (bundled JSON or the embedded C# setup) and keeps the config's key source in step with it.</summary>
    internal static class GameKeyExport
    {
        public const string JsonPath = "Assets/Resources/" + GameEncryptionKeyConfig.ResourcePath + ".json";

        public static bool JsonExists => File.Exists(JsonPath);

        /// <summary>Writes the AES key with its read history.</summary>
        public static string WriteJson(EncryptionKeys keys, EncryptionMode mode)
        {
            string json = GameEncryptionKeyConfig.ToJson(keys, mode);
            Directory.CreateDirectory(Path.GetDirectoryName(JsonPath));
            File.WriteAllText(JsonPath, json, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(JsonPath);
            return JsonPath;
        }

        /// <summary>Returns null on success, otherwise the reason it failed. Sets the config's key source to the exported one.</summary>
        public static string Export(DataFormatState state, bool embedded, string folder, out string path, bool replaceConfirmed = false)
        {
            DataIOConfig config = state.Config;
            EncryptionKeys keys = state.Keys;
            path = null;
            if (config.encryptionMode != EncryptionMode.Aes)
            {
                return "Unsupported saved encryption mode. Set up AES first.";
            }

            if (!keys.CanWrite(config.encryptionMode))
            {
                return "There is no AES key yet. Generate one on the Encryption tab first.";
            }

            // The shipped copy must never be the only copy of a key.
            if (state.KeysUnstored)
            {
                state.SaveKeysToFile();
            }

            if (embedded)
            {
                string error = ShippingGenerator.Generate(folder, KeyDelivery.Embedded, config, keys, out path, replaceConfirmed);
                if (error != null)
                {
                    return error;
                }
            }
            else
            {
                path = WriteJson(keys, config.encryptionMode);
            }

            SyncKeySource(state, embedded ? KeySource.Provider : KeySource.BundledJson);
            return null;
        }

        /// <summary>A delivery you just created is the one the game should read, so the config follows it.</summary>
        public static void SyncKeySource(DataFormatState state, KeySource source)
        {
            if (state.Config.keySource != source)
            {
                state.Edit("Set key source", c => c.keySource = source);
            }
        }
    }
}
