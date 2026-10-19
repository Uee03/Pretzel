using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pretzel.Setup.Paths;
using UnityEngine;

namespace Pretzel.Setup.Settings
{
    /// <summary>
    /// Front for the project and user settings files. A missing or corrupt file falls back to defaults,
    /// and a corrupt file is only replaced when something is saved (the old one stays as .bak).
    /// </summary>
    public static class PretzelSettings
    {
        private const int SchemaVersion = 1;

        private static readonly JsonSerializer Serializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            ObjectCreationHandling = ObjectCreationHandling.Replace
        });

        private static PretzelProjectSettings _project;
        private static PretzelUserSettings _user;

        // Resolved on demand, because Application.dataPath cannot be read from a static initializer.
        private static string ProjectFile => Path.Combine(PretzelPaths.ProjectFolder, "ProjectSettings", "Pretzel", "pretzel-settings.json");

        private static string UserFile => Path.Combine(PretzelPaths.ProjectFolder, "UserSettings", "Pretzel", "pretzel-user.json");

        public static PretzelProjectSettings Project
        {
            get
            {
                if (_project == null)
                {
                    _project = Load<PretzelProjectSettings>(ProjectFile);
                    _project.Normalize();
                }

                return _project;
            }
        }

        public static PretzelUserSettings User => _user ??= Load<PretzelUserSettings>(UserFile);

        public static void SaveProject() => Save(ProjectFile, Project);

        public static void SaveUser() => Save(UserFile, User);

        public static void ResetProject()
        {
            _project = new PretzelProjectSettings();
            SaveProject();
        }

        public static void ResetUser()
        {
            _user = new PretzelUserSettings();
            SaveUser();
        }

        /// <summary>Drops the cached copies so the next read loads from disk, for example after a git pull.</summary>
        public static void Reload()
        {
            _project = null;
            _user = null;
        }

        private static T Load<T>(string path) where T : new()
        {
            if (!File.Exists(path))
            {
                return new T();
            }

            try
            {
                JToken root = JToken.Parse(File.ReadAllText(path));

                // Files are written as { "schemaVersion": n, "data": { ... } }. A bare object is accepted too.
                JToken data = root is JObject envelope && envelope["schemaVersion"] != null && envelope["data"] != null
                    ? envelope["data"]
                    : root;

                return data.ToObject<T>(Serializer) ?? new T();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Pretzel] Could not read '{Path.GetFileName(path)}': {ex.Message} Using defaults.");
                return new T();
            }
        }

        private static void Save<T>(string path, T value)
        {
            JObject envelope = new()
            {
                ["schemaVersion"] = SchemaVersion,
                ["data"] = JToken.FromObject(value, Serializer)
            };

            Directory.CreateDirectory(Path.GetDirectoryName(path));

            // Write beside the target first, so a crash never leaves a half-written settings file.
            string temp = path + ".tmp";
            File.WriteAllText(temp, envelope.ToString(Formatting.Indented));
            if (File.Exists(path))
            {
                File.Replace(temp, path, path + ".bak");
            }
            else
            {
                File.Move(temp, path);
            }
        }
    }
}
