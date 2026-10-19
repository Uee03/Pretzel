using System;
using System.Threading;
using Newtonsoft.Json.Linq;
using Pretzel.Data;
using UnityEngine;

namespace Pretzel.Samples.Data
{
    public class DataUsageExample : MonoBehaviour
    {
        [SerializeField] private SampleAudioSettings _audioSettings;

        private readonly CancellationTokenSource _lifetime = new();

        private async void Start()
        {
            // Default store: JSON under persistentDataPath/SaveData.
            DataIO.Save("profile", new PlayerProfile { displayName = "Pretzel", level = 3 });

            LoadResult<PlayerProfile> result = DataIO.Load<PlayerProfile>("profile");
            if (result.Ok)
            {
                Debug.Log($"Loaded {result.Value.displayName}, level {result.Value.level}.");
            }
            else
            {
                Debug.Log($"No profile ({result.Status}): {result.Error}");
            }

            // Slots and folders live in the key.
            DataKey slotKey = new("profile", slot: 2, folder: "Profiles");
            DataIO.Save(slotKey, new PlayerProfile());
            Debug.Log($"Slots: {string.Join(", ", DataIO.ListSlots("Profiles"))}");

            await UseCustomStoreAsync();
        }

        private async Awaitable UseCustomStoreAsync()
        {
            DataStoreOptions options = new() { PrettyPrint = true };
            options.Transforms.Add(new GZipTransform());
            options.AddMigration<PlayerProfile>(new DelegateMigration(0, 2, MigrateToV2));
            options.AddMigration<PlayerProfile>(new DelegateMigration(1, 2, MigrateToV2));

            DataStore store = new(DataRoots.PersistentData("SampleStore"), extension: "sav", options: options);

            try
            {
                await store.SaveAsync("profile", new PlayerProfile { level = 10 }, _lifetime.Token);
                LoadResult<PlayerProfile> loaded = await store.LoadAsync<PlayerProfile>("profile", _lifetime.Token);
                Debug.Log($"Async load: {loaded.Status}, level {loaded.Value?.level}.");
            }
            catch (OperationCanceledException)
            {
                // The component was destroyed while the save was running.
            }

            // ScriptableObjects load into the existing asset, and need the JsonUtility serializer.
            if (_audioSettings)
            {
                DataStore settingsStore = new(DataRoots.PersistentData("Settings"), new JsonUtilitySerializer());
                settingsStore.Save("audio", _audioSettings);
                settingsStore.LoadInto("audio", _audioSettings);
            }
        }

        private static string MigrateToV2(string payload)
        {
            JObject json = JObject.Parse(payload);
            if (json.TryGetValue("name", out JToken name))
            {
                json["displayName"] = name;
                json.Remove("name");
            }

            return json.ToString();
        }

        private void OnDestroy()
        {
            _lifetime.Cancel();
            _lifetime.Dispose();
        }
    }
}
