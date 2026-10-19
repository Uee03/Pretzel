using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace Pretzel.Data
{
    /// <summary>
    /// The per-project key file, <c>UserSettings/Pretzel/pretzel-keys.json</c>. It is plain text on purpose, so keep it out of version control.
    /// Only the Editor reads it. Player builds never look for it.
    /// </summary>
    public static class EncryptionKeyFile
    {
        public static string Path => System.IO.Path.Combine(Directory.GetParent(Application.dataPath).FullName, "UserSettings", "Pretzel", "pretzel-keys.json");

        public static bool Exists => File.Exists(Path);

        /// <summary>Returns null when the file is missing or unreadable.</summary>
        public static EncryptionKeys Load()
        {
            try
            {
                if (!File.Exists(Path))
                {
                    return null;
                }

                FileDto dto = JsonConvert.DeserializeObject<FileDto>(File.ReadAllText(Path));
                if (dto == null)
                {
                    return null;
                }

                EncryptionKeys keys = new()
                {
                    AesKey = string.IsNullOrWhiteSpace(dto.aesKey) ? null : EncryptionKeys.DecodeAesKey(dto.aesKey),
                    RsaPublicKey = EncryptionKeys.DecodeXml(dto.rsaPublicKey),
                    RsaPrivateKey = EncryptionKeys.DecodeXml(dto.rsaPrivateKey)
                };
                return keys.IsEmpty ? null : keys;
            }
            catch (System.Exception ex) when (ex is IOException or JsonException or System.UnauthorizedAccessException)
            {
                Debug.LogWarning($"[Pretzel.Data] Could not read '{Path}': {ex.Message}");
                return null;
            }
        }

        public static void Save(EncryptionKeys keys)
        {
            FileDto dto = new()
            {
                aesKey = keys.HasAesKey ? EncryptionKeys.EncodeAesKey(keys.AesKey) : null,
                rsaPublicKey = EncryptionKeys.EncodeXml(keys.RsaPublicKey),
                rsaPrivateKey = EncryptionKeys.EncodeXml(keys.RsaPrivateKey)
            };

            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
            File.WriteAllText(Path, JsonConvert.SerializeObject(dto, Formatting.Indented));
        }

        public static void Delete()
        {
            File.Delete(Path);
        }

        private sealed class FileDto
        {
            public string aesKey;
            public string rsaPublicKey;
            public string rsaPrivateKey;
        }
    }
}
