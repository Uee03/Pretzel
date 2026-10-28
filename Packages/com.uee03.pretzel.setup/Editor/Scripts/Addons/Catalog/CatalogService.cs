using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Pretzel.Setup.Paths;
using Pretzel.Setup.Settings;
using UnityEngine;
using UnityEngine.Networking;

namespace Pretzel.Setup.Addons.Catalog
{
    /// <summary>
    /// The built-in catalog merged with the catalog files listed in the Pretzel settings.
    /// An entry from a source replaces a built-in entry with the same id. Pinned versions from the settings win over both.
    /// </summary>
    public static class CatalogService
    {
        private static readonly List<CatalogEntry> _remote = new();
        private static readonly List<string> _errors = new();

        public static IReadOnlyList<string> LoadErrors => _errors;

        public static event Action Changed;

        public static IReadOnlyList<CatalogEntry> GetEntries()
        {
            Dictionary<string, CatalogEntry> byId = new(StringComparer.OrdinalIgnoreCase);
            List<CatalogEntry> ordered = new();

            foreach (CatalogEntry entry in BuiltInCatalog.Create().Concat(_remote.Select(e => e.Clone())))
            {
                if (string.IsNullOrWhiteSpace(entry.id) || string.IsNullOrWhiteSpace(entry.packageName))
                {
                    continue;
                }

                if (byId.TryGetValue(entry.id, out CatalogEntry existing))
                {
                    ordered[ordered.IndexOf(existing)] = entry;
                }
                else
                {
                    ordered.Add(entry);
                }

                byId[entry.id] = entry;
            }

            foreach (PinnedVersion pin in PretzelSettings.Project.pinnedVersions)
            {
                if (pin != null && byId.TryGetValue(pin.id ?? string.Empty, out CatalogEntry pinned) && !string.IsNullOrWhiteSpace(pin.version))
                {
                    pinned.version = pin.version;
                }
            }

            return ordered;
        }

        public static void SetPin(string id, string version)
        {
            List<PinnedVersion> pins = PretzelSettings.Project.pinnedVersions;
            pins.RemoveAll(p => string.Equals(p.id, id, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(version))
            {
                pins.Add(new PinnedVersion { id = id, version = version.Trim() });
            }
        }

        public static string GetPin(string id)
        {
            return PretzelSettings.Project.pinnedVersions.Find(p => string.Equals(p.id, id, StringComparison.OrdinalIgnoreCase))?.version;
        }

        /// <summary>Reloads every catalog source from the settings. Failed sources are listed in <see cref="LoadErrors"/>.</summary>
        public static async Awaitable RefreshAsync()
        {
            _remote.Clear();
            _errors.Clear();

            foreach (string source in PretzelSettings.Project.catalogSources.Where(s => !string.IsNullOrWhiteSpace(s)).ToList())
            {
                try
                {
                    CatalogFile file = await LoadAsync(source.Trim());
                    if (file?.entries != null)
                    {
                        _remote.AddRange(file.entries);
                    }
                }
                catch (Exception ex)
                {
                    _errors.Add($"{source}: {ex.Message}");
                }
            }

            Changed?.Invoke();
        }

        private static async Awaitable<CatalogFile> LoadAsync(string source)
        {
            string json;
            if (source.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || source.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                using UnityWebRequest request = UnityWebRequest.Get(source);
                UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    await Awaitable.NextFrameAsync();
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    throw new InvalidDataException(request.error);
                }

                json = request.downloadHandler.text;
            }
            else
            {
                string path = Path.IsPathRooted(source) ? source : Path.Combine(PretzelPaths.ProjectFolder, source);
                json = File.ReadAllText(path);
            }

            // Replace, so list fields that have a default value are not appended to.
            return JsonConvert.DeserializeObject<CatalogFile>(json, new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace });
        }
    }
}
