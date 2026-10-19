using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Pretzel.Setup.Paths;
using Pretzel.Setup.Addons.Catalog;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Pretzel.Setup.Addons.Services
{
    /// <summary>
    /// Wraps the Package Manager client. One request runs at a time, and each install is a single
    /// AddAndRemove call, because a domain reload after the first package would drop a queue of several.
    /// </summary>
    public static class PackageService
    {
        private static readonly Dictionary<string, PackageInfo> Installed = new(StringComparer.Ordinal);

        private static Request _request;
        private static ListRequest _list;
        private static bool _listAgain;

        public static event Action Changed;

        public static bool IsBusy => _request != null;

        /// <summary>What the running request is doing, or null.</summary>
        public static string BusyLabel { get; private set; }

        /// <summary>The message of the last failed request, or null.</summary>
        public static string LastError { get; private set; }

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            Events.registeredPackages += _ => Refresh();
            Refresh();
        }

        public static bool IsInstalled(string packageName) => packageName != null && Installed.ContainsKey(packageName);

        public static PackageInfo Get(string packageName)
        {
            return packageName != null && Installed.TryGetValue(packageName, out PackageInfo info) ? info : null;
        }

        public static void Refresh()
        {
            if (_list != null)
            {
                _listAgain = true;
                return;
            }

            _list = Client.List(true, true);
            StartPolling();
        }

        /// <summary>Installs the entries and every catalog dependency that is missing, in one request.</summary>
        public static bool Install(IEnumerable<CatalogEntry> wanted, IReadOnlyList<CatalogEntry> catalog, string label)
        {
            List<CatalogEntry> missing = new();
            foreach (CatalogEntry entry in wanted)
            {
                Collect(entry, catalog, missing);
            }

            missing.RemoveAll(e => IsInstalled(e.packageName));
            if (missing.Count == 0)
            {
                return false;
            }

            if (IsBusy)
            {
                return false;
            }

            List<string> identifiers = new();
            foreach (CatalogEntry entry in missing)
            {
                if (entry.source == CatalogSource.OpenUpm)
                {
                    string manifest = Path.Combine(PretzelPaths.ProjectFolder, "Packages", "manifest.json");
                    ManifestEditor.AddOpenUpmDependency(manifest, entry.packageName, entry.version);
                }
                else
                {
                    identifiers.Add(entry.Identifier);
                }
            }

            return identifiers.Count > 0 ? Run(label, identifiers.ToArray(), null) : Resolve(label);
        }

        public static bool InstallIdentifiers(IEnumerable<string> identifiers, string label)
        {
            string[] add = identifiers.Distinct().ToArray();
            return add.Length > 0 && Run(label, add, null);
        }

        public static bool Remove(string packageName, string label)
        {
            return IsInstalled(packageName) && Run(label, null, new[] { packageName });
        }

        /// <summary>Asks for confirmation, then removes the package.</summary>
        public static bool Uninstall(string packageName, string displayName)
        {
            if (!IsInstalled(packageName) || IsBusy)
            {
                return false;
            }

            if (!EditorUtility.DisplayDialog("Uninstall " + displayName, $"Remove {displayName} ({packageName}) from this project?", "Uninstall", "Cancel"))
            {
                return false;
            }

            return Remove(packageName, "Uninstalling " + displayName);
        }

        private static bool Run(string label, string[] add, string[] remove)
        {
            if (IsBusy)
            {
                return false;
            }

            LastError = null;
            BusyLabel = label;
            _request = Client.AddAndRemove(add, remove);
            StartPolling();
            Changed?.Invoke();
            return true;
        }

        private static bool Resolve(string label)
        {
            // Resolve has no request object. The registeredPackages event refreshes the list when it finishes.
            LastError = null;
            Client.Resolve();
            return true;
        }

        private static void Collect(CatalogEntry entry, IReadOnlyList<CatalogEntry> catalog, List<CatalogEntry> result)
        {
            if (result.Contains(entry))
            {
                return;
            }

            result.Add(entry);
            foreach (string id in entry.dependencies ?? new List<string>())
            {
                CatalogEntry dependency = catalog.FirstOrDefault(e => e.id == id);
                if (dependency != null)
                {
                    Collect(dependency, catalog, result);
                }
                else
                {
                    Debug.LogWarning($"[Pretzel] '{entry.id}' depends on '{id}', which is not in the catalog.");
                }
            }
        }

        private static void StartPolling()
        {
            EditorApplication.update -= Poll;
            EditorApplication.update += Poll;
        }

        // Package Manager requests have no completion event, so they are polled until they finish.
        private static void Poll()
        {
            bool listDone = _list == null || _list.IsCompleted;
            bool requestDone = _request == null || _request.IsCompleted;
            if (!listDone || !requestDone)
            {
                return;
            }

            EditorApplication.update -= Poll;

            if (_list != null)
            {
                if (_list.Status == StatusCode.Success)
                {
                    Installed.Clear();
                    foreach (PackageInfo info in _list.Result)
                    {
                        Installed[info.name] = info;
                    }
                }

                _list = null;
            }

            if (_request != null)
            {
                if (_request.Status == StatusCode.Failure)
                {
                    LastError = _request.Error?.message ?? "The Package Manager request failed.";
                    Debug.LogWarning($"[Pretzel] {BusyLabel}: {LastError}");
                }

                _request = null;
                BusyLabel = null;
                _listAgain = true;
            }

            if (_listAgain)
            {
                _listAgain = false;
                Refresh();
            }

            Changed?.Invoke();
        }
    }
}
