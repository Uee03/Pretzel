using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pretzel.Data;
using UnityEngine;

namespace Pretzel.Samples.Data
{
    /// <summary>
    /// Keys, slots, a custom store with migrations, async saves and ScriptableObject settings.
    /// Everything is written under a sample folder, so importing it never touches your game's saves.
    /// </summary>
    public sealed class DataUsageExample : MonoBehaviour
    {
        private const string SampleFolder = "PretzelSamples";

        [SerializeField] private SampleAudioSettings _audioSettings;

        private DataStore _profileStore;
        private DataStore _settingsStore;
        private SampleAudioSettings _runtimeAudio;

        private void Awake()
        {
            // Create each store once and keep it. A store snapshots its options when constructed.
            DataStoreOptions options = new();
            options.Transforms.Add(new BinaryDataStrategy());
            // Old files without a schema (0) and schema 1 files both used "name".
            options.AddMigration<PlayerProfile>(new DelegateMigration(0, 2, RenameNameToDisplayName));
            options.AddMigration<PlayerProfile>(new DelegateMigration(1, 2, RenameNameToDisplayName));
            _profileStore = new DataStore(DataRoots.PersistentData(SampleFolder + "/Profiles"), extension: "sav", options: options);

            // ScriptableObjects load into an existing instance and need the JsonUtility serializer.
            _settingsStore = new DataStore(DataRoots.PersistentData(SampleFolder + "/Settings"), new JsonUtilitySerializer());
        }

        private async void Start()
        {
            CancellationToken cancellationToken = destroyCancellationToken;
            try
            {
                UseDefaultStore();
                await UseCustomStoreAsync(cancellationToken);
                LoadAudioSettings();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Destroyed while a save or load was running. The store finishes or skips the write safely.
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException
                or ArgumentException or NotSupportedException or JsonException or CryptographicException)
            {
                // Saves throw on I/O or configuration problems. Keep progress in memory and offer a retry.
                Debug.LogException(ex, this);
            }
        }

        private void OnDestroy()
        {
            if (_runtimeAudio)
            {
                Destroy(_runtimeAudio);
            }
        }

        private void UseDefaultStore()
        {
            // DataIO.Default follows the DataIOConfig asset (Pretzel > Data Format), or plain JSON under
            // persistentDataPath/SaveData when there is none. An Encrypted config needs its key first.
            DataKey profileKey = new("profile", folder: SampleFolder);
            DataIO.Save(profileKey, new PlayerProfile { displayName = "Pretzel", level = 3 });
            Report("Default store", DataIO.Load<PlayerProfile>(profileKey));

            // Slots and folders are part of the key, so one API covers every save.
            DataIO.Save(new DataKey("profile", slot: 2, folder: SampleFolder), new PlayerProfile());
            Debug.Log($"Sample slots: {string.Join(", ", DataIO.ListSlots(SampleFolder))}", this);
        }

        private async Awaitable UseCustomStoreAsync(CancellationToken cancellationToken)
        {
            // Serialization happens here on the main thread; file I/O runs on a background thread.
            await _profileStore.SaveAsync("profile", new PlayerProfile { level = 10 }, cancellationToken);
            Report("Custom store", await _profileStore.LoadAsync<PlayerProfile>("profile", cancellationToken));
        }

        private void LoadAudioSettings()
        {
            if (!_audioSettings)
            {
                return;
            }

            // Load into a runtime copy, so Play mode never changes the asset in the Editor.
            _runtimeAudio = Instantiate(_audioSettings);
            LoadResult<SampleAudioSettings> result = _settingsStore.LoadInto("audio", _runtimeAudio);
            switch (result.Status)
            {
                case DataStatus.Ok:
                    Debug.Log($"Audio settings loaded. Master volume: {_runtimeAudio.masterVolume}.", this);
                    break;
                case DataStatus.NotFound:
                    _settingsStore.Save("audio", _runtimeAudio);
                    break;
                default:
                    // Keep the defaults and leave the damaged file for the player or support to recover.
                    Debug.LogError($"Audio settings could not be read ({result.ErrorCode}): {result.Error}", this);
                    break;
            }
        }

        private void Report(string source, LoadResult<PlayerProfile> result)
        {
            switch (result.Status)
            {
                case DataStatus.Ok:
                    if (result.RecoveredFromBackup)
                    {
                        Debug.LogWarning($"{source}: the main file was damaged, so the backup was loaded.", this);
                    }

                    Debug.Log($"{source}: loaded {result.Value.displayName}, level {result.Value.level}.", this);
                    break;
                case DataStatus.NotFound:
                    Debug.Log($"{source}: no profile yet.", this);
                    break;
                default:
                    // Never overwrite a corrupt save with a new profile. Offer recovery instead.
                    Debug.LogError($"{source}: the profile could not be read ({result.ErrorCode}): {result.Error}", this);
                    break;
            }
        }

        private static string RenameNameToDisplayName(string payload)
        {
            JObject json = JObject.Parse(payload);
            if (json.TryGetValue("name", out JToken name))
            {
                json["displayName"] = name;
                json.Remove("name");
            }

            return json.ToString(Formatting.None);
        }
    }
}
