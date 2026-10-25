#if UNITY_EDITOR
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

                string json = File.ReadAllText(Path);
                FileDto dto = JsonConvert.DeserializeObject<FileDto>(json);
                if (dto == null)
                {
                    return null;
                }

                EncryptionKeys keys = new()
                {
                    AesKey = string.IsNullOrWhiteSpace(dto.aesKey) ? null : EncryptionKeys.DecodeAesKey(dto.aesKey)
                };
                if (!keys.HasAesKey && !string.IsNullOrWhiteSpace(dto.aesKey))
                {
                    Debug.LogWarning("[Pretzel.Data] The Editor key file contains an invalid AES key; it must be 32 bytes encoded as Base64.");
                }
                if (dto.previousAesKeys != null)
                {
                    foreach (string old in dto.previousAesKeys)
                    {
                        keys.AddPreviousAesKey(string.IsNullOrWhiteSpace(old) ? null : EncryptionKeys.DecodeAesKey(old));
                    }
                }

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
                previousAesKeys = keys.PreviousAesKeys.Count == 0 ? null : keys.PreviousAesKeys.ConvertAll(EncryptionKeys.EncodeAesKey).ToArray()
            };

            string path = Path;
            string temp = path + ".tmp";
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            File.WriteAllText(temp, JsonConvert.SerializeObject(dto, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            // Replace in one step, so a crash mid-write never destroys the only copy of a key.
            if (File.Exists(path))
            {
                File.Replace(temp, path, null);
            }
            else
            {
                File.Move(temp, path);
            }
        }

        public static void Delete()
        {
            File.Delete(Path);
        }

        private sealed class FileDto
        {
            public string aesKey;
            public string[] previousAesKeys;
        }
    }
}
#endif
