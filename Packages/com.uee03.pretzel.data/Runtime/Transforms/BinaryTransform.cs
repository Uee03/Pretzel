using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Linq;

#pragma warning disable CS0618 // The bundled Newtonsoft marks BsonReader and BsonWriter obsolete, but they are the only BSON types it ships.

namespace Pretzel.Data
{
    /// <summary>
    /// Turns the serializer's JSON into BSON, a binary encoding of the JSON data model, and back.
    /// It sits after the serializer, so schema versions, migrations and every serializer keep working.
    /// </summary>
    public sealed class BinaryTransform : IDataTransform
    {
        private const string RootName = "v";

        private static readonly UTF8Encoding Utf8 = new(false);

        public byte[] Encode(byte[] data)
        {
            JToken token;
            using (StreamReader text = new(new MemoryStream(data), Utf8, true))
            using (JsonTextReader reader = new(text) { DateParseHandling = DateParseHandling.None })
            {
                token = JToken.ReadFrom(reader);
            }

            // BSON documents must be objects, so any JSON root is wrapped.
            JObject root = new() { [RootName] = token };
            using MemoryStream output = new();
            using (BsonWriter writer = new(output) { CloseOutput = false })
            {
                root.WriteTo(writer);
            }

            return output.ToArray();
        }

        public byte[] Decode(byte[] data)
        {
            using BsonReader reader = new(new MemoryStream(data));
            JObject root = JObject.Load(reader);
            JToken token = root[RootName] ?? throw new InvalidDataException("The binary data has no Pretzel root value.");
            return Utf8.GetBytes(token.ToString(Formatting.None));
        }
    }
}
