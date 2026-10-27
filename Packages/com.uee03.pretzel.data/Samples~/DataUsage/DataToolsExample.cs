using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Pretzel.Data;
using UnityEngine;

namespace Pretzel.Samples.Data
{
    /// <summary>
    /// Keys, GUIDs, JSON helpers, interface fields, listing and deleting saves, and the byte transforms.
    /// Add to a GameObject and press Play. Files go under persistentDataPath/PretzelSamples/Tools.
    /// </summary>
    public sealed class DataToolsExample : MonoBehaviour
    {
        private const string SampleFolder = "PretzelSamples";

        // SerializableGuid has its own drawer, with buttons to copy and regenerate it.
        [SerializeField] private SerializableGuid _playerId = SerializableGuid.NewGuid();

        private DataStore _store;

        private void Awake()
        {
            // One store for quest logs: binary strategy written as Base64 text, interface fields allowed for two reward types.
            _store = DataStoreFactory.CreateWithStrategy(
                DataRoots.PersistentData(SampleFolder + "/Tools"),
                DataStoreFactory.CreateStrategy(DataFormat.Binary),
                KnownTypesBinder.CreateSerializer(typeof(CoinReward), typeof(ItemReward)),
                base64: true,
                configure: options => options.SlotPrefix = "Save_");
        }

        private async void Start()
        {
            CancellationToken cancellationToken = destroyCancellationToken;
            try
            {
                KeyExamples();
                GuidExamples();
                JsonExamples();
                StoreExamples();
                await AsyncStoreExamplesAsync(cancellationToken);
                TransformExamples();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Destroyed during an async save or load.
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException
                or ArgumentException or NotSupportedException or JsonException or CryptographicException)
            {
                // InvalidOperationException: for example an Encrypted DataIOConfig whose key is not available yet.
                Debug.LogException(ex, this);
            }
        }

        private void KeyExamples()
        {
            // A string converts to a key. Slots and folders make separate files from one name.
            DataKey key = "quest";
            DataKey slotted = key.WithSlot(2).WithFolder("Campaign");
            Debug.Log($"{key} -> {slotted}, valid {slotted.IsValid}, same as new key: {slotted == new DataKey("quest", 2, "Campaign")}", this);

            // Keys are validated, so a name can never point outside the store folder.
            try
            {
                _ = new DataKey("../outside");
            }
            catch (ArgumentException ex)
            {
                Debug.Log("Rejected key: " + ex.Message, this);
            }

            Debug.Log($"default(DataKey) is {default(DataKey)}, valid {default(DataKey).IsValid}", this);
        }

        private void GuidExamples()
        {
            Guid guid = _playerId;                         // implicit to System.Guid
            string text = _playerId;                       // implicit to string
            SerializableGuid copy = (SerializableGuid)text; // explicit from string, because parsing can fail
            Debug.Log($"Player id {text}, round trip equal {copy == _playerId && copy.ToGuid() == guid}", this);

            if (!SerializableGuid.TryParse("not-a-guid", out _))
            {
                Debug.Log("TryParse rejects bad input without throwing.", this);
            }

            SerializableGuid empty = new(SerializableGuid.EmptyGuidString());
            Debug.Log($"Empty: {empty.IsEmpty}, a new id string: {SerializableGuid.NewGuidString()}", this);

            SerializableGuid regenerated = new(Guid.NewGuid());
            regenerated.Regenerate();
            Debug.Log($"Regenerated to {regenerated.GuidString}", this);
        }

        private void JsonExamples()
        {
            // The JSON helpers use a shared Newtonsoft serializer and never create the DataIO store.
            PlayerProfile profile = new() { displayName = "Ada", level = 5, position = new Vector3(1f, 2f, 3f) };
            string json = profile.ToJson(prettyPrint: true);
            Debug.Log(json, this);

            PlayerProfile loaded = json.FromJson<PlayerProfile>();
            Debug.Log($"Loaded {loaded.displayName} at {loaded.position}", this);

            // Overwrite copies values into an existing object, for example one referenced elsewhere.
            "{\"level\":6}".FromJsonOverwrite(loaded);
            Debug.Log($"Level after overwrite: {loaded.level}", this);

            // Pass a serializer to use JsonUtility rules, for example for a ScriptableObject.
            Debug.Log(profile.ToJson(serializer: new JsonUtilitySerializer()), this);
        }

