using System;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Pretzel.Data
{
    /// <summary>Versioned binary values written with BinaryWriter and read with BinaryReader. Not BinaryFormatter.</summary>
    public sealed class BinaryTransform : IDataTransform
    {
        private const int MaxDepth = 128;
        private static readonly byte[] _magic = { (byte)'P', (byte)'Z', (byte)'B', (byte)'N', 1 };
        private static readonly UTF8Encoding _utf8 = new(false, true);

        private enum Tag : byte
        {
            Null, Boolean, Integer, Double, String, Object, Array, BigInteger
        }

        public static bool HasHeader(byte[] data)
        {
            return data != null && data.Length >= 4
                && data[0] == _magic[0] && data[1] == _magic[1] && data[2] == _magic[2] && data[3] == _magic[3];
        }

        public byte[] Encode(byte[] data)
        {
            using StringReader text = new(_utf8.GetString(data));
            using JsonTextReader json = new(text) { DateParseHandling = DateParseHandling.None, MaxDepth = MaxDepth };
            JToken token = JToken.ReadFrom(json);
            if (json.Read())
            {
                throw new InvalidDataException("Unexpected content after the JSON value.");
            }

            using MemoryStream output = new();
            using BinaryWriter writer = new(output, _utf8, true);
            writer.Write(_magic);
            Write(writer, token, 0);
            return output.ToArray();
        }

        public byte[] Decode(byte[] data)
        {
            using MemoryStream input = new(data, false);
            using BinaryReader reader = new(input, _utf8);
            foreach (byte expected in _magic)
            {
                if (reader.ReadByte() != expected)
                {
                    throw new InvalidDataException("Unsupported Pretzel binary header or version.");
                }
            }

            JToken token = Read(reader, 0);
            if (input.Position != input.Length)
            {
                throw new InvalidDataException("Unexpected bytes after the binary value.");
            }

            return _utf8.GetBytes(token.ToString(Formatting.None));
        }

        private static void Write(BinaryWriter writer, JToken token, int depth)
        {
            CheckDepth(depth);
            switch (token.Type)
            {
                case JTokenType.Object:
                    JObject obj = (JObject)token;
                    writer.Write((byte)Tag.Object);
                    writer.Write(obj.Count);
                    foreach (JProperty property in obj.Properties())
                    {
                        writer.Write(property.Name);
                        Write(writer, property.Value, depth + 1);
                    }
                    break;
                case JTokenType.Array:
                    JArray array = (JArray)token;
                    writer.Write((byte)Tag.Array);
                    writer.Write(array.Count);
                    foreach (JToken item in array)
                    {
                        Write(writer, item, depth + 1);
                    }
                    break;
                case JTokenType.Integer:
                    object integer = ((JValue)token).Value;
                    if (integer is BigInteger big)
                    {
                        writer.Write((byte)Tag.BigInteger);
                        writer.Write(big.ToString(CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        writer.Write((byte)Tag.Integer);
                        writer.Write(Convert.ToInt64(integer, CultureInfo.InvariantCulture));
                    }
                    break;
                case JTokenType.Float:
                    writer.Write((byte)Tag.Double);
                    writer.Write(token.Value<double>());
                    break;
                case JTokenType.String:
                    writer.Write((byte)Tag.String);
                    writer.Write(token.Value<string>());
                    break;
                case JTokenType.Boolean:
                    writer.Write((byte)Tag.Boolean);
                    writer.Write(token.Value<bool>());
                    break;
                case JTokenType.Null:
                    writer.Write((byte)Tag.Null);
                    break;
                default:
                    throw new InvalidDataException($"Unsupported binary value type: {token.Type}.");
            }
        }

        private static JToken Read(BinaryReader reader, int depth)
        {
            CheckDepth(depth);
            Tag tag = (Tag)reader.ReadByte();
            switch (tag)
            {
                case Tag.Null:
                    return JValue.CreateNull();
                case Tag.Boolean:
                    byte boolean = reader.ReadByte();
                    if (boolean > 1)
                    {
                        throw new InvalidDataException("Invalid binary boolean.");
                    }
                    return new JValue(boolean == 1);
                case Tag.Integer:
                    return new JValue(reader.ReadInt64());
                case Tag.Double:
                    return new JValue(reader.ReadDouble());
                case Tag.String:
                    return new JValue(reader.ReadString());
                case Tag.BigInteger:
                    return new JValue(BigInteger.Parse(reader.ReadString(), CultureInfo.InvariantCulture));
                case Tag.Object:
                    int properties = ReadCount(reader);
                    JObject obj = new();
                    for (int i = 0; i < properties; i++)
                    {
                        string name = reader.ReadString();
                        if (obj.ContainsKey(name))
                        {
                            throw new InvalidDataException($"Duplicate binary property: {name}.");
                        }
                        obj.Add(name, Read(reader, depth + 1));
                    }
                    return obj;
                case Tag.Array:
                    int count = ReadCount(reader);
                    JArray array = new();
                    for (int i = 0; i < count; i++)
                    {
                        array.Add(Read(reader, depth + 1));
                    }
                    return array;
                default:
                    throw new InvalidDataException($"Unknown binary value tag: {(byte)tag}.");
            }
        }

        private static int ReadCount(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            if (count < 0 || count > reader.BaseStream.Length - reader.BaseStream.Position)
            {
                throw new InvalidDataException("Invalid binary collection length.");
            }
            return count;
        }

        private static void CheckDepth(int depth)
        {
            if (depth > MaxDepth)
            {
                throw new InvalidDataException("The binary value exceeds the maximum nesting depth.");
            }
        }
    }
}
