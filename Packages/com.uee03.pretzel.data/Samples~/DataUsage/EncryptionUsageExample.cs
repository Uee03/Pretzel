using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using Newtonsoft.Json;
using Pretzel.Data;
using UnityEngine;

namespace Pretzel.Samples.Data
{
    /// <summary>
    /// AES-encrypted saves. Add to a GameObject and use its component context menu in Play mode.
    /// The static methods are the patterns to copy into your own startup and sign-in code.
    /// </summary>
    public sealed class EncryptionUsageExample : MonoBehaviour
    {
        private static readonly DataKey ExampleKey = new("encryption-example-profile", slot: 1, folder: "PretzelSamples");

        [ContextMenu("Encryption/Save and load configured AES profile")]
        public void SaveAndLoadConfiguredAes()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Enter Play mode before running the example.", this);
                return;
            }

            try
            {
                // A local store built from the DataIOConfig asset and its key source. DataIO.Default is left alone.
                DataStore store = RequireAesConfig().CreateStore();
                store.Save(ExampleKey, new PlayerProfile { displayName = "Encrypted player", level = 7 });
                Report(store.Load<PlayerProfile>(ExampleKey), this);
            }
            catch (Exception ex) when (IsExpectedFailure(ex))
            {
                Debug.LogError("Encrypted example failed: " + ex.Message, this);
            }
        }

        /// <summary>Explicit AES store, independent of DataIOConfig and DataIO.Default. Create it once and keep it.</summary>
        public static DataStore CreateAesStore(EncryptionKeys keys)
        {
            return DataStoreFactory.Create(DataRoots.PersistentData("PretzelSamples/Aes"),
                DataFormat.Encrypted, keys, EncryptionMode.Aes);
        }

        // Examples of explicit sources; choose one and reuse the same key across launches:
        // CreateAesStore(GameEncryptionKeyConfig.Load()); // Bundled JSON.
        // CreateAesStore(EncryptionKeys.FromEnvironment("PRETZEL_MYGAME")); // Process environment, not a .env parser.
        // CreateAesStore(EncryptionKeys.Resolve("PRETZEL_MYGAME")); // Provider > environment > Editor file > JSON.
        // CreateAesStore(GameEncryptionKeyConfig.FromJson(downloadedJson)); // JSON supplied by your own code.
        // Do not GenerateAesKey() on each launch: a different key cannot open yesterday's saves.

        /// <summary>
        /// After sign-in, fetches a Base64 AES key with your HTTPS client and makes DataIO use it.
        /// Returns false when the service returned no valid key. Cancellation and request failures propagate to the caller.
        /// </summary>
        public static async Awaitable<bool> EnableEncryptedSavesAfterSignInAsync(
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

            DataIOConfig config = RequireAesConfig();
            string encoded = await requestKeyAsync(accessToken, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            byte[] key = EncryptionKeys.DecodeAesKey(encoded);
            if (key == null || key.Length != EncryptionKeys.AesKeySize)
            {
                return false;
            }

            // Keep the key in memory only. Never log the token or the key, or put them in PlayerPrefs.
            DataIO.Default = config.CreateStore(keys: new EncryptionKeys { AesKey = key });
            return true;
        }

        /// <summary>On sign-out: forget the store and any provided key, so the next user cannot read this user's saves.</summary>
        public static void SignOut()
        {
            DataIO.Reset();
            EncryptionKeys.Provider = null;
        }

        private static DataIOConfig RequireAesConfig()
        {
            DataIOConfig config = DataIOConfig.Load();
            if (!config || config.format != DataFormat.Encrypted || config.encryptionMode != EncryptionMode.Aes)
            {
                throw new InvalidOperationException("Choose Encrypted and AES in Pretzel > Data Format first.");
            }

            return config;
        }

        private static bool IsExpectedFailure(Exception ex)
        {
            return ex is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException
                or NotSupportedException or CryptographicException or JsonException;
        }

        private static void Report(LoadResult<PlayerProfile> result, UnityEngine.Object context)
        {
            switch (result.Status)
            {
                case DataStatus.Ok:
                    Debug.Log($"Loaded {result.Value.displayName}, level {result.Value.level}. From backup: {result.RecoveredFromBackup}.", context);
                    break;
                case DataStatus.NotFound:
                    Debug.Log("No example save yet. Start a new profile.", context);
                    break;
                default:
                    // MissingKey or AuthenticationFailed: restore the right key; never overwrite the file with a new profile.
                    Debug.LogError($"Could not read the encrypted save ({result.ErrorCode}): {result.Error}", context);
                    break;
            }
        }
    }
}