        private void StoreExamples()
        {
            DataKey key = new("quest", slot: 1);
            QuestLog log = new()
            {
                title = "Find the pretzel",
                rewards = { new CoinReward { amount = 50 }, new ItemReward { itemId = "salt", count = 3 } }
            };
            _store.Save(key, log);
            Debug.Log($"Exists: {_store.Exists(key)}", this);

            if (_store.TryLoad(key, out QuestLog loaded))
            {
                // The interface list comes back with the concrete reward types.
                foreach (IQuestReward reward in loaded.rewards)
                {
                    Debug.Log($"Reward: {reward.Describe()}", this);
                }
            }

            // ValueOr gives a fallback for a missing save. Check Status first when corruption must be reported.
            LoadResult<QuestLog> missing = _store.Load<QuestLog>(new DataKey("quest", slot: 9));
            QuestLog fallback = missing.ValueOr(new QuestLog { title = "New quest" });
            Debug.Log($"Slot 9: {missing.Status} ({missing.ErrorCode}), using '{fallback.title}'", this);

            if (_store.Load<QuestLog>(key).TryGetValue(out QuestLog again))
            {
                Debug.Log($"Loaded again: {again.title}", this);
            }

            _store.Save(new DataKey("quest", slot: 3), log);
            Debug.Log($"Slots: {string.Join(", ", _store.ListSlots())}", this);
            foreach (DataKey saved in _store.List())
            {
                Debug.Log($"Saved item: {saved}", this);
            }

            Debug.Log($"Deleted slot 3: {_store.Delete(new DataKey("quest", slot: 3))}", this);
        }

        private async Awaitable AsyncStoreExamplesAsync(CancellationToken cancellationToken)
        {
            DataKey key = new("quest", slot: 4);
            await _store.SaveAsync(key, new QuestLog { title = "Async quest" }, cancellationToken);
            Debug.Log($"Exists async: {await _store.ExistsAsync(key, cancellationToken)}", this);

            IReadOnlyList<DataKey> keys = await _store.ListAsync(cancellationToken: cancellationToken);
            IReadOnlyList<int> slots = await _store.ListSlotsAsync(cancellationToken: cancellationToken);
            Debug.Log($"{keys.Count} saved item(s) in slots {string.Join(", ", slots)}", this);

            QuestLog target = new();
            LoadResult<QuestLog> into = await _store.LoadIntoAsync(key, target, cancellationToken);
            Debug.Log($"LoadIntoAsync: {into.Status}, title '{target.title}'", this);

            Debug.Log($"Deleted async: {await _store.DeleteAsync(key, cancellationToken)}", this);

            // The static DataIO facade has the same async calls for the default store.
            DataKey defaultKey = new("tools-example", folder: SampleFolder);
            await DataIO.SaveAsync(defaultKey, new PlayerProfile(), cancellationToken);
            LoadResult<PlayerProfile> profile = await DataIO.LoadAsync<PlayerProfile>(defaultKey, cancellationToken);
            Debug.Log($"DataIO async: {profile.Status}, exists {await DataIO.ExistsAsync(defaultKey, cancellationToken)}", this);
            await DataIO.DeleteAsync(defaultKey, cancellationToken);
        }

        private void TransformExamples()
        {
            byte[] json = Encoding.UTF8.GetBytes("{\"level\":1}");

            // Strategies are interchangeable byte transforms. Each has a default file extension.
            IDataFormatStrategy plain = new PlainTextStrategy();
            IDataFormatStrategy binary = new BinaryDataStrategy();
            byte[] packed = binary.Encode(json);
            Debug.Log($"Plain .{plain.DefaultExtension}, binary .{binary.DefaultExtension}, encrypted .{DataFormats.DefaultExtension(DataFormat.Encrypted)}", this);
            Debug.Log($"Binary header present: {BinaryTransform.HasHeader(packed)}, decodes back: {Encoding.UTF8.GetString(binary.Decode(packed))}", this);

            // Base64 keeps binary data safe in text-only places, such as a cloud save string.
            Base64Transform base64 = new();
            string text = Encoding.ASCII.GetString(base64.Encode(packed));
            byte[] restored = Base64Transform.FromBase64Text(text);
            Debug.Log($"Base64 round trip: {Encoding.UTF8.GetString(binary.Decode(restored))}", this);
        }
    }
}
