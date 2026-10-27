using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pretzel.Setup.Settings;
using UnityEditor.PackageManager;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Pretzel.Setup.Addons.Catalog
{
    public static class ModuleCatalog
    {
        private const string CatalogPath = "Editor/Catalog/modules.json";

        private static IReadOnlyList<ModuleInfo> _all;

        /// <summary>The repository and tag modules install from: the Settings values, else the git source this package was installed from.</summary>
        public static (string Url, string Version) ResolveSource(PretzelProjectSettings settings)
        {
            if (!string.IsNullOrWhiteSpace(settings.moduleSourceUrl))
            {
                return (settings.moduleSourceUrl.Trim(), (settings.moduleSourceVersion ?? string.Empty).Trim());
            }

            return InferSource();
        }

        // A git package id looks like name@url?path=sub#revision.
        private static (string Url, string Version) InferSource()
        {
            PackageInfo info = PackageInfo.FindForAssembly(typeof(ModuleCatalog).Assembly);
            if (info == null || info.source != PackageSource.Git)
            {
                return (null, null);
            }

            int at = info.packageId.IndexOf('@');
            if (at < 0)
            {
                return (null, null);
            }

            string rest = info.packageId.Substring(at + 1);
            string version = string.Empty;
            int hash = rest.IndexOf('#');
            if (hash >= 0)
            {
                version = rest.Substring(hash + 1);
                rest = rest.Substring(0, hash);
            }

            int query = rest.IndexOf('?');
            return (query >= 0 ? rest.Substring(0, query) : rest, version);
        }

        /// <summary>
        /// Every Pretzel module, read once from <c>Editor/Catalog/modules.json</c>. That file is generated from the module
        /// manifests by <c>.github/scripts/Update-ModuleCatalog.ps1</c>; do not edit it by hand.
        /// </summary>
        public static IReadOnlyList<ModuleInfo> All => _all ??= Load();

        private static IReadOnlyList<ModuleInfo> Load()
        {
            PackageInfo info = PackageInfo.FindForAssembly(typeof(ModuleCatalog).Assembly);
            string path = info == null ? null : Path.Combine(info.resolvedPath, CatalogPath);
            if (path == null || !File.Exists(path))
            {
                Debug.LogWarning($"Pretzel module catalog not found at {path ?? CatalogPath}. The Addons tab lists no modules.");
                return Array.Empty<ModuleInfo>();
            }

            try
            {
                return Parse(File.ReadAllText(path));
            }
            catch (Exception ex) when (ex is JsonException || ex is IOException || ex is InvalidCastException)
            {
                Debug.LogError($"Pretzel module catalog at {path} could not be read: {ex.Message}");
                return Array.Empty<ModuleInfo>();
            }
        }

        /// <summary>Parses the generated catalog format.</summary>
        public static IReadOnlyList<ModuleInfo> Parse(string json)
        {
            JObject root = JObject.Parse(json);
            List<ModuleInfo> modules = new();
            foreach (JToken module in root["modules"] ?? new JArray())
            {
                List<(string, string)> optional = new();
                foreach (JToken library in module["optionalLibraries"] ?? new JArray())
                {
                    optional.Add(((string)library["displayName"], (string)library["packageName"]));
                }

                modules.Add(new ModuleInfo(
                    (string)module["name"],
                    (string)module["displayName"],
                    (string)module["description"],
                    Strings(module["dependencies"]),
                    optional.ToArray(),
                    Strings(module["packages"])));
            }

            return modules;
        }

        private static string[] Strings(JToken token)
        {
            return token == null ? Array.Empty<string>() : token.Values<string>().ToArray();
        }
    }
}
