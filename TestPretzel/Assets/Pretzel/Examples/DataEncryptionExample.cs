using System;
using Pretzel.Data;
using UnityEngine;

namespace Pretzel.TestProject
{
    /// <summary>Add to a GameObject; run the component context-menu examples in Play mode.</summary>
    public sealed class DataEncryptionExample : MonoBehaviour
    {
        [Serializable]
        public sealed class SaveData
        {
            public string playerName = "Pretzel";
            public int level = 5;
        }

        [ContextMenu("Encryption/AES - configured offline save and load")]
        public void UseConfiguredAes()
        {
            DataIOConfig config = DataIOConfig.Load();
            if (!config || config.format != DataFormat.Encrypted || config.encryptionMode != EncryptionMode.Aes)
            {
                Debug.LogError("Choose Encrypted + AES in Pretzel > Data Format. Generate a key, then export JSON "
                               + "or embedded C# on Keys, or set the environment variables.");
                return;
            }

            // JSON/env keys resolve automatically; embedded PretzelDataSetup registers its provider before Start.
            DataIO.Default = config.CreateStore();
            DataIO.Save("encryption-example", new SaveData());
            Show(DataIO.Load<SaveData>("encryption-example"));
        }

        [ContextMenu("Encryption/AES - isolated temporary-key round trip")]
        public void DemonstrateAes()
        {
            // Demo only: fresh key AND fresh folder each time. Real saves must reuse a backed-up key.
            EncryptionKeys keys = new() { AesKey = EncryptionKeys.GenerateAesKey() };
            string folder = "PretzelEncryptionExamples/" + Guid.NewGuid().ToString("N");
            DataStore store = DataStoreFactory.Create(DataRoots.TemporaryCache(folder),
                DataFormat.Encrypted, keys, EncryptionMode.Aes);
            store.Save("profile", new SaveData());
            Show(store.Load<SaveData>("profile"));
            store.Delete("profile");
        }

        [ContextMenu("Encryption/RSA - write with configured public key")]
        public void WriteRsaReport()
        {
            DataIOConfig config = DataIOConfig.Load();
            if (!config || config.format != DataFormat.Encrypted || config.encryptionMode != EncryptionMode.Rsa)
            {
                Debug.LogError("Choose Encrypted + RSA in Pretzel > Data Format, generate a pair, and export the public key.");
                return;
            }

            EncryptionKeys keys = EncryptionKeys.Resolve(config.environmentPrefix);
            if (keys == null || !keys.HasPublicKey)
            {
                Debug.LogError("No RSA public key found. Export it on the Keys tab.");
                return;
            }

            DataStore client = DataStoreFactory.Create(DataRoots.PersistentData("PretzelEncryptionExamples/RsaOutbox"),
                DataFormat.Encrypted, new EncryptionKeys { RsaPublicKey = keys.RsaPublicKey }, EncryptionMode.Rsa);
            client.Save("report", new SaveData());
            Debug.Log("RSA report written to persistentDataPath/PretzelEncryptionExamples/RsaOutbox/report.pretzel. "
                      + "Only the server's private key can decrypt it; do not ship that key.");
        }

#if UNITY_EDITOR
        [ContextMenu("Encryption/RSA - simulate trusted server decryption (Editor only)")]
        public void ReadRsaReportAsTrustedServer()
        {
            DataIOConfig config = DataIOConfig.Load();
            EncryptionKeys keys = config ? EncryptionKeys.Resolve(config.environmentPrefix) : null;
            if (keys == null || !keys.HasPrivateKey)
            {
                Debug.LogError("This Editor-only server simulation needs the original RSA private key in Editor storage. "
                               + "Never export the private key into the game.");
                return;
            }

            DataStore server = DataStoreFactory.Create(DataRoots.PersistentData("PretzelEncryptionExamples/RsaOutbox"),
                DataFormat.Encrypted, new EncryptionKeys { RsaPrivateKey = keys.RsaPrivateKey }, EncryptionMode.Rsa);
            Show(server.Load<SaveData>("report"));
        }
#endif

        private static void Show(LoadResult<SaveData> result)
        {
            if (result.Ok)
            {
                Debug.Log($"Decrypted {result.Value.playerName}, level {result.Value.level}. Backup: {result.RecoveredFromBackup}");
            }
            else if (result.Status == DataStatus.NotFound)
            {
                Debug.Log("No example save found. Run the save example first.");
            }
            else
            {
                Debug.LogError($"Cannot load example (wrong key or damaged file): {result.Error}");
            }
        }
    }
}
