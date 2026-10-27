using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Pretzel.Data.Editor
{
    internal readonly struct CodecResult
    {
        public readonly bool Ok;
        public readonly string Text;
        public readonly string Message;

        private CodecResult(bool ok, string text, string message)
        {
            Ok = ok;
            Text = text;
            Message = message;
        }

        public static CodecResult Success(string text, string message) => new(true, text, message);

        public static CodecResult Fail(string message) => new(false, string.Empty, message);
    }

    /// <summary>Decodes and encodes pasted text with the same transforms a <see cref="DataStore"/> uses.</summary>
    internal static class DataCodec
    {
        private static readonly UTF8Encoding _utf8 = new(false);

        /// <summary>Plain JSON is used as it is. Anything else is read as Base64 of the file bytes.</summary>
        public static CodecResult Decode(string input, DataFormat? forced, EncryptionKeys keys)
        {
            string text = input?.Trim();
            if (string.IsNullOrEmpty(text))
            {
                return CodecResult.Fail("Paste some text first.");
            }

            if (forced == DataFormat.PlainText || (forced == null && (text[0] == '{' || text[0] == '[')))
            {
                return CodecResult.Success(Pretty(text), "Plain text JSON.");
            }

            byte[] bytes;
            try
            {
                bytes = Base64Transform.FromBase64Text(text);
            }
            catch (FormatException)
            {
                return CodecResult.Fail("The text is neither JSON nor valid Base64.");
            }

            DataFormat? format = forced ?? Detect(bytes);
            if (format == null)
            {
                return CodecResult.Fail("Could not tell which format this is. Choose one in the list.");
            }

            try
            {
                IDataTransform transform = TransformFor(format.Value, keys);
                byte[] decoded = transform.Decode(bytes);
                string json = _utf8.GetString(decoded).TrimStart('\uFEFF');
                return CodecResult.Success(Pretty(json), $"Decoded {format.Value} data ({bytes.Length} bytes).");
            }
            catch (Exception ex)
            {
                return CodecResult.Fail($"{format.Value}: {ex.Message}");
            }
        }

        /// <summary>The result is the file's text for Base64 output, otherwise the file bytes shown as Base64.</summary>
        public static CodecResult Encode(string json, DataFormat format, EncryptionMode mode, bool base64, EncryptionKeys keys)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return CodecResult.Fail("Paste some JSON first.");
            }

            try
            {
                JToken.Parse(json);
            }
            catch (JsonReaderException ex)
            {
                return CodecResult.Fail("The input is not valid JSON: " + ex.Message);
            }

            if (format == DataFormat.PlainText)
            {
                return CodecResult.Success(Pretty(json), "Plain text needs no encoding.");
            }

            try
            {
                byte[] bytes = _utf8.GetBytes(json);
                foreach (IDataTransform transform in DataStoreFactory.CreateTransforms(format, keys, mode, base64))
                {
                    bytes = transform.Encode(bytes);
                }

                string text = base64 ? Encoding.ASCII.GetString(bytes) : Convert.ToBase64String(bytes);
                return CodecResult.Success(text, $"Encoded as {format} ({bytes.Length} bytes). Shown as Base64 text.");
            }
            catch (Exception ex)
            {
                return CodecResult.Fail($"{format}: {ex.Message}");
            }
        }

        public static DataFormat? Detect(byte[] bytes)
        {
            if (bytes.Length >= 4 && bytes[0] == 'P' && bytes[1] == 'R' && bytes[2] == 'T' && bytes[3] == 'Z')
            {
                return DataFormat.Encrypted;
            }

            if (BinaryTransform.HasHeader(bytes))
            {
                return DataFormat.Binary;
            }

            return null;
        }

        /// <summary>Text files are shown as text, anything else as Base64 of its bytes.</summary>
        public static string FileToText(byte[] bytes)
        {
            int count = Math.Min(bytes.Length, 1024);
            for (int i = 0; i < count; i++)
            {
                byte b = bytes[i];
                if (b < 0x09 || (b > 0x0D && b < 0x20))
                {
                    return Convert.ToBase64String(bytes);
                }
            }

            return _utf8.GetString(bytes).TrimStart('\uFEFF');
        }

        private static IDataTransform TransformFor(DataFormat format, EncryptionKeys keys)
        {
            switch (format)
            {
                case DataFormat.Binary:
                    return new BinaryTransform();
                case DataFormat.Encrypted:
                    if (keys == null || keys.IsEmpty)
                    {
                        throw new InvalidOperationException("No keys are available. Generate or store keys first, or paste keys under 'Use other keys'.");
                    }

                    return new EncryptionTransform(keys);
                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, null);
            }
        }

        private static string Pretty(string json)
        {
            try
            {
                using JsonTextReader reader = new(new StringReader(json)) { DateParseHandling = DateParseHandling.None };
                return JToken.ReadFrom(reader).ToString(Formatting.Indented);
            }
            catch (JsonReaderException)
            {
                return json;
            }
        }
    }
}
