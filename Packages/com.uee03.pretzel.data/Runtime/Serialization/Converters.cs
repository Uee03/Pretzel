using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Pretzel.Data
{
    internal sealed class Vector2Converter : JsonConverter<Vector2>
    {
        public override void WriteJson(JsonWriter writer, Vector2 value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.x);
            writer.WritePropertyName("y");
            writer.WriteValue(value.y);
            writer.WriteEndObject();
        }

        public override Vector2 ReadJson(JsonReader reader, Type objectType, Vector2 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return default;
            }

            JObject obj = JObject.Load(reader);
            return new Vector2(obj.Value<float>("x"), obj.Value<float>("y"));
        }
    }

    internal sealed class Vector3Converter : JsonConverter<Vector3>
    {
        public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.x);
            writer.WritePropertyName("y");
            writer.WriteValue(value.y);
            writer.WritePropertyName("z");
            writer.WriteValue(value.z);
            writer.WriteEndObject();
        }

        public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return default;
            }

            JObject obj = JObject.Load(reader);
            return new Vector3(obj.Value<float>("x"), obj.Value<float>("y"), obj.Value<float>("z"));
        }
    }

    internal sealed class QuaternionConverter : JsonConverter<Quaternion>
    {
        public override void WriteJson(JsonWriter writer, Quaternion value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.x);
            writer.WritePropertyName("y");
            writer.WriteValue(value.y);
            writer.WritePropertyName("z");
            writer.WriteValue(value.z);
            writer.WritePropertyName("w");
            writer.WriteValue(value.w);
            writer.WriteEndObject();
        }

        public override Quaternion ReadJson(JsonReader reader, Type objectType, Quaternion existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return Quaternion.identity;
            }

            JObject obj = JObject.Load(reader);
            return new Quaternion(obj.Value<float>("x"), obj.Value<float>("y"), obj.Value<float>("z"), obj.Value<float>("w"));
        }
    }

    internal sealed class ColorConverter : JsonConverter<Color>
    {
        public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("r");
            writer.WriteValue(value.r);
            writer.WritePropertyName("g");
            writer.WriteValue(value.g);
            writer.WritePropertyName("b");
            writer.WriteValue(value.b);
            writer.WritePropertyName("a");
            writer.WriteValue(value.a);
            writer.WriteEndObject();
        }

        public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return default;
            }

            JObject obj = JObject.Load(reader);
            return new Color(obj.Value<float>("r"), obj.Value<float>("g"), obj.Value<float>("b"), obj.Value<float>("a"));
        }
    }

    /// <summary>Stores <see cref="SerializableGuid"/> as a plain string, because its backing field is private.</summary>
    internal sealed class SerializableGuidConverter : JsonConverter<SerializableGuid>
    {
        public override void WriteJson(JsonWriter writer, SerializableGuid value, JsonSerializer serializer)
        {
            writer.WriteValue(value?.ToString());
        }

        public override SerializableGuid ReadJson(JsonReader reader, Type objectType, SerializableGuid existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            string text;
            if (reader.TokenType == JsonToken.StartObject)
            {
                // JsonUtility writes the backing field, so accept saves made before switching serializers.
                text = JObject.Load(reader).Value<string>(SerializableGuid.GuidFieldName);
            }
            else
            {
                text = reader.Value as string;
            }

            if (string.IsNullOrEmpty(text))
            {
                return new SerializableGuid();
            }

            return SerializableGuid.TryParse(text, out SerializableGuid guid)
                ? guid
                : throw new JsonSerializationException($"'{text}' is not a valid GUID.");
        }
    }
}
