using System.Collections.Generic;
using Newtonsoft.Json;

namespace Pretzel.Data
{
    /// <summary>Default serializer. Handles collections, dictionaries and the common Unity value types.</summary>
    public sealed class NewtonsoftSerializer : ISerializer
    {
        private readonly JsonSerializerSettings _settings;

        public NewtonsoftSerializer() : this(CreateDefaultSettings())
        {
        }

        public NewtonsoftSerializer(JsonSerializerSettings settings)
        {
            _settings = settings ?? CreateDefaultSettings();
        }

        public static JsonSerializerSettings CreateDefaultSettings()
        {
            return new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                // The default (Auto) appends loaded items to lists that a field initializer already filled.
                ObjectCreationHandling = ObjectCreationHandling.Replace,
                Converters = new List<JsonConverter>
                {
                    new Vector2Converter(),
                    new Vector3Converter(),
                    new QuaternionConverter(),
                    new ColorConverter(),
                    new SerializableGuidConverter()
                }
            };
        }

        public string Serialize<T>(T value, bool prettyPrint)
        {
            return JsonConvert.SerializeObject(value, typeof(T), prettyPrint ? Formatting.Indented : Formatting.None, _settings);
        }

        public T Deserialize<T>(string text)
        {
            return JsonConvert.DeserializeObject<T>(text, _settings);
        }

        public void Populate<T>(string text, T target)
        {
            JsonConvert.PopulateObject(text, target, _settings);
        }
    }
}
