using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Pretzel.Data;
using UnityEngine.Scripting;

namespace Pretzel.Samples.Data
{
    /// <summary>A reward stored behind an interface. Newtonsoft writes the concrete type, limited by <see cref="KnownTypesBinder"/>.</summary>
    public interface IQuestReward
    {
        string Describe();
    }

    [Serializable, Preserve]
    public sealed class CoinReward : IQuestReward
    {
        public int amount;

        public string Describe() => amount + " coins";
    }

    [Serializable, Preserve]
    public sealed class ItemReward : IQuestReward
    {
        public string itemId;
        public int count = 1;

        public string Describe() => count + " x " + itemId;
    }

    [Serializable, Preserve, SchemaVersion(1)]
    public sealed class QuestLog
    {
        public SerializableGuid questId = SerializableGuid.NewGuid();
        public string title;
        public List<IQuestReward> rewards = new();
    }

    /// <summary>
    /// Allow-list for <see cref="TypeNameHandling"/>. Without a binder, a save file could name any type in the game,
    /// which is a known deserialization attack. Short names also keep files valid when a type moves to another assembly.
    /// </summary>
    public sealed class KnownTypesBinder : ISerializationBinder
    {
        private readonly Dictionary<string, Type> _types = new(StringComparer.Ordinal);
        private readonly Dictionary<Type, string> _names = new();

        public KnownTypesBinder(params Type[] types)
        {
            foreach (Type type in types)
            {
                _types.Add(type.Name, type);
                _names.Add(type, type.Name);
            }
        }

        public Type BindToType(string assemblyName, string typeName)
        {
            return _types.TryGetValue(typeName, out Type type)
                ? type
                : throw new JsonSerializationException($"Type '{typeName}' is not allowed in save data.");
        }

        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            assemblyName = null;
            typeName = _names.TryGetValue(serializedType, out string name)
                ? name
                : throw new JsonSerializationException($"Type '{serializedType}' is not registered with KnownTypesBinder.");
        }

        /// <summary>Default Pretzel settings plus polymorphic interface fields, limited to the given types.</summary>
        public static NewtonsoftSerializer CreateSerializer(params Type[] types)
        {
            JsonSerializerSettings settings = NewtonsoftSerializer.CreateDefaultSettings();
            settings.TypeNameHandling = TypeNameHandling.Auto;
            settings.SerializationBinder = new KnownTypesBinder(types);
            return new NewtonsoftSerializer(settings);
        }
    }
}
