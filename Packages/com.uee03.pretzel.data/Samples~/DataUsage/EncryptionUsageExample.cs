using System;
using System.Threading;
using Pretzel.Data;
using UnityEngine;

namespace Pretzel.Samples.Data
{
    /// <summary>Add to a GameObject and use its component context menu in Play mode.</summary>
    public sealed class EncryptionUsageExample : MonoBehaviour
    {
        [ContextMenu("Encryption/Save and load configured AES profile")]
        public void SaveAndLoadConfiguredAes()
        {
            RequireConfig(EncryptionMode.Aes);

            // JSON, environment variables and generated embedded setup all use the same DataIO API.
            // Recreate the default after configuration changes; otherwise an existing store keeps its keys.
            DataIO.Default = DataIOConfig.Load().CreateStore();
            DataKey key = new("encryption-example-profile", slot: 1, folder: "Examples");
            DataIO.Save(key, new PlayerProfile { displayName = "Encrypted player", level = 7 });
            Report(DataIO.Load<PlayerProfile>(key));
        }

        [ContextMenu("Encryption/Write RSA report for server")]
        public void WriteRsaReport()
        {
            DataIOConfig config = RequireConfig(EncryptionMode.Rsa);
            EncryptionKeys resolved = EncryptionKeys.Resolve(config.environmentPrefix);
            if (resolved == null || !resolved.HasPublicKey)
            {
                throw new InvalidOperationException("Generate and export an RSA public key on the Keys tab first.");
            }

            // Deliberately give the client only the public key, even when the Editor has the private key.
            DataStore client = DataStoreFactory.Create(DataRoots.PersistentData("EncryptionExamples/RsaOutbox"),
                DataFormat.Encrypted, new EncryptionKeys { RsaPublicKey = resolved.RsaPublicKey }, EncryptionMode.Rsa);
            client.Save("report", new PlayerProfile { displayName = "Report from game", level = 12 });
            Debug.Log("Wrote report.pretzel under persistentDataPath/EncryptionExamples/RsaOutbox. "
                      + "Send it over HTTPS to your server. The public key cannot read this file.");
        }

        /// <summary>Call on a trusted server/tool with its private key and the received file's folder.</summary>
        public static LoadResult<PlayerProfile> ReadRsaReportOnServer(string receivedFolder, EncryptionKeys serverKeys)
        {
            if (serverKeys == null || !serverKeys.HasPrivateKey)
            {
                throw new InvalidOperationException("Only the trusted server/tool should have the RSA private key.");
            }

            DataStore server = DataStoreFactory.Create(DataRoots.Absolute(receivedFolder), DataFormat.Encrypted,
                new EncryptionKeys { RsaPrivateKey = serverKeys.RsaPrivateKey }, EncryptionMode.Rsa);
            LoadResult<PlayerProfile> result = server.Load<PlayerProfile>("report");
            Report(result);
            return result;
        }

        /// <summary>Explicit AES store, independent of DataIOConfig and DataIO.Default.</summary>
        public static DataStore CreateAesStore(EncryptionKeys keys)
        {
            return DataStoreFactory.Create(DataRoots.PersistentData("EncryptionExamples/Aes"),
                DataFormat.Encrypted, keys, EncryptionMode.Aes);
        }

        // Examples of explicit sources; choose one and reuse the same key across launches:
        // CreateAesStore(GameEncryptionKeyConfig.Load()); // Bundled JSON.
        // CreateAesStore(EncryptionKeys.FromEnvironment("PRETZEL_MYGAME")); // Process environment, not a .env parser.
        // CreateAesStore(EncryptionKeys.Resolve("PRETZEL_MYGAME")); // Provider > environment > Editor file > JSON.
        // CreateAesStore(GameEncryptionKeyConfig.FromJson(downloadedJson)); // JSON supplied by your own code.
        // Do not GenerateAesKey() on each launch: a different key cannot open yesterday's saves.

        /// <summary>After sign-in, fetch a Base64 AES key with your HTTPS client, then enable encrypted saves.</summary>
        public static async Awaitable LoadAesKeyAfterSignInAsync(
            string accessToken,
            Func<string, CancellationToken, Awaitable<string>> requestKeyAsync,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                throw new ArgumentException("Sign in before requesting a key.", nameof(accessToken));
            }

            if (requestKeyAsync == null)
            {
                throw new ArgumentNullException(nameof(requestKeyAsync));
            }

            DataIOConfig config = RequireConfig(EncryptionMode.Aes);
            string encoded = await requestKeyAsync(accessToken, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            EncryptionKeys keys = new()
            {
                AesKey = string.IsNullOrWhiteSpace(encoded) ? null : EncryptionKeys.DecodeAesKey(encoded)
            };
            if (!keys.HasAesKey)
            {
                throw new InvalidOperationException("The key service must return a Base64-encoded 32 byte AES key.");
            }

            // Keep the key in memory. Never log the token/key or put it in PlayerPrefs.
            DataStore store = config.CreateStore(keys: keys);
            EncryptionKeys.Provider = () => keys;
            DataIO.Default = store;
            await DataIO.SaveAsync("authenticated-example-profile",
                new PlayerProfile { displayName = "Signed-in player" }, cancellationToken);
            Report(await DataIO.LoadAsync<PlayerProfile>("authenticated-example-profile", cancellationToken));
        }

        private static DataIOConfig RequireConfig(EncryptionMode mode)
        {
            DataIOConfig config = DataIOConfig.Load();
            if (!config || config.format != DataFormat.Encrypted || config.encryptionMode != mode)
            {
                throw new InvalidOperationException($"Choose Encrypted and {mode} in Pretzel > Data Format first.");
            }

            return config;
        }

        private static void Report(LoadResult<PlayerProfile> result)
        {
            switch (result.Status)
            {
                case DataStatus.Ok:
                    Debug.Log($"Loaded {result.Value.displayName}, level {result.Value.level}. Backup: {result.RecoveredFromBackup}");
                    break;
                case DataStatus.NotFound:
                    Debug.Log("No example save yet. Start a new profile.");
                    break;
                case DataStatus.Corrupt:
                    Debug.LogError($"Could not decrypt the example save (wrong key or damaged data): {result.Error}");
                    break;
            }
        }
    }
}
